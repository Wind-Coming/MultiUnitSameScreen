using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
public struct EcsTestTag : IComponentData
{
}

public struct EcsTestSharedTag : ISharedComponentData
{
}

public class GroupTest : MonoBehaviour
{
    Entity entity;
    Entity child;
    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            var m_Manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            entity = m_Manager.CreateEntity(typeof(EcsTestTag));
            child = m_Manager.CreateEntity(typeof(EcsTestSharedTag));

            var group = m_Manager.AddBuffer<LinkedEntityGroup>(entity);
            group.Add(entity);
            group.Add(child);
        }

        if(Input.GetMouseButtonDown(1))
        {
            var m_Manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            m_Manager.DestroyEntity(entity);
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            var m_Manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            m_Manager.DestroyEntity(child);
        }

    }
}
