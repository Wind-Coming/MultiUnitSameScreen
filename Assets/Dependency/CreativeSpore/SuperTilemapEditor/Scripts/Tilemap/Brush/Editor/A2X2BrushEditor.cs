using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(A2X2Brush))]
    public class A2X2BrushEditor : TilesetBrushEditor
    {
        [MenuItem("Assets/Create/SuperTilemapEditor/Brush/A2X2Brush", priority = 50)]
        public static A2X2Brush CreateAsset()
        {
            return EditorUtils.CreateAssetInSelectedDirectory<A2X2Brush>();
        }

        A2X2Brush m_brush;

        BrushTileGridControl m_brushTileGridControl = new BrushTileGridControl();
        public override void OnEnable()
        {
            base.OnEnable();
            m_brush = (A2X2Brush)target;
        }

        void OnDisable()
        {
            m_brushTileGridControl.Tileset = null; // avoid receiving OnTileSelection
        }
        /*
        static char[] s_tileEmptyChar = new char[]
        {
            '╔', '╗',
            '╚', '╝',
        };
        */
        static int[] s_tileIdxMap = new int[]
        {
            2, 3,
            0, 1,
        };
            static int[] s_symbolIdxMap = new int[]
        {
            6, 12,
            3, 9,
        };

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (!m_brush.Tileset) return;

            serializedObject.Update();
            EditorGUILayout.Space();

            m_brushTileGridControl.Tileset = m_brush.Tileset;
            m_brushTileGridControl.Display(target, m_brush.TileIds, s_tileIdxMap, 2, 2, m_brush.Tileset.VisualTileSize, s_symbolIdxMap);

            Repaint();
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}