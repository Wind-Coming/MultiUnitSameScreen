using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;
 
public class GSprite : MonoBehaviour
{
 
    public struct ECS
    {
 
        public Vector3 position; //显示坐标
        public Vector3 scale;    //显示缩放
        public Vector2 pivot;    //锚点区域
    }
    private BatchRendererGroup m_BatchRendererGroup;
    private Mesh m_Mesh;
    private Material m_Material;
    private int m_BatchIndex = -1;
    private JobHandle m_Hadle;
  
    private NativeList<ECS> m_SpriteData;
    private List<Vector4> m_SpriteDataOffset = new List<Vector4>();
 
    public Sprite sprite1;
    public Sprite sprite2;
    public Sprite sprite3;
    public Shader shader;
 
    void Start()
    {
        m_SpriteData = new NativeList<ECS>(100, Allocator.Persistent);
        m_Mesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        m_Material = new Material(shader) { enableInstancing = true };
        m_Material.mainTexture = sprite1.texture;
 
        //添加图片
        AddSprite(sprite1, Vector3.zero, Vector3.one * 0.3f, 1); //显示图片一部分(横向0.5f)
        AddSprite(sprite2, Vector3.zero + new Vector3(1,0,0), Vector3.one * 0.3f, 1f); //显示完整图片(整体缩小0.5)
        AddSprite(sprite3, Vector3.zero + new Vector3(1, 1, 0), Vector3.one * 0.3f, 1f); //显示完整图片
 
        Refresh();
    }
 
 
    void AddSprite(Sprite sprite, Vector2 localPosition, Vector2 localScale ,float slider)
    {
        float perunit = sprite.pixelsPerUnit;
        Vector3 scale = new Vector3((sprite.rect.width / perunit) * localScale.x, (sprite.rect.height / perunit) * localScale.y, 1f);
        Vector4 rect = GetSpreiteRect(sprite);
        scale.x *= slider;
        rect.x *= slider;
 
        ECS obj = new ECS();
        obj.position = localPosition;
        obj.pivot = new Vector2(sprite.pivot.x / perunit * localScale.x, sprite.pivot.y / perunit * localScale.y);
        obj.scale = scale;
        m_SpriteData.Add(obj);
        m_SpriteDataOffset.Add(rect);
    }
 
 
    private void Refresh()
    {
 
        //1.参与渲染的sprite数量发生变化（增加、删除）需要重新m_BatchRendererGroup.AddBatch
        RefreshElement();
        //2.参与渲染的sprite数量没有发生变化，只是坐标发生变化，那么在Job里重新计算坐标
        RefreshPosition();
        //3.参与渲染的sprite数量没有发生变化、坐标也没有发生变化，例如：血条 显示图片一部分，只需要重新刷新MaterialPropertyBlock即可。
        RefreshBlock();
    }
    private void RefreshElement()
    {
        if (m_BatchRendererGroup == null)
        {
            m_BatchRendererGroup = new BatchRendererGroup(OnPerformCulling);
        }
        else
        {
            m_BatchRendererGroup.RemoveBatch(m_BatchIndex);
        }
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetVectorArray("_Offset", m_SpriteDataOffset);
        m_BatchIndex = m_BatchRendererGroup.AddBatch(
          m_Mesh,
          0,
          m_Material,
          0,
          ShadowCastingMode.Off,
          false,
          false,
          default(Bounds),
          m_SpriteData.Length,
          block,
          null);
    }
 
    void RefreshPosition()
    {
        m_Hadle.Complete();
 
        m_Hadle = new UpdateMatrixJob
        {
            Matrices = m_BatchRendererGroup.GetBatchMatrices(m_BatchIndex),
            objects = m_SpriteData,
        }.Schedule(m_SpriteData.Length, 32);
    }
 
 
    void RefreshBlock()
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        block.SetVectorArray("_Offset", m_SpriteDataOffset);
        m_BatchRendererGroup.SetInstancingData(m_BatchIndex, m_SpriteData.Length, block);
    }
 
    public JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext)
    {
        //sprite不需要处理镜头裁切，所以这里直接完成job
        m_Hadle.Complete();
        return m_Hadle;
    }
 
    [BurstCompile]
    private struct UpdateMatrixJob : IJobParallelFor
    {
        public NativeArray<Matrix4x4> Matrices;
        [ReadOnly] public NativeList<ECS> objects;
        public void Execute(int index)
        {
            //通过锚点计算sprite实际的位置
            ECS go = objects[index];
            var position = go.position;
            float x = position.x + (go.scale.x * 0.5f) - go.pivot.x;
            float y = position.y + (go.scale.y * 0.5f) - go.pivot.y;
 
            Matrices[index] = Matrix4x4.TRS(float3(x, y, position.z),
                Unity.Mathematics.quaternion.identity,
                objects[index].scale);
        }
    }
 
 
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
 
    private void OnDestroy()
    {
        if (m_BatchRendererGroup != null)
        {
            m_BatchRendererGroup.Dispose();
            m_BatchRendererGroup = null;
        }
        m_SpriteData.Dispose();
    }
}