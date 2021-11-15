using System;
using System.Diagnostics;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    internal unsafe partial struct EntityComponentStore
    {
        // ----------------------------------------------------------------------------------------------------------
        // PUBLIC
        // ----------------------------------------------------------------------------------------------------------

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void CheckInternalConsistency(object[] managedComponentData)
        {
            Assert.IsTrue(ManagedChangesTracker.Empty);
            var managedComponentIndices = new UnsafeBitArray(m_ManagedComponentIndex, Allocator.Temp);

            Assert.IsTrue(managedComponentData.Length >= m_ManagedComponentIndex);
            for (int i = m_ManagedComponentIndex; i < managedComponentData.Length; ++i)
                Assert.IsNull(managedComponentData[i]);

            EntityComponentStore* selfPtr;
            fixed(EntityComponentStore* self = &this) { selfPtr = self; }  // This is safe - we're allocated on the native heap

            // Iterate by archetype
            var entityCountByArchetype = 0;
            for (var i = 0; i < m_Archetypes.Length; ++i)
            {
                var archetype = m_Archetypes.Ptr[i];
                int managedTypeBegin = archetype->FirstManagedComponent;
                int managedTypeEnd = archetype->ManagedComponentsEnd;
                Assert.AreEqual((IntPtr)selfPtr, (IntPtr)archetype->EntityComponentStore);

                var countInArchetype = 0;
                for (var j = 0; j < archetype->Chunks.Count; ++j)
                {
                    var chunk = archetype->Chunks.p[j];
                    Assert.IsTrue(chunk->Archetype == archetype);
                    Assert.IsTrue(chunk->Capacity >= chunk->Count);
                    Assert.AreEqual(chunk->Count, archetype->Chunks.GetChunkEntityCount(j));

                    var chunkEntities = (Entity*)chunk->Buffer;
                    AssertEntitiesExist(chunkEntities, chunk->Count);

                    if (chunk->Count < chunk->Capacity)
                    {
                        if (archetype->NumSharedComponents == 0)
                        {
                            Assert.IsTrue(chunk->ListWithEmptySlotsIndex >= 0 && chunk->ListWithEmptySlotsIndex < archetype->ChunksWithEmptySlots.Length);
                            Assert.IsTrue(chunk == archetype->ChunksWithEmptySlots.Ptr[chunk->ListWithEmptySlotsIndex]);
                        }
                        else
                        {
                            Assert.IsTrue(archetype->FreeChunksBySharedComponents.Contains(chunk));
                        }
                    }

                    countInArchetype += chunk->Count;

                    if (chunk->Archetype->HasChunkHeader) // Chunk entities with chunk components are not supported
                    {
                        Assert.IsFalse(chunk->Archetype->HasChunkComponents);
                    }

                    Assert.AreEqual(chunk->Archetype->HasChunkComponents, chunk->metaChunkEntity != Entity.Null);
                    if (chunk->metaChunkEntity != Entity.Null)
                    {
                        var chunkHeaderTypeIndex = TypeManager.GetTypeIndex<ChunkHeader>();
                        AssertEntitiesExist(&chunk->metaChunkEntity, 1);
                        AssertEntityHasComponent(chunk->metaChunkEntity, chunkHeaderTypeIndex);
                        var chunkHeader =
                            *(ChunkHeader*)GetComponentDataWithTypeRO(chunk->metaChunkEntity,
                                chunkHeaderTypeIndex);
                        Assert.IsTrue(chunk == chunkHeader.ArchetypeChunk.m_Chunk);
                        var metaChunk = GetChunk(chunk->metaChunkEntity);
                        Assert.IsTrue(metaChunk->Archetype == chunk->Archetype->MetaChunkArchetype);
                        Assert.IsTrue(chunkHeader.ArchetypeChunk.m_EntityComponentStore == selfPtr);
                    }

                    for (int iType = managedTypeBegin; iType < managedTypeEnd; ++iType)
                    {
                        var managedIndicesInChunk = (int*)(chunk->Buffer + archetype->Offsets[iType]);
                        for (int ie = 0; ie < chunk->Count; ++ie)
                        {
                            var index = managedIndicesInChunk[ie];
                            if (index == 0)
                                continue;

                            Assert.AreEqual(managedComponentData[index].GetType(), TypeManager.GetType(archetype->Types[iType].TypeIndex));
                            Assert.IsTrue(index < m_ManagedComponentIndex, "Managed component index in chunk is out of range.");
                            Assert.IsFalse(managedComponentIndices.IsSet(index), "Managed component index is used multiple times.");
                            managedComponentIndices.Set(index, true);
                        }
                    }
                }

                Assert.AreEqual(countInArchetype, archetype->EntityCount);

                entityCountByArchetype += countInArchetype;
            }

            for (int i = 0; i < m_ManagedComponentIndex; ++i)
                Assert.AreEqual(managedComponentData[i] != null, managedComponentIndices.IsSet(i));

            var freeManagedIndices = (int*)m_ManagedComponentFreeIndex.Ptr;
            var freeManagedCount = m_ManagedComponentFreeIndex.Length / sizeof(int);

            for (int i = 0; i < freeManagedCount; ++i)
            {
                var index = freeManagedIndices[i];
                Assert.IsTrue(0 < index && index < m_ManagedComponentIndex, "Managed component index in free list is out of range.");
                Assert.IsFalse(managedComponentIndices.IsSet(index), "Managed component was marked as free but is used in chunk.");
                managedComponentIndices.Set(index, true);
            }

            Assert.IsTrue(m_ManagedComponentIndex - 1 == 0 || managedComponentIndices.TestAll(1, m_ManagedComponentIndex - 1), "Managed component index has leaked.");
            managedComponentIndices.Dispose();

            // Iterate by free list
            Assert.IsTrue(m_EntityInChunkByEntity[m_NextFreeEntityIndex].Chunk == null);

            var entityCountByFreeList = EntitiesCapacity;
            int freeIndex = m_NextFreeEntityIndex;
            while (freeIndex != -1)
            {
                Assert.IsTrue(m_EntityInChunkByEntity[freeIndex].Chunk == null);
                Assert.IsTrue(freeIndex < EntitiesCapacity);

                freeIndex = m_EntityInChunkByEntity[freeIndex].IndexInChunk;

                entityCountByFreeList--;
            }

            // iterate by entities
            var entityCountByEntities = 0;
            var entityType = TypeManager.GetTypeIndex<Entity>();
            for (var i = 0; i != EntitiesCapacity; i++)
            {
                var chunk = m_EntityInChunkByEntity[i].Chunk;
                if (chunk == null)
                    continue;

                entityCountByEntities++;
                var archetype = m_ArchetypeByEntity[i];
                Assert.AreEqual((IntPtr)archetype, (IntPtr)chunk->Archetype);
                Assert.AreEqual(entityType, archetype->Types[0].TypeIndex);
                Assert.IsTrue(m_EntityInChunkByEntity[i].IndexInChunk < m_EntityInChunkByEntity[i].Chunk->Count);
                var entity = *(Entity*)ChunkDataUtility.GetComponentDataRO(m_EntityInChunkByEntity[i].Chunk,
                    m_EntityInChunkByEntity[i].IndexInChunk, 0);
                Assert.AreEqual(i, entity.Index);
                Assert.AreEqual(m_VersionByEntity[i], entity.Version);

                Assert.IsTrue(Exists(entity));
            }


            Assert.AreEqual(entityCountByEntities, entityCountByArchetype);

            // Enabling this fails SerializeEntitiesWorksWithBlobAssetReferences.
            // There is some special entity 0 usage in the serialization code.

            // @TODO: Review with simon looks like a potential leak?
            //Assert.AreEqual(entityCountByEntities, entityCountByFreeList);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AssertSameEntities(EntityComponentStore* rhs, EntityComponentStore* lhs)
        {
            Assert.AreEqual(rhs->EntitiesCapacity, lhs->EntitiesCapacity);
            var lhsEntities = lhs->m_EntityInChunkByEntity;
            var rhsEntities = rhs->m_EntityInChunkByEntity;
            int capacity = lhs->EntitiesCapacity;
            for (int i = 0; i != capacity; i++)
            {
                if (lhsEntities[i].Chunk == null && rhsEntities[i].Chunk == null)
                    continue;

                if (lhsEntities[i].IndexInChunk != rhsEntities[i].IndexInChunk)
                    Assert.AreEqual(lhsEntities[i].IndexInChunk, rhsEntities[i].IndexInChunk);
            }
            Assert.AreEqual(rhs->m_NextFreeEntityIndex, lhs->m_NextFreeEntityIndex);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void ValidateEntity(Entity entity)
        {
            if (entity.Index < 0)
                throw new ArgumentException(
                    $"All entities created using EntityCommandBuffer.CreateEntity must be realized via playback(). One of the entities is still deferred (Index: {entity.Index}).");
            if ((uint)entity.Index >= (uint)EntitiesCapacity)
                throw new ArgumentException(
                    "An Entity index is larger than the capacity of the EntityManager. This means the entity was created by a different world or the entity.Index got corrupted or incorrectly assigned and it may not be used on this EntityManager.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void AssertArchetypeComponents(ComponentTypeInArchetype* types, int count)
        {
            if (count < 1)
                throw new ArgumentException($"Invalid component count");
            if (types[0].TypeIndex == 0)
                throw new ArgumentException($"Component type may not be null");
            if (types[0].TypeIndex != m_EntityType)
                throw new ArgumentException($"The Entity ID must always be the first component");

            for (var i = 1; i < count; i++)
            {
                if (types[i - 1].TypeIndex == types[i].TypeIndex)
                    throw new ArgumentException(
                        $"It is not allowed to have two components of the same type on the same entity. ({types[i - 1]} and {types[i]})");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertCanCreateArchetype(ComponentType* componentTypes, int componentTypeCount)
        {
            var entityTypeInfo = GetTypeInfo(m_EntityType);
            var archetypeInstanceSize = GetComponentArraySize(entityTypeInfo.SizeInChunk, 1);
            for (int i = 0; i < componentTypeCount; i++)
            {
                var componentTypeInfo = GetTypeInfo(componentTypes[i].TypeIndex);
                var componentInstanceSize = GetComponentArraySize(componentTypeInfo.SizeInChunk, 1);
                archetypeInstanceSize += componentInstanceSize;
            }
            var chunkDataSize = Chunk.GetChunkBufferSize();
            if (archetypeInstanceSize > chunkDataSize)
                throw new ArgumentException($"Archetype too large to fit in chunk. Instance size {archetypeInstanceSize} bytes.  Maximum chunk size {chunkDataSize}.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertEntitiesExist(Entity* entities, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var entity = entities + i;

                ValidateEntity(*entity);

                int index = entity->Index;
                var exists = m_VersionByEntity[index] == entity->Version &&
                    m_EntityInChunkByEntity[index].Chunk != null;
                if (!exists)
                    throw new ArgumentException(
                        "All entities passed to EntityManager must exist. One of the entities has already been destroyed or was never created.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertValidEntities(Entity* entities, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var entity = entities + i;

                ValidateEntity(*entity);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertEntityHasComponent(Entity entity, ComponentType componentType)
        {
            if (HasComponent(entity, componentType))
                return;

            if (!Exists(entity))
                throw new ArgumentException("The entity does not exist");

            throw new ArgumentException($"A component with type:{componentType} has not been added to the entity.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertEntityHasComponent(Entity entity, int componentType)
        {
            AssertEntityHasComponent(entity, ComponentType.FromTypeIndex(componentType));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertCanAddComponent(Archetype* archetype, ComponentType componentType)
        {
            if (componentType == m_EntityComponentType)
                throw new ArgumentException("Cannot add Entity as a component.");

            if (componentType.IsSharedComponent && (archetype->NumSharedComponents == kMaxSharedComponentCount))
                throw new InvalidOperationException($"Cannot add more than {kMaxSharedComponentCount} SharedComponent to a single Archetype");

            var componentTypeInfo = GetTypeInfo(componentType.TypeIndex);
            var componentInstanceSize = GetComponentArraySize(componentTypeInfo.SizeInChunk, 1);
            var archetypeInstanceSize = archetype->InstanceSizeWithOverhead + componentInstanceSize;
            var chunkDataSize = Chunk.GetChunkBufferSize();
            if (archetypeInstanceSize > chunkDataSize)
                throw new InvalidOperationException("Entity archetype component data is too large. Previous archetype size per instance {archetype->InstanceSizeWithOverhead}  bytes. Attempting to add component size {componentInstanceSize} bytes. Maximum chunk size {chunkDataSize}.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertCanAddComponent(Entity entity, ComponentType componentType)
        {
            if (!Exists(entity))
                throw new InvalidOperationException("The entity does not exist");

            AssertCanAddComponent(GetArchetype(entity), componentType);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertCanAddComponent(Entity entity, int componentType)
        {
            AssertCanAddComponent(entity, ComponentType.FromTypeIndex(componentType));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertCanAddComponent(UnsafeMatchingArchetypePtrList archetypeList, ComponentType componentType)
        {
            for (int i = 0; i < archetypeList.Length; i++)
                AssertCanAddComponent(archetypeList.Ptr[i]->Archetype, componentType);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertCanAddComponents(Entity entity, ComponentTypes types)
        {
            for (int i = 0; i < types.Length; ++i)
                AssertCanAddComponent(entity, ComponentType.FromTypeIndex(types.GetTypeIndex(i)));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertCanRemoveComponent(Entity entity, ComponentType componentType)
        {
            if (componentType == m_EntityComponentType)
                throw new ArgumentException("Cannot remove Entity as a component. Use DestroyEntity if you want to delete Entity and all associated components.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertCanRemoveComponents(Entity entity, ComponentTypes types)
        {
            for (int i = 0; i < types.Length; ++i)
                AssertCanRemoveComponent(entity, ComponentType.FromTypeIndex(types.GetTypeIndex(i)));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertWillDestroyAllInLinkedEntityGroup(NativeArray<ArchetypeChunk> chunkArray,
            ArchetypeChunkBufferType<LinkedEntityGroup> linkedGroupType,
            ref Entity errorEntity,
            ref Entity errorReferencedEntity)
        {
            var chunks = (ArchetypeChunk*)chunkArray.GetUnsafeReadOnlyPtr();
            var chunksCount = chunkArray.Length;

            var tempChunkStateFlag = (uint)ChunkFlags.TempAssertWillDestroyAllInLinkedEntityGroup;
            for (int i = 0; i != chunksCount; i++)
            {
                var chunk = chunks[i].m_Chunk;
                Assert.IsTrue((chunk->Flags & tempChunkStateFlag) == 0);
                chunk->Flags |= tempChunkStateFlag;
            }

            errorEntity = default;
            errorReferencedEntity = default;

            for (int i = 0; i < chunkArray.Length; ++i)
            {
                if (!chunks[i].Has(linkedGroupType))
                    continue;

                var chunk = chunks[i];
                var buffers = chunk.GetBufferAccessor(linkedGroupType);

                for (int b = 0; b != buffers.Length; b++)
                {
                    var buffer = buffers[b];
                    int entityCount = buffer.Length;
                    var entities = (Entity*)buffer.GetUnsafeReadOnlyPtr();
                    for (int e = 0; e != entityCount; e++)
                    {
                        var referencedEntity = entities[e];
                        if (Exists(referencedEntity))
                        {
                            var referencedChunk = GetChunk(referencedEntity);

                            if ((referencedChunk->Flags & tempChunkStateFlag) == 0)
                            {
                                errorEntity = entities[0];
                                errorReferencedEntity = referencedEntity;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i != chunksCount; i++)
            {
                var chunk = chunks[i].m_Chunk;
                Assert.IsTrue((chunk->Flags & tempChunkStateFlag) != 0);
                chunk->Flags &= ~tempChunkStateFlag;
            }
        }

        public void ThrowDestroyEntityError(Entity errorEntity, Entity errorReferencedEntity)
        {
            ThrowDestroyEntityErrorFancy(errorEntity, errorReferencedEntity);
            throw new ArgumentException($"DestroyEntity(EntityQuery query) is destroying an entity which contains a LinkedEntityGroup with an entity not included in the query. If you want to destroy entities using a query all linked entities must be contained in the query.. For more detail, disable Burst compilation.");
        }

        [BurstDiscard]
        private void ThrowDestroyEntityErrorFancy(Entity errorEntity, Entity errorReferencedEntity)
        {
            throw new ArgumentException($"DestroyEntity(EntityQuery query) is destroying entity {errorEntity} which contains a LinkedEntityGroup and the entity {errorReferencedEntity} in that group is not included in the query. If you want to destroy entities using a query all linked entities must be contained in the query..");
        }


        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AssertArchetypeDoesNotRemoveSystemStateComponents(Archetype* src, Archetype* dst)
        {
            int o = 0;
            int n = 0;

            for (; n < src->TypesCount && o < dst->TypesCount;)
            {
                int srcType = src->Types[o].TypeIndex;
                int dstType = dst->Types[n].TypeIndex;
                if (srcType == dstType)
                {
                    o++;
                    n++;
                }
                else if (dstType > srcType)
                {
                    if (src->Types[o].IsSystemStateComponent)
                        throw new System.ArgumentException(
                            $"SystemState components may not be removed via SetArchetype: {src->Types[o]}");
                    o++;
                }
                else
                    n++;
            }

            for (; o < src->TypesCount; o++)
            {
                if (src->Types[o].IsSystemStateComponent)
                    throw new System.ArgumentException($"SystemState components may not be removed via SetArchetype: {src->Types[o]}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertCanAddChunkComponent(NativeArray<ArchetypeChunk> chunkArray, ComponentType componentType)
        {
            var chunks = (ArchetypeChunk*)chunkArray.GetUnsafeReadOnlyPtr();
            for (int i = 0; i < chunkArray.Length; ++i)
            {
                var chunk = chunks[i].m_Chunk;
                if (ChunkDataUtility.GetIndexInTypeArray(chunk->Archetype, componentType.TypeIndex) != -1)
                    throw new ArgumentException(
                        $"A chunk component with type:{componentType} has already been added to the chunk.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertCanInstantiateEntities(Entity srcEntity, Entity* outputEntities, int instanceCount)
        {
            if (HasComponent(srcEntity, m_LinkedGroupType))
            {
                var header = (BufferHeader*)GetComponentDataWithTypeRO(srcEntity, m_LinkedGroupType);
                var entityPtr = (Entity*)BufferHeader.GetElementPointer(header);
                var entityCount = header->Length;

                if (entityCount == 0 || entityPtr[0] != srcEntity)
                    throw new ArgumentException("LinkedEntityGroup[0] must always be the Entity itself.");

                for (int i = 0; i < entityCount; i++)
                {
                    if (!Exists(entityPtr[i]))
                        throw new ArgumentException(
                            $"The srcEntity's LinkedEntityGroup references an entity that is invalid. (Entity at index {i} on the LinkedEntityGroup.)");

                    var archetype = GetArchetype(entityPtr[i]);
                    if (archetype->InstantiateArchetype == null)
                        throw new ArgumentException(
                            $"The srcEntity's LinkedEntityGroup references an entity that has already been destroyed. (Entity at index {i} on the LinkedEntityGroup. Only system state components are left on the entity)");
                }
            }
            else
            {
                if (!Exists(srcEntity))
                    throw new ArgumentException("srcEntity is not a valid entity");

                var srcArchetype = GetArchetype(srcEntity);
                if (srcArchetype->InstantiateArchetype == null)
                    throw new ArgumentException(
                        "srcEntity is not instantiable because it has already been destroyed. (Only system state components are left on it)");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public void AssertCanInstantiateEntities(Entity* srcEntity, int entityCount, bool removePrefab)
        {
            for (int i = 0; i < entityCount; i++)
            {
                if (!Exists(srcEntity[i]))
                    throw new ArgumentException(
                        $"The srcEntity[{i}] references an entity that is invalid.");

                var archetype = GetArchetype(srcEntity[i]);
                if ((removePrefab ? archetype->InstantiateArchetype : archetype->CopyArchetype) == null)
                    throw new ArgumentException(
                        $"The srcEntity[{i}] references an entity that has already been destroyed. (Only system state components are left on the entity)");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AssertValidEntityQuery(EntityQuery query, EntityComponentStore* store)
        {
            var e = query._GetImpl()->_Access->EntityComponentStore;
            if (e != store)
            {
                AssertValidEntityQuery(e, store);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AssertValidEntityQuery(EntityComponentStore* queryStore, EntityComponentStore* store)
        {
            if (queryStore != store)
            {
                if (queryStore ==  null)
                    throw new System.ArgumentException("The EntityQuery has been disposed and can no longer be used.");
                else
                    throw new System.ArgumentException("EntityQuery is associated with a different world");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void AssertValidArchetype(EntityComponentStore* queryStore, EntityArchetype archetype)
        {
            #if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (archetype._DebugComponentStore != queryStore)
            {
                if (archetype.Archetype == null)
                    throw new System.ArgumentException("The EntityArchetype has not been allocated");
                else
                    throw new System.ArgumentException("The EntityArchetype was not created by this EntityManager");
            }

            #endif
        }

        // ----------------------------------------------------------------------------------------------------------
        // INTERNAL
        // ----------------------------------------------------------------------------------------------------------
    }
}
