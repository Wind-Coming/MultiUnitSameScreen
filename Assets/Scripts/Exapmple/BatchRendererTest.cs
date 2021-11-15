using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

public class BatchRendererTest : MonoBehaviour
{
    public Mesh mesh;
    public Material material;
    public Sprite[] sprites;

    private MaterialPropertyBlock block;
    private Matrix4x4[] matrices;

    private BatchRendererGroup m_BatchRendererGroup;
    private int m_BatchIndex ;
    private JobHandle m_Hadle;

    private Vector4 GetSpreiteRect(Sprite sprite)
    {
        var uvs = sprite.uv;
        Vector4 rect = new Vector4();
        rect[0] = uvs[1].x - uvs[0].x;
        rect[1] = uvs[0].y - uvs[2].y;
        rect[2] = uvs[2].x;
        rect[3] = uvs[2].y;
        return rect;
    }

    void OnEnable()
    {
        if(sprites == null || sprites.Length == 0)
        {
            return;
        }

        //matrices = new Matrix4x4[sprites.Length];
        
        int num = 1023;
        block = new MaterialPropertyBlock();

        List<Vector4> rects = new List<Vector4>();
        for(int i = 0; i < num; i++)
        {
            //matrices[i] = Matrix4x4.TRS(Vector3.right * i, Quaternion.Euler(45, 0, 0), Vector3.one);
            rects.Add(GetSpreiteRect(sprites[Random.Range(0, sprites.Length)]));
        }
        block.SetVectorArray("_Offset", rects);

        m_BatchRendererGroup = new BatchRendererGroup(OnPerformCulling);
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        m_BatchIndex = m_BatchRendererGroup.AddBatch(mesh, 0, material, 0, ShadowCastingMode.Off, false, false, bounds, num, null, null);

        m_BatchRendererGroup.SetInstancingData(m_BatchIndex, num, block);

        NativeArray<Matrix4x4> Matrices = m_BatchRendererGroup.GetBatchMatrices(m_BatchIndex);
        for(int i = 0; i < Matrices.Length; i++)
        {
            Matrices[i] = Matrix4x4.TRS(new Vector3(i / 100, 0, i % 100), Quaternion.Euler(45, 0, 0), Vector3.one * 2);
        }
    }

    public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext)
    {
        //sprite不需要处理镜头裁切，所以这里直接完成job
        m_Hadle.Complete();
        return m_Hadle;
    }


    void OnDisable()
    {
        m_BatchRendererGroup.Dispose();
    }
    
    // Update is called once per frame
    void Update()
    {
        if(mesh == null || material == null || sprites == null)
        {
            return;
        }

        m_BatchRendererGroup.RemoveBatch(m_BatchIndex);
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 10000);
        m_BatchIndex = m_BatchRendererGroup.AddBatch(mesh, 0, material, 0, ShadowCastingMode.Off, false, false, bounds, 1000, null, null);

        m_BatchRendererGroup.SetInstancingData(m_BatchIndex, 1000, block);

        NativeArray<Matrix4x4> Matrices = m_BatchRendererGroup.GetBatchMatrices(m_BatchIndex);
        for(int i = 0; i < Matrices.Length; i++)
        {
            Matrices[i] = Matrix4x4.TRS(new Vector3(i / 100, 0, i % 100), Quaternion.Euler(45, 0, 0), Vector3.one * 2);
        }

        //Graphics.DrawMesh(mesh, transform.position, transform.rotation, material, 0, Camera.main, 0, block);
        //Graphics.DrawMeshInstanced(mesh, 0, material, matrices, matrices.Length, block);
    }
}
