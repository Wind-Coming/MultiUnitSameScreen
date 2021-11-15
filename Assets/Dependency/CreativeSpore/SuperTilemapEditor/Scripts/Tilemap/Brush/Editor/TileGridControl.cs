using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{
    public class TileGridControl
    {

        public delegate uint GetTileData(int tileIdx);
        public delegate void SetTileData(int tileIdx, uint tileData);

        public bool ShowHelpBox = true;
        public bool AllowBrushSelection = true;
        public string HelpBoxText = "Select  a tile from the grid, then select a tile from Tile Selection Window to change the tile.\nOr select a group of tiles and press Autocomplete from selection.";

        public int SelectedTileIdx { get { return m_selectedTileIdx; } }
        public int Width { get { return m_width; } }
        public int Height { get { return m_height; } }
        public Object TargetObj { get { return m_target; } }
        public Tileset Tileset
        {
            get{return m_tileset;}
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
        private int m_width;
        private int m_height;
        private Texture2D m_backgroundTexture;
        private GetTileData m_getTileDataFunc;
        private SetTileData m_setTileDataFunc;
        int m_selectedTileIdx = -1;
        bool m_hasFocus = false;
        int m_tileIdOff = 0;
        int m_tileIdOffSkipIdx = 0;
        Object m_target;
        bool m_hasChanged = false;

        public TileGridControl(Object target, int width, int height, Texture2D backgroundTexture, GetTileData getTileDataFunc, SetTileData setTileDataFunc)
        {
            m_target = target;
            m_width = width;
            m_height = height;
            m_backgroundTexture = backgroundTexture;
            m_getTileDataFunc = getTileDataFunc;
            m_setTileDataFunc = setTileDataFunc;
        }

        ~TileGridControl()
        {
            Tileset = null;
        }


        void OnTileSelected(Tileset source, int prevTileId, int newTileId)
        {
            if (m_selectedTileIdx >= 0 && m_hasFocus)
            {
                m_tileIdOff = 0;
                uint brushTileData = m_getTileDataFunc(m_selectedTileIdx);
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
                m_setTileDataFunc( m_selectedTileIdx, brushTileData);
                m_hasChanged = true;
            }
            EditorUtility.SetDirty(m_target);
        }

        void OnBrushSelected(Tileset source, int prevBrushId, int newBrushId)
        {
            if (AllowBrushSelection)
            {
                if (m_selectedTileIdx >= 0 && m_hasFocus)
                {
                    m_tileIdOff = 0;
                    uint brushTileData = m_getTileDataFunc(m_selectedTileIdx);
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

                    m_setTileDataFunc(m_selectedTileIdx, brushTileData);
                    m_hasChanged = true;
                }
                EditorUtility.SetDirty(m_target);
            }
        }

        private Rect m_tileSelectionRect;
        private bool m_displayAutocompleteBtn = false; // fix gui warning when button appears
        public void Display( Vector2 visualTileSize )
        {
            GUI.changed |= m_hasChanged;
            m_hasChanged = false;
            Event e = Event.current;
            bool isLeftMouseReleased = e.type == EventType.MouseUp && e.button == 0;
            if (isLeftMouseReleased)
            {
                m_hasFocus = m_tileSelectionRect.Contains(e.mousePosition);
            }

            bool hasEmptyTiles = false;
            int size = m_width * m_height;
            Color cSelectedBorderColor = new Color(1f, 1f, 0f, 1f);
            if (!m_hasFocus) cSelectedBorderColor *= .8f;
            GUILayout.BeginHorizontal();
            {
                // Draw Autotile Combination Control
                GUI.backgroundColor = Tileset.BackgroundColor;
                GUILayoutUtility.GetRect(visualTileSize.x * m_width, visualTileSize.y * m_height + 1f);
                Rect rArea = GUILayoutUtility.GetLastRect();
                {
                    if (m_backgroundTexture)
                        GUI.DrawTexture(new Rect(rArea.position, Vector2.Scale(visualTileSize, new Vector2(m_width, m_height))), m_backgroundTexture);
                    GUI.backgroundColor = Color.white;
                    for (int tileIdx = 0; tileIdx < size; ++tileIdx)
                    {
                        int gx = tileIdx % m_width;
                        int gy = tileIdx / m_width;
                        Rect rVisualTile = new Rect(gx * visualTileSize.x, gy * visualTileSize.y, visualTileSize.x, visualTileSize.y);
                        rVisualTile.position += rArea.position;
                        uint tileData = m_getTileDataFunc(tileIdx);
                        hasEmptyTiles |= tileData == Tileset.k_TileData_Empty;
                        TilesetBrush brush = Tileset.FindBrush(Tileset.GetBrushIdFromTileData(tileData));
                        if (brush)
                        {
                            tileData = TilesetBrush.ApplyAndMergeTileFlags(brush.PreviewTileData(), tileData);
                        }
                        int tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
                        if (tileId != Tileset.k_TileId_Empty)
                        {
                            TilesetEditor.DoGUIDrawTileFromTileData(rVisualTile, tileData, Tileset);
                        }

                        Color bgColor = new Color(1f - Tileset.BackgroundColor.r, 1f - Tileset.BackgroundColor.g, 1f - Tileset.BackgroundColor.b, Tileset.BackgroundColor.a);
                        HandlesEx.DrawRectWithOutline(rVisualTile, m_selectedTileIdx == tileIdx ? new Color(0f, 0f, 0f, 0.1f) : new Color(), m_selectedTileIdx == tileIdx ? cSelectedBorderColor : bgColor);

                        if (isLeftMouseReleased && rVisualTile.Contains(e.mousePosition))
                        {
                            m_selectedTileIdx = tileIdx;
                            EditorWindow wnd = EditorWindow.focusedWindow;
                            TileSelectionWindow.Show(Tileset);
                            TileSelectionWindow.Instance.Ping();
                            wnd.Focus();
                            GUI.FocusControl("");
                        }
                    }
                }
                
                uint brushTileData = m_selectedTileIdx >= 0 ? m_getTileDataFunc(m_selectedTileIdx) : Tileset.k_TileData_Empty;
                brushTileData = DoTileDataPropertiesLayout(brushTileData, Tileset, AllowBrushSelection);
                if (m_selectedTileIdx >= 0)
                {
                    m_setTileDataFunc(m_selectedTileIdx, brushTileData);
                }
            }
            GUILayout.EndHorizontal();

            if (e.type == EventType.Repaint)
            {
                m_tileSelectionRect = GUILayoutUtility.GetLastRect();
            }

            m_displayAutocompleteBtn = e.type == EventType.Layout ? !hasEmptyTiles && m_tileIdOff != 0 : m_displayAutocompleteBtn;
            if ( size > 1 && m_displayAutocompleteBtn && GUILayout.Button("Autocomplete relative to last change"))
            {
                Undo.RecordObject(m_target, "MultipleTileChanged");
                for (int tileIdx = 0; tileIdx < size; ++tileIdx)
                {
                    if (tileIdx != m_tileIdOffSkipIdx)
                    {
                        int brushTileId = (int)(m_getTileDataFunc(tileIdx) & Tileset.k_TileDataMask_TileId);
                        brushTileId += m_tileIdOff;
                        if (brushTileId < 0 || brushTileId >= m_tileset.Tiles.Count)
                        {
                            m_setTileDataFunc(tileIdx, Tileset.k_TileData_Empty);
                        }
                        else
                        {
                            uint tileData = m_getTileDataFunc(tileIdx);
                            tileData &= ~Tileset.k_TileDataMask_TileId;
                            tileData |= (uint)(brushTileId & Tileset.k_TileDataMask_TileId);
                            m_setTileDataFunc(tileIdx, tileData);
                        }
                    }
                }
                m_tileIdOff = 0;
                EditorUtility.SetDirty(m_target);
            }
            if (Tileset.TileSelection != null && Tileset.TileSelection.selectionData.Count == m_width * m_height && Tileset.TileSelection.rowLength == m_width)
            {
                if (GUILayout.Button("Autocomplete from selection"))
                {
                    Undo.RecordObject(m_target, "MultipleTileChanged");
                    for (int tileIdx = 0; tileIdx < size; ++tileIdx)
                    {
                        int selectionIdx = (tileIdx % m_width) + (m_height - 1 - tileIdx / m_width) * m_width;
                        int brushTileId = (int)(Tileset.TileSelection.selectionData[selectionIdx] & Tileset.k_TileDataMask_TileId);
                        m_setTileDataFunc(tileIdx, (uint)(brushTileId & Tileset.k_TileDataMask_TileId));
                    }
                    m_tileIdOff = 0;
                    EditorUtility.SetDirty(m_target);
                }
            }

            if (ShowHelpBox)
            {
                EditorGUILayout.HelpBox(HelpBoxText, MessageType.Info);
            }
        }

        public static uint DoTileDataPropertiesLayout(uint tileData, Tileset tileset, bool displayBrush = true)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
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
                        if (brush && brush.Tileset != tileset)
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

                if (GUILayout.Button("Reset"))
                {
                    tileData = Tileset.k_TileData_Empty;
                }

                EditorGUIUtility.labelWidth = 0;
                GUI.enabled = true;
            }
            EditorGUILayout.EndVertical();
            return tileData;
        }
    }
}