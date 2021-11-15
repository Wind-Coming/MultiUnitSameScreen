using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;
using System.Collections.Generic;

// [MaterialProperty("_Color", MaterialPropertyFormat.Float4)]
// public struct MyOwnColor : IComponentData
// {
//     public float4 Value;
// }

// ReSharper disable once InconsistentNaming
[AddComponentMenu("DOTS Samples/SpawnFromMonoBehaviour/Spawner")]
public class Spawner_FromMonoBehaviour : MonoBehaviour
{
    public GameObject Prefab;
    public TileData tile;
    public int num = 0;
    public bool updateGenerate = false;

    void Start()
    {
        Add();
    }

    void Update()
    {
        if(updateGenerate && Input.GetMouseButton(0))
        {
            for (int i = 0; i < 10; i++)
            {
                Add();
            }
        }
    }

    void Add()
    {
        // Create entity prefab from the game object hierarchy once
        var settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, null);
        var prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(Prefab, settings);
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        List<TileUnit> units = tile.GetUnits(2);

        for (int x = 0; x < 128; x++)
        {
            TileUnit tu = units[0];

            // Efficiently instantiate a bunch of entities from the already converted entity prefab
            var instance = entityManager.Instantiate(prefab);

            // Place the instantiated entity in a grid with some noise
            var position = Vector3.zero;//tu.localPosition;
            entityManager.SetComponentData(instance, new Translation { Value = position });
            entityManager.AddComponentData(instance, new Scale { Value = tu.scale.x });
            entityManager.AddComponentData(instance, new Rotation { Value = Quaternion.Euler(45, 0, 0) });
            //entityManager.AddSharedComponentData(instance, new FrozenRenderSceneTag { });

            if(tu.rect != Vector4.zero)
                entityManager.AddComponentData(instance, new MaterialPropertyComponent { Value = tu.rect });

            RenderMesh rm = entityManager.GetSharedComponentData<RenderMesh>(instance);
            rm.mesh = tu.mesh;
            rm.material = tu.material;
            rm.receiveShadows = false;
            rm.layer = 0;
            rm.subMesh = 0;
            entityManager.SetSharedComponentData(instance, rm);

            num++;
        }

    }
}
