using System;
using Unity.Entities;
using Unity.Mathematics;


public struct TileInfo : IComponentData
{
    public int2 Value;
    public int lod;
}

public struct LodScaleCom : IComponentData
{
    public float3 orgPos;
    public float2 orgScale;
}