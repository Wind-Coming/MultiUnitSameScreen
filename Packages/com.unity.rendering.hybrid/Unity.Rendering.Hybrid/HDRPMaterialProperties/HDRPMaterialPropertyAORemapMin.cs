using Unity.Entities;

#if ENABLE_HYBRID_RENDERER_V2
namespace Unity.Rendering
{
    [MaterialProperty("_AORemapMin"           , MaterialPropertyFormat.Float)]
    [GenerateAuthoringComponent]
    public struct HDRPMaterialPropertyAORemapMin : IComponentData { public float  Value; }
}
#endif
