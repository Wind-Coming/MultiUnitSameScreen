using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    [CanEditMultipleObjects]
    [CustomEditor(typeof(BrushFortySeven))]
    // Created by Nikola Kasabov and modified by CreativeSpore.
    public class BrushFortySevenEditor : TilesetBrushEditor
    {
        [MenuItem("Assets/Create/SuperTilemapEditor/Brush/Brush47", priority = 50)]
        public static BrushFortySeven CreateAsset()
        {
            BrushFortySeven brush = EditorUtils.CreateAssetInSelectedDirectory<BrushFortySeven>();
            return brush;
        }

        BrushFortySeven m_brush;

        private static Texture2D s_bgTexture;
        public static Texture2D BackgroundTexture
        {
            get
            {
                if (!s_bgTexture)
                {
                    s_bgTexture = new Texture2D(21, 21, TextureFormat.ARGB32, false, false);
                    s_bgTexture.hideFlags = HideFlags.DontSave;
                    s_bgTexture.wrapMode = TextureWrapMode.Clamp;
                    s_bgTexture.filterMode = FilterMode.Point;
                    int[] data = new int[]
                    {
                        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                        1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1,
                        1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1,
                        1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1,
                        1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1,
                        1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1,
                        1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1,
                        1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1,
                        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                        0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                        0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 1, 0, 1,
                        0, 0, 1, 1, 0, 1, 1, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
                        0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1,
                        0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                        0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0,
                        0, 0, 1, 1, 0, 1, 1, 0, 0, 1, 0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0,
                        0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0,
                        0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1,
                        0, 0, 1, 1, 0, 0, 1, 0, 0, 0, 0, 1, 1, 0, 1, 1, 0, 1, 0, 0, 0,
                        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                        1, 0, 1, 1, 0, 1, 0, 0, 1, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0,
                    };
                    Color32[] colors = s_bgTexture.GetPixels32();
                    Color32 cEmpty = new Color(0f, 0f, 0f, 0f);
                    Color32 cColor = new Color(.6f, 1f, 1f, 1f);
                    for (int i = 0; i < data.Length; ++i)
                    {
                        int cIdx = (s_bgTexture.height - 1 - (i / s_bgTexture.width)) * s_bgTexture.width + (i % s_bgTexture.width);
                        colors[cIdx] = (data[i] > 0) ? cColor : cEmpty;
                    }
                    s_bgTexture.SetPixels32(colors);
                    s_bgTexture.Apply();
                }
                return s_bgTexture;
            }
        }

        TileGridControl m_brushTileGridControl;
        public override void OnEnable()
        {
            base.OnEnable();
            m_brush = (BrushFortySeven)target;
            if (m_brushTileGridControl == null)
            {
                m_brushTileGridControl = new TileGridControl(target, 7, 7, BackgroundTexture,
                    (int tileIdx) => { return m_brush.TileIds[s_tileIdxMap[tileIdx]]; },
                    (int tileIdx, uint tileData) => { m_brush.TileIds[s_tileIdxMap[tileIdx]] = tileData; }
                    );
                m_brushTileGridControl.AllowBrushSelection = true;
            }
        }

        void OnDisable()
        {
            m_brushTileGridControl.Tileset = null;
        }

		static int[] s_tileIdxMap = new int[]
		{
			 0,  1,  2,  3,  4,  5,  6,
             7,  8,  9, 10, 11, 12, 13,
            14, 15, 16, 17, 18, 19, 20,
            21, 22, 23, 24, 25, 26, 27,
            28, 29, 30, 31, 32, 33, 34,
            35, 36, 37, 38, 39, 40, 41,
            42, 43, 44, 45, 46, 47, 48,
		};


		public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (!m_brush.Tileset) return;

            m_brushTileGridControl.Tileset = m_brush.Tileset;
            m_brushTileGridControl.Display(m_brush.Tileset.VisualTileSize);
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