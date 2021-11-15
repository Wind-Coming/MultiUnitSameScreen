using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    public class BrushTileGridControl
    {
        [System.Flags]
        public enum eNeighbourFlags
        {
            North = 1,
            East = 1 << 1,
            South = 1 << 2,
            West = 1 << 3,
        }

        static Texture2D[] s_tileSymbolTextures = null;

        public bool ShowHelpBox = true;

        public static int GetTileNeighboursFlags(bool west, bool south, bool east, bool north)
        {
            return (west ? 1 << 3 : 0) | (south ? 1 << 2 : 0) | (east ? 1 << 1 : 0) | (north ? 1 << 0 : 0);
        }

        public static Texture2D GetTileSymbolTexture(bool west, bool south, bool east, bool north)
        {
            int idx = GetTileNeighboursFlags(west, south, east, north);
            return GetTileSymbolTexture(idx);
        }

        public static Texture2D GetTileSymbolTexture(int tileNeighboursFlags)
        {
            if (s_tileSymbolTextures == null)
            {
                s_tileSymbolTextures = new Texture2D[16];
                for (int i = 0; i < s_tileSymbolTextures.Length; ++i)
                {
                    Texture2D tex = new Texture2D(3, 3);
                    tex.hideFlags = HideFlags.DontSave;
                    tex.anisoLevel = 0;
                    tex.filterMode = FilterMode.Point;
                    s_tileSymbolTextures[i] = tex;

                    bool west = ((eNeighbourFlags)i & eNeighbourFlags.West) != 0;
                    bool south = ((eNeighbourFlags)i & eNeighbourFlags.South) != 0;
                    bool east = ((eNeighbourFlags)i & eNeighbourFlags.East) != 0;
                    bool north = ((eNeighbourFlags)i & eNeighbourFlags.North) != 0;

                    Color x = new Color(0, 0, 0, 0.2f);
                    Color d = new Color(0, 0, 0, 0.1f);
                    Color o = new Color(1, 1, 1, 0.1f);
                    Color[] colors = new Color[]
                {
                    o/*6*/, o/*7*/, o/*8*/,
                    o/*3*/, o/*4*/, o/*5*/,
                    o/*0*/, o/*1*/, o/*2*/,
                };
                    /*
                    if( i == 0 )
                    {
                        colors[4] = x;
                    }
                    else*/
                    {
                        // lateral
                        {
                            if (!north)
                            {
                                colors[6] = colors[7] = colors[8] = x;
                            }
                            if (!east)
                            {
                                colors[8] = colors[5] = colors[2] = x;
                            }
                            if (!south)
                            {
                                colors[0] = colors[1] = colors[2] = x;
                            }
                            if (!west)
                            {
                                colors[0] = colors[3] = colors[6] = x;
                            }
                        }
                        // diagonals
                        {
                            if (north && west)
                            {
                                colors[6] = d;
                            }
                            if (north && east)
                            {
                                colors[8] = d;
                            }
                            if (east && south)
                            {
                                colors[2] = d;
                            }
                            if (south && west)
                            {
                                colors[0] = d;
                            }
                        }
                    }

                    tex.SetPixels(colors);
                    tex.Apply();
                }
            }
            return s_tileSymbolTextures[tileNeighboursFlags % s_tileSymbolTextures.Length];
        }

        ~BrushTileGridControl()
        {
            Tileset = null;
        }

        public Tileset Tileset
        {
            get
            {
                return m_tileset;
            }

            set
            {
                if (m_tileset != value)
                {
                    if (m_tileset != null)
                    {
                        m_tileset.OnTileSelected -= OnTileSelected;
                        m_tileset.OnBrushSelected -= OnBrushSelected;
                    }
                    if (value != null)
                    {
                        value.OnTileSelected += OnTileSelected;
                        value.OnBrushSelected += OnBrushSelected;
                    }
                    m_tileset = value;
                }
            }
        }

        Tileset m_tileset;
        int m_selectedTileIdx = -1;
        int m_tileIdOff = 0;
        int m_tileIdOffSkipIdx = 0;
        uint[] m_aBrushTileData;
        Object m_target;
        bool m_hasChanged = false;

        void OnTileSelected(Tileset source, int prevTileId, int newTileId)
        {
            if (m_selectedTileIdx >= 0)
            {
                m_tileIdOff = 0;
                uint brushTileData = m_aBrushTileData[m_selectedTileIdx];
                if(brushTileData == Tileset.k_TileData_Empty)
                {
                    brushTileData = 0u; // reset flags and everything
                }
                int tileId = (int)(brushTileData & Tileset.k_TileDataMask_TileId);
                if (tileId != Tileset.k_TileId_Empty && newTileId != tileId)
                {
                    m_tileIdOff = newTileId - tileId;
                    m_tileIdOffSkipIdx = m_selectedTileIdx;
                }
                Undo.RecordObject(m_target, "TileChanged");
                brushTileData &= ~Tileset.k_TileDataMask_BrushId;
                brushTileData &= ~Tileset.k_TileDataMask_TileId;
                brushTileData |= (uint)(newTileId & Tileset.k_TileDataMask_TileId);
                m_aBrushTileData[m_selectedTileIdx] = brushTileData;
                m_hasChanged = true;
            }
            EditorUtility.SetDirty(m_target);
        }

        void OnBrushSelected(Tileset source, int prevBrushId, int newBrushId)
        {
            if (m_selectedTileIdx >= 0)
            {
                m_tileIdOff = 0;
                uint brushTileData = m_aBrushTileData[m_selectedTileIdx];
                if (brushTileData == Tileset.k_TileData_Empty)
                {
                    brushTileData = 0u; // reset flags and everything
                }
                Undo.RecordObject(m_target, "BrushChanged");

                TilesetBrush brush = Tileset.FindBrush(newBrushId);
                int tileId = (int)(brush.PreviewTileData() & Tileset.k_TileDataMask_TileId);
                brushTileData &= Tileset.k_TileDataMask_Flags;
                brushTileData |= (uint)(newBrushId << 16) & Tileset.k_TileDataMask_BrushId;
                brushTileData |= (uint)(tileId & Tileset.k_TileDataMask_TileId);

                m_aBrushTileData[m_selectedTileIdx] = brushTileData;
                m_hasChanged = true;
            }
            EditorUtility.SetDirty(m_target);
        }

        private Rect m_tileSelectionRect;
        private bool m_displayAutocompleteBtn = false; // fix gui warning when button appears
        public void Display(Object target, uint[] aTileData, int[] tileIdxMap, int gridWidth, int gridHeight, Vector2 visualTileSize, int[] symbolIdxMap)
        {
            GUI.changed = m_hasChanged;
            m_hasChanged = false;
            m_target = target;
            m_aBrushTileData = aTileData;
            Event e = Event.current;
            bool isRightMouseReleased = e.type == EventType.MouseUp && e.button == 0;
            if (isRightMouseReleased && !m_tileSelectionRect.Contains(e.mousePosition))
            {
                m_selectedTileIdx = -1;
            }

            GUILayout.BeginHorizontal();

            // Draw Autotile Combination Control
            GUI.backgroundColor = Tileset.BackgroundColor;
            GUILayout.BeginScrollView(Vector2.zero, (GUIStyle)"Button", GUILayout.Width(visualTileSize.x * gridWidth), GUILayout.Height(visualTileSize.y * gridHeight + 1f));
            GUI.backgroundColor = Color.white;
            for (int i = 0; i < gridWidth * gridHeight; ++i)
            {
                int gx = i % gridWidth;
                int gy = i / gridWidth;
                int tileIdx = tileIdxMap[i];
                Rect rVisualTile = new Rect(gx * visualTileSize.x, gy * visualTileSize.y, visualTileSize.x, visualTileSize.y);
                uint tileData = m_aBrushTileData[tileIdx];
                TilesetBrush brush = Tileset.FindBrush(Tileset.GetBrushIdFromTileData(tileData));
                if (brush)
                {
                    tileData = TilesetBrush.ApplyAndMergeTileFlags(brush.PreviewTileData(), tileData);
                }
                int tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
                if (tileId != Tileset.k_TileId_Empty)
                {
                    //Rect tileUV = Tileset.Tiles[tileId].uv;
                    //GUI.DrawTextureWithTexCoords(rVisualTile, Tileset.AtlasTexture, tileUV, true);
                    TilesetEditor.DoGUIDrawTileFromTileData(rVisualTile, tileData, Tileset);
                }
                else if (symbolIdxMap != null)
                {
                    GUI.DrawTexture(rVisualTile, GetTileSymbolTexture((byte)symbolIdxMap[i]), ScaleMode.ScaleToFit, true);
                }

                Color bgColor = new Color(1f - Tileset.BackgroundColor.r, 1f - Tileset.BackgroundColor.g, 1f - Tileset.BackgroundColor.b, Tileset.BackgroundColor.a);
                HandlesEx.DrawRectWithOutline(rVisualTile, m_selectedTileIdx == tileIdx ? new Color(0f, 0f, 0f, 0.1f) : new Color(), m_selectedTileIdx == tileIdx ? new Color(1f, 1f, 0f, 1f) : bgColor);

                if (isRightMouseReleased && rVisualTile.Contains(e.mousePosition))
                {
                    m_selectedTileIdx = tileIdx;
                    EditorWindow wnd = EditorWindow.focusedWindow;
                    TileSelectionWindow.Show(Tileset);
                    TileSelectionWindow.Instance.Ping();
                    wnd.Focus();
                    GUI.FocusControl("");
                }
            }
            GUILayout.EndScrollView();

            uint brushTileData = m_selectedTileIdx >= 0 ? m_aBrushTileData[m_selectedTileIdx] : Tileset.k_TileData_Empty;
            brushTileData = DoTileDataPropertiesLayout(brushTileData, Tileset);
            if (m_selectedTileIdx >= 0)
            {
                m_aBrushTileData[m_selectedTileIdx] = brushTileData;
            }

            GUILayout.EndHorizontal();
            if (e.type == EventType.Repaint)
            {
                m_tileSelectionRect = GUILayoutUtility.GetLastRect();
            }

            bool hasEmptyTiles = ArrayUtility.Contains<uint>(m_aBrushTileData, Tileset.k_TileData_Empty);
            m_displayAutocompleteBtn = e.type == EventType.Layout ? !hasEmptyTiles && m_tileIdOff != 0 : m_displayAutocompleteBtn;
            if (m_displayAutocompleteBtn && GUILayout.Button("Autocomplete relative to last change"))
            {
                Undo.RecordObject(m_target, "MultipleTileChanged");
                for (int i = 0; i < tileIdxMap.Length; ++i)
                {
                    int tileIdx = tileIdxMap[i];
                    if (tileIdx != m_tileIdOffSkipIdx)
                    {
                        int brushTileId = (int)(m_aBrushTileData[tileIdx] & Tileset.k_TileDataMask_TileId);
                        brushTileId += m_tileIdOff;
                        if (brushTileId < 0 || brushTileId >= m_tileset.Tiles.Count)
                        {
                            m_aBrushTileData[tileIdx] = Tileset.k_TileData_Empty;
                        }
                        else
                        {
                            m_aBrushTileData[tileIdx] &= ~Tileset.k_TileDataMask_TileId;
                            m_aBrushTileData[tileIdx] |= (uint)(brushTileId & Tileset.k_TileDataMask_TileId);
                        }
                    }
                }
                m_tileIdOff = 0;
                EditorUtility.SetDirty(m_target);
            }
            if (Tileset.TileSelection != null && Tileset.TileSelection.selectionData.Count == gridWidth * gridHeight && Tileset.TileSelection.rowLength == gridWidth)
            {
                if (GUILayout.Button("Autocomplete from selection"))
                {
                    Undo.RecordObject(m_target, "MultipleTileChanged");
                    for (int i = 0; i < tileIdxMap.Length; ++i)
                    {
                        int tileIdx = tileIdxMap[i];
                        int selectionIdx = (i % gridWidth) + ( gridHeight - 1 - i / gridWidth) * gridWidth;
                        int brushTileId = (int)(Tileset.TileSelection.selectionData[selectionIdx] & Tileset.k_TileDataMask_TileId);
                        m_aBrushTileData[tileIdx] = (uint)(brushTileId & Tileset.k_TileDataMask_TileId);
                    }
                    m_tileIdOff = 0;
                    EditorUtility.SetDirty(m_target);
                }
            }

            if (ShowHelpBox)
            {
                EditorGUILayout.HelpBox("Select  a tile from the grid, then select a tile from Tile Selection Window to change the tile.\nOr select a group of tiles and press Autocomplete from selection.", MessageType.Info);
            }
        }

        public static uint DoTileDataPropertiesLayout(uint tileData, Tileset tileset, bool displayBrush = true)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUI.enabled = tileData != Tileset.k_TileData_Empty;
            EditorGUIUtility.labelWidth = 100;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Toggle("Flip Horizontally", (tileData & Tileset.k_TileFlag_FlipH) != 0);
            if (EditorGUI.EndChangeCheck())
            {
                tileData ^= Tileset.k_TileFlag_FlipH;
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Toggle("Flip Vertically", (tileData & Tileset.k_TileFlag_FlipV) != 0);
            if (EditorGUI.EndChangeCheck())
            {
                tileData ^= Tileset.k_TileFlag_FlipV;
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Toggle("Rotate 90º", (tileData & Tileset.k_TileFlag_Rot90) != 0);
            if (EditorGUI.EndChangeCheck())
            {
                tileData ^= Tileset.k_TileFlag_Rot90;
            }

            if (displayBrush)
            {
                EditorGUI.BeginChangeCheck();
                int brushId = Tileset.GetBrushIdFromTileData(tileData);
                TilesetBrush brush = tileset.FindBrush(brushId);
                brush = (TilesetBrush)EditorGUILayout.ObjectField("Brush", brush, typeof(TilesetBrush), false);
                if (EditorGUI.EndChangeCheck())
                {
                    if ( brush && brush.Tileset != tileset)
                    {
                        Debug.LogWarning("The brush " + brush.name + " belongs to a different tileset and cannot be selected! ");
                    }
                    else
                    {
                        brushId = brush != null ? tileset.FindBrushId(brush.name) : Tileset.k_BrushId_Default;
                        int tileId = brush != null ? (int)(brush.PreviewTileData() & Tileset.k_TileDataMask_TileId) : Tileset.GetTileIdFromTileData(tileData);
                        tileData &= Tileset.k_TileDataMask_Flags;
                        tileData |= (uint)(brushId << 16) & Tileset.k_TileDataMask_BrushId;
                        tileData |= (uint)(tileId & Tileset.k_TileDataMask_TileId);
                    }
                }
            }

            if(GUILayout.Button("Reset"))
            {
                tileData = Tileset.k_TileData_Empty;
            }

            EditorGUIUtility.labelWidth = 0;
            GUI.enabled = true;

            EditorGUILayout.EndVertical();
            return tileData;
        }
    }
}