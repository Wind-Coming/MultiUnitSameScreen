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
    public unsafe class CustomMesh
    {
        public int quadNum;
        public NativeArray<Vector3> m_vertext;
        public NativeArray<Vector2> m_uvs;
        public ushort[] m_triangles; 
        private bool hasSetTriangle;
        public Mesh mesh;

        public CustomMesh(int num)
        {
            quadNum = num;
            mesh = new Mesh();
            m_vertext = new NativeArray<Vector3>(num * 4, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            m_uvs = new NativeArray<Vector2>(num * 4, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            m_triangles = new ushort[num * 6];

            for(ushort i = 0; i < num; i++)
            {
                m_triangles[i * 6 + 0] = (ushort)(i * 4);
                m_triangles[i * 6 + 1] = (ushort)(i * 4 + 1);
                m_triangles[i * 6 + 2] = (ushort)(i * 4 + 2);

                m_triangles[i * 6 + 3] = (ushort)(i * 4);
                m_triangles[i * 6 + 4] = (ushort)(i * 4 + 2);
                m_triangles[i * 6 + 5] = (ushort)(i * 4 + 3);
            }

            hasSetTriangle = false;
        }

        public void UpdateMesh(int realNum)
        {
            mesh.SetVertices(m_vertext);
            mesh.SetUVs(0, m_uvs);

            if(!hasSetTriangle)
            {
                mesh.SetTriangles(m_triangles, 0, false);
                hasSetTriangle = true;
            }

            Vector3* ptr = (Vector3*)m_vertext.GetUnsafePtr();
            ptr += realNum * 4;
            UnsafeUtility.MemClear(ptr, sizeof(Vector3) * ( m_vertext.Length - realNum * 4 ));

            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000);//手动裁剪，不用交给摄像机
        }
    }
    
    public unsafe class CustomRenderVertexGroup
    {
        const int kMaxBatchCount = 64 * 1024;
        const int kMaxArchetypeProperties = 256;

        EntityManager m_EntityManager;
        ComponentSystemBase m_ComponentSystem;
        EntityQuery m_CullingJobDependencyGroup;
        BatchRendererGroup m_BatchRendererGroup;

        Camera camera;

        Dictionary<int, List<CustomMesh>> cmeshPool = new Dictionary<int, List<CustomMesh>>();
        List<CustomMesh> drawingMesh = new List<CustomMesh>();

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


        public CustomRenderVertexGroup(EntityManager entityManager, ComponentSystemBase componentSystem,
                                             EntityQuery cullingJobDependencyGroup)
        {
            camera = Camera.main;

            m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling);
            m_EntityManager = entityManager;
            m_ComponentSystem = componentSystem;
            m_CullingJobDependencyGroup = cullingJobDependencyGroup;

            m_RemoveBatchMarker = new ProfilerMarker("BatchRendererGroup.Remove");

#if UNITY_EDITOR
            m_CullingStats = (CullingStats*)UnsafeUtility.Malloc(JobsUtility.MaxJobThreadCount * sizeof(CullingStats),
                64, Allocator.Persistent);
#endif
        }


        public void Dispose()
        {
#if UNITY_EDITOR
            UnsafeUtility.Free(m_CullingStats, Allocator.Persistent);

            m_CullingStats = null;
#endif
            m_BatchRendererGroup.Dispose();
            ClearMesh();
        }

        public void Clear()
        {
            m_BatchRendererGroup.Dispose();
            m_BatchRendererGroup = new BatchRendererGroup(this.OnPerformCulling);
            m_PrevLODParams = new LODGroupExtensions.LODParams();
            m_PrevCameraPos = default(float3);
            m_PrevLodDistanceScale = 0.0f;
            m_ResetLod = true;

            ClearMesh();
        }

        public void ClearMesh()
        {
            foreach(var v in cmeshPool)
            {
                for(int i = 0; i < v.Value.Count; i++)
                {
                    v.Value[i].m_vertext.Dispose();
                    v.Value[i].m_uvs.Dispose();
                }
            }

            foreach(var v in drawingMesh)
            {
                v.m_vertext.Dispose();
                v.m_uvs.Dispose();
            }

            cmeshPool.Clear();
            drawingMesh.Clear();
        }

        public unsafe JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext)
        {
            return new JobHandle();
        }

        static unsafe void CopyTo(NativeSlice<UvCom> transforms, int count, Vector2* resultMatrices, int offset)
        {
            // @TODO: This is using unsafe code because the Unity DrawInstances API takes a Matrix4x4[] instead of NativeArray.
            Assert.AreEqual(sizeof(float2x4), sizeof(UvCom));
            {
                UvCom* sourceMatrices = (UvCom*) transforms.GetUnsafeReadOnlyPtr();
                UnsafeUtility.MemCpy(resultMatrices + offset * 4, sourceMatrices , UnsafeUtility.SizeOf<float2x4>() * count);
            }
        }



        static unsafe void CopyTo(NativeSlice<VertexCom> transforms, int count, Vector3* resultMatrices, int offset)
        {
            // @TODO: This is using unsafe code because the Unity DrawInstances API takes a Matrix4x4[] instead of NativeArray.
            Assert.AreEqual(sizeof(float3x4), sizeof(VertexCom));
            {
                VertexCom* sourceMatrices = (VertexCom*) transforms.GetUnsafeReadOnlyPtr();
                UnsafeUtility.MemCpy(resultMatrices + offset * 4, sourceMatrices , UnsafeUtility.SizeOf<float3x4>() * count);
            }
        }

        public CustomMesh GetMesh(int num)
        {
            if(num <= 0)
            {
                Debug.LogError("num = 0！！！！！");
            }
            int low = 128;
            while(low < num)
            {
                low = low << 1;
            }

            if(!cmeshPool.ContainsKey(low))
            {
                cmeshPool.Add(low, new List<CustomMesh>());
            }

            if (cmeshPool[low].Count > 0)
            {
                CustomMesh cm = cmeshPool[low][0];
                cmeshPool[low].RemoveAt(0);
                return cm;
            }
            else
            {
                CustomMesh cm = new CustomMesh(low);
                return cm;
            }
        }

        public void RestoreMesh(CustomMesh cm)
        {
            cmeshPool[cm.quadNum].Add(cm);
        }

        public void RemoveTag()
        {
            for(int i = 0; i < drawingMesh.Count; i++)
            {
                RestoreMesh(drawingMesh[i]);
            }

            drawingMesh.Clear();
        }

        public void BeginBatchGroup()
        {
        }

        public unsafe void AddBatch(FrozenRenderSceneTag tag, int rendererSharedComponentIndex, int batchInstanceCount,
            NativeArray<ArchetypeChunk> chunks, NativeArray<int> sortedChunkIndices, int startSortedIndex,
            int chunkCount, EditorRenderData data)
        {
            // Create the batch with extremely large placeholder bounds at first
            var bigBounds = new Bounds(new Vector3(0, 0, 0), new Vector3(1048576.0f, 1048576.0f, 1048576.0f));

            var rendererSharedComponent =
                m_EntityManager.GetSharedComponentData<RenderMesh>(rendererSharedComponentIndex);
            var material = rendererSharedComponent.material;

            if (material == null)
            {
                return;
            }


            var localVertexType = m_ComponentSystem.GetArchetypeChunkComponentType<VertexCom>(true);
            var localUvComType = m_ComponentSystem.GetArchetypeChunkComponentType<UvCom>(true);

            int runningOffset = 0;

            int totalNum = 0;
            for (int i = 0; i < chunkCount; ++i)
            {
                var chunk = chunks[sortedChunkIndices[startSortedIndex + i]];
                totalNum += chunk.Count;
            }

            CustomMesh cmesh = GetMesh(totalNum);
            drawingMesh.Add(cmesh);

            Vector3* vptr = (Vector3*)cmesh.m_vertext.GetUnsafePtr();
            Vector2* uptr = (Vector2*)cmesh.m_uvs.GetUnsafePtr();

            Profiler.BeginSample("Copy Data");
            for (int i = 0; i < chunkCount; ++i)
            {
                var chunk = chunks[sortedChunkIndices[startSortedIndex + i]];

                Assert.IsTrue(chunk.Count <= 128);

                var vert = chunk.GetNativeArray(localVertexType);
                var uvs = chunk.GetNativeArray(localUvComType);

                CopyTo(vert, chunk.Count, vptr, runningOffset);
                CopyTo(uvs, chunk.Count, uptr, runningOffset);

                runningOffset += chunk.Count;
            }
            Profiler.EndSample();

            Profiler.BeginSample("SetVertex");
            cmesh.UpdateMesh(runningOffset);
            Profiler.EndSample();

            Profiler.BeginSample("Draw");
            Graphics.DrawMesh(cmesh.mesh, Vector3.zero, Quaternion.identity, material, 0, camera);
            Profiler.EndSample();
        }

        public void EndBatchGroup(FrozenRenderSceneTag tag, NativeArray<ArchetypeChunk> chunks,
            NativeArray<int> sortedChunkIndices)
        {
        }
    }
}
