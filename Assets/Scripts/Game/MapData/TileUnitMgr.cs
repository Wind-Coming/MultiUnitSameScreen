using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Spenve;

public class TileUnitMgr : SingletonDestory<TileUnitMgr>
{
    internal EntityArchetype archetype;
    internal EntityArchetype meshArchetype;

    public int num = 0;
    public TileData tile;

    // Start is called before the first frame update
    void Start()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        meshArchetype = entityManager.CreateArchetype(
                ComponentType.ChunkComponentReadOnly<ChunkWorldRenderBounds>(),
                ComponentType.ReadOnly<WorldRenderBounds>(),
                ComponentType.ReadOnly<LocalToWorld>(),
                ComponentType.ReadOnly<RenderMesh>()
            );

        archetype = entityManager.CreateArchetype(typeof(UvCom), typeof(VertexCom), typeof(RenderMesh), typeof(LodScaleCom));
    }


    public void CreateTile(int2 vector)
    {
        //TODO 用ecs创建提高效率

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        var entity = entityManager.CreateEntity(typeof(TileInfo));
        entityManager.SetComponentData(entity, new TileInfo { Value = new int2(vector.x, vector.y), lod = LodMgr.Instance.m_iCurrentLod });
        var group = entityManager.AddBuffer<LinkedEntityGroup>(entity);
        group.Add(entity);

        List<TileUnit> units = tile.GetUnits(LodMgr.Instance.m_iCurrentLod);//todo

        Profiler.BeginSample("Create Entity:"+ units.Count);
        for (int x = 0; x < units.Count; x++)
        {
            TileUnit tu = units[x];

            Entity instance;

            Profiler.BeginSample("Create");

            if (tu.isSprite)
            {
                instance = entityManager.CreateEntity(archetype);

            }
            else
            {
                instance = entityManager.CreateEntity(meshArchetype);//entityManager.Instantiate(prefab);
            }

            Profiler.EndSample();

            Profiler.BeginSample("Set Shared Component");

            RenderMesh rm = entityManager.GetSharedComponentData<RenderMesh>(instance);
            rm.material = tu.material;
            rm.layer = 0;

            Profiler.EndSample();

            Profiler.BeginSample("Set Component");

            var position = tu.localPosition + new Vector3(vector.x, 0, vector.y) * 300;

            if (tu.isSprite)
            {
                float2 scale;
                scale.x = tu.scale.x * tu.rect.x * 3;
                scale.y = tu.scale.y * 0.707f * tu.rect.y * 3;
                Vector3 p0 = position + new Vector3(-scale.x, -scale.y, -scale.y);
                Vector3 p1 = position + new Vector3(-scale.x, scale.y, scale.y);
                Vector3 p2 = position + new Vector3(scale.x, scale.y, scale.y);
                Vector3 p3 = position + new Vector3(scale.x, -scale.y, -scale.y);
                entityManager.SetComponentData(instance, new VertexCom { Value = new float3x4(p0, p1, p2, p3) });
                entityManager.SetComponentData(instance, new UvCom { Value = new float2x4(tu.uvs[2], tu.uvs[0], tu.uvs[1], tu.uvs[3]) });
                entityManager.SetComponentData(instance, new LodScaleCom { orgPos = position, orgScale = scale });

            }
            else
            {
                Profiler.BeginSample("Not Sprite Create");
                rm.mesh = tu.mesh;
                entityManager.AddComponentData(instance, new Translation { Value = position });
                entityManager.AddComponentData(instance, new Scale { Value = tu.scale.x });
                entityManager.AddComponentData(instance, new Rotation { Value = tu.rotation });
                Profiler.EndSample();
            }

            //group.Add(instance);
            entityManager.GetBuffer<LinkedEntityGroup>(entity).Add(instance);

            entityManager.SetSharedComponentData(instance, rm);

            Profiler.EndSample();

            num++;
        }

        Profiler.EndSample();
    }
}
