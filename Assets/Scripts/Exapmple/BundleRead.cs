using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleRead : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        AssetBundle ab = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/fog.lb");
        Debug.Log(ab);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
