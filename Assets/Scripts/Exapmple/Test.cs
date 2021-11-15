using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    AssetBundle ab;
    GameObject go;
    Object goAsset;


    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A)) {
            ab = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/hero");
        }

        if (Input.GetKeyDown(KeyCode.B)) {
            ab.Unload(false);
        }

        if (Input.GetKeyDown(KeyCode.C)) {
            goAsset = ab.LoadAsset("HR_Caesar.prefab");
            go = Instantiate(goAsset) as GameObject;

            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        if (Input.GetKeyDown(KeyCode.D)) {
            Destroy(go);
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            ab.Unload(true);
        }

        if (Input.GetKeyDown(KeyCode.F)) {
            Resources.UnloadUnusedAssets();
        }

        if (Input.GetKeyDown(KeyCode.G)) {
            GameObject.DestroyImmediate(goAsset, true);
        }


    }
}
