using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Jobs;

public class AnimSystem : SystemBase
{

    [BurstCompile]
    public struct AnimComJob : IJobParallelFor
    {
        [DeallocateOnJobCompletion] public NativeArray<ArchetypeChunk> chunks;
        [ReadOnly]public NativeHashMap<int, float2x4> spritesDic;

        public ArchetypeChunkComponentType<AnimCom> animType;
        public ArchetypeChunkComponentType<UvCom> uvType;
        public int frame;
        
        public void Execute(int chunkIndex)
        {
            if(chunkIndex % 5 != frame)
                return;

            var chunk = chunks[chunkIndex];
            var anims = chunk.GetNativeArray(animType);
            var uvs = chunk.GetNativeArray(uvType);
            var animLength = spritesDic.Count() / 5;

            for(int i = 0; i < chunk.Count; i++)
            {
                var anim = anims[i];
                var uv = uvs[i];
                int key = anim.Value.x * 100000000 + anim.Value.y * 1000 + anim.Value.z;

                uv.Value = spritesDic[key];

                anim.Value.z++;

                if (anim.Value.z >= animLength)
                {
                    anim.Value.z = 0;
                }

                anims[i] = anim;
                uvs[i] = uv;
            }
        }
    }

    float time;
    int frame;
    EntityQuery query;

    //[ReadOnly] public NativeHashMap<int, float2x4> spritesDic;
    protected override void OnStartRunning()
    {
        query = GetEntityQuery(typeof(AnimCom), typeof(UvCom));
    }
    
    // OnUpdate runs on the main thread.
    protected override void OnUpdate()
    {
        // time += Time.DeltaTime;
        // if(time > 0.07f)
        // {
        //     time -= 0.07f;
        // }
        // else
        // {
        //     return;
        // }

        frame ++;
        if(frame > 5)
            frame = 0;

        var chunks = query.CreateArchetypeChunkArray(Allocator.TempJob);
        var animComJob = new AnimComJob
        {
            chunks = chunks,
            spritesDic = ArmyMgr.Instance.clip.spritesDic,
            animType = GetArchetypeChunkComponentType<AnimCom>(),
            uvType = GetArchetypeChunkComponentType<UvCom>(),
            frame = frame,
        };

        Dependency = animComJob.Schedule(chunks.Length, 32, Dependency);
        // time += Time.DeltaTime;
        // if(time > 0.07f)
        // {
        //     time -= 0.07f;
        // }
        // else
        // {
        //     return;
        // }

        // NativeHashMap<int, float2x4> spritesDic = ArmyMgr.Instance.clip.spritesDic;

        // Entities.ForEach((Entity entity, int nativeThreadIndex, ref UvCom uv, ref AnimCom anim ) =>
        // {
        //     int key = anim.Value.x * 100000000 + anim.Value.y * 1000 + anim.Value.z;

        //     uv.Value = spritesDic[key];
            
        //     anim.Value.z ++;

        //     if(anim.Value.z >= (spritesDic.Count() / 5 ))
        //     {
        //         anim.Value.z = 0;
        //     }
        // }).Schedule();
    }
}
