using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(RoadBrush))]
    public class RoadBrushEditor : TilesetBrushEditor
    {
        [MenuItem("Assets/Create/SuperTilemapEditor/Brush/RoadBrush", priority = 50)]
        public static RoadBrush CreateAsset()
        {
            RoadBrush brush = EditorUtils.CreateAssetInSelectedDirectory<RoadBrush>();
            return brush;
        }

        RoadBrush m_brush;

        BrushTileGridControl m_brushTileGridControl = new BrushTileGridControl();
        public override void OnEnable()
        {
            base.OnEnable();
            m_brush = (RoadBrush)target;
        }

        void OnDisable()
        {
            m_brushTileGridControl.Tileset = null;
        }

        /*
        static char[] s_tileEmptyChar = new char[]
        {
            '°', '╞', '═', '╡',
            '╥', '╔', '╦', '╗',
            '║', '╠', '╬', '╣',
            '╨', '╚', '╩', '╝',
        };
        */
        static int[] s_tileIdxMap = new int[]
        {
            0, 2, 10, 8,
            4, 6, 14, 12,
            5, 7, 15, 13,
            1, 3, 11, 9,
        };

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (!m_brush.Tileset) return;

            m_brushTileGridControl.Tileset = m_brush.Tileset;
            m_brushTileGridControl.Display(target, m_brush.TileIds, s_tileIdxMap, 4, 4, m_brush.Tileset.VisualTileSize, s_tileIdxMap);
            EditorGUILayout.Space();

            Repaint();
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}