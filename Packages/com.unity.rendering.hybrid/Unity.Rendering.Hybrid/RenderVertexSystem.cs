using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Rendering
{
    /// <summary>
    /// Renders all Entities containing both RenderMesh & LocalToWorld components.
    /// </summary>
    [ExecuteAlways]
    //@TODO: Necessary due to empty component group. When Component group and archetype chunks are unified this should be removed
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(UpdatePresentationSystemGroup))]
    public class RenderVertexSystem : JobComponentSystem
    {
        EntityQuery m_DynamicGroup;

        EntityQuery m_CullingJobDependencyGroup;
        CustomRenderVertexGroup m_InstancedRenderMeshBatchGroup;

#if UNITY_EDITOR
        EditorRenderData m_DefaultEditorRenderData = new EditorRenderData
        {SceneCullingMask = UnityEditor.SceneManagement.EditorSceneManager.DefaultSceneCullingMask};
#else
        EditorRenderData m_DefaultEditorRenderData = new EditorRenderData { SceneCullingMask = ~0UL };
#endif

        protected override void OnCreate()
        {
            m_DynamicGroup = GetEntityQuery(
                ComponentType.ReadOnly<RenderMesh>(),
                ComponentType.ReadOnly<VertexCom>(),
                ComponentType.ReadOnly<UvCom>()

            );

            m_InstancedRenderMeshBatchGroup = new CustomRenderVertexGroup(EntityManager, this, m_CullingJobDependencyGroup);
        }

        protected override void OnDestroy()
        {
            m_InstancedRenderMeshBatchGroup.Dispose();
        }

        public void CacheMeshBatchRendererGroup(FrozenRenderSceneTag tag, NativeArray<ArchetypeChunk> chunks,
            int chunkCount)
        {
            var RenderMeshType = GetArchetypeChunkSharedComponentType<RenderMesh>();
            var meshInstanceFlippedTagType = GetArchetypeChunkComponentType<RenderMeshFlippedWindingTag>();
            var editorRenderDataType = GetArchetypeChunkSharedComponentType<EditorRenderData>();

            Profiler.BeginSample("Sort Shared Renderers");
            var chunkRenderer = new NativeArray<int>(chunkCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var sortedChunks = new NativeArraySharedInt(chunkRenderer, Allocator.TempJob);

            var gatherChunkRenderersJob = new GatherChunkRenderers
            {
                Chunks = chunks,
                RenderMeshType = RenderMeshType,
                ChunkRenderer = chunkRenderer
            };
            var gatherChunkRenderersJobHandle = gatherChunkRenderersJob.Schedule(chunkCount, 64);
            var sortedChunksJobHandle = sortedChunks.Schedule(gatherChunkRenderersJobHandle);
            sortedChunksJobHandle.Complete();
            Profiler.EndSample();

            var sharedRenderCount = sortedChunks.SharedValueCount;
            var sharedRendererCounts = sortedChunks.GetSharedValueIndexCountArray();
            var sortedChunkIndices = sortedChunks.GetSortedIndices();

            m_InstancedRenderMeshBatchGroup.BeginBatchGroup();
            Profiler.BeginSample("Add New Batches");
            {
                var sortedChunkIndex = 0;
                for (int i = 0; i < sharedRenderCount; i++)
                {
                    var startSortedChunkIndex = sortedChunkIndex;
                    var endSortedChunkIndex = startSortedChunkIndex + sharedRendererCounts[i];

                    while (sortedChunkIndex < endSortedChunkIndex)
                    {
                        var chunkIndex = sortedChunkIndices[sortedChunkIndex];
                        var chunk = chunks[chunkIndex];
                        var rendererSharedComponentIndex = chunk.GetSharedComponentIndex(RenderMeshType);

                        var editorRenderDataIndex = chunk.GetSharedComponentIndex(editorRenderDataType);
                        var editorRenderData = m_DefaultEditorRenderData;
                        if (editorRenderDataIndex != -1)
                            editorRenderData =
                                EntityManager.GetSharedComponentData<EditorRenderData>(editorRenderDataIndex);

                        var remainingEntitySlots = 1023;
                        int instanceCount = chunk.Count;
                        int startSortedIndex = sortedChunkIndex;
                        int batchChunkCount = 1;

                        remainingEntitySlots -= chunk.Count;
                        sortedChunkIndex++;

                        while (remainingEntitySlots > 0)
                        {
                            if (sortedChunkIndex >= endSortedChunkIndex)
                                break;

                            var nextChunkIndex = sortedChunkIndices[sortedChunkIndex];
                            var nextChunk = chunks[nextChunkIndex];
                            if (nextChunk.Count > remainingEntitySlots)
                                break;
#if UNITY_EDITOR
                            if (editorRenderDataIndex != nextChunk.GetSharedComponentIndex(editorRenderDataType))
                                break;
#endif

                            remainingEntitySlots -= nextChunk.Count;
                            instanceCount += nextChunk.Count;
                            batchChunkCount++;
                            sortedChunkIndex++;
                        }

                        m_InstancedRenderMeshBatchGroup.AddBatch(tag, rendererSharedComponentIndex, instanceCount,
                            chunks, sortedChunkIndices, startSortedIndex, batchChunkCount,
                            editorRenderData);
                    }
                }
            }
            Profiler.EndSample();
            m_InstancedRenderMeshBatchGroup.EndBatchGroup(tag, chunks, sortedChunkIndices);

            chunkRenderer.Dispose();
            sortedChunks.Dispose();
        }

        void UpdateDynamicRenderBatches()
        {
            m_InstancedRenderMeshBatchGroup.RemoveTag();
            
            Profiler.BeginSample("CreateArchetypeChunkArray");
            var chunks = m_DynamicGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            Profiler.EndSample();

            
            if (chunks.Length > 0)
            {
                CacheMeshBatchRendererGroup(new FrozenRenderSceneTag(), chunks, chunks.Length);
            }

            chunks.Dispose();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps.Complete(); // #todo

            Profiler.BeginSample("UpdateDynamicRenderBatches");
            UpdateDynamicRenderBatches();
            Profiler.EndSample();

            return new JobHandle();
        }

#if UNITY_EDITOR
        public CullingStats ComputeCullingStats()
        {
            return m_InstancedRenderMeshBatchGroup.ComputeCullingStats();
        }
#endif
    }
}
