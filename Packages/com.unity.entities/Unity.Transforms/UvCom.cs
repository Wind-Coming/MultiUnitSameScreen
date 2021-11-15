using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Transforms
{
    [Serializable]
    [WriteGroup(typeof(LocalToWorld))]
    public struct UvCom : IComponentData
    {
        public float2x4 Value;
    }
}
