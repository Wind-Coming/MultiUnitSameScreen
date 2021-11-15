using System;
using System.Collections.Generic;
#if !NET_DOTS
using System.Reflection;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

// Remove this once DOTSRuntime can use Unity.Properties
[assembly: InternalsVisibleTo("Unity.TinyConversion")]
[assembly: InternalsVisibleTo("Unity.Entities.Runtime.Build")]

namespace Unity.Entities.Serialization
{
    public static partial class SerializeUtility
    {
#if !NET_DOTS
        /// <summary>
        /// Custom adapter used during serialization to add special type handling for <see cref="Entity"/> and <see cref="BlobAssetReference{T}"/>.
        /// </summary>
        unsafe class ManagedObjectSerializeAdapter :
            Unity.Serialization.Binary.Adapters.IBinaryAdapter<Entity>,
            Unity.Serialization.Binary.Adapters.IBinaryAdapter<BlobAssetReferenceData>
        {
            /// <summary>
            /// Entity remapping which is applied during serialization.
            /// </summary>
            readonly EntityRemapUtility.EntityRemapInfo* m_EntityRemapInfo;

            /// <summary>
            /// A map of <see cref="BlobAssetReferenceData"/> to index in the serialized batch.
            /// </summary>
            readonly NativeHashMap<BlobAssetPtr, int> m_BlobAssetMap;

            /// <summary>
            /// An array of absolute byte offsets for all blob assets within the serialized batch.
            /// </summary>
            readonly NativeArray<int> m_BlobAssetOffsets;

            public ManagedObjectSerializeAdapter(
                EntityRemapUtility.EntityRemapInfo* entityRemapInfo,
                NativeHashMap<BlobAssetPtr, int> blobAssetMap,
                NativeArray<int> blobAssetOffsets)
            {
                m_EntityRemapInfo = entityRemapInfo;
                m_BlobAssetMap = blobAssetMap;
                m_BlobAssetOffsets = blobAssetOffsets;
            }

            void Unity.Serialization.Binary.Adapters.IBinaryAdapter<Entity>.Serialize(UnsafeAppendBuffer* writer, Entity value)
            {
                value = EntityRemapUtility.RemapEntity(m_EntityRemapInfo, value);
                writer->Add(value.Index);
                writer->Add(value.Version);
            }

            void Unity.Serialization.Binary.Adapters.IBinaryAdapter<BlobAssetReferenceData>.Serialize(UnsafeAppendBuffer* writer, BlobAssetReferenceData value)
            {
                var offset = -1;

                if (value.m_Ptr != null)
                {
                    if (!m_BlobAssetMap.TryGetValue(new BlobAssetPtr(value.Header), out var index))
                        throw new InvalidOperationException($"Trying to serialize a BlobAssetReference but the asset has not been included in the batch.");

                    offset = m_BlobAssetOffsets[index];
                }

                writer->Add(offset);
            }

            Entity Unity.Serialization.Binary.Adapters.IBinaryAdapter<Entity>.Deserialize(UnsafeAppendBuffer.Reader* reader)
                => throw new InvalidOperationException($"{nameof(ManagedObjectSerializeAdapter)} should only be used for writing and never for reading!");

            BlobAssetReferenceData Unity.Serialization.Binary.Adapters.IBinaryAdapter<BlobAssetReferenceData>.Deserialize(UnsafeAppendBuffer.Reader* reader)
                => throw new InvalidOperationException($"{nameof(ManagedObjectSerializeAdapter)} should only be used for writing and never for reading!");
        }

        /// <summary>
        /// Custom adapter used during de-serialization to add special type handling for <see cref="Entity"/> and <see cref="BlobAssetReference{T}"/>.
        /// </summary>
        unsafe class MangedObjectBlobAssetReader :
            Unity.Serialization.Binary.Adapters.IBinaryAdapter<Entity>,
            Unity.Serialization.Binary.Adapters.IBinaryAdapter<BlobAssetReferenceData>
        {
            readonly byte* m_BlobAssetBatch;

            public MangedObjectBlobAssetReader(byte* blobAssetBatch)
            {
                m_BlobAssetBatch = blobAssetBatch;
            }

            void Unity.Serialization.Binary.Adapters.IBinaryAdapter<BlobAssetReferenceData>.Serialize(UnsafeAppendBuffer* writer, BlobAssetReferenceData value)
                => throw new InvalidOperationException($"{nameof(MangedObjectBlobAssetReader)} should only be used for reading and never for writing!");

            void Unity.Serialization.Binary.Adapters.IBinaryAdapter<Entity>.Serialize(UnsafeAppendBuffer* writer, Entity value)
                => throw new InvalidOperationException($"{nameof(MangedObjectBlobAssetReader)} should only be used for reading and never for writing!");

            Entity Unity.Serialization.Binary.Adapters.IBinaryAdapter<Entity>.Deserialize(UnsafeAppendBuffer.Reader* reader)
            {
                reader->ReadNext(out int index);
                reader->ReadNext(out int version);
                return new Entity {Index = index, Version = version};
            }

            BlobAssetReferenceData Unity.Serialization.Binary.Adapters.IBinaryAdapter<BlobAssetReferenceData>.Deserialize(UnsafeAppendBuffer.Reader* reader)
            {
                reader->ReadNext(out int offset);
                return offset == -1 ? default : new BlobAssetReferenceData {m_Ptr = m_BlobAssetBatch + offset};
            }
        }
#endif
        internal unsafe struct BlobAssetPtr : IEquatable<BlobAssetPtr>
        {
            public BlobAssetPtr(BlobAssetHeader* header)
            {
                this.header = header;
            }

            public readonly BlobAssetHeader* header;
            public bool Equals(BlobAssetPtr other)
            {
                return header == other.header;
            }

