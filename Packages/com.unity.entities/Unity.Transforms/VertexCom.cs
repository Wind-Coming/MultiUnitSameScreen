using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Transforms
{
    public struct VertexCom : IComponentData
    {
        public float3x4 Value;
    }
}
