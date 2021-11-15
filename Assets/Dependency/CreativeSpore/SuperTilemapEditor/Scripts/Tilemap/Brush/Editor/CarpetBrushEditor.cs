using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(CarpetBrush))]
    public class CarpetBrushEditor : TilesetBrushEditor
    {
        [MenuItem("Assets/Create/SuperTilemapEditor/Brush/CarpetBrush", priority = 50)]
        public static CarpetBrush CreateAsset()
        {
            CarpetBrush brush = EditorUtils.CreateAssetInSelectedDirectory<CarpetBrush>();
            return brush;
        }

        CarpetBrush m_brush;

        BrushTileGridControl m_brushTileGridControl = new BrushTileGridControl();
        BrushTileGridControl m_interiorCornersControl = new BrushTileGridControl();
        public override void OnEnable()
        {
            base.OnEnable();
            m_brush = (CarpetBrush)target;
        }

        void OnDisable()
        {
            m_brushTileGridControl.Tileset = null;
            m_interiorCornersControl.Tileset = null;
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
            6, 14, 12,
            7, 15, 13,
            3, 11, 9,
        };

        static int[] s_interiorCornersIdxMap = new int[]
        {
            2, 3,
            0, 1,
        };

        static int[] s_interiorCornersSymbolIdx = new int[]
        {
            6, 12,
            3, 9,
        };

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (!m_brush.Tileset) return;

            m_brushTileGridControl.Tileset = m_brush.Tileset;
            EditorGUI.BeginChangeCheck();
            m_brushTileGridControl.Display(target, m_brush.TileIds, s_tileIdxMap, 3, 3, m_brush.Tileset.VisualTileSize, s_tileIdxMap);
            if( EditorGUI.EndChangeCheck() )
            {
                // Fill unused positions with the center tile [15] ╬ because this brush derived from roads but use only some combinations
                m_brush.TileIds[0] = m_brush.TileIds[2] = m_brush.TileIds[10] = m_brush.TileIds[8] 
                    = m_brush.TileIds[4] = m_brush.TileIds[5] = m_brush.TileIds[1] = m_brush.TileIds[15];
                /*NOTE: this will allow creating prefabs on top attached to the top tiles when drawing a horizontal line
                m_brush.TileIds[2] = m_brush.TileIds[6];
                m_brush.TileIds[10] = m_brush.TileIds[14];
                m_brush.TileIds[8] = m_brush.TileIds[12];
                */
                EditorUtility.SetDirty(target);
            }
            EditorGUILayout.Space();

            m_interiorCornersControl.Tileset = m_brush.Tileset;
            m_interiorCornersControl.ShowHelpBox = false;
            m_interiorCornersControl.Display(target, m_brush.InteriorCornerTileIds, s_interiorCornersIdxMap, 2, 2, m_brush.Tileset.VisualTileSize, s_interiorCornersSymbolIdx);
            EditorGUILayout.HelpBox("Select the 4 interior corners", MessageType.Info);

            Repaint();
            serializedObject.ApplyModifiedProperties();
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }
}