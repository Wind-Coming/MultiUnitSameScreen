using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{
    [CustomEditor(typeof(BrushBehaviour))]
    public class BrushBehaviourEditor : Editor
    {
        [MenuItem("SuperTilemapEditor/Brush/Create Tilemap From Selection %t")]
        private static GameObject CreateTilemapFromBrush()
        {
            if (BrushBehaviour.Exists)
            {
                GameObject brushTilemap = new GameObject(GameObjectUtility.GetUniqueNameForSibling(null, "TilemapSelection"));
                brushTilemap.transform.position = BrushBehaviour.Instance.transform.position;
                brushTilemap.transform.rotation = BrushBehaviour.Instance.transform.rotation;
                brushTilemap.transform.localScale = BrushBehaviour.Instance.transform.localScale;
                STETilemap tilemapBhv = brushTilemap.AddComponent<STETilemap>();
                tilemapBhv.Tileset = BrushBehaviour.Instance.BrushTilemap.Tileset;
                tilemapBhv.Material = BrushBehaviour.Instance.BrushTilemap.Material;
                BrushBehaviour.Instance.Paint(tilemapBhv, Vector2.zero);
                return brushTilemap;
            }
            return null;
        }

        [MenuItem("SuperTilemapEditor/Brush/Create Prefab From Selection %#t")]
        private static void CreatePrefabFromBrush()
        {
            if (BrushBehaviour.Exists)
            {
                GameObject brushTilemap = CreateTilemapFromBrush();
                string path = AssetDatabase.GetAssetOrScenePath(Selection.activeObject);
                if (string.IsNullOrEmpty(path))
                {
                    path = "Assets/";
                }
                path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), brushTilemap.name + ".prefab").Replace(@"\", @"/");
                path = AssetDatabase.GenerateUniqueAssetPath(path);
                GameObject prefab = EditorCompatibilityUtils.CreatePrefab(path, brushTilemap);
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                GameObject.DestroyImmediate(brushTilemap);
            }
        }
    }
}
