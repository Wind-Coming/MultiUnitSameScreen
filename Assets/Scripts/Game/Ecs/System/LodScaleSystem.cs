using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public class LodScaleSystem : SystemBase
{
    private float orgScale = 0;
    
    // OnUpdate runs on the main thread.
    protected override void OnUpdate()
    {
        int lod = LodMgr.Instance.m_iCurrentLod;
        float scale = LodMgr.Instance.m_fCurrentScale;
        if(lod >= 1 || scale == orgScale)
            return;

        orgScale = scale;

        Entities.ForEach((Entity entity, int nativeThreadIndex, ref VertexCom vert, in LodScaleCom tile) =>
        {
            float2 newScale = scale * tile.orgScale;
            vert.Value.c0 = tile.orgPos + new float3(-newScale.x, -newScale.y, -newScale.y);
            vert.Value.c1 = tile.orgPos + new float3(-newScale.x, newScale.y, newScale.y);
            vert.Value.c2 = tile.orgPos + new float3(newScale.x, newScale.y, newScale.y);
            vert.Value.c3 = tile.orgPos + new float3(newScale.x, -newScale.y, -newScale.y);

        }).ScheduleParallel();
            
    }
}
