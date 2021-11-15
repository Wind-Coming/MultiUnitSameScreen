using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System;
using System.Linq;
using UnityEditorInternal;

namespace CreativeSpore.SuperTilemapEditor
{
    [Serializable]
    public class TilesetControl
    {

        private class Styles
        {
            static Styles s_instance;
            public static Styles Instance{get{ if (s_instance == null) s_instance = new Styles(); return s_instance;} }
            public GUIStyle scrollStyle = new GUIStyle("ScrollView");
            public GUIStyle customBox = new GUIStyle("Box");
        }

        public Tileset Tileset
        {
            get { return m_tileset; }
            set
            {
                if (m_tileset != value)
                {
                    m_updateScrollPos = false;
                    m_tileset = value;
                }
            }
        }

        private Tileset m_tileset;
        private bool m_displayBrushReordList = false;
        private Vector2 m_tileScrollSpeed = Vector2.zero;
        private int m_visibleTileCount = 1;
        private List<uint> m_visibleTileList = new List<uint>();
        private Rect m_rTileScrollSize;
        private Rect m_rTileScrollArea;
        private Rect m_rBrushScrollArea;
        private float m_lastTime;
        private float m_timeDt;
        private Vector2 m_lastTileScrollMousePos;
        private Color m_prevBgColor;
        private TilesetBrush m_selectBrushInInspector;

        #region SharedTilesetData
        private class SharedTilesetData
        {
            public Tileset tileset;
            public ReorderableList tileViewList;
            public ReorderableList brushList;
            public Vector2 tilesScrollPos;
            public Vector2 brushesScrollPos;
            public int tileViewRowLength = 1;
            public KeyValuePair<int, Rect> startDragTileIdxRect;
            public KeyValuePair<int, Rect> endDragTileIdxRect;
            public KeyValuePair<int, Rect> pointedTileIdxRect;
        }
        private SharedTilesetData m_sharedData;

        private Dictionary<Tileset, SharedTilesetData> m_dicTilesetSharedData = new Dictionary<Tileset, SharedTilesetData>();
        private SharedTilesetData GetSharedTilesetData(Tileset tileset)
        {
            SharedTilesetData sharedData = null;
            if (tileset)
            {
                if (!m_dicTilesetSharedData.TryGetValue(tileset, out sharedData))
                {
                    sharedData = new SharedTilesetData();
                    sharedData.tileset = tileset;
                    m_dicTilesetSharedData[tileset] = sharedData;
                }
            }
            return sharedData;
        }
        #endregion

