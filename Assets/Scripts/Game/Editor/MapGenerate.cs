using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class MapGenerate : EditorWindow
{
    [MenuItem("Window/MapGenerate")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        MapGenerate window = (MapGenerate)EditorWindow.GetWindow(typeof(MapGenerate));
        window.ShowUtility();
    }

    public Object Obj;
    public Vector2Int size;

    // public Mesh mesh;
    // public Material material;

    // void OnEnable()
    // {
    //     SceneView.duringSceneGui += OnSceneGUI;

    // }

    // void OnDisable()
    // {
    //     SceneView.duringSceneGui -= OnSceneGUI;
    // }

    // void OnSceneGUI(SceneView sceneView)
    // {
    //     Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, material, 0, sceneView.camera);
    // }

    void OnGUI()
    {
        Obj = EditorGUILayout.ObjectField(Obj, typeof(Object));
        size = EditorGUILayout.Vector2IntField("size", size);

        // mesh = (Mesh)EditorGUILayout.ObjectField(mesh, typeof(Mesh));

        // material = (Material)EditorGUILayout.ObjectField(material, typeof(Material));

        if(GUILayout.Button("generate"))
        {
            if(Obj != null)
            {
                Transform trans = null;
                if(Selection.activeGameObject != null)
                {
                    trans = Selection.activeGameObject.transform;
                }

                for(int i = 0; i < size.x; i++)
                {
                    for(int j = 0; j < size.y; j++)
                    {
                        GameObject gameObject = PrefabUtility.InstantiatePrefab(Obj) as GameObject;
                        gameObject.name = i.ToString() + "_" + j.ToString();
                        gameObject.transform.position = new Vector3((j * 300 + 150), 0, i * 300 + 150);
                        if(trans != null)
                        {
                            gameObject.transform.parent = trans;
                        }
                    }

                    EditorUtility.DisplayProgressBar("Creating Map", "", i * 1.0f / 40);
                }

                EditorUtility.ClearProgressBar();
            }
        }
    }
}
