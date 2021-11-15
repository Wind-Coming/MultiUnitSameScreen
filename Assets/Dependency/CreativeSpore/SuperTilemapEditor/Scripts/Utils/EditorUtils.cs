using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditorInternal;
using UnityEditor;
namespace CreativeSpore.SuperTilemapEditor
{    
    public static class EditorUtils
    {
        public static T CreateAssetInSelectedDirectory<T>(string name = null) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            ProjectWindowUtil.CreateAsset(asset, (string.IsNullOrEmpty(name) ? "New " + typeof(T).Name : name) + ".asset");
            return asset;
            /*
            T asset = ScriptableObject.CreateInstance<T>();

            string path = GetAssetSelectedPath();

            string objName = typeof(T).ToString();
            string objExt = Path.GetExtension(objName);
            if (!string.IsNullOrEmpty(objExt))
            {
                objName = objExt.Remove(0, 1);
            }
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + (string.IsNullOrEmpty(name)? "/New " + objName : "/"+name) + ".asset");

            AssetDatabase.CreateAsset(asset, assetPathAndName);
            Selection.activeObject = asset;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            return asset;*/
        }

        public static string GetAssetSelectedPath()
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "")
            {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "")
            {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }
            return path;
        }

        // Get the sorting layer names
        public static string[] GetSortingLayerNames()
        {
            System.Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
            return (string[])sortingLayersProperty.GetValue(null, new object[0]);
        }

        // Get the sorting layer UniqueIds
        public static int[] GetSortingLayerUniqueIDs()
        {
            System.Type internalEditorUtilityType = typeof(InternalEditorUtility);
            PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerUniqueIDs", BindingFlags.Static | BindingFlags.NonPublic);
            return (int[])sortingLayersProperty.GetValue(null, new object[0]);
        }

        public static string GetSortingLayerNameById( int sortingLayerId )
        {
            int idx = ArrayUtility.FindIndex(GetSortingLayerUniqueIDs(), (x) => x == sortingLayerId);
            if(idx >= 0)
            {
                return GetSortingLayerNames()[idx];
            }
            return "";
        }

        public static int GetSortingLayerIdByName(string sortingLayerName)
        {
            int idx = ArrayUtility.FindIndex(GetSortingLayerNames(), (x) => x == sortingLayerName);
            if (idx >= 0)
            {
                return GetSortingLayerUniqueIDs()[idx];
            }
            return -1;
        }

        public static void SortingLayerFieldLayout(GUIContent label, SerializedProperty layerID, GUIStyle style, GUIStyle labelStyle)
        {
            Rect position = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.popup, new GUILayoutOption[0]);
            SortingLayerField(position, label, layerID, style, labelStyle);
        }

        public static void SortingLayerField(Rect position, GUIContent label, SerializedProperty layerID, GUIStyle style, GUIStyle labelStyle)
        {
            Event e = Event.current;
            int[] sortingLayerUniqueIDs = EditorUtils.GetSortingLayerUniqueIDs();
            string[] sortingLayerNames = EditorUtils.GetSortingLayerNames();

            ArrayUtility.Add<string>(ref sortingLayerNames, string.Empty);
            ArrayUtility.Add<string>(ref sortingLayerNames, "Add Sorting Layer...");

            GUIContent[] array = new GUIContent[sortingLayerNames.Length];
            for (int i = 0; i < sortingLayerNames.Length; i++)
            {
                array[i] = new GUIContent(sortingLayerNames[i]);
            }

            EditorUtility.SelectMenuItemFunction setEnumValueDelegate = (object userData, string[] options, int selected) =>
            {
                if (selected == options.Length - 1)
                {
                    //((TagManager)EditorApplication.tagManager).m_DefaultExpandedFoldout = "SortingLayers";
                    PropertyInfo tagManagerPropertyInfo = typeof(EditorApplication).GetProperty("tagManager", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetProperty);
                    if (tagManagerPropertyInfo != null)
                    {
                        System.Object tagManager = (System.Object)tagManagerPropertyInfo.GetValue(typeof(EditorApplication), null);
                        FieldInfo fieldInfo = tagManager.GetType().GetField("m_DefaultExpandedFoldout");
                        if (fieldInfo != null)
                        {
                            fieldInfo.SetValue(tagManager, "SortingLayers");
                        }
                    }
                    EditorApplication.ExecuteMenuItem("Edit/Project Settings/Tags and Layers");
                }
                else
                {
                    layerID.intValue = sortingLayerUniqueIDs[selected];
                }
            };

            int sortingLayerIdx = System.Array.IndexOf(sortingLayerUniqueIDs, layerID.intValue);

            Rect rPopup = position;
            rPopup.x += EditorGUIUtility.labelWidth;
            rPopup.width -= EditorGUIUtility.labelWidth;

            if (e.type == EventType.Repaint)
            {
                labelStyle.Draw(position, label, false, false, false, false);
                style.Draw(rPopup, sortingLayerNames[sortingLayerIdx], false, false, false, false);
            }

            if (position.Contains(e.mousePosition) && e.isMouse && e.button == 0)
            {
                EditorUtility.DisplayCustomMenu(rPopup, array, sortingLayerIdx, setEnumValueDelegate, null);
            }
        }

        public static bool DoToggleIconButton(string label, bool value, GUIContent icon)
        {
            GUIStyle btnStyle = new GUIStyle("Button");
            btnStyle.padding = new RectOffset(0, 0, 0, 0);
            btnStyle.margin = new RectOffset(0, 0, 0, 0);

            Rect controlRect = EditorGUILayout.GetControlRect(true, 23f, new GUILayoutOption[0]);
            Rect position = new Rect(controlRect.xMin + EditorGUIUtility.labelWidth, controlRect.yMin, 33f, 23f);
            GUIContent content = new GUIContent(label);
            Vector2 vector = GUI.skin.label.CalcSize(content);
            Rect position2 = new Rect(position.xMax + 5f, controlRect.yMin + (controlRect.height - vector.y) * 0.5f, vector.x, controlRect.height);
            value = GUI.Toggle(position, value, icon, btnStyle);
            GUI.Label(position2, label);
            return value;
        }

        public static bool DoToggleButton(string label, bool value)
        {
            GUIStyle btnStyle = (GUIStyle)"Button";
            if (GUILayout.Button(label, value ? new GUIStyle("Button") { normal = { background = btnStyle.active.background } } : (GUIStyle)"Button"))
            {
                value = !value;
            }
            return value;
        }
    }
}
#endif