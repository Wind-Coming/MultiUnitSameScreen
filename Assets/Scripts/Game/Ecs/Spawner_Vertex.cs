using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;
using System.Collections.Generic;
using UnityEngine.Profiling;

// [MaterialProperty("_Color", MaterialPropertyFormat.Float4)]
// public struct MyOwnColor : IComponentData
// {
//     public float4 Value;
// }

// ReSharper disable once InconsistentNaming
[AddComponentMenu("DOTS Samples/SpawnFromMonoBehaviour/Spawner")]
public class Spawner_Vertex : MonoBehaviour
{
    public bool updateGenerate = false;

    void Start()
    {
        TileUnitMgr.Instance.CreateTile(int2.zero);
    }

    void Update()
    {
        if(updateGenerate && Input.GetMouseButtonDown(0))
        {
            for (int i = 0; i < 10; i++)
            {
                TileUnitMgr.Instance.CreateTile(int2.zero);
            }
        }
    }
}
