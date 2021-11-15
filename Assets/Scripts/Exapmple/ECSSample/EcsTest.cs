using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

public class EcsTest : MonoBehaviour
{
    public GameObject obj;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log( World.DefaultGameObjectInjectionWorld );
        var setting = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, null);
        var prefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(obj, setting);
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        var ins = entityManager.Instantiate(prefab);
        var pos = Vector3.zero;
        entityManager.SetComponentData(ins, new Translation(){Value = pos});
    }
}
