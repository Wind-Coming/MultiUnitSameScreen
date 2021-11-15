using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public class Remove_Vertex : SystemBase
{
    EntityCommandBufferSystem m_Barrier;

    protected override void OnCreate()
    {
        m_Barrier = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    // OnUpdate runs on the main thread.
    protected override void OnUpdate()
    {
        var commandBuffer = m_Barrier.CreateCommandBuffer().ToConcurrent();
        int lod = LodMgr.Instance.m_iCurrentLod;
        NativeList<int2> allCells = MapMgr.Instance.allCells;

        Entities.ForEach((Entity entity, int nativeThreadIndex, in TileInfo tile ) =>
        {
            
            if(!allCells.Contains(new int2(tile.Value.x, tile.Value.y)) || tile.lod != lod)
            {
                commandBuffer.DestroyEntity(nativeThreadIndex, entity);
            }
        }).Schedule();
            
        m_Barrier.AddJobHandleForProducer(Dependency);
    }
}
