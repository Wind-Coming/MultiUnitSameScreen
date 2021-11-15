using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Unity.Rendering
{
    public unsafe class CustomRenderGroup
    {
        const int kMaxBatchCount = 64 * 1024;
        const int kMaxArchetypeProperties = 256;

        EntityManager m_EntityManager;
        ComponentSystemBase m_ComponentSystem;
        JobHandle m_CullingJobDependency;
        JobHandle m_LODDependency;
        EntityQuery m_CullingJobDependencyGroup;
        BatchRendererGroup m_BatchRendererGroup;

        // Our idea of batches. This is indexed by local batch indices.
        NativeMultiHashMap<LocalGroupKey, BatchChunkData> m_BatchToChunkMap;

        // Maps from internal to external batch ids
        NativeArray<int> m_InternalToExternalIds;
        NativeArray<int> m_ExternalToInternalIds;

        // These arrays are parallel and allocated up to kMatchBatchCount. They are indexed by local batch indices.
        NativeArray<FrozenRenderSceneTag> m_Tags;
        NativeArray<byte> m_ForceLowLOD;

        // Tracks the highest index (+1) in use across InstanceCounts/Tags/LodSkip.
        int m_InternalBatchRange;
        int m_ExternalBatchCount;

        // Per-batch material properties
        List<MaterialPropertyBlock> m_MaterialPropertyBlocks;

        // This is a hack to allocate local batch indices in response to external batches coming and going
        int m_LocalIdCapacity;
        NativeArray<int> m_LocalIdPool;

        public int LastUpdatedOrderVersion = -1;

        Matrix4x4[] m_MatricesArray = new Matrix4x4[1023];
        Vector4[] m_MaterialArgArray = new Vector4[1023];
        MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
        Camera camera;

#if UNITY_EDITOR
        float m_CamMoveDistance;
#endif

#if UNITY_EDITOR
        private CullingStats* m_CullingStats = null;

        public CullingStats ComputeCullingStats()
        {
            var result = default(CullingStats);
            for (int i = 0; i < JobsUtility.MaxJobThreadCount; ++i)
            {
                ref var s = ref m_CullingStats[i];

                for (int f = 0; f < (int)CullingStats.kCount; ++f)
                {
                    result.Stats[f] += s.Stats[f];
                }
            }

            result.CameraMoveDistance = m_CamMoveDistance;
            return result;
        }

#endif

        private bool m_ResetLod;

        LODGroupExtensions.LODParams m_PrevLODParams;
        float3 m_PrevCameraPos;
        float m_PrevLodDistanceScale;

        ProfilerMarker m_RemoveBatchMarker;

        struct MaterialPropertyType
        {
            public int nameId;
            public int nameIdArray;
            public int typeIndex;
            public MaterialPropertyFormat format;
            public int numFormatComponents;
        };

        struct MaterialPropertyPointer
        {
            public float* ptr;
            public ArchetypeChunkComponentTypeDynamic type;
            public int numFormatComponents;
        };

        List<MaterialPropertyType> m_MaterialPropertyTypes;
        Dictionary<int, string> m_MaterialPropertyOverriddenBy;
        MaterialPropertyPointer[] m_MaterialPropertyPointers;

        bool supportGpuInstance = true;

        public CustomRenderGroup(EntityManager entityManager, ComponentSystemBase componentSystem,
                                             EntityQuery cullingJobDependencyGroup)
        {
            supportGpuInstance = SystemInfo.supportsInstancing;
            camera = Camera.main;

            m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling);
            m_EntityManager = entityManager;
            m_ComponentSystem = componentSystem;
            m_CullingJobDependencyGroup = cullingJobDependencyGroup;
            m_BatchToChunkMap = new NativeMultiHashMap<LocalGroupKey, BatchChunkData>(32, Allocator.Persistent);
            m_LocalIdPool = new NativeArray<int>(kMaxBatchCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_Tags = new NativeArray<FrozenRenderSceneTag>(kMaxBatchCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_ForceLowLOD = new NativeArray<byte>(kMaxBatchCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_InternalToExternalIds = new NativeArray<int>(kMaxBatchCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_ExternalToInternalIds = new NativeArray<int>(kMaxBatchCount, Allocator.Persistent,
                NativeArrayOptions.UninitializedMemory);
            m_ResetLod = true;
            m_InternalBatchRange = 0;
            m_ExternalBatchCount = 0;

            m_RemoveBatchMarker = new ProfilerMarker("BatchRendererGroup.Remove");

#if UNITY_EDITOR
            m_CullingStats = (CullingStats*)UnsafeUtility.Malloc(JobsUtility.MaxJobThreadCount * sizeof(CullingStats),
                64, Allocator.Persistent);
#endif
            m_MaterialPropertyBlocks = new List<MaterialPropertyBlock>();

            // Collect all components with [MaterialProperty] attribute
            m_MaterialPropertyTypes = new List<MaterialPropertyType>();
            m_MaterialPropertyOverriddenBy = new Dictionary<int, string>();
            foreach (var typeInfo in TypeManager.AllTypes)
            {
                var type = typeInfo.Type;
                if (typeof(IComponentData).IsAssignableFrom(type))
                {
                    var attributes = type.GetCustomAttributes(typeof(MaterialPropertyAttribute), false);
                    if (attributes.Length > 0)
                    {
                        var format = ((MaterialPropertyAttribute)attributes[0]).Format;
                        int numFormatComponents = 1;
                        switch (format)
                        {
                            case MaterialPropertyFormat.Float:
                                numFormatComponents = 1;
                                break;
                            case MaterialPropertyFormat.Float2:
                                numFormatComponents = 2;
                                break;
                            case MaterialPropertyFormat.Float3:
                                numFormatComponents = 3;
                                break;
                            case MaterialPropertyFormat.Float4:
                                numFormatComponents = 4;
                                break;
                            case MaterialPropertyFormat.Float2x4:
                                numFormatComponents = 8;
                                break;
                            case MaterialPropertyFormat.Float4x4:
                                numFormatComponents = 16;
                                break;
                        }

                        string propertyName = ((MaterialPropertyAttribute)attributes[0]).Name;
                        int nameID = Shader.PropertyToID(propertyName);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        if (m_MaterialPropertyOverriddenBy.ContainsKey(nameID))
                        {
                            string overridingComponent = m_MaterialPropertyOverriddenBy[nameID];
                            throw new InvalidOperationException(
                                $"Component \"{type.Name}\" cannot override material property \"{propertyName}\" because it has already been overridden by component \"{overridingComponent}\"");
                        }
                        else
                        {
                            m_MaterialPropertyOverriddenBy[nameID] = type.Name;
                        }

                        if (UnsafeUtility.SizeOf(type) != numFormatComponents * sizeof(float))
                        {
                            throw new InvalidOperationException(
                                $"Material property component {type} (size = {UnsafeUtility.SizeOf(type)}) cannot be reinterpreted as {numFormatComponents} floats (size = {numFormatComponents * sizeof(float)}). Sizes must match.");
                        }
#endif

                        m_MaterialPropertyTypes.Add(new MaterialPropertyType
                        {
                            typeIndex = TypeManager.GetTypeIndex(type), nameId = nameID,
                            nameIdArray =
                                Shader.PropertyToID(((MaterialPropertyAttribute)attributes[0]).Name + "_Array"),
                            format = format, numFormatComponents = numFormatComponents
                        });
                    }
                }
            }

            m_MaterialPropertyPointers = new MaterialPropertyPointer[m_MaterialPropertyTypes.Count];

            ResetLocalIdPool();
        }

        private void ResetLocalIdPool()
        {
            m_LocalIdCapacity = kMaxBatchCount;
            for (int i = 0; i < kMaxBatchCount; ++i)
            {
                m_LocalIdPool[i] = kMaxBatchCount - i - 1;
            }
        }

        public void Dispose()
        {
#if UNITY_EDITOR
            UnsafeUtility.Free(m_CullingStats, Allocator.Persistent);

            m_CullingStats = null;
#endif
            m_LocalIdPool.Dispose();
            m_ExternalToInternalIds.Dispose();
            m_InternalToExternalIds.Dispose();
            m_BatchRendererGroup.Dispose();
            m_BatchToChunkMap.Dispose();
            m_Tags.Dispose();
            m_ForceLowLOD.Dispose();
            m_ResetLod = true;
            m_InternalBatchRange = 0;
            m_ExternalBatchCount = 0;
            m_MaterialPropertyBlocks.Clear();
        }

        public void Clear()
        {
            m_BatchRendererGroup.Dispose();
            m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling);
            m_PrevLODParams = new LODGroupExtensions.LODParams();
            m_PrevCameraPos = default(float3);
            m_PrevLodDistanceScale = 0.0f;
            m_ResetLod = true;
            m_InternalBatchRange = 0;
            m_ExternalBatchCount = 0;

            m_BatchToChunkMap.Clear();
            m_MaterialPropertyBlocks.Clear();

            ResetLocalIdPool();
        }

        public int AllocLocalId()
        {
            Assert.IsTrue(m_LocalIdCapacity > 0);
            int result = m_LocalIdPool[m_LocalIdCapacity - 1];
            --m_LocalIdCapacity;
            return result;
        }

        public void FreeLocalId(int id)
        {
            Assert.IsTrue(m_LocalIdCapacity < kMaxBatchCount);
            int result = m_LocalIdPool[m_LocalIdCapacity] = id;
            ++m_LocalIdCapacity;
        }

        public void ResetLod()
        {
            m_PrevLODParams = new LODGroupExtensions.LODParams();
            m_ResetLod = true;
        }

        public unsafe JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext)
        {
#if false
            // Reset all visible counts to 0 - to not crash Unity when there is bugs in this code during dev.
            for (int i = 0; i < cullingContext.batchVisibility.Length; ++i)
            {
                var v = cullingContext.batchVisibility[i];
                v.visibleCount = 0;
                cullingContext.batchVisibility[i] = v;
            }
#endif

            if (LastUpdatedOrderVersion != m_EntityManager.GetComponentOrderVersion<RenderMesh>())
            {
                // Debug.LogError("The chunk layout of RenderMesh components has changed between updating and culling. This is not allowed, rendering is disabled.");
                return default(JobHandle);
            }

            var batchCount = cullingContext.batchVisibility.Length;
            if (batchCount == 0)
                return new JobHandle();
            ;

            var lodParams = LODGroupExtensions.CalculateLODParams(cullingContext.lodParameters);

            Profiler.BeginSample("OnPerformCulling");

            int cullingPlaneCount = cullingContext.cullingPlanes.Length;
            int packetCount = (cullingPlaneCount + 3) >> 2;
            var planes = FrustumPlanes.BuildSOAPlanePackets(cullingContext.cullingPlanes, Allocator.TempJob);

            bool singleThreaded = false;

            JobHandle cullingDependency;
            var resetLod = m_ResetLod || (!lodParams.Equals(m_PrevLODParams));
            if (resetLod)
            {
                // Depend on all component ata we access + previous jobs since we are writing to a single
                // m_ChunkInstanceLodEnableds array.
                var lodJobDependency = JobHandle.CombineDependencies(m_CullingJobDependency,
                    m_CullingJobDependencyGroup.GetDependency());

                float cameraMoveDistance = math.length(m_PrevCameraPos - lodParams.cameraPos);
                var lodDistanceScaleChanged = lodParams.distanceScale != m_PrevLodDistanceScale;

#if UNITY_EDITOR
                // Record this separately in the editor for stats display
                m_CamMoveDistance = cameraMoveDistance;
#endif

                var selectLodEnabledJob = new HybridV1SelectLodEnabled
                {
                    ForceLowLOD = m_ForceLowLOD,
                    LODParams = lodParams,
                    RootLodRequirements = m_ComponentSystem.GetArchetypeChunkComponentType<RootLodRequirement>(true),
                    InstanceLodRequirements = m_ComponentSystem.GetArchetypeChunkComponentType<LodRequirement>(true),
                    CameraMoveDistanceFixed16 =
                        Fixed16CamDistance.FromFloatCeil(cameraMoveDistance * lodParams.distanceScale),
                    DistanceScale = lodParams.distanceScale,
                    DistanceScaleChanged = lodDistanceScaleChanged,
#if UNITY_EDITOR
                    Stats = m_CullingStats,
#endif
                };

                cullingDependency = m_LODDependency = selectLodEnabledJob.Schedule(m_BatchToChunkMap,
                    singleThreaded ? 150000 : m_BatchToChunkMap.Capacity / 64, lodJobDependency);

                m_PrevLODParams = lodParams;
                m_PrevLodDistanceScale = lodParams.distanceScale;
                m_PrevCameraPos = lodParams.cameraPos;
                m_ResetLod = false;
#if UNITY_EDITOR
                UnsafeUtility.MemClear(m_CullingStats, sizeof(CullingStats) * JobsUtility.MaxJobThreadCount);
#endif
            }
            else
            {
                // Depend on all component ata we access + previous m_LODDependency job
                cullingDependency =
                    JobHandle.CombineDependencies(m_LODDependency, m_CullingJobDependencyGroup.GetDependency());
            }

            var batchCullingStates = new NativeArray<BatchCullingState>(m_InternalBatchRange, Allocator.TempJob,
                NativeArrayOptions.ClearMemory);

            var simpleCullingJob = new HybridV1SimpleCullingJob
            {
                Planes = planes,
                BatchCullingStates = batchCullingStates,
                BoundsComponent = m_ComponentSystem.GetArchetypeChunkComponentType<WorldRenderBounds>(true),
                IndexList = cullingContext.visibleIndices,
                Batches = cullingContext.batchVisibility,
                InternalToExternalRemappingTable = m_InternalToExternalIds,
#if UNITY_EDITOR
                Stats = m_CullingStats,
#endif
            };

            var simpleCullingJobHandle =
                simpleCullingJob.Schedule(m_BatchToChunkMap, singleThreaded ? 150000 : 1024, cullingDependency);

            DidScheduleCullingJob(simpleCullingJobHandle);

            Profiler.EndSample();
            return simpleCullingJobHandle;
        }


        static unsafe void CopyTo(NativeSlice<LocalToWorld> transforms, int count, Matrix4x4[] outMatrices, int offset)
        {
            // @TODO: This is using unsafe code because the Unity DrawInstances API takes a Matrix4x4[] instead of NativeArray.
            Assert.AreEqual(sizeof(Matrix4x4), sizeof(LocalToWorld));
            fixed (Matrix4x4* resultMatrices = outMatrices)
            {
                LocalToWorld* sourceMatrices = (LocalToWorld*) transforms.GetUnsafeReadOnlyPtr();
                UnsafeUtility.MemCpy(resultMatrices + offset, sourceMatrices , UnsafeUtility.SizeOf<Matrix4x4>() * count);
            }
        }

        static unsafe void CopyTo(NativeSlice<MaterialPropertyComponent> transforms, int count, Vector4[] outMatrices, int offset)
        {
            // @TODO: This is using unsafe code because the Unity DrawInstances API takes a Matrix4x4[] instead of NativeArray.
            Assert.AreEqual(sizeof(Vector4), sizeof(MaterialPropertyComponent));
            fixed (Vector4* resultMatrices = outMatrices) {
                MaterialPropertyComponent* sourceMatrices = (MaterialPropertyComponent*)transforms.GetUnsafeReadOnlyPtr();
                UnsafeUtility.MemCpy(resultMatrices + offset, sourceMatrices, UnsafeUtility.SizeOf<Vector4>() * count);
            }
        }

        public void BeginBatchGroup()
        {
        }

        public unsafe void AddBatch(FrozenRenderSceneTag tag, int rendererSharedComponentIndex, int batchInstanceCount,
            NativeArray<ArchetypeChunk> chunks, NativeArray<int> sortedChunkIndices, int startSortedIndex,
            int chunkCount, bool flippedWinding, EditorRenderData data)
        {
            // Create the batch with extremely large placeholder bounds at first
            var bigBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));

            var rendererSharedComponent =
                m_EntityManager.GetSharedComponentData<RenderMesh>(rendererSharedComponentIndex);
            var mesh = rendererSharedComponent.mesh;
            var material = rendererSharedComponent.material;
            var castShadows = rendererSharedComponent.castShadows;
            var receiveShadows = rendererSharedComponent.receiveShadows;
            var subMeshIndex = rendererSharedComponent.subMesh;
            var layer = rendererSharedComponent.layer;

            if (mesh == null || material == null)
            {
                return;
            }

            Profiler.BeginSample("AddBatch");

            var localToWorldType = m_ComponentSystem.GetArchetypeChunkComponentType<LocalToWorld>(true);
            var matPropType = m_ComponentSystem.GetArchetypeChunkComponentType<MaterialPropertyComponent>(true);

            int runningOffset = 0;

            Profiler.BeginSample("Copy Data");

            bool useInstance = supportGpuInstance && material.enableInstancing;

            for (int i = 0; i < chunkCount; ++i)
            {
                var chunk = chunks[sortedChunkIndices[startSortedIndex + i]];

                Assert.IsTrue(chunk.Count <= 128);

                var localToWorld = chunk.GetNativeArray(localToWorldType);
                var matProp = chunk.GetNativeArray(matPropType);

                if(useInstance){
                    CopyTo(localToWorld, chunk.Count, m_MatricesArray, runningOffset);
                    CopyTo(matProp, chunk.Count, m_MaterialArgArray, runningOffset);
                }
                else
                {
                    for(int j = 0; j < localToWorld.Length; j++)
                    {
                        Graphics.DrawMesh(mesh, localToWorld[j].Value, material, 0, camera);
                    }
                }

                runningOffset += chunk.Count;
            }
            Profiler.EndSample();

            Profiler.BeginSample("Draw Instance");

            if (useInstance)
            {
                propertyBlock.SetVectorArray("_Offset", m_MaterialArgArray);
                Graphics.DrawMeshInstanced(mesh, 0, material, m_MatricesArray, runningOffset, propertyBlock, ShadowCastingMode.Off, false, 0, camera);
            }
            Profiler.EndSample();

            Profiler.EndSample();
            SanityCheck();
        }

        private void SanityCheck()
        {
#if false
            //Debug.Log($"SanityCheck ir {m_InternalBatchRange} ec {m_ExternalBatchCount}");
            Assert.IsTrue(m_InternalBatchRange >= m_ExternalBatchCount);

            var populated = 0;

            var lookup = new Dictionary<int, bool>();

            for (int i = 0; i < m_InternalBatchRange; ++i)
            {
                var internalId = i;
                var externalId = m_InternalToExternalIds[i];
                if (externalId == -1)
                    continue;
                if (externalId >= m_ExternalBatchCount)
                {
                    Debug.Log($"Invalid external id {externalId} for internal id {i} (max {m_ExternalBatchCount})");
                }
                else
                {
                    if (lookup.ContainsKey(externalId))
                    {
                        Debug.Log($"Duplicate mapping e={externalId} at internal id {i}");
                    }
                    else
                    {
                        lookup.Add(externalId, true);
                    }
                }
            }

            if (lookup.Count != m_ExternalBatchCount)
            {
                Debug.Log($"Unreachable external batches: have {lookup.Count} but need {m_ExternalBatchCount}");
            }

            lookup.Clear();

            for (int i = 0; i < m_ExternalBatchCount; ++i)
            {
                var externalId = i;
                var internalId = m_ExternalToInternalIds[i];
                if (internalId < 0 || internalId >= m_InternalBatchRange)
                {
                    Debug.Log($"Invalid internal id {internalId} for external id {externalId}");
                }
                else
                {
                    if (lookup.ContainsKey(internalId))
                    {
                        Debug.Log($"Duplicate mapping e={externalId} to internal id {internalId}");
                    }
                    else
                    {
                        lookup.Add(internalId, true);
                    }
                }

                var ext2 = m_InternalToExternalIds[internalId];
                if (ext2 != externalId)
                {
                    Debug.Log($"Invalid round trip for internal id {internalId} for external id {externalId}; got {ext2}");
                }
            }

            if (lookup.Count != m_ExternalBatchCount)
            {
                Debug.Log($"Bad count of internal batches: have {lookup.Count} but need {m_ExternalBatchCount}");
            }
#endif
        }

        public void EndBatchGroup(FrozenRenderSceneTag tag, NativeArray<ArchetypeChunk> chunks,
            NativeArray<int> sortedChunkIndices)
        {
            // Disable force low lod  based on loading a streaming zone
            if (tag.SectionIndex > 0 && tag.HasStreamedLOD != 0)
            {
                for (int i = 0; i < m_InternalBatchRange; i++)
                {
                    if (m_Tags[i].SceneGUID.Equals(tag.SceneGUID))
                    {
                        m_ForceLowLOD[i] = 0;
                    }
                }
            }
        }

        public void RemoveTag(FrozenRenderSceneTag tag)
        {
            // Enable force low lod based on the high lod being streamed out
            if (tag.SectionIndex > 0 && tag.HasStreamedLOD != 0)
            {
                for (int i = 0; i < m_InternalBatchRange; i++)
                {
                    if (m_Tags[i].SceneGUID.Equals(tag.SceneGUID))
                    {
                        m_ForceLowLOD[i] = 1;
                    }
                }
            }

            Profiler.BeginSample("RemoveTag");
            // Remove any tag that need to go
            for (int i = m_InternalBatchRange - 1; i >= 0; i--)
            {
                var shouldRemove = m_Tags[i].Equals(tag);
                if (!shouldRemove)
                    continue;

                var externalBatchIndex = m_InternalToExternalIds[i];
                if (externalBatchIndex == -1)
                    continue;

                //Debug.Log($"Removing internal index {i} for external index {externalBatchIndex}; pre batch count = {m_ExternalBatchCount}");

                m_RemoveBatchMarker.Begin();
                m_BatchRendererGroup.RemoveBatch(externalBatchIndex);
                m_RemoveBatchMarker.End();

                // I->E: [ x: 0, y: 1, z: 2 ]  -> [ x: 0, y: ?, z: 2 ]
                // E->I: [ 0: x, 1: y, 2: z ]  -> [ 0: x, 1: z ]
                // B:    [ A B C ]             -> [ A C ]


                // Update remapping for external block. The render group will swap with the end, so replicate that behavior.
                var swappedInternalId = m_ExternalToInternalIds[m_ExternalBatchCount - 1];

                m_ExternalToInternalIds[externalBatchIndex] = swappedInternalId;
                m_InternalToExternalIds[swappedInternalId] = externalBatchIndex;

                // Return local id to pool
                FreeLocalId(i);

                // Invalidate id remapping table for this internal id
                m_InternalToExternalIds[i] = -1;

                m_Tags[i] = default(FrozenRenderSceneTag);

                var localKey = new LocalGroupKey {Value = i};
                m_BatchToChunkMap.Remove(localKey);

                m_ExternalBatchCount--;
            }

            Profiler.EndSample();

            SanityCheck();
        }

        public void CompleteJobs()
        {
            m_CullingJobDependency.Complete();
            m_CullingJobDependencyGroup.CompleteDependency();
        }

        void DidScheduleCullingJob(JobHandle job)
        {
            m_CullingJobDependency = JobHandle.CombineDependencies(job, m_CullingJobDependency);
            m_CullingJobDependencyGroup.AddDependency(job);
        }
    }
}
