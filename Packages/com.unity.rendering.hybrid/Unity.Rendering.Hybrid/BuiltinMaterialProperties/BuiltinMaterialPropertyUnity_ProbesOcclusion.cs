using Unity.Entities;
using Unity.Mathematics;

#if ENABLE_HYBRID_RENDERER_V2
namespace Unity.Rendering
{
    // This type is registered as a material property override manually.
    public struct BuiltinMaterialPropertyUnity_ProbesOcclusion : IComponentData { public float4   Value; }
}
#endif
