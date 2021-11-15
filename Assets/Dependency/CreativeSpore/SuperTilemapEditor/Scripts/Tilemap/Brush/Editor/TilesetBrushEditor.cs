using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TilesetBrush))]
    public class TilesetBrushEditor : Editor
    {

        SerializedProperty m_tileset;
        SerializedProperty m_autotilingMode;
        SerializedProperty m_group;
        SerializedProperty m_showInPalette;

        public virtual void OnEnable()
        {
            m_tileset = serializedObject.FindProperty("Tileset");
            m_autotilingMode = serializedObject.FindProperty("m_autotilingMode");
            m_group = serializedObject.FindProperty("m_group");
            m_showInPalette = serializedObject.FindProperty("m_showInPalette");
        }

        public override void OnInspectorGUI()
        {
            DoInspectorGUI();
        }

        public void DoInspectorGUI()
        {
            serializedObject.Update();
            TilesetBrush brush = (TilesetBrush)target;
            if (brush.Tileset == null)
            {
                EditorGUILayout.HelpBox("Select a tileset first", MessageType.Info);
                EditorGUILayout.PropertyField(m_tileset);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.PropertyField(m_tileset);
            EditorGUILayout.PropertyField(m_showInPalette);
            m_group.intValue = TilesetEditor.DoGroupFieldLayout(brush.Tileset, "Group", m_group.intValue);
            string sAutotilingModeTooltip =
                "Autotiling Mode:\n" +
                "Self: autotile only with brushes of same type\n" +
                "Other: autotile with any other not empty tile\n" +
                "Group: autotile with brushes of a group that autotile the brush group";
            m_autotilingMode.intValue = System.Convert.ToInt32( EditorCompatibilityUtils.EnumMaskField(new GUIContent("Autotiling Mode", sAutotilingModeTooltip), brush.AutotilingMode) );

            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            }
        }        
    }
}