            public override int GetHashCode()
            {
                BlobAssetHeader* onStack = header;
                return (int)math.hash(&onStack, sizeof(BlobAssetHeader*));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BufferPatchRecord
        {
            public int ChunkOffset;
            public int AllocSizeBytes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct BlobAssetRefPatchRecord
        {
            public int ChunkOffset;
            public int BlobDataOffset;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SharedComponentRecord
        {
            public ulong StableTypeHash;
            public int ComponentSize;
        }

        public static int CurrentFileFormatVersion = 48;

        public static unsafe void DeserializeWorld(ExclusiveEntityTransaction manager, BinaryReader reader, object[] unityObjects = null)
        {
            var access = manager.EntityManager.GetCheckedEntityDataAccess();
            var s = access->EntityComponentStore;
            var mcs = access->ManagedComponentStore;

            if (s->CountEntities() != 0)
            {
                throw new ArgumentException(
                    $"DeserializeWorld can only be used on completely empty EntityManager. Please create a new empty World and use EntityManager.MoveEntitiesFrom to move the loaded entities into the destination world instead.");
            }
            int storedVersion = reader.ReadInt();
            if (storedVersion != CurrentFileFormatVersion)
            {
                throw new ArgumentException(
                    $"Attempting to read a entity scene stored in an old file format version (stored version : {storedVersion}, current version : {CurrentFileFormatVersion})");
            }

            var types = ReadTypeArray(reader);
            int totalEntityCount;

            var archetypes = ReadArchetypes(reader, types, manager, out totalEntityCount);

            var totalBlobAssetSize = reader.ReadInt();
            byte* allBlobAssetData = null;

            var blobAssetRefChunks = new NativeList<ArchetypeChunk>();
            var blobAssetOwner = default(BlobAssetOwner);
            if (totalBlobAssetSize != 0)
            {
                allBlobAssetData = (byte*)UnsafeUtility.Malloc((long)totalBlobAssetSize, 16, Allocator.Persistent);
                if (totalBlobAssetSize > int.MaxValue)
                    throw new System.ArgumentException("Blobs larger than 2GB are currently not supported");

                reader.ReadBytes(allBlobAssetData, totalBlobAssetSize);

                blobAssetOwner = new BlobAssetOwner(allBlobAssetData, totalBlobAssetSize);
                blobAssetRefChunks = new NativeList<ArchetypeChunk>(32, Allocator.Temp);
            }

            var numSharedComponents = ReadSharedComponentMetadata(reader, out var sharedComponentArray, out var sharedComponentRecordArray);
            var sharedComponentRemap = new NativeArray<int>(numSharedComponents + 1, Allocator.Temp);

            int sharedAndManagedDataSize = reader.ReadInt();
            int managedComponentCount = reader.ReadInt();
#if !NET_DOTS
            var sharedAndManagedBuffer = new UnsafeAppendBuffer(sharedAndManagedDataSize, 16, Allocator.Temp);
            sharedAndManagedBuffer.ResizeUninitialized(sharedAndManagedDataSize);
            reader.ReadBytes(sharedAndManagedBuffer.Ptr, sharedAndManagedDataSize);
            var sharedAndManagedStream = sharedAndManagedBuffer.AsReader();
            var managedDataReader = new ManagedObjectBinaryReader(&sharedAndManagedStream, (UnityEngine.Object[])unityObjects);
            managedDataReader.AddAdapter(new MangedObjectBlobAssetReader(allBlobAssetData));
            ReadSharedComponents(manager, managedDataReader, sharedComponentRemap, sharedComponentRecordArray);
            mcs.ResetManagedComponentStoreForDeserialization(managedComponentCount, ref *s);

            // Deserialize all managed components
            for (int i = 0; i < managedComponentCount; ++i)
            {
                ulong typeHash = sharedAndManagedStream.ReadNext<ulong>();
                int typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(typeHash);
                Type managedType = TypeManager.GetTypeInfo(typeIndex).Type;
                object obj = managedDataReader.ReadObject(managedType);
                mcs.SetManagedComponentValue(i + 1, obj);
            }
#else
            ReadSharedComponents(manager, reader, sharedAndManagedDataSize, sharedComponentRemap, sharedComponentRecordArray);
#endif

            manager.AllocateConsecutiveEntitiesForLoading(totalEntityCount);

            int totalChunkCount = reader.ReadInt();
            var chunksWithMetaChunkEntities = new NativeList<ArchetypeChunk>(totalChunkCount, Allocator.Temp);

            int sharedComponentArraysIndex = 0;
            for (int i = 0; i < totalChunkCount; ++i)
            {
                var chunk = s->AllocateChunk();
                reader.ReadBytes(chunk, Chunk.kChunkSize);

                var archetype = chunk->Archetype = archetypes[(int)chunk->Archetype].Archetype;
                var numSharedComponentsInArchetype = chunk->Archetype->NumSharedComponents;
                int* sharedComponentValueArray = (int*)sharedComponentArray.GetUnsafePtr() + sharedComponentArraysIndex;

                for (int j = 0; j < numSharedComponentsInArchetype; ++j)
                {
                    // The shared component 0 is not part of the array, so an index equal to the array size is valid.
                    if (sharedComponentValueArray[j] > numSharedComponents)
                    {
                        throw new ArgumentException(
                            $"Archetype uses shared component at index {sharedComponentValueArray[j]} but only {numSharedComponents} are available, check if the shared scene has been properly loaded.");
                    }
                }

                var remapedSharedComponentValues = stackalloc int[archetype->NumSharedComponents];

                RemapSharedComponentIndices(remapedSharedComponentValues, archetype, sharedComponentRemap, sharedComponentValueArray);

                sharedComponentArraysIndex += numSharedComponentsInArchetype;

                // Allocate additional heap memory for buffers that have overflown into the heap, and read their data.
                int bufferAllocationCount = reader.ReadInt();
                if (bufferAllocationCount > 0)
                {
                    var bufferPatches = new NativeArray<BufferPatchRecord>(bufferAllocationCount, Allocator.Temp);
                    reader.ReadArray(bufferPatches, bufferPatches.Length);

                    // TODO: PERF: Batch malloc interface.
                    for (int pi = 0; pi < bufferAllocationCount; ++pi)
                    {
                        var target = (BufferHeader*)OffsetFromPointer(chunk->Buffer, bufferPatches[pi].ChunkOffset);

                        // TODO: Alignment
                        target->Pointer = (byte*)UnsafeUtility.Malloc(bufferPatches[pi].AllocSizeBytes, 8, Allocator.Persistent);

                        reader.ReadBytes(target->Pointer, bufferPatches[pi].AllocSizeBytes);
                    }

                    bufferPatches.Dispose();
                }

                if (totalBlobAssetSize != 0 && archetype->ContainsBlobAssetRefs)
                {
                    blobAssetRefChunks.Add(new ArchetypeChunk(chunk, s));
                    PatchBlobAssetsInChunkAfterLoad(chunk, allBlobAssetData);
                }

                ChunkDataUtility.AddExistingChunk(chunk, remapedSharedComponentValues);
                mcs.Playback(ref s->ManagedChangesTracker);

                if (chunk->metaChunkEntity != Entity.Null)
                {
                    chunksWithMetaChunkEntities.Add(new ArchetypeChunk(chunk, s));
                }
            }

            if (totalBlobAssetSize != 0)
            {
                manager.AddSharedComponent(blobAssetRefChunks.AsArray(), blobAssetOwner);
                blobAssetRefChunks.Dispose();
            }

            for (int i = 0; i < chunksWithMetaChunkEntities.Length; ++i)
            {
                var chunk = chunksWithMetaChunkEntities[i].m_Chunk;
                manager.SetComponentData(chunk->metaChunkEntity, new ChunkHeader {ArchetypeChunk = chunksWithMetaChunkEntities[i]});
            }

            blobAssetOwner.Release();
            chunksWithMetaChunkEntities.Dispose();
            archetypes.Dispose();
            types.Dispose();
            sharedComponentRemap.Dispose();
#if !NET_DOTS
            sharedAndManagedBuffer.Dispose();
#endif

            // Chunks have now taken over ownership of the shared components (reference counts have been added)
            // so remove the ref that was added on deserialization
            for (int i = 0; i < numSharedComponents; ++i)
            {
                mcs.RemoveReference(i + 1);
            }
        }

        private static unsafe NativeArray<EntityArchetype> ReadArchetypes(BinaryReader reader, NativeArray<int> types, ExclusiveEntityTransaction entityManager,
            out int totalEntityCount)
        {
            int archetypeCount = reader.ReadInt();
            var archetypes = new NativeArray<EntityArchetype>(archetypeCount, Allocator.Temp);
            totalEntityCount = 0;
            var tempComponentTypes = new NativeList<ComponentType>(Allocator.Temp);
            for (int i = 0; i < archetypeCount; ++i)
            {
                var archetypeEntityCount = reader.ReadInt();
                totalEntityCount += archetypeEntityCount;
                int archetypeComponentTypeCount = reader.ReadInt();
                tempComponentTypes.Clear();
                for (int iType = 0; iType < archetypeComponentTypeCount; ++iType)
                {
                    int typeHashIndexInFile = reader.ReadInt();
                    int typeHashIndexInFileNoFlags = typeHashIndexInFile & TypeManager.ClearFlagsMask;
                    int typeIndex = types[typeHashIndexInFileNoFlags];
                    if (TypeManager.IsChunkComponent(typeHashIndexInFile))
                        typeIndex = TypeManager.MakeChunkComponentTypeIndex(typeIndex);

                    tempComponentTypes.Add(ComponentType.FromTypeIndex(typeIndex));
                }

                archetypes[i] = entityManager.CreateArchetype((ComponentType*)tempComponentTypes.GetUnsafePtr(),
                    tempComponentTypes.Length);
            }

            tempComponentTypes.Dispose();
            return archetypes;
        }

        private static NativeArray<int> ReadTypeArray(BinaryReader reader)
        {
            int typeCount = reader.ReadInt();
            var typeHashBuffer = new NativeArray<ulong>(typeCount, Allocator.Temp);

            reader.ReadArray(typeHashBuffer, typeCount);

            var types = new NativeArray<int>(typeCount, Allocator.Temp);
            for (int i = 0; i < typeCount; ++i)
            {
                var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(typeHashBuffer[i]);
                if (typeIndex < 0)
                    throw new ArgumentException($"Cannot find TypeIndex for type hash {typeHashBuffer[i].ToString()}. Ensure your runtime depends on all assemblies defining the Component types your data uses.");

                types[i] = typeIndex;
            }

            typeHashBuffer.Dispose();
            return types;
        }

        unsafe internal static UnsafeArchetypePtrList GetAllArchetypes(EntityComponentStore* entityComponentStore, Allocator allocator)
        {
            int count = 0;
            for (var i = 0; i < entityComponentStore->m_Archetypes.Length; ++i)
            {
                var archetype = entityComponentStore->m_Archetypes.Ptr[i];
                if (archetype->EntityCount > 0)
                    count++;
            }

            var archetypes = new UnsafeArchetypePtrList(count, allocator);
            archetypes.Resize(count, NativeArrayOptions.UninitializedMemory);
            count = 0;
            for (var i = 0; i < entityComponentStore->m_Archetypes.Length; ++i)
            {
                var archetype = entityComponentStore->m_Archetypes.Ptr[i];
                if (archetype->EntityCount > 0)
                    archetypes.Ptr[count++] = entityComponentStore->m_Archetypes.Ptr[i];
            }

            return archetypes;
        }

        public static unsafe void SerializeWorld(EntityManager entityManager, BinaryWriter writer)
        {
            var entityRemapInfos = new NativeArray<EntityRemapUtility.EntityRemapInfo>(entityManager.EntityCapacity, Allocator.Temp);
            SerializeWorldInternal(entityManager, writer, out var referencedObjects, entityRemapInfos);
            entityRemapInfos.Dispose();
        }

        public static unsafe void SerializeWorld(EntityManager entityManager, BinaryWriter writer, out object[] referencedObjects)
        {
            var entityRemapInfos = new NativeArray<EntityRemapUtility.EntityRemapInfo>(entityManager.EntityCapacity, Allocator.Temp);
            SerializeWorldInternal(entityManager, writer, out referencedObjects, entityRemapInfos);
            entityRemapInfos.Dispose();
        }

        public static unsafe void SerializeWorld(EntityManager entityManager, BinaryWriter writer, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos)
        {
            SerializeWorldInternal(entityManager, writer, out var referencedObjects, entityRemapInfos);
        }

        public static unsafe void SerializeWorld(EntityManager entityManager, BinaryWriter writer, out object[] referencedObjects, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos)
        {
            SerializeWorldInternal(entityManager, writer, out referencedObjects, entityRemapInfos);
        }

        /// <summary>
        /// Gets the entity representing the scene section with the index passed in.
        /// If createIfMissing is true the section entity is created if it doesn't already exist.
        /// Metadata components added to this section entity will be serialized into the entity scene header.
        /// At runtime these components will be added to the scene section entities when the scene is resolved.
        /// Only struct IComponentData components without BlobAssetReferences or Entity members are supported.
        /// </summary>
        /// <param name="sectionIndex">The section index for which to get the scene section entity</param>
        /// <param name="manager">The entity manager to which the entity belongs</param>
        /// <param name="cachedSceneSectionEntityQuery">The EntityQuery used to find the entity. Initially an null query should be passed in,
        /// the same query can the be passed in for subsequent calls to avoid recreating the query</param>
        /// <param name="createIfMissing">If true the section entity is created if it doesn't already exist. If false Entity.Null is returned for missing section entities</param>
        /// <returns>The entity representing the scene section</returns>
        public static Entity GetSceneSectionEntity(int sectionIndex, EntityManager manager, ref EntityQuery cachedSceneSectionEntityQuery, bool createIfMissing = true)
        {
            if (cachedSceneSectionEntityQuery == default)
                cachedSceneSectionEntityQuery = manager.CreateEntityQuery(ComponentType.ReadOnly<SectionMetadataSetup>());
            var sectionComponent = new SectionMetadataSetup {SceneSectionIndex = sectionIndex};
            cachedSceneSectionEntityQuery.SetSharedComponentFilter(sectionComponent);
            using (var sectionEntities = cachedSceneSectionEntityQuery.ToEntityArray(Allocator.TempJob))
            {
                if (sectionEntities.Length == 0)
                {
                    if (!createIfMissing)
                        return Entity.Null;
                    var sceneSectionEntity = manager.CreateEntity();
                    manager.AddSharedComponentData(sceneSectionEntity, sectionComponent);
                    return sceneSectionEntity;
                }

                if (sectionEntities.Length == 1)
                    return sectionEntities[0];

                throw new InvalidOperationException($"Multiple scene section entities with section index {sectionIndex} found");
            }
        }

        internal static unsafe void SerializeWorldInternal(EntityManager entityManager, BinaryWriter writer, out object[] referencedObjects, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos, bool isDOTSRuntime = false)
        {
            writer.Write(CurrentFileFormatVersion);

            var access = entityManager.GetCheckedEntityDataAccess();
            var entityComponentStore = access->EntityComponentStore;
            var mcs = access->ManagedComponentStore;

            var archetypeArray = GetAllArchetypes(entityComponentStore, Allocator.Temp);

            var typeHashToIndexMap = new UnsafeHashMap<ulong, int>(1024, Allocator.Temp);
            for (int i = 0; i != archetypeArray.Length; i++)
            {
                var archetype = archetypeArray.Ptr[i];
                for (int iType = 0; iType < archetype->TypesCount; ++iType)
                {
                    var typeIndex = archetype->Types[iType].TypeIndex;
                    var typeInfo = TypeManager.GetTypeInfo(typeIndex);
                    var hash = typeInfo.StableTypeHash;
#if !NET_DOTS
                    ValidateTypeForSerialization(typeInfo);
#endif
                    typeHashToIndexMap.TryAdd(hash, i);
                }
            }

            using (var typeHashSet = typeHashToIndexMap.GetKeyArray(Allocator.Temp))
            {
                writer.Write(typeHashSet.Length);
                foreach (ulong hash in typeHashSet)
                    writer.Write(hash);

                for (int i = 0; i < typeHashSet.Length; ++i)
                    typeHashToIndexMap[typeHashSet[i]] = i;
            }

            var sharedComponentMapping = GatherSharedComponents(archetypeArray, out var sharedComponentArraysTotalCount);
            var sharedComponentArrays = new NativeArray<int>(sharedComponentArraysTotalCount, Allocator.Temp);
            FillSharedComponentArrays(sharedComponentArrays, archetypeArray, sharedComponentMapping);

            var sharedComponentsToSerialize = new int[sharedComponentMapping.Count() - 1];
            using (var keyArray = sharedComponentMapping.GetKeyArray(Allocator.Temp))
            {
                foreach (var key in keyArray)
                {
                    if (key == 0)
                        continue;

                    if (sharedComponentMapping.TryGetValue(key, out var val))
                        sharedComponentsToSerialize[val - 1] = key;
                }
            }

            WriteArchetypes(writer, archetypeArray, typeHashToIndexMap);

            GatherAllUsedBlobAssets(entityManager, sharedComponentsToSerialize, isDOTSRuntime, archetypeArray, out var blobAssets, out var blobAssetMap);

            var blobAssetOffsets = new NativeArray<int>(blobAssets.Length, Allocator.Temp);
            int totalBlobAssetSize = sizeof(BlobAssetBatch);

            for (int i = 0; i < blobAssets.Length; ++i)
            {
                totalBlobAssetSize += sizeof(BlobAssetHeader);
                blobAssetOffsets[i] = totalBlobAssetSize;
                totalBlobAssetSize += Align16(blobAssets[i].header->Length);
            }

            writer.Write(totalBlobAssetSize);
            var blobAssetBatch = BlobAssetBatch.CreateForSerialize(blobAssets.Length, totalBlobAssetSize);
            writer.WriteBytes(&blobAssetBatch, sizeof(BlobAssetBatch));
            var zeroBytes = int4.zero;
            for (int i = 0; i < blobAssets.Length; ++i)
            {
                var blobAssetLength = blobAssets[i].header->Length;
                var blobAssetHash = blobAssets[i].header->Hash;
                var header = BlobAssetHeader.CreateForSerialize(Align16(blobAssetLength), blobAssetHash);
                writer.WriteBytes(&header, sizeof(BlobAssetHeader));
                writer.WriteBytes(blobAssets[i].header + 1, blobAssetLength);
                writer.WriteBytes(&zeroBytes, header.Length - blobAssetLength);
            }

            writer.Write(sharedComponentArrays.Length);
            writer.WriteArray(sharedComponentArrays);
            sharedComponentArrays.Dispose();

            //TODO: ensure chunks are defragged?
            var bufferPatches = new NativeList<BufferPatchRecord>(128, Allocator.Temp);
            var totalChunkCount = GenerateRemapInfo(entityManager, archetypeArray, entityRemapInfos);

            referencedObjects = null;

            WriteSharedAndManagedComponents(
                entityManager,
                archetypeArray,
                sharedComponentsToSerialize,
                writer,
                out referencedObjects,
                isDOTSRuntime,
                (EntityRemapUtility.EntityRemapInfo*)entityRemapInfos.GetUnsafePtr(),
                blobAssetMap,
                blobAssetOffsets);

            writer.Write(totalChunkCount);

            var stackBytes = stackalloc byte[Chunk.kChunkSize];
            var tempChunk = (Chunk*)stackBytes;
            int currentManagedComponentIndex = 1;
            for (int a = 0; a < archetypeArray.Length; ++a)
            {
                var archetype = archetypeArray.Ptr[a];

                for (var ci = 0; ci < archetype->Chunks.Count; ++ci)
                {
                    var chunk = archetype->Chunks.p[ci];
                    bufferPatches.Clear();

                    UnsafeUtility.MemCpy(tempChunk, chunk, Chunk.kChunkSize);
                    tempChunk->metaChunkEntity = EntityRemapUtility.RemapEntity(ref entityRemapInfos, tempChunk->metaChunkEntity);

                    // Prevent patching from touching buffers allocated memory
                    BufferHeader.PatchAfterCloningChunk(tempChunk);
                    PatchManagedComponentIndices(tempChunk, archetype, ref currentManagedComponentIndex, mcs);

                    byte* tempChunkBuffer = tempChunk->Buffer;
                    EntityRemapUtility.PatchEntities(archetype->ScalarEntityPatches, archetype->ScalarEntityPatchCount, archetype->BufferEntityPatches, archetype->BufferEntityPatchCount, tempChunkBuffer, tempChunk->Count, ref entityRemapInfos);
                    if (archetype->ContainsBlobAssetRefs)
                        PatchBlobAssetsInChunkBeforeSave(tempChunk, chunk, blobAssetOffsets, blobAssetMap);

                    FillPatchRecordsForChunk(chunk, bufferPatches);

                    ClearChunkHeaderComponents(tempChunk);
                    ChunkDataUtility.MemsetUnusedChunkData(tempChunk, 0);
                    tempChunk->Archetype = (Archetype*)a;

                    writer.WriteBytes(tempChunk, Chunk.kChunkSize);

                    writer.Write(bufferPatches.Length);

                    if (bufferPatches.Length > 0)
                    {
                        writer.WriteList(bufferPatches);

                        // Write heap backed data for each required patch.
                        // TODO: PERF: Investigate static-only deserialization could manage one block and mark in pointers somehow that they are not indiviual
                        for (int i = 0; i < bufferPatches.Length; ++i)
                        {
                            var patch = bufferPatches[i];
                            var header = (BufferHeader*)OffsetFromPointer(tempChunk->Buffer, patch.ChunkOffset);
                            writer.WriteBytes(header->Pointer, patch.AllocSizeBytes);
                            BufferHeader.Destroy(header);
                        }
                    }
                }
            }

            archetypeArray.Dispose();
            blobAssets.Dispose();
            blobAssetMap.Dispose();

            bufferPatches.Dispose();

            typeHashToIndexMap.Dispose();
        }

        static int Align16(int x)
        {
            return (x + 15) & ~15;
        }

        unsafe static void PatchManagedComponentIndices(Chunk* chunk, Archetype* archetype, ref int currentManagedIndex, ManagedComponentStore managedComponentStore)
        {
            for (int i = 0; i < archetype->NumManagedComponents; ++i)
            {
                var index = archetype->TypeMemoryOrder[i + archetype->FirstManagedComponent];
                var managedComponentIndices = (int*)ChunkDataUtility.GetComponentDataRO(chunk, 0, index);
                for (int ei = 0; ei < chunk->Count; ++ei)
                {
                    if (managedComponentIndices[ei] == 0)
                        continue;

                    var obj = managedComponentStore.GetManagedComponent(managedComponentIndices[ei]);
                    if (obj == null)
                        managedComponentIndices[ei] = 0;
                    else
                        managedComponentIndices[ei] = currentManagedIndex++;
                }
            }
        }

        static unsafe void WriteSharedAndManagedComponents(
            EntityManager entityManager,
            UnsafeArchetypePtrList archetypeArray,
            int[] sharedComponentIndicies,
            BinaryWriter writer,
            out object[] referencedObjects,
            bool isDOTSRuntime,
            EntityRemapUtility.EntityRemapInfo* remapping,
            NativeHashMap<BlobAssetPtr, int> blobAssetMap,
            NativeArray<int> blobAssetOffsets)
        {
            int managedComponentCount = 0;
            referencedObjects = null;
            var allManagedObjectsBuffer = new UnsafeAppendBuffer(0, 16, Allocator.Temp);

// We only support serialization in dots runtime for some unit tests but we currently can't support shared component serialization so skip it
#if !NET_DOTS
            var access = entityManager.GetCheckedEntityDataAccess();
            var mcs = access->ManagedComponentStore;
            var managedObjectClone = new ManagedObjectClone();
            var managedObjectRemap = new ManagedObjectRemap();

            var sharedComponentRecordArray = new NativeArray<SharedComponentRecord>(sharedComponentIndicies.Length, Allocator.Temp);
            if (!isDOTSRuntime)
            {
                var propertiesWriter = new ManagedObjectBinaryWriter(&allManagedObjectsBuffer);

                // Custom handling for blob asset fields. This adapter will take care of writing out the byte offset for each blob asset encountered.
                propertiesWriter.AddAdapter(new ManagedObjectSerializeAdapter(remapping, blobAssetMap, blobAssetOffsets));

                for (int i = 0; i < sharedComponentIndicies.Length; ++i)
                {
                    var index = sharedComponentIndicies[i];
                    var sharedData = mcs.GetSharedComponentDataNonDefaultBoxed(index);
                    var type = sharedData.GetType();
                    var typeIndex = TypeManager.GetTypeIndex(type);
                    var typeInfo = TypeManager.GetTypeInfo(typeIndex);
                    var managedObject = Convert.ChangeType(sharedData, type);

                    propertiesWriter.WriteObject(managedObject);

                    sharedComponentRecordArray[i] = new SharedComponentRecord()
                    {
                        StableTypeHash = typeInfo.StableTypeHash,
                        ComponentSize = -1
                    };
                }

                for (int a = 0; a < archetypeArray.Length; ++a)
                {
                    var archetype = archetypeArray.Ptr[a];
                    if (archetype->NumManagedComponents == 0)
                        continue;

                    for (var ci = 0; ci < archetype->Chunks.Count; ++ci)
                    {
                        var chunk = archetype->Chunks.p[ci];

                        for (int i = 0; i < archetype->NumManagedComponents; ++i)
                        {
                            var index = archetype->TypeMemoryOrder[i + archetype->FirstManagedComponent];
                            var managedComponentIndices = (int*)ChunkDataUtility.GetComponentDataRO(chunk, 0, index);
                            var cType = TypeManager.GetTypeInfo(archetype->Types[index].TypeIndex);

                            for (int ei = 0; ei < chunk->Count; ++ei)
                            {
                                if (managedComponentIndices[ei] == 0)
                                    continue;

                                var obj = mcs.GetManagedComponent(managedComponentIndices[ei]);
                                if (obj == null)
                                    continue;

                                if (obj.GetType() != cType.Type)
                                {
                                    throw new InvalidOperationException($"Managed object type {obj.GetType()} doesn't match component type in archetype {cType.Type}");
                                }

                                managedComponentCount++;
                                allManagedObjectsBuffer.Add<ulong>(cType.StableTypeHash);
                                propertiesWriter.WriteObject(obj);
                            }
                        }
                    }
                }
                referencedObjects = propertiesWriter.GetUnityObjects();
            }
            else
            {
                for (int i = 0; i < sharedComponentIndicies.Length; ++i)
                {
                    var index = sharedComponentIndicies[i];
                    object obj = mcs.GetSharedComponentDataNonDefaultBoxed(index);

                    Type type = obj.GetType();
                    var typeIndex = TypeManager.GetTypeIndex(type);
                    var typeInfo = TypeManager.GetTypeInfo(typeIndex);
                    int size = UnsafeUtility.SizeOf(type);

                    sharedComponentRecordArray[i] = new SharedComponentRecord()
                    {
                        StableTypeHash = typeInfo.StableTypeHash,
                        ComponentSize = size
                    };

                    var dataPtr = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(obj, out ulong handle);
                    dataPtr += TypeManager.ObjectOffset;

                    if (typeInfo.HasEntities)
                    {
                        var offsets = TypeManager.GetEntityOffsets(typeInfo);
                        for (var offsetIndex = 0; offsetIndex < typeInfo.EntityOffsetCount; offsetIndex++)
                            *(Entity*) (dataPtr + offsets[offsetIndex].Offset) = EntityRemapUtility.RemapEntity(remapping, *(Entity*) (dataPtr + offsets[offsetIndex].Offset));
                    }

                    if (typeInfo.BlobAssetRefOffsetCount > 0)
                    {
                        PatchBlobAssetRefInfoBeforeSave(dataPtr, TypeManager.GetBlobAssetRefOffsets(typeInfo), typeInfo.BlobAssetRefOffsetCount, blobAssetOffsets, blobAssetMap);
                    }

                    allManagedObjectsBuffer.Add(dataPtr, size);
                    UnsafeUtility.ReleaseGCObject(handle);
                }
            }
#else
            var sharedComponentRecordArray = new NativeArray<SharedComponentRecord>(0, Allocator.Temp);
#endif

            writer.Write(sharedComponentRecordArray.Length);
            writer.WriteArray(sharedComponentRecordArray);

            writer.Write(allManagedObjectsBuffer.Length);

            writer.Write(managedComponentCount);
            writer.WriteBytes(allManagedObjectsBuffer.Ptr, allManagedObjectsBuffer.Length);

            sharedComponentRecordArray.Dispose();
            allManagedObjectsBuffer.Dispose();
        }

#if NET_DOTS
        static unsafe void ReadSharedComponents(ExclusiveEntityTransaction manager, BinaryReader reader, int expectedReadSize, NativeArray<int> sharedComponentRemap, NativeArray<SharedComponentRecord> sharedComponentRecordArray)
        {
            int tempBufferSize = 0;
            for (int i = 0; i < sharedComponentRecordArray.Length; ++i)
                tempBufferSize = math.max(sharedComponentRecordArray[i].ComponentSize, tempBufferSize);
            var buffer = stackalloc byte[tempBufferSize];

            sharedComponentRemap[0] = 0;

            var access = manager.EntityManager.GetCheckedEntityDataAccess();
            var mcs = access->ManagedComponentStore;

            int sizeRead = 0;
            for (int i = 0; i < sharedComponentRecordArray.Length; ++i)
            {
                var record = sharedComponentRecordArray[i];

                reader.ReadBytes(buffer, record.ComponentSize);
                sizeRead += record.ComponentSize;

                var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(record.StableTypeHash);
                if (typeIndex == -1)
                {
                    Console.WriteLine($"Can't find type index for type hash {record.StableTypeHash.ToString()}");
                    throw new InvalidOperationException();
                }

                var data = TypeManager.ConstructComponentFromBuffer(typeIndex, buffer);

                // TODO: this recalculation should be removed once we merge the NET_DOTS and non NET_DOTS hashcode calculations
                var hashCode = TypeManager.GetHashCode(data, typeIndex); // record.hashCode;
                int runtimeIndex = mcs.InsertSharedComponentAssumeNonDefault(typeIndex, hashCode, data);

                sharedComponentRemap[i + 1] = runtimeIndex;
            }

            Assert.AreEqual(expectedReadSize, sizeRead, "The amount of shared component data we read doesn't match the amount we serialized.");
        }

#else
        static unsafe void ReadSharedComponents(ExclusiveEntityTransaction manager, ManagedObjectBinaryReader managedDataReader, NativeArray<int> sharedComponentRemap, NativeArray<SharedComponentRecord> sharedComponentRecordArray)
        {
            // 0 index is special and means default shared component value
            // Also see below the offset + 1 indices for the same reason
            sharedComponentRemap[0] = 0;

            ManagedComponentStore mcs = manager.EntityManager.GetCheckedEntityDataAccess()->ManagedComponentStore;

            for (int i = 0; i < sharedComponentRecordArray.Length; ++i)
            {
                var record = sharedComponentRecordArray[i];
                var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(record.StableTypeHash);
                var typeInfo = TypeManager.GetTypeInfo(typeIndex);
                var managedObject = managedDataReader.ReadObject(typeInfo.Type);
                var currentHash = TypeManager.GetHashCode(managedObject, typeInfo.TypeIndex);
                var sharedComponentIndex = mcs.InsertSharedComponentAssumeNonDefault(typeIndex, currentHash, managedObject);

                // When deserialization a shared component it is possible that it's hashcode changes if for example the referenced object (a UnityEngine.Object for example) becomes null.
                // This can result in the sharedComponentIndex at serialize time being different from the sharedComponentIndex at load time.
                // Thus we keep a remap table to handle this potential remap.
                // NOTE: in most cases the remap table will always be all indices matching,
                // But it doesn't look like it's worth optimizing this away at this point.
                sharedComponentRemap[i + 1] = sharedComponentIndex;
            }
        }

        // True when a component is valid to using in world serialization. A component IsSerializable when it is valid to blit
        // the data across storage media. Thus components containing pointers have an IsSerializable of false as the component
        // is blittable but no longer valid upon deserialization.
        private static bool IsTypeValidForSerialization(Type type)
        {
            if (type.GetCustomAttribute<ChunkSerializableAttribute>() != null)
                return true;

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                if (field.IsStatic)
                    continue;

                if (field.FieldType.IsPointer || (field.FieldType == typeof(UIntPtr) || field.FieldType == typeof(IntPtr)))
                {
                    return false;
                }
                else if (field.FieldType.IsValueType && !field.FieldType.IsPrimitive && !field.FieldType.IsEnum)
                {
                    if (!IsTypeValidForSerialization(field.FieldType))
                        return false;
                }
            }

            return true;
        }

        private static void ValidateTypeForSerialization(TypeManager.TypeInfo typeInfo)
        {
            // Shared Components are expected to be handled specially and are not required to be blittable
            if (typeInfo.Category == TypeManager.TypeCategory.ISharedComponentData)
            {
                if (typeInfo.HasEntities)
                {
                    throw new ArgumentException(
                        $"Shared component type '{TypeManager.GetType(typeInfo.TypeIndex)}' contains an (potentially nested) Entity field. " +
                        $"Serializing of shared components with Entity fields is not supported");
                }
                return;
            }

            if (!IsTypeValidForSerialization(typeInfo.Type))
            {
                throw new ArgumentException($"Blittable component type '{TypeManager.GetType(typeInfo.TypeIndex)}' contains a (potentially nested) pointer field. " +
                    $"Serializing bare pointers will likely lead to runtime errors. Remove this field and consider serializing the data " +
                    $"it points to another way such as by using a BlobAssetReference or a [Serializable] ISharedComponent. If for whatever " +
                    $"reason the pointer field should in fact be serialized, add the [ChunkSerializable] attribute to your type to bypass this error.");
            }
        }

#endif

        static unsafe int ReadSharedComponentMetadata(BinaryReader reader, out NativeArray<int> sharedComponentArrays, out NativeArray<SharedComponentRecord> sharedComponentRecordArray)
        {
            int sharedComponentArraysLength = reader.ReadInt();
            sharedComponentArrays = new NativeArray<int>(sharedComponentArraysLength, Allocator.Temp);
            reader.ReadArray(sharedComponentArrays, sharedComponentArraysLength);

            var sharedComponentRecordArrayLength = reader.ReadInt();
            sharedComponentRecordArray = new NativeArray<SharedComponentRecord>(sharedComponentRecordArrayLength, Allocator.Temp);
            reader.ReadArray(sharedComponentRecordArray, sharedComponentRecordArrayLength);

            return sharedComponentRecordArrayLength;
        }

        static unsafe void GatherAllUsedBlobAssets(
            EntityManager entityManager,
            int[] sharedComponentIndices,
            bool isDOTSRuntime,
            UnsafeArchetypePtrList archetypeArray,
            out NativeList<BlobAssetPtr> blobAssets,
            out NativeHashMap<BlobAssetPtr, int> blobAssetMap)
        {
            blobAssetMap = new NativeHashMap<BlobAssetPtr, int>(100, Allocator.Temp);

            blobAssets = new NativeList<BlobAssetPtr>(100, Allocator.Temp);
            for (int a = 0; a < archetypeArray.Length; ++a)
            {
                var archetype = archetypeArray.Ptr[a];
                if (!archetype->ContainsBlobAssetRefs)
                    continue;

                var typeCount = archetype->TypesCount;
                for (var ci = 0; ci < archetype->Chunks.Count; ++ci)
                {
                    var chunk = archetype->Chunks.p[ci];
                    var entityCount = chunk->Count;
                    for (var unordered_ti = 0; unordered_ti < typeCount; ++unordered_ti)
                    {
                        var ti = archetype->TypeMemoryOrder[unordered_ti];
                        var type = archetype->Types[ti];
                        if (type.IsZeroSized || type.IsManagedComponent)
                            continue;

                        var ct = TypeManager.GetTypeInfo(type.TypeIndex);
                        var blobAssetRefCount = ct.BlobAssetRefOffsetCount;
                        if (blobAssetRefCount == 0)
                            continue;

                        var blobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsets(ct);
                        var chunkBuffer = chunk->Buffer;

                        if (blobAssetRefCount > 0)
                        {
                            int subArrayOffset = archetype->Offsets[ti];
                            byte* componentArrayStart = OffsetFromPointer(chunkBuffer, subArrayOffset);

                            if (type.IsBuffer)
                            {
                                BufferHeader* header = (BufferHeader*)componentArrayStart;
                                int strideSize = archetype->SizeOfs[ti];
                                int elementSize = ct.ElementSize;

                                for (int bi = 0; bi < entityCount; ++bi)
                                {
                                    var bufferStart = BufferHeader.GetElementPointer(header);
                                    var bufferEnd = bufferStart + header->Length * elementSize;
                                    for (var componentData = bufferStart; componentData < bufferEnd; componentData += elementSize)
                                    {
                                        AddBlobAssetRefInfo(componentData, blobAssetRefOffsets, blobAssetRefCount, ref blobAssetMap, ref blobAssets);
                                    }

                                    header = (BufferHeader*)OffsetFromPointer(header, strideSize);
                                }
                            }
                            else
                            {
                                int componentSize = archetype->SizeOfs[ti];
                                byte* end = componentArrayStart + componentSize * entityCount;
                                for (var componentData = componentArrayStart; componentData < end; componentData += componentSize)
                                {
                                    AddBlobAssetRefInfo(componentData, blobAssetRefOffsets, blobAssetRefCount, ref blobAssetMap, ref blobAssets);
                                }
                            }
                        }
                    }
                }
            }

#if !NET_DOTS
            var access = entityManager.GetCheckedEntityDataAccess();
            var mcs = access->ManagedComponentStore;
            var managedObjectBlobs = new ManagedObjectBlobs();

            if (!isDOTSRuntime)
            {
                for (var i = 0; i < sharedComponentIndices.Length; i++)
                {
                    var sharedComponentIndex = sharedComponentIndices[i];
                    var sharedComponentValue = mcs.GetSharedComponentDataNonDefaultBoxed(sharedComponentIndex);
                    managedObjectBlobs.GatherBlobAssetReferences(sharedComponentValue, blobAssets, blobAssetMap);
                }

                for (var archetypeIndex = 0; archetypeIndex < archetypeArray.Length; archetypeIndex++)
                {
                    var archetype = archetypeArray.Ptr[archetypeIndex];

                    if (archetype->NumManagedComponents == 0)
                        continue;

                    for (var chunkIndex = 0; chunkIndex < archetype->Chunks.Count; chunkIndex++)
                    {
                        var chunk = archetype->Chunks.p[chunkIndex];

                        for (var unorderedTypeIndexInArchetype = 0; unorderedTypeIndexInArchetype < archetype->NumManagedComponents; ++unorderedTypeIndexInArchetype)
                        {
                            var typeIndexInArchetype = archetype->TypeMemoryOrder[archetype->FirstManagedComponent + unorderedTypeIndexInArchetype];
                            var managedComponentIndices = (int*)ChunkDataUtility.GetComponentDataRO(chunk, 0, typeIndexInArchetype);
                            var typeInfo = TypeManager.GetTypeInfo(archetype->Types[typeIndexInArchetype].TypeIndex);

                            for (var entityIndex = 0; entityIndex < chunk->Count; entityIndex++)
                            {
                                if (managedComponentIndices[entityIndex] == 0)
                                    continue;

                                var managedComponentValue = mcs.GetManagedComponent(managedComponentIndices[entityIndex]);

                                if (managedComponentValue == null)
                                    continue;

                                if (managedComponentValue.GetType() != typeInfo.Type)
                                {
                                    throw new InvalidOperationException($"Managed object type {managedComponentValue.GetType()} doesn't match component type in archetype {typeInfo.Type}");
                                }

                                managedObjectBlobs.GatherBlobAssetReferences(managedComponentValue, blobAssets, blobAssetMap);
                            }
                        }
                    }
                }
            }
            else
            {
                for (var i = 0; i < sharedComponentIndices.Length; i++)
                {
                    var sharedComponentIndex = sharedComponentIndices[i];
                    var sharedComponentValue = mcs.GetSharedComponentDataNonDefaultBoxed(sharedComponentIndex);
                    managedObjectBlobs.GatherBlobAssetReferences(sharedComponentValue, blobAssets, blobAssetMap);
                }

                for (var i = 0; i < sharedComponentIndices.Length; i++)
                {
                    var sharedComponentIndex = sharedComponentIndices[i];
                    var sharedComponentValue = mcs.GetSharedComponentDataNonDefaultBoxed(sharedComponentIndex);

                    var type = sharedComponentValue.GetType();
                    var typeIndex = TypeManager.GetTypeIndex(type);
                    var typeInfo = TypeManager.GetTypeInfo(typeIndex);

                    var ptr = (byte*)UnsafeUtility.PinGCObjectAndGetAddress(sharedComponentValue, out var handle);
                    ptr += TypeManager.ObjectOffset;

                    var blobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsets(typeInfo);
                    var blobAssetRefCount = typeInfo.BlobAssetRefOffsetCount;
                    AddBlobAssetRefInfo(ptr, blobAssetRefOffsets, blobAssetRefCount, ref blobAssetMap, ref blobAssets);

                    UnsafeUtility.ReleaseGCObject(handle);
                }
            }
#endif
        }

        private static unsafe void AddBlobAssetRefInfo(byte* componentData, TypeManager.EntityOffsetInfo* blobAssetRefOffsets, int blobAssetRefCount,
            ref NativeHashMap<BlobAssetPtr, int> blobAssetMap, ref NativeList<BlobAssetPtr> blobAssets)
        {
            for (int i = 0; i < blobAssetRefCount; ++i)
            {
                var blobAssetRefOffset = blobAssetRefOffsets[i].Offset;
                var blobAssetRefPtr = (BlobAssetReferenceData*)(componentData + blobAssetRefOffset);
                if (blobAssetRefPtr->m_Ptr == null)
                    continue;

                var blobAssetPtr = new BlobAssetPtr(blobAssetRefPtr->Header);
                if (!blobAssetMap.TryGetValue(blobAssetPtr, out var blobAssetIndex))
                {
                    blobAssetIndex = blobAssets.Length;
                    blobAssets.Add(blobAssetPtr);
                    blobAssetMap.TryAdd(blobAssetPtr, blobAssetIndex);
                }
            }
        }

        private static unsafe void PatchBlobAssetsInChunkBeforeSave(Chunk* tempChunk, Chunk* originalChunk,
            NativeArray<int> blobAssetOffsets, NativeHashMap<BlobAssetPtr, int> blobAssetMap)
        {
            var archetype = originalChunk->Archetype;
            var typeCount = archetype->TypesCount;
            var entityCount = originalChunk->Count;
            for (var unordered_ti = 0; unordered_ti < typeCount; ++unordered_ti)
            {
                var ti = archetype->TypeMemoryOrder[unordered_ti];
                var type = archetype->Types[ti];
                if (type.IsZeroSized || type.IsManagedComponent)
                    continue;

                var ct = TypeManager.GetTypeInfo(type.TypeIndex);
                var blobAssetRefCount = ct.BlobAssetRefOffsetCount;
                if (blobAssetRefCount == 0)
                    continue;

                var blobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsets(ct);
                var chunkBuffer = tempChunk->Buffer;
                int subArrayOffset = archetype->Offsets[ti];
                byte* componentArrayStart = OffsetFromPointer(chunkBuffer, subArrayOffset);

                if (type.IsBuffer)
                {
                    BufferHeader* header = (BufferHeader*)componentArrayStart;
                    int strideSize = archetype->SizeOfs[ti];
                    var elementSize = ct.ElementSize;

                    for (int bi = 0; bi < entityCount; ++bi)
                    {
                        var bufferStart = BufferHeader.GetElementPointer(header);
                        var bufferEnd = bufferStart + header->Length * elementSize;
                        for (var componentData = bufferStart; componentData < bufferEnd; componentData += elementSize)
                        {
                            PatchBlobAssetRefInfoBeforeSave(componentData, blobAssetRefOffsets, blobAssetRefCount, blobAssetOffsets, blobAssetMap);
                        }

                        header = (BufferHeader*)OffsetFromPointer(header, strideSize);
                    }
                }
                else if (blobAssetRefCount > 0)
                {
                    int size = archetype->SizeOfs[ti];
                    byte* end = componentArrayStart + size * entityCount;
                    for (var componentData = componentArrayStart; componentData < end; componentData += size)
                    {
                        PatchBlobAssetRefInfoBeforeSave(componentData, blobAssetRefOffsets, blobAssetRefCount, blobAssetOffsets, blobAssetMap);
                    }
                }
            }
        }

        private static unsafe void PatchBlobAssetRefInfoBeforeSave(byte* componentData, TypeManager.EntityOffsetInfo* blobAssetRefOffsets, int blobAssetRefCount,
            NativeArray<int> blobAssetOffsets, NativeHashMap<BlobAssetPtr, int> blobAssetMap)
        {
            for (int i = 0; i < blobAssetRefCount; ++i)
            {
                var blobAssetRefOffset = blobAssetRefOffsets[i].Offset;
                var blobAssetRefPtr = (BlobAssetReferenceData*)(componentData + blobAssetRefOffset);
                int value = -1;
                if (blobAssetRefPtr->m_Ptr != null)
                {
                    value = blobAssetMap[new BlobAssetPtr(blobAssetRefPtr->Header)];
                    value = blobAssetOffsets[value];
                }
                blobAssetRefPtr->m_Ptr = (byte*)value;
            }
        }

        private static unsafe void PatchBlobAssetsInChunkAfterLoad(Chunk* chunk, byte* allBlobAssetData)
        {
            var archetype = chunk->Archetype;
            var typeCount = archetype->TypesCount;
            var entityCount = chunk->Count;
            for (var unordered_ti = 0; unordered_ti < typeCount; ++unordered_ti)
            {
                var ti = archetype->TypeMemoryOrder[unordered_ti];
                var type = archetype->Types[ti];
                if (type.IsZeroSized)
                    continue;

                var ct = TypeManager.GetTypeInfo(type.TypeIndex);
                var blobAssetRefCount = ct.BlobAssetRefOffsetCount;
                if (blobAssetRefCount == 0)
                    continue;

                var blobAssetRefOffsets = TypeManager.GetBlobAssetRefOffsets(ct);
                var chunkBuffer = chunk->Buffer;
                int subArrayOffset = archetype->Offsets[ti];
                byte* componentArrayStart = OffsetFromPointer(chunkBuffer, subArrayOffset);

                if (type.IsBuffer)
                {
                    BufferHeader* header = (BufferHeader*)componentArrayStart;
                    int strideSize = archetype->SizeOfs[ti];
                    var elementSize = ct.ElementSize;

                    for (int bi = 0; bi < entityCount; ++bi)
                    {
                        var bufferStart = BufferHeader.GetElementPointer(header);
                        for (int ei = 0; ei < header->Length; ++ei)
                        {
                            byte* componentData = bufferStart + ei * elementSize;
                            for (int i = 0; i < blobAssetRefCount; ++i)
                            {
                                var offset = blobAssetRefOffsets[i].Offset;
                                var blobAssetRefPtr = (BlobAssetReferenceData*)(componentData + offset);
                                int value = (int)blobAssetRefPtr->m_Ptr;
                                byte* ptr = null;
                                if (value != -1)
                                {
                                    ptr = allBlobAssetData + value;
                                }
                                blobAssetRefPtr->m_Ptr = ptr;
                            }
                        }

                        header = (BufferHeader*)OffsetFromPointer(header, strideSize);
                    }
                }
                else if (blobAssetRefCount > 0)
                {
                    int size = archetype->SizeOfs[ti];
                    byte* end = componentArrayStart + size * entityCount;
                    for (var componentData = componentArrayStart; componentData < end; componentData += size)
                    {
                        for (int i = 0; i < blobAssetRefCount; ++i)
                        {
                            var offset = blobAssetRefOffsets[i].Offset;
                            var blobAssetRefPtr = (BlobAssetReferenceData*)(componentData + offset);
                            int value = (int)blobAssetRefPtr->m_Ptr;
                            byte* ptr = null;
                            if (value != -1)
                            {
                                ptr = allBlobAssetData + value;
                            }
                            blobAssetRefPtr->m_Ptr = ptr;
                        }
                    }
                }
            }
        }

        private static unsafe void FillPatchRecordsForChunk(Chunk* chunk, NativeList<BufferPatchRecord> bufferPatches)
        {
            var archetype = chunk->Archetype;
            byte* tempChunkBuffer = chunk->Buffer;
            int entityCount = chunk->Count;

            // Find all buffer pointer locations and work out how much memory the deserializer must allocate on load.
            for (int ti = 0; ti < archetype->TypesCount; ++ti)
            {
                int index = archetype->TypeMemoryOrder[ti];
                var type = archetype->Types[index];
                if (type.IsZeroSized)
                    continue;

                if (type.IsBuffer)
                {
                    var ct = TypeManager.GetTypeInfo(type.TypeIndex);
                    int subArrayOffset = archetype->Offsets[index];
                    BufferHeader* header = (BufferHeader*)OffsetFromPointer(tempChunkBuffer, subArrayOffset);
                    int stride = archetype->SizeOfs[index];
                    var elementSize = ct.ElementSize;

                    for (int bi = 0; bi < entityCount; ++bi)
                    {
                        if (header->Pointer != null)
                        {
                            int capacityInBytes = elementSize * header->Capacity;
                            bufferPatches.Add(new BufferPatchRecord
                            {
                                ChunkOffset = (int)(((byte*)header) - tempChunkBuffer),
                                AllocSizeBytes = capacityInBytes
                            });
                        }

                        header = (BufferHeader*)OffsetFromPointer(header, stride);
                    }
                }
            }
        }

        static unsafe void FillSharedComponentIndexRemap(int* remapArray, Archetype* archetype)
        {
            int i = 0;
            for (int iType = 1; iType < archetype->TypesCount; ++iType)
            {
                int orderedIndex = archetype->TypeMemoryOrder[iType] - archetype->FirstSharedComponent;
                if (0 <= orderedIndex && orderedIndex < archetype->NumSharedComponents)
                    remapArray[i++] = orderedIndex;
            }
        }

        static unsafe void RemapSharedComponentIndices(int* destValues, Archetype* archetype, NativeArray<int> remappedIndices, int* sourceValues)
        {
            int i = 0;
            for (int iType = 1; iType < archetype->TypesCount; ++iType)
            {
                int orderedIndex = archetype->TypeMemoryOrder[iType] - archetype->FirstSharedComponent;
                if (0 <= orderedIndex && orderedIndex < archetype->NumSharedComponents)
                    destValues[orderedIndex] = remappedIndices[sourceValues[i++]];
            }
        }

        private static unsafe void FillSharedComponentArrays(NativeArray<int> sharedComponentArrays, UnsafeArchetypePtrList archetypeArray, NativeHashMap<int, int> sharedComponentMapping)
        {
            int index = 0;
            for (int iArchetype = 0; iArchetype < archetypeArray.Length; ++iArchetype)
            {
                var archetype = archetypeArray.Ptr[iArchetype];
                int numSharedComponents = archetype->NumSharedComponents;
                if (numSharedComponents == 0)
                    continue;
                var sharedComponentIndexRemap = stackalloc int[numSharedComponents];

                FillSharedComponentIndexRemap(sharedComponentIndexRemap, archetype);
                for (int iChunk = 0; iChunk < archetype->Chunks.Count; ++iChunk)
                {
                    var sharedComponents = archetype->Chunks.p[iChunk]->SharedComponentValues;
                    for (int iType = 0; iType < numSharedComponents; iType++)
                    {
                        int remappedIndex = sharedComponentIndexRemap[iType];
                        sharedComponentArrays[index++] = sharedComponentMapping[sharedComponents[remappedIndex]];
                    }
                }
            }
            Assert.AreEqual(sharedComponentArrays.Length, index);
        }

        private static unsafe NativeHashMap<int, int> GatherSharedComponents(UnsafeArchetypePtrList archetypeArray, out int sharedComponentArraysTotalCount)
        {
            sharedComponentArraysTotalCount = 0;
            var sharedIndexToSerialize = new NativeHashMap<int, int>(1024, Allocator.Temp);
            sharedIndexToSerialize.TryAdd(0, 0); // All default values map to 0
            int nextIndex = 1;
            for (int iArchetype = 0; iArchetype < archetypeArray.Length; ++iArchetype)
            {
                var archetype = archetypeArray.Ptr[iArchetype];
                sharedComponentArraysTotalCount += archetype->Chunks.Count * archetype->NumSharedComponents;

                int numSharedComponents = archetype->NumSharedComponents;
                for (int iType = 0; iType < numSharedComponents; iType++)
                {
                    var sharedComponents = archetype->Chunks.GetSharedComponentValueArrayForType(iType);
                    for (int iChunk = 0; iChunk < archetype->Chunks.Count; ++iChunk)
                    {
                        int sharedComponentIndex = sharedComponents[iChunk];
                        if (!sharedIndexToSerialize.TryGetValue(sharedComponentIndex, out var val))
                        {
                            sharedIndexToSerialize.TryAdd(sharedComponentIndex, nextIndex++);
                        }
                    }
                }
            }

            return sharedIndexToSerialize;
        }

        private static unsafe void ClearChunkHeaderComponents(Chunk* chunk)
        {
            int chunkHeaderTypeIndex = TypeManager.GetTypeIndex<ChunkHeader>();
            var archetype = chunk->Archetype;
            var typeIndexInArchetype = ChunkDataUtility.GetIndexInTypeArray(chunk->Archetype, chunkHeaderTypeIndex);
            if (typeIndexInArchetype == -1)
                return;

            var buffer = chunk->Buffer;
            var length = chunk->Count;
            var startOffset = archetype->Offsets[typeIndexInArchetype];
            var chunkHeaders = (ChunkHeader*)(buffer + startOffset);
            for (int i = 0; i < length; ++i)
            {
                chunkHeaders[i] = ChunkHeader.Null;
            }
        }

        static unsafe byte* OffsetFromPointer(void* ptr, int offset)
        {
            return ((byte*)ptr) + offset;
        }

        static unsafe void WriteArchetypes(BinaryWriter writer, UnsafeArchetypePtrList archetypeArray, UnsafeHashMap<ulong, int> typeHashToIndexMap)
        {
            writer.Write(archetypeArray.Length);

            for (int a = 0; a < archetypeArray.Length; ++a)
            {
                var archetype = archetypeArray.Ptr[a];

                writer.Write(archetype->EntityCount);
                writer.Write(archetype->TypesCount - 1);
                for (int i = 1; i < archetype->TypesCount; ++i)
                {
                    var componentType = archetype->Types[i];
                    int flag = componentType.IsChunkComponent ? TypeManager.ChunkComponentTypeFlag : 0;
                    var hash = TypeManager.GetTypeInfo(componentType.TypeIndex).StableTypeHash;
                    writer.Write(typeHashToIndexMap[hash] | flag);
                }
            }
        }

        static unsafe int GenerateRemapInfo(EntityManager entityManager, UnsafeArchetypePtrList archetypeArray, NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapInfos)
        {
            int nextEntityId = 1; //0 is reserved for Entity.Null;

            int totalChunkCount = 0;
            for (int archetypeIndex = 0; archetypeIndex < archetypeArray.Length; ++archetypeIndex)
            {
                var archetype = archetypeArray.Ptr[archetypeIndex];
                for (int i = 0; i < archetype->Chunks.Count; ++i)
                {
                    var chunk = archetype->Chunks.p[i];
                    for (int iEntity = 0; iEntity < chunk->Count; ++iEntity)
                    {
                        var entity = *(Entity*)ChunkDataUtility.GetComponentDataRO(chunk, iEntity, 0);
                        EntityRemapUtility.AddEntityRemapping(ref entityRemapInfos, entity, new Entity { Version = 0, Index = nextEntityId });
                        ++nextEntityId;
                    }

                    totalChunkCount += 1;
                }
            }

            return totalChunkCount;
        }
    }
}
