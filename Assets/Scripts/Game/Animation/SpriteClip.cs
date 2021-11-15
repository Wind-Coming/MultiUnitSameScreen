using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Collections;
using Unity.Mathematics;

[Serializable]
public class SpriteHash
{
    //通过动画名字，角度，帧数组合计算
    public int hash;
    public UnityEngine.Sprite sprite;
}

[Serializable]
public class SpriteClip : ScriptableObject
{
    [SerializeField]
    public SpriteHash[] sprites;

    public NativeHashMap<int, float2x4> spritesDic;

    public bool playPercent = false;//目前只针对翻滚动画

    public void Init()
    {
        if(sprites == null)
            return;

        spritesDic = new NativeHashMap<int, float2x4>(sprites.Length, Allocator.Persistent);

        for(int i = 0; i < sprites.Length; i++)
        {
            if(spritesDic.ContainsKey(sprites[i].hash))
            {
                Debug.Log("重复hash" + sprites[i].hash + "    index:" + i);
                continue;
            }
            Vector2[] uvs = sprites[i].sprite.uv;
            spritesDic.Add(sprites[i].hash, new float2x4(uvs[2], uvs[0], uvs[1], uvs[3]));
        }
    }

    public float2x4 GetSprite(int anim, int angle, int frame)
    {
        int key = anim * 100000000 + angle * 1000 + frame;
        float2x4 sp;
        if( spritesDic.TryGetValue(key, out sp) )
        {
            return sp;
        }
        return float2x4.zero;
    }

    public int Length
    {
        get
        {
            if (sprites != null)
                return sprites.Length;
            else
                return 0;
        }
    }

    public int FrameCount
    {
        get
        {
            return Length / 5;
        }
    }

    public void Dispose()
    {
        spritesDic.Dispose();
    }
}