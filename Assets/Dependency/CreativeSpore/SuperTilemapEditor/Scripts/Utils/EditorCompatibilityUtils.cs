#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using UnityEditor;
namespace CreativeSpore.SuperTilemapEditor
{
    public class EditorCompatibilityUtils
    {
        public static string CtrKeyName { get { return ((Application.platform == RuntimePlatform.OSXEditor) ? "Command" : "Ctrl"); } }
        public static string AltKeyName { get { return ((Application.platform == RuntimePlatform.OSXEditor) ? "Option" : "Alt"); } }

#if UNITY_5_6_OR_NEWER
        public static void DotCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType = EventType.Ignore)
        {
            Handles.DotHandleCap(controlID, position, rotation, size, Event.current.type);
        }
        public static void CubeCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType = EventType.Ignore)
        {
            Handles.CubeHandleCap(controlID, position, rotation, size, Event.current.type);
        }
        public static void CircleCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType = EventType.Ignore)
        {
            Handles.CircleHandleCap(controlID, position, rotation, size, Event.current.type);
        }
        public static void SphereCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType = EventType.Ignore)
        {
            Handles.SphereHandleCap(controlID, position, rotation, size, Event.current.type);
        }
#else
        public static void DotCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            Handles.DotCap(controlID, position, rotation, size);
        }
        public static void CubeCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            Handles.CubeCap(controlID, position, rotation, size);
        }
        public static void CircleCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            Handles.CircleCap(controlID, position, rotation, size);
        }
        public static void SphereCap(int controlID, Vector3 position, Quaternion rotation, float size)
        {
            Handles.SphereCap(controlID, position, rotation, size);
        }
#endif
#if UNITY_2017_3_OR_NEWER
        public static System.Enum EnumMaskField(GUIContent label, System.Enum enumValue)
        {
            return EditorGUILayout.EnumFlagsField(label, enumValue);
        }
#else
        public static System.Enum EnumMaskField(GUIContent label, System.Enum enumValue)
        {
            return EditorGUILayout.EnumMaskField(label, enumValue);
        }
#endif

#if UNITY_2018_3_OR_NEWER
        public static GameObject CreatePrefab(string path, GameObject brushTilemap)
        {
            bool success;
            GameObject obj = PrefabUtility.SaveAsPrefabAsset(brushTilemap, path, out success);
            Debug.Assert(success, "Couldn't create the prefab asset!");
            return obj;
        }

        public static bool IsPrefab(GameObject obj)
        {
            return UnityEditor.PrefabUtility.GetPrefabAssetType(obj) != PrefabAssetType.NotAPrefab;
        }
#else
        public static GameObject CreatePrefab(string path, GameObject brushTilemap)
        {
            return PrefabUtility.CreatePrefab(path, brushTilemap);
        }

        public static bool IsPrefab(GameObject obj)
        {
            return UnityEditor.PrefabUtility.GetPrefabType(obj) == UnityEditor.PrefabType.Prefab;
        }
#endif
    }
}
#endif
