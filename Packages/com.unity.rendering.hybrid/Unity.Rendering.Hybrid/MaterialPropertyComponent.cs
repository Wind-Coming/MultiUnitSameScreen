using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Rendering
{
    // NOTE: This is a code example material override component for setting RGBA color to hybrid renderer.
    //       You should implement your own material property override components inside your own project.

    [Serializable]
    [MaterialProperty("_Offset", MaterialPropertyFormat.Float4)]
    public struct MaterialPropertyComponent : IComponentData
    {
        public float4 Value;
    }

    namespace Authoring
    {
        [DisallowMultipleComponent]
        [RequiresEntityConversion]
        [ConverterVersion("joe", 1)]
        public class MaterialPropertyComponent : MonoBehaviour
        {
            public Vector4 v4;
        }

        [WorldSystemFilter(WorldSystemFilterFlags.HybridGameObjectConversion)]
        public class MaterialPropertyComponentSystem : GameObjectConversionSystem
        {
            protected override void OnUpdate()
            {
                Entities.ForEach((MaterialPropertyComponent uMaterialColor) =>
                {
                    var entity = GetPrimaryEntity(uMaterialColor);
                    var data = new Unity.Rendering.MaterialPropertyComponent { Value = uMaterialColor.v4 };
                    DstEntityManager.AddComponentData(entity, data);
                });
            }
        }
    }
}