        private bool m_updateScrollPos = false;
        private MouseDblClick m_dblClick = new MouseDblClick();
        public bool Display()
        {
            //NOTE: for performance, the brush animations has been disabled. Set needsRepaint to true to enable brush animation in the inspector.
            bool needsRepaint = false;
            AssetPreview.SetPreviewTextureCacheSize(256); //FIX clickeing issues when displaying multiple prefab previews (when a tile has a prefab attached
            Event e = Event.current;
            m_dblClick.Update();

            //FIX: when a tileset is changed, the layout change and during some frames, the BeginScrollView could return wrong values
            // This will make sure the scroll position is updated after mouse is over the control
            if (e.isMouse || e.type == EventType.ScrollWheel)
            {
                m_updateScrollPos = true;
            }

            // This way a gui exception is avoided
            if( e.type == EventType.Layout && m_selectBrushInInspector != null)
            {
                Selection.activeObject = m_selectBrushInInspector;
                m_selectBrushInInspector = null;
            }

            if (m_lastTime == 0f)
            {
                m_lastTime = Time.realtimeSinceStartup;
            }
            m_timeDt = Time.realtimeSinceStartup - m_lastTime;
            m_lastTime = Time.realtimeSinceStartup;

            if (Tileset == null)
            {
                EditorGUILayout.HelpBox("There is no tileset selected", MessageType.Info);
                return false;
            }
            else if (Tileset.AtlasTexture == null)
            {
                EditorGUILayout.HelpBox("There is no atlas texture set", MessageType.Info);
                return false;
            }
            else if (Tileset.Tiles.Count == 0)
            {
                EditorGUILayout.HelpBox("There are no tiles to show in the current tileset", MessageType.Info);
                return false;
            }            

            m_sharedData = GetSharedTilesetData(Tileset);

            float visualTilePadding = 1;
            bool isLeftMouseReleased = e.type == EventType.MouseUp && e.button == 0;
            bool isRightMouseReleased = e.type == EventType.MouseUp && e.button == 1;
            bool isInsideTileScrollArea = e.isMouse && m_rTileScrollArea.Contains(e.mousePosition);
            bool isInsideBrushScrollArea = e.isMouse && m_rBrushScrollArea.Contains(e.mousePosition);

            // Create TileView ReorderableList
            if (m_sharedData.tileViewList == null || m_sharedData.tileViewList.list != Tileset.TileViews)
            {               
                m_sharedData.tileViewList = TilesetEditor.CreateTileViewReorderableList(Tileset);
                m_sharedData.tileViewList.onSelectCallback += (ReorderableList list) =>
                {
                    /* NOTE: this will select the tileview for the painting brush. Commented just in case.
                    if(list.index >= 0)
                    {
                        TileSelection tileSelection = Tileset.TileViews[list.index].tileSelection.Clone();
                        tileSelection.FlipVertical();
                        Tileset.TileSelection = tileSelection;
                    }
                    else*/
                        RemoveTileSelection();
                };
                m_sharedData.tileViewList.onRemoveCallback += (ReorderableList list) =>
                {
                    RemoveTileSelection();
                };
            }

            // Draw TileView ReorderableList
            {
                GUI.color = Color.cyan;
                GUILayout.BeginVertical(Styles.Instance.customBox);
                m_sharedData.tileViewList.index = Mathf.Clamp(m_sharedData.tileViewList.index, -1, Tileset.TileViews.Count - 1);
                m_sharedData.tileViewList.DoLayoutList();
                Rect rList = GUILayoutUtility.GetLastRect();
                if (e.isMouse && !rList.Contains(e.mousePosition))
                {
                    m_sharedData.tileViewList.ReleaseKeyboardFocus();
                }
                GUILayout.EndVertical();
                GUI.color = Color.white;
            }
            TileView tileView = m_sharedData.tileViewList != null && m_sharedData.tileViewList.index >= 0 ? Tileset.TileViews[m_sharedData.tileViewList.index] : null;

            if (tileView == null)
            {
                // NOTE: this is a fast fix to allow changing the TileRowLength. At some point, the Tile Palette was based on the Tileset.Width and this cannot be modified by design.
                // So I have changed this to set the Width with the TileRowLength value

                //Tileset.TileRowLength = Mathf.Clamp(EditorGUILayout.IntField("TileRowLength", Tileset.TileRowLength), 1, Tileset.Width);
                EditorGUI.BeginChangeCheck();
                Tileset.TileRowLength = Mathf.Clamp(EditorGUILayout.IntField("TileRowLength", Tileset.TileRowLength), 1, Tileset.Tiles.Count);
                if(EditorGUI.EndChangeCheck())
                {
                    Tileset.Width = Tileset.TileRowLength;
                }
            }

            
            List<string> viewNameList = new List<string>() { "(All)" };
            viewNameList.AddRange(Tileset.TileViews.Select(x => x.name));
            string[] tileViewNames = viewNameList.ToArray();
            int[] tileViewValues = Enumerable.Range(-1, Tileset.TileViews.Count + 1).ToArray();
            EditorGUI.BeginChangeCheck();
            m_sharedData.tileViewList.index = EditorGUILayout.IntPopup("Tileset View", m_sharedData.tileViewList.index, tileViewNames, tileViewValues);
            if (EditorGUI.EndChangeCheck())
            {
                RemoveTileSelection();
            }

            // Draw Background Color Selector
            Tileset.BackgroundColor = EditorGUILayout.ColorField("Background Color", Tileset.BackgroundColor);
            if (m_prevBgColor != Tileset.BackgroundColor || Styles.Instance.scrollStyle.normal.background == null)
            {
                m_prevBgColor = Tileset.BackgroundColor;
                if (Styles.Instance.scrollStyle.normal.background == null) Styles.Instance.scrollStyle.normal.background = new Texture2D(1, 1) { hideFlags = HideFlags.DontSave };
                Styles.Instance.scrollStyle.normal.background.SetPixel(0, 0, Tileset.BackgroundColor);
                Styles.Instance.scrollStyle.normal.background.Apply();
            }
            //---

            // Draw Zoom Selector
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(EditorGUIUtility.FindTexture("ViewToolZoom"), GUILayout.Width(35f));
            float visualTileZoom = EditorGUILayout.Slider(Tileset.VisualTileSize.x / Tileset.TilePxSize.x, 0.25f, 4f);
            Tileset.VisualTileSize = visualTileZoom * Tileset.TilePxSize;
            if (GUILayout.Button("Reset", GUILayout.Width(50f))) Tileset.VisualTileSize = new Vector2(32f * Tileset.TilePxSize.x / Tileset.TilePxSize.y, 32f);
            EditorGUILayout.EndHorizontal();
            //---

            string sTileIdLabel = Tileset.SelectedTileId != Tileset.k_TileId_Empty? " (id:" + Tileset.SelectedTileId + ")" : "";
            EditorGUILayout.LabelField("Tile Palette" + sTileIdLabel, EditorStyles.boldLabel);

            m_sharedData.tileViewRowLength = tileView != null ? tileView.tileSelection.rowLength : Tileset.TileRowLength;
            // keeps values safe
            m_sharedData.tileViewRowLength = Mathf.Max(1, m_sharedData.tileViewRowLength);

            float tileAreaWidth = m_sharedData.tileViewRowLength * (Tileset.VisualTileSize.x + visualTilePadding) + 4f;
            float tileAreaHeight = (Tileset.VisualTileSize.y + visualTilePadding) * (1 + (m_visibleTileCount - 1) / m_sharedData.tileViewRowLength) + 4f;
            
            //float minTileScrollHeight = (Tileset.VisualTileSize.y + visualTilePadding) * 6f;// NOTE: GUILayout.MinHeight is not working with BeginScrollView            
            Vector2 tilesScrollPos = EditorGUILayout.BeginScrollView(m_sharedData.tilesScrollPos, Styles.Instance.scrollStyle/*, GUILayout.MinHeight(minTileScrollHeight)*/);
            if(m_updateScrollPos) m_sharedData.tilesScrollPos = tilesScrollPos;
            {
                // Scroll Moving Drag
                if (e.type == EventType.MouseDrag && (e.button == 1 || e.button == 2))
                {
                    m_sharedData.tilesScrollPos -= e.delta;
                }
                else
                {
                    DoAutoScroll();
                }

                if (e.isMouse)
                {
                    m_lastTileScrollMousePos = e.mousePosition;
                }
                if (Tileset.Tiles != null)
                {
                    GUILayoutUtility.GetRect(tileAreaWidth, tileAreaHeight);
                    int lastVisibleTileCount = m_visibleTileCount;
                    m_visibleTileCount = 0;
                    List<uint> visibleTileList = new List<uint>();
                    int tileViewWidth = tileView != null ? tileView.tileSelection.rowLength : Tileset.Width;
                    int tileViewHeight = tileView != null ? ((tileView.tileSelection.selectionData.Count - 1) / tileView.tileSelection.rowLength) + 1 : Tileset.Height;
                    int totalCount = ((((tileViewWidth - 1) / m_sharedData.tileViewRowLength) + 1) * m_sharedData.tileViewRowLength) * tileViewHeight;
                    int tileIdOffset = 0;
                    for (int i = 0; i < totalCount; ++i)
                    {
                        int tileId = GetTileIdFromIdx(i, m_sharedData.tileViewRowLength, tileViewWidth, tileViewHeight) + tileIdOffset;
                        uint tileData = (uint)tileId;
                        if (tileView != null && tileId != Tileset.k_TileId_Empty)
                        {
                            int viewIdx = tileId - tileIdOffset;
                            if (viewIdx >= tileView.tileSelection.selectionData.Count)
                            {
                                tileData = Tileset.k_TileData_Empty;
                            }
                            else
                            {
                                tileData = tileView.tileSelection.selectionData[viewIdx];
                            }
                            tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
                        }
                        Tile tile = Tileset.GetTile(tileId);
                        while (tile != null && tile.uv == default(Rect)) // skip invalid tiles
                        {
                            tile = Tileset.GetTile(++tileId);
                            tileData = (uint)tileId;
                            tileIdOffset = tileId;
                        }
                        visibleTileList.Add(tileData);

                        int tx = m_visibleTileCount % m_sharedData.tileViewRowLength;
                        int ty = m_visibleTileCount / m_sharedData.tileViewRowLength;
                        Rect rVisualTile = new Rect(2 + tx * (Tileset.VisualTileSize.x + visualTilePadding), 2 + ty * (Tileset.VisualTileSize.y + visualTilePadding), Tileset.VisualTileSize.x, Tileset.VisualTileSize.y);

                        // Optimization, skipping not visible tiles
                        Rect rLocalVisualTile = rVisualTile; rLocalVisualTile.position -= m_sharedData.tilesScrollPos;
                        if (!rLocalVisualTile.Overlaps(m_rTileScrollSize))
                        {
                            ; // Do Nothing
                        }
                        else
                        //---
                        {
                            // Draw Tile
                            if (tile == null)
                            {
                                HandlesEx.DrawRectWithOutline(rVisualTile, new Color(0f, 0f, 0f, 0.2f), new Color(0f, 0f, 0f, 0.2f));
                            }
                            else
                            {
                                HandlesEx.DrawRectWithOutline(rVisualTile, new Color(0f, 0f, 0f, 0.1f), new Color(0f, 0f, 0f, 0.1f));
                                TilesetEditor.DoGUIDrawTileFromTileData(rVisualTile, tileData, Tileset);                                
                            }

                            Rect rTileRect = new Rect(2 + tx * (Tileset.VisualTileSize.x + visualTilePadding), 2 + ty * (Tileset.VisualTileSize.y + visualTilePadding), (Tileset.VisualTileSize.x + visualTilePadding), (Tileset.VisualTileSize.y + visualTilePadding));
                            if (rVisualTile.Contains(e.mousePosition))
                            {
                                if (e.type == EventType.MouseDrag && e.button == 0)
                                    m_sharedData.pointedTileIdxRect = new KeyValuePair<int, Rect>(m_visibleTileCount, rTileRect);
                                else if (e.type == EventType.MouseDown && e.button == 0)
                                    m_sharedData.startDragTileIdxRect = m_sharedData.pointedTileIdxRect = m_sharedData.endDragTileIdxRect = new KeyValuePair<int, Rect>(m_visibleTileCount, rTileRect);
                                else if (e.type == EventType.MouseUp && e.button == 0)
                                {
                                    m_sharedData.endDragTileIdxRect = new KeyValuePair<int, Rect>(m_visibleTileCount, rTileRect);
                                    DoSetTileSelection();
                                }
                            }

                            if ((isLeftMouseReleased || isRightMouseReleased) && isInsideTileScrollArea && rVisualTile.Contains(e.mousePosition)
                                && (m_sharedData.startDragTileIdxRect.Key == m_sharedData.endDragTileIdxRect.Key) // and there is not dragging selection
                                && m_rTileScrollSize.Contains(e.mousePosition - m_sharedData.tilesScrollPos))// and it's inside the scroll area
                            {
                                Tileset.SelectedTileId = tileId;

                                //Give focus to SceneView to get key events
                                FocusSceneView();

                                if(isRightMouseReleased)
                                {
                                    TilePropertiesWindow.Show(Tileset);
                                }
                            }
                            else if (tile != null && Tileset.SelectedTileId == tileId)
                            {
                                HandlesEx.DrawRectWithOutline(rTileRect, new Color(0f, 0f, 0f, 0.1f), new Color(1f, 1f, 0f, 1f));
                            }                            
                        }

                        ++m_visibleTileCount;
                    }
                    m_visibleTileList = visibleTileList;
                    if (lastVisibleTileCount != m_visibleTileCount)
                        needsRepaint |= true;

                    // Draw selection rect
                    if (m_sharedData.startDragTileIdxRect.Key != m_sharedData.pointedTileIdxRect.Key /*&& m_startDragTileIdxRect.Key == m_endDragTileIdxRect.Key*/)
                    {
                        Rect rSelection = new Rect(m_sharedData.startDragTileIdxRect.Value.center, m_sharedData.pointedTileIdxRect.Value.center - m_sharedData.startDragTileIdxRect.Value.center);
                        rSelection.Set(Mathf.Min(rSelection.xMin, rSelection.xMax), Mathf.Min(rSelection.yMin, rSelection.yMax), Mathf.Abs(rSelection.width), Mathf.Abs(rSelection.height));
                        rSelection.xMin -= m_sharedData.startDragTileIdxRect.Value.width / 2;
                        rSelection.xMax += m_sharedData.startDragTileIdxRect.Value.width / 2;
                        rSelection.yMin -= m_sharedData.startDragTileIdxRect.Value.height / 2;
                        rSelection.yMax += m_sharedData.startDragTileIdxRect.Value.height / 2;
                        HandlesEx.DrawRectWithOutline(rSelection, new Color(0f, 0f, 0f, 0.1f), new Color(1f, 1f, 1f, 1f));
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            if (e.type == EventType.Repaint)
            {
                m_rTileScrollArea = GUILayoutUtility.GetLastRect();
                Rect lastTileScrollSize = m_rTileScrollSize;
                m_rTileScrollSize = m_rTileScrollArea;
                m_rTileScrollSize.position = Vector2.zero; // reset position to the Contains and Overlaps inside the tile scroll view without repositioning the position of local positions
                if (tileAreaWidth > m_rTileScrollSize.width)
                    m_rTileScrollSize.height -= GUI.skin.verticalScrollbar.fixedWidth;
                if (tileAreaHeight > m_rTileScrollSize.height)
                    m_rTileScrollSize.width -= GUI.skin.verticalScrollbar.fixedWidth;
                if (lastTileScrollSize != m_rTileScrollSize)
                    needsRepaint |= true;
            }

            EditorGUILayout.BeginHorizontal();
            string sBrushIdLabel = Tileset.SelectedBrushId > 0 ? " (id:" + Tileset.SelectedBrushId + ")" : "";
            EditorGUILayout.LabelField("Brush Palette" + sBrushIdLabel, EditorStyles.boldLabel);
            m_displayBrushReordList = EditorUtils.DoToggleButton("Display List", m_displayBrushReordList);
            EditorGUILayout.EndHorizontal();

            string[] brushTypeArray = Tileset.GetBrushTypeArray();
            if (brushTypeArray != null && brushTypeArray.Length > 0)
                Tileset.BrushTypeMask = EditorGUILayout.MaskField("Brush Mask", Tileset.BrushTypeMask, brushTypeArray);

            int tileRowLength = (int)(m_rTileScrollSize.width / (Tileset.VisualTileSize.x + visualTilePadding));
            if (tileRowLength <= 0) tileRowLength = 1;
            float fBrushesScrollMaxHeight = Screen.height / 4;
            //commented because m_rTileScrollSize.width.height was changed to Screen.height;  fBrushesScrollMaxHeight -= fBrushesScrollMaxHeight % 2; // sometimes because size of tile scroll affects size of brush scroll, the height is dancing between +-1, so this is always taking the pair value
            float fBrushesScrollHeight = Mathf.Min(fBrushesScrollMaxHeight, 4 + (Tileset.VisualTileSize.y + visualTilePadding) * (1 + (Tileset.Brushes.Count / tileRowLength)));
            EditorGUILayout.BeginVertical(GUILayout.MinHeight(fBrushesScrollHeight));
            if (m_displayBrushReordList)
            {
                DisplayBrushReorderableList();
            }
            else
            {
                bool refreshBrushes = false;
                Vector2 brushesScrollPos = EditorGUILayout.BeginScrollView(m_sharedData.brushesScrollPos, Styles.Instance.scrollStyle);
                if (m_updateScrollPos) m_sharedData.brushesScrollPos = brushesScrollPos;
                {
                    Rect rScrollView = new Rect(0, 0, m_rTileScrollSize.width, 0);
                    tileRowLength = Mathf.Clamp((int)rScrollView.width / (int)(Tileset.VisualTileSize.x + visualTilePadding), 1, tileRowLength);
                    if (Tileset.Brushes != null)
                    {
                        GUILayout.Space((Tileset.VisualTileSize.y + visualTilePadding) * (1 + (Tileset.Brushes.Count - 1) / tileRowLength));
                        for (int i = 0, idx = 0; i < Tileset.Brushes.Count; ++i, ++idx)
                        {
                            Tileset.BrushContainer brushCont = Tileset.Brushes[i];
                            if (brushCont.BrushAsset == null || brushCont.BrushAsset.Tileset != Tileset)
                            {
                                refreshBrushes = true;
                                continue;
                            }
                            if (!brushCont.BrushAsset.ShowInPalette || !Tileset.IsBrushVisibleByTypeMask(brushCont.BrushAsset))
                            {
                                --idx;
                                continue;
                            }

                            int tx = idx % tileRowLength;
                            int ty = idx / tileRowLength;
                            Rect rVisualTile = new Rect(2 + tx * (Tileset.VisualTileSize.x + visualTilePadding), 2 + ty * (Tileset.VisualTileSize.y + visualTilePadding), Tileset.VisualTileSize.x, Tileset.VisualTileSize.y);
                            //Fix Missing Tileset reference
                            if(brushCont.BrushAsset.Tileset == null)
                            {
                                Debug.LogWarning("Fixed missing tileset reference in brush " + brushCont.BrushAsset.name + " Id("+brushCont.Id+")");
                                brushCont.BrushAsset.Tileset = Tileset;
                            }
                            uint tileData = Tileset.k_TileData_Empty;
                            if (brushCont.BrushAsset.IsAnimated())
                            {
                                tileData = brushCont.BrushAsset.GetAnimTileData();
                            }
                            else
                            {
                                tileData = brushCont.BrushAsset.PreviewTileData();
                            }
                            TilesetEditor.DoGUIDrawTileFromTileData(rVisualTile, tileData, Tileset, brushCont.BrushAsset.GetAnimUV());
                            if ((isLeftMouseReleased || isRightMouseReleased || m_dblClick.IsDblClick) && isInsideBrushScrollArea && rVisualTile.Contains(Event.current.mousePosition))
                            {
                                Tileset.SelectedBrushId = brushCont.Id;
                                RemoveTileSelection();
                                if (m_dblClick.IsDblClick)
                                {
                                    EditorGUIUtility.PingObject(brushCont.BrushAsset);
                                    m_selectBrushInInspector = brushCont.BrushAsset;
                                }
                                if (isRightMouseReleased)
                                {
                                    TilePropertiesWindow.Show(Tileset);
                                }
                            }
                            else if (Tileset.SelectedBrushId == brushCont.Id)
                            {
                                Rect rSelection = new Rect(2 + tx * (Tileset.VisualTileSize.x + visualTilePadding), 2 + ty * (Tileset.VisualTileSize.y + visualTilePadding), (Tileset.VisualTileSize.x + visualTilePadding), (Tileset.VisualTileSize.y + visualTilePadding));
                                HandlesEx.DrawRectWithOutline(rSelection, new Color(0f, 0f, 0f, 0.1f), new Color(1f, 1f, 0f, 1f));
                            }
                        }
                    }

                    if (refreshBrushes)
                    {
                        Tileset.RemoveInvalidBrushes();
                        Tileset.UpdateBrushTypeArray();
                    }
                }
                EditorGUILayout.EndScrollView();
                if (e.type == EventType.Repaint)
                {
                    m_rBrushScrollArea = GUILayoutUtility.GetLastRect();
                }
            }
            EditorGUILayout.EndVertical();
            
            if (GUILayout.Button("Import all brushes found in the project"))
            {
                TilesetEditor.AddAllBrushesFoundInTheProject(Tileset);
                EditorUtility.SetDirty(Tileset);
            }

            needsRepaint |= Event.current.isMouse;
            return needsRepaint;
        }

        private void DisplayBrushReorderableList()
        {
            Event e = Event.current;
            if (m_sharedData.brushList == null || m_sharedData.brushList.list != Tileset.Brushes)
            {
                if (e.type != EventType.Layout)
                {
                    m_sharedData.brushList = TilesetEditor.CreateBrushReorderableList(Tileset);
                    m_sharedData.brushList.onSelectCallback += (ReorderableList list) =>
                    {
                        Tileset.BrushContainer brushCont = Tileset.Brushes[list.index];
                        Tileset.SelectedBrushId = brushCont.Id;
                        RemoveTileSelection();
                        if (m_dblClick.IsDblClick)
                        {
                            EditorGUIUtility.PingObject(brushCont.BrushAsset);
                            m_selectBrushInInspector = brushCont.BrushAsset;
                        }
                    };
                }
            }
            else
            {
                GUILayout.BeginVertical(Styles.Instance.customBox);
                m_sharedData.brushList.index = Tileset.Brushes.FindIndex(x => x.Id == Tileset.SelectedBrushId);
                m_sharedData.brushList.index = Mathf.Clamp(m_sharedData.brushList.index, -1, Tileset.Brushes.Count - 1);
                m_sharedData.brushList.elementHeight = Tileset.VisualTileSize.y + 10f;
                m_sharedData.brushList.DoLayoutList();
                Rect rList = GUILayoutUtility.GetLastRect();
                if (e.isMouse && !rList.Contains(e.mousePosition))
                {
                    m_sharedData.brushList.ReleaseKeyboardFocus();
                }
                GUILayout.EndVertical();
            }
        }

        private void FocusSceneView()
        {
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.Focus();
            else if (SceneView.sceneViews.Count > 0)
                ((SceneView)SceneView.sceneViews[0]).Focus();
        }

        private void DoAutoScroll()
        {
            Event e = Event.current;
            if (m_rTileScrollSize.Contains(e.mousePosition - m_sharedData.tilesScrollPos))
            {
                if (e.type == EventType.MouseDrag && e.button == 0)
                {
                    Vector2 mouseMoveDisp = e.mousePosition - m_lastTileScrollMousePos;
                    float autoScrollDist = 40;
                    float autoScrollSpeed = 500;
                    Vector2 mousePosition = e.mousePosition - m_sharedData.tilesScrollPos;
                    float leftFactorX = mouseMoveDisp.x < 0f ? 1f - Mathf.Pow(Mathf.Clamp01(mousePosition.x / autoScrollDist), 2) : 0f;
                    float rightFactorX = mouseMoveDisp.x > 0f ? 1f - Mathf.Pow(Mathf.Clamp01((m_rTileScrollSize.width - mousePosition.x) / autoScrollDist), 2) : 0f;
                    float topFactorY = mouseMoveDisp.y < 0f ? 1f - Mathf.Pow(Mathf.Clamp01(mousePosition.y / autoScrollDist), 2) : 0f;
                    float bottomFactorY = mouseMoveDisp.y > 0f ? 1f - Mathf.Pow(Mathf.Clamp01((m_rTileScrollSize.height - mousePosition.y) / autoScrollDist), 2) : 0f;
                    m_tileScrollSpeed = autoScrollSpeed * new Vector2((-leftFactorX + rightFactorX), (-topFactorY + bottomFactorY));
                }
                else if (e.type == EventType.MouseUp)
                {
                    m_tileScrollSpeed = Vector2.zero;
                }
            }
            else if (e.isMouse)
            {
                m_tileScrollSpeed = Vector2.zero;
            }

            m_sharedData.tilesScrollPos += m_timeDt * m_tileScrollSpeed;
        }

        private int GetTileIdFromIdx(int idx, int rowLength, int width, int height)
        {
            int cWxH = rowLength * height;
            int n = idx % cWxH;
            if (((idx / cWxH) * rowLength) + (idx % rowLength) >= width)
            {
                return Tileset.k_TileId_Empty;
            }
            return (n / rowLength) * width + idx % rowLength + (idx / cWxH) * rowLength;
        }

        private void RemoveTileSelection()
        {
            m_sharedData.pointedTileIdxRect = m_sharedData.startDragTileIdxRect = m_sharedData.endDragTileIdxRect;
            Tileset.TileSelection = null;
        }

        private void DoSetTileSelection()
        {
            if (m_sharedData.startDragTileIdxRect.Key != m_sharedData.endDragTileIdxRect.Key)
            {
                int tx_start = Mathf.Min(m_sharedData.startDragTileIdxRect.Key % m_sharedData.tileViewRowLength, m_sharedData.endDragTileIdxRect.Key % m_sharedData.tileViewRowLength);
                int ty_start = Mathf.Min(m_sharedData.startDragTileIdxRect.Key / m_sharedData.tileViewRowLength, m_sharedData.endDragTileIdxRect.Key / m_sharedData.tileViewRowLength);
                int tx_end = Mathf.Max(m_sharedData.startDragTileIdxRect.Key % m_sharedData.tileViewRowLength, m_sharedData.endDragTileIdxRect.Key % m_sharedData.tileViewRowLength);
                int ty_end = Mathf.Max(m_sharedData.startDragTileIdxRect.Key / m_sharedData.tileViewRowLength, m_sharedData.endDragTileIdxRect.Key / m_sharedData.tileViewRowLength);
                List<uint> selectionData = new List<uint>();
                int tileIdx = 0;
                for (int ty = ty_end; ty >= ty_start; --ty) // NOTE: this goes from bottom to top to fit the tilemap coordinate system
                {
                    for (int tx = tx_start; tx <= tx_end; ++tx, ++tileIdx)
                    {
                        int visibleTileIdx = ty * m_sharedData.tileViewRowLength + tx;
                        uint tileData = m_visibleTileList[visibleTileIdx];
                        selectionData.Add(tileData);
                    }
                }
                Tileset.TileSelection = new TileSelection(selectionData, tx_end - tx_start + 1);
                FocusSceneView(); //Give focus to SceneView to get key events
            }
        }
    }
}
