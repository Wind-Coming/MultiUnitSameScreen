using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;
using System.Collections.Generic;
using UnityEngine.Profiling;
using Spenve;
using UnityEngine.UI;

public class ArmyMgr : SingletonDestory<ArmyMgr>
{
    internal EntityArchetype archetype;

    public SpriteClip clip;
    public Material material;
    public int num = 0;

    public Text numText;
    
    // Start is called before the first frame update
    void Start()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        archetype = entityManager.CreateArchetype(typeof(UvCom), typeof(VertexCom), typeof(RenderMesh), typeof(AnimCom));
        clip.Init();
    }

    void Update()
    {
        // if(Input.GetKey(KeyCode.Q))
        // {
        //     CreateUnit(clip.GetSprite(1, 0, 0));
        // }
    }
    
    // void OnGUI()
    // {
    //     if(GUI.Button(new Rect(0, 60, 200, 100), "add"))
    //     {
    //         for(int i = 0; i < 20; i++)
    //             CreateUnit(clip.GetSprite(1, 0, 0));
    //     }
    //     
    //     GUI.Label(new Rect(0, 160, 200, 100), num.ToString());
    // }

    public void Create5000()
    {
        for(int i = 0; i < 250; i++)
            CreateUnit(clip.GetSprite(1, 0, 0));

        numText.text = "当前单位：" + num;
    }
    
    public void CreateUnit(float2x4 uvs)
    {
        //TODO 用ecs创建提高效率
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Profiler.BeginSample("Create Units");
        for (int x = 0; x < 20; x++)
        {
            Entity instance = entityManager.CreateEntity(archetype);

            RenderMesh rm = entityManager.GetSharedComponentData<RenderMesh>(instance);
            rm.material = material;
            rm.layer = 0;

            var position = MapMgr.Instance.GetCenter() + new Vector3(UnityEngine.Random.Range(-100, 100), 0, UnityEngine.Random.Range(-100, 100));

            float offset = 0.5f * 5;
            float offsetyz = offset * 0.707f;
            Vector3 p0 = position + new Vector3(-offset, -offsetyz, -offsetyz);
            Vector3 p1 = position + new Vector3(-offset, offsetyz, offsetyz);
            Vector3 p2 = position + new Vector3(offset, offsetyz, offsetyz);
            Vector3 p3 = position + new Vector3(offset, -offsetyz, -offsetyz);
            entityManager.SetComponentData(instance, new VertexCom { Value = new float3x4(p0, p1, p2, p3) });
            entityManager.SetComponentData(instance, new UvCom { Value = uvs });
            entityManager.SetComponentData(instance, new AnimCom { Value = new int3(1, 0, UnityEngine.Random.Range(0, 8)) });

            entityManager.SetSharedComponentData(instance, rm);

            num++;
        }

        Profiler.EndSample();
    }

    void OnDestroy()
    {
        clip.Dispose();
    }
}
