using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Reflection;

namespace CreativeSpore.SuperTilemapEditor
{
    [CustomPropertyDrawer(typeof(SortingLayerAttribute))]
    public class SortingLayerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                Debug.LogError("SortedLayer property should be an integer ( the layer id )");
            }
            else
            {
                SortingLayerField(position, label, property, EditorStyles.popup, EditorStyles.label);
            }
        }

        public static void SortingLayerField(Rect position, GUIContent label, SerializedProperty layerID, GUIStyle style, GUIStyle labelStyle)
        {
            MethodInfo methodInfo = typeof(EditorGUI).GetMethod("SortingLayerField", BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(Rect), typeof(GUIContent), typeof(SerializedProperty), typeof(GUIStyle), typeof(GUIStyle) }, null);

            if (methodInfo != null)
            {
                object[] parameters = new object[] { position, label, layerID, style, labelStyle };
                methodInfo.Invoke(null, parameters);
            }
        }       
    }
}