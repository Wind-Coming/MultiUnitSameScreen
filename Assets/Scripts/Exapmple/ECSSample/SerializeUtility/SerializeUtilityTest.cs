using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Collections;

public class SerializeUtilityTest : MonoBehaviour
{
    [SerializeField]
    ReferencedUnityObjects sharedComponentsInObject;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        { 
            int[] oute; 
            using (var writer = new StreamBinaryWriter(Application.dataPath + "/test.bytes"))
            {
                //SerializeUtility.SerializeWorld(World.DefaultGameObjectInjectionWorld.EntityManager, writer );
                SerializeUtilityHybrid.Serialize(World.DefaultGameObjectInjectionWorld.EntityManager, writer, out sharedComponentsInObject);
                #if UNITY_EDITOR
                UnityEditor.AssetDatabase.CreateAsset(sharedComponentsInObject, "Assets/testRef.asset");
                #endif
            }
        }
        else if(Input.GetKeyDown(KeyCode.B))
        {
            using (var reader = new StreamBinaryReader(Application.dataPath + "/test.bytes"))
            {
                World localwolrd = new World("local");
                var eManager = localwolrd.EntityManager;
                SerializeUtilityHybrid.Deserialize(eManager, reader, sharedComponentsInObject );

                var entities = localwolrd.EntityManager.GetAllEntities();
                //Move To Current World
                NativeArray<EntityRemapUtility.EntityRemapInfo> entityRemapping = localwolrd.EntityManager.CreateEntityRemapArray(Allocator.TempJob);
                World.DefaultGameObjectInjectionWorld.EntityManager.MoveEntitiesFrom(localwolrd.EntityManager, entityRemapping);

                for (int j = 0; j < entities.Length; j++)
                {
                    entities[j] = EntityRemapUtility.RemapEntity(ref entityRemapping, entities[j]);
                }
                entityRemapping.Dispose();

                localwolrd.Dispose();

            }
        }
    }
}
