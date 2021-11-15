using UnityEngine;
using System.Collections;
using UnityEditor;
using System;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;

namespace CreativeSpore.SuperTilemapEditor
{

    [Serializable]
    public class TilePropertiesControl
    {
        private class Styles
        {
            static Styles s_instance;
            public static Styles Instance 
            {
                get 
                {
                    if (s_instance == null)
                        s_instance = new Styles();
                    return s_instance;
                }
            }

            public GUIStyle colliderBgStyle = new GUIStyle("ScrollView");
            public GUIStyle collVertexHandleStyle = new GUIStyle("U2D.dragDot")
            {
                normal = { textColor = Color.cyan},
                contentOffset = Vector2.one * 8f,
            };
            public GUIStyle vertexCoordStyle = new GUIStyle("Label")
            {
                normal = { textColor = Color.cyan},
                fontStyle = FontStyle.Bold,
            };                            
        }

        public enum eEditMode
        {
            Parameters,
            Collider,
            Prefab,
            Autotiling
        }

        public Tileset Tileset;

        [SerializeField]
        private eEditMode m_editMode;

        private Vector2 m_scrollPos = Vector2.zero;
        public void Display()
        {
            if (Tileset == null)
            {
                EditorGUILayout.HelpBox("There is no tileset selected", MessageType.Info);
                return;
            }

            if (Tileset.AtlasTexture == null)
            {
                EditorGUILayout.HelpBox("There is no atlas texture set", MessageType.Info);
                return;
            }

            if (Tileset.SelectedTileId == Tileset.k_TileId_Empty && Tileset.TileSelection == null && Tileset.SelectedBrushId == Tileset.k_BrushId_Default)
            {
                EditorGUILayout.HelpBox("There is no tile selected", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical();
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, GUILayout.Width(EditorGUIUtility.currentViewWidth));
            {
                string[] editModeNames = System.Enum.GetNames(typeof(eEditMode));
                m_editMode = (eEditMode)GUILayout.Toolbar((int)m_editMode, editModeNames);
                switch (m_editMode)
                {
                    case eEditMode.Collider: DisplayCollider(); break;
                    case eEditMode.Parameters: DisplayParameters(); break;
                    case eEditMode.Prefab: DisplayPrefab(); break;
                    case eEditMode.Autotiling: DisplayAutotiling(); break;
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();


            if (GUI.changed)
            {
                EditorUtility.SetDirty(Tileset);
            }
        }
        
        private void DisplayPrefab()
        {
            if (Tileset.SelectedBrushId != Tileset.k_BrushId_Default)
            {
                EditorGUILayout.LabelField("Brush tile editing not allowed", EditorStyles.boldLabel);
            }
            else
            {
                bool isMultiselection = Tileset.TileSelection != null;
                Tile selectedTile = isMultiselection ? Tileset.Tiles[(int)(Tileset.TileSelection.selectionData[0] & Tileset.k_TileDataMask_TileId)] : Tileset.SelectedTile;
                GUILayoutUtility.GetRect(1, 1, GUILayout.Width(Tileset.VisualTileSize.x), GUILayout.Height(Tileset.VisualTileSize.y));
                Rect tileUV = selectedTile.uv;
                GUI.color = Tileset.BackgroundColor;
                GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), EditorGUIUtility.whiteTexture, tileUV, true);
                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), Tileset.AtlasTexture, tileUV, true);

                if (isMultiselection)
                {
                    EditorGUILayout.LabelField("* Multi-selection Edition", EditorStyles.boldLabel);
                }
                EditorGUI.BeginChangeCheck();
                TilePrefabData prefabData = selectedTile.prefabData;
                float savedLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 80;
                prefabData.offset = EditorGUILayout.Vector3Field("Offset", prefabData.offset);
                prefabData.offsetMode = (TilePrefabData.eOffsetMode)EditorGUILayout.EnumPopup("Offset Mode", prefabData.offsetMode);
                prefabData.rotation = EditorGUILayout.Vector3Field("Rotation", prefabData.rotation);
                EditorGUI.BeginChangeCheck();
                GameObject prevPrefab = prefabData.prefab;
                prefabData.prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", prefabData.prefab, typeof(GameObject), false);
                bool isPrefabChanged = EditorGUI.EndChangeCheck();
                // Special case for 3D tiles where tilemap will be rotated over the plane XZ
                if(isPrefabChanged && !prevPrefab && prefabData.rotation == Vector3.zero && prefabData.prefab && prefabData.prefab.GetComponentInChildren<MeshRenderer>())
                {
                    prefabData.rotation = new Vector3(-90, 0, 0);
                    prefabData.showPrefabPreviewInTilePalette = true;                    
                }

                GUILayout.BeginHorizontal();
                Texture2D prefabPreview = AssetPreview.GetAssetPreview(selectedTile.prefabData.prefab);                
                GUILayout.Box(prefabPreview, prefabPreview != null? (GUIStyle)"Box" : GUIStyle.none);
                GUILayout.EndHorizontal();

                if( prefabData.prefab )
                {                    
                    EditorGUIUtility.labelWidth = 260;
                    if (prefabPreview)
                        prefabData.showPrefabPreviewInTilePalette = EditorGUILayout.Toggle("Display the prefab preview in the tile palette", prefabData.showPrefabPreviewInTilePalette);
                    EditorGUIUtility.labelWidth = savedLabelWidth;
                    prefabData.showTileWithPrefab = EditorGUILayout.Toggle("Show Tile With Prefab", prefabData.showTileWithPrefab);
                }

                if(EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Tileset, "Tile Prefab Data Changed");
                    if (isMultiselection)
                    {
                        for (int i = 0; i < Tileset.TileSelection.selectionData.Count; ++i)
                        {
                            Tile tile = Tileset.Tiles[(int)(Tileset.TileSelection.selectionData[i] & Tileset.k_TileDataMask_TileId)];
                            GameObject savedPrefab = tile.prefabData.prefab;
                            tile.prefabData = prefabData;
                            if (!isPrefabChanged)
                                tile.prefabData.prefab = savedPrefab;
                        }
                    }
                    else
                    {
                        selectedTile.prefabData = prefabData;
                    }
                    EditorUtility.SetDirty(Tileset);
                }                
            }
        }

        private TilesetBrushEditor m_brushEditor;
        private void DisplayAutotiling()
        {
            if (Tileset.SelectedBrushId != Tileset.k_BrushId_Default)
            {
                TilesetBrush brush = Tileset.FindBrush(Tileset.SelectedBrushId);
                if(brush)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayoutUtility.GetRect(1, 1, GUILayout.Width(Tileset.VisualTileSize.x), GUILayout.Height(Tileset.VisualTileSize.y));
                    TilesetEditor.DoGUIDrawTileFromTileData(GUILayoutUtility.GetLastRect(), brush.GetAnimTileData(), Tileset, brush.GetAnimUV());
                    GUILayout.Label("("+brush.name+":"+brush.GetType().Name+")", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();

                    if(!m_brushEditor || m_brushEditor.target != brush)
                        m_brushEditor = TilesetBrushEditor.CreateEditor(brush) as TilesetBrushEditor;
                    (m_brushEditor as TilesetBrushEditor).DoInspectorGUI();
                }
            }
            else
            {
                bool isMultiselection = Tileset.TileSelection != null;
                Tile selectedTile = isMultiselection ? Tileset.Tiles[(int)(Tileset.TileSelection.selectionData[0] & Tileset.k_TileDataMask_TileId)] : Tileset.SelectedTile;
                GUILayoutUtility.GetRect(1, 1, GUILayout.Width(Tileset.VisualTileSize.x), GUILayout.Height(Tileset.VisualTileSize.y));
                Rect tileUV = selectedTile.uv;
                GUI.color = Tileset.BackgroundColor;
                GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), EditorGUIUtility.whiteTexture, tileUV, true);
                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(GUILayoutUtility.GetLastRect(), Tileset.AtlasTexture, tileUV, true);

                if (isMultiselection)
                {
                    EditorGUILayout.LabelField("* Multi-selection Edition", EditorStyles.boldLabel);
                }
                EditorGUI.BeginChangeCheck();

                selectedTile.autilingGroup = TilesetEditor.DoGroupFieldLayout(Tileset, "Group", selectedTile.autilingGroup);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(Tileset, "Tile Autotiling Data Changed");
                    if (isMultiselection)
                    {
                        for (int i = 0; i < Tileset.TileSelection.selectionData.Count; ++i)
                        {
                            Tile tile = Tileset.Tiles[(int)(Tileset.TileSelection.selectionData[i] & Tileset.k_TileDataMask_TileId)];
                            tile.autilingGroup = selectedTile.autilingGroup;
                        }
                    }
                    EditorUtility.SetDirty(Tileset);
                }
            }
        }

        ReorderableList m_tileParameterList = null;
        UnityEngine.Object m_parameterListOwner = null;
        int m_prevSelectedTileId = Tileset.k_TileId_Empty;
        Vector2 m_tileParamScrollViewPos;
        private void DisplayParameters()
        {
            Event e = Event.current;
            TileSelection tileSelection = Tileset.TileSelection;
            if (tileSelection != null)
            {
                EditorGUILayout.LabelField("Multi-tile editing. Only merge allowed.", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("This will combine all the parameters found in the tile selection so if a parameter is missing in a tile, it will add the parameter, but if it has it already, it won't change the value.", MessageType.Info);
                if(GUILayout.Button("Merge Tile Parameters"))
                {
                    MergeTileParameters(tileSelection.selectionData);
                }
                EditorGUILayout.Space();
            }
            else
            {
                UnityEngine.Object prevOwner = m_parameterListOwner;
                if (Tileset.SelectedBrushId != Tileset.k_BrushId_Default)
                {
                    m_parameterListOwner = Tileset.FindBrush(Tileset.SelectedBrushId);
                }
                else
                {
                    m_parameterListOwner = Tileset;
                }

                if (m_parameterListOwner != null)
                {
                    if (prevOwner != m_parameterListOwner || Tileset.SelectedTileId != m_prevSelectedTileId || m_tileParameterList == null)
                    {
                        m_prevSelectedTileId = Tileset.SelectedTileId;
                        m_tileParameterList = CreateParameterReorderableList( m_parameterListOwner is Tileset ?
                            ((Tileset)m_parameterListOwner).SelectedTile.paramContainer :
                            ((TilesetBrush)m_parameterListOwner).Params);
                        m_tileParameterList.onChangedCallback += (ReorderableList list) =>
                        {
                            EditorUtility.SetDirty(m_parameterListOwner);
                        };
                    }

                    GUILayoutUtility.GetRect(1, 1, GUILayout.Width(Tileset.VisualTileSize.x), GUILayout.Height(Tileset.VisualTileSize.y));
                    uint tilePreviewData = m_parameterListOwner is Tileset ?
                        (uint)(((Tileset)m_parameterListOwner).SelectedTileId & Tileset.k_TileDataMask_TileId)
                        :
                        ((TilesetBrush)m_parameterListOwner).GetAnimTileData();
                    Rect customUV = m_parameterListOwner is Tileset ? default(Rect) : ((TilesetBrush)m_parameterListOwner).GetAnimUV();
                    GUI.color = Tileset.BackgroundColor;
                    GUI.DrawTexture(GUILayoutUtility.GetLastRect(), EditorGUIUtility.whiteTexture);
                    GUI.color = Color.white;
                    TilesetEditor.DoGUIDrawTileFromTileData(GUILayoutUtility.GetLastRect(), tilePreviewData, Tileset, customUV);

                    m_tileParamScrollViewPos = EditorGUILayout.BeginScrollView(m_tileParamScrollViewPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
                    EditorGUI.BeginChangeCheck();
                    m_tileParameterList.DoLayoutList();
                    Rect rList = GUILayoutUtility.GetLastRect();
                    if (e.type == EventType.MouseDown && !rList.Contains(e.mousePosition))
                    {
                        m_tileParameterList.ReleaseKeyboardFocus();
                        m_tileParameterList.index = -1;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(m_parameterListOwner);
                    }
                    EditorGUILayout.EndScrollView();
                }
            }
        }

        private void MergeTileParameters(List<uint> listOfTileData)
        {
            List<Parameter> fullParameterList = new List<Parameter>();
            foreach(uint tileData in listOfTileData)
            {
                ParameterContainer @params = TilemapUtils.GetParamsFromTileData(Tileset, tileData);
                foreach(Parameter param in @params.ParameterList)
                {
                    if(!fullParameterList.Exists(o => o.name.Equals(param.name)))
                    {
                        fullParameterList.Add(param);
                    }
                }
            }
            foreach (uint tileData in listOfTileData)
            {
                ParameterContainer @params = TilemapUtils.GetParamsFromTileData(Tileset, tileData);
                foreach (Parameter param in fullParameterList)
                {
                    if (!@params.ParameterList.Exists(o => o.name.Equals(param.name)))
                    {
                        @params.ParameterList.Add(param.Clone());
                    }
                }
            }
        }

        private static Vector2[] s_fullCollTileVertices = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
        private static TileColliderData s_copiedColliderData;
        private int m_activeVertexIdx = -1;
        private Rect m_rTile;
        private Vector2 m_mousePos;
        private bool m_isDragging = false;
        private Color m_prevBgColor;
        private Vector2[] m_savedVertexData;
        static private bool s_showHelp = false;
        private void DisplayCollider()
        {
            Event e = Event.current;

            if (Tileset.SelectedBrushId != Tileset.k_BrushId_Default)
            {
                EditorGUILayout.LabelField("Brush tile editing not allowed", EditorStyles.boldLabel);
                return;
            }

            bool isMultiselection = Tileset.TileSelection != null;
            bool saveChanges = false;
            Tile selectedTile = isMultiselection ? Tileset.Tiles[(int)(Tileset.TileSelection.selectionData[0] & Tileset.k_TileDataMask_TileId)] : Tileset.SelectedTile;

            if (e.type == EventType.MouseDown)
            {
                m_isDragging = true;
                if (selectedTile.collData.vertices != null)
                {
                    m_savedVertexData = new Vector2[selectedTile.collData.vertices.Length];
                    selectedTile.collData.vertices.CopyTo(m_savedVertexData, 0);
                }
            }
            else if (e.type == EventType.MouseUp)
            {
                m_isDragging = false;
            }

            Tileset.BackgroundColor = EditorGUILayout.ColorField(Tileset.BackgroundColor);
            if (m_prevBgColor != Tileset.BackgroundColor || Styles.Instance.colliderBgStyle.normal.background == null)
            {
                m_prevBgColor = Tileset.BackgroundColor;
                if (Styles.Instance.colliderBgStyle.normal.background == null) Styles.Instance.colliderBgStyle.normal.background = new Texture2D(1, 1) { hideFlags = HideFlags.DontSave };
                Styles.Instance.colliderBgStyle.normal.background.SetPixel(0, 0, Tileset.BackgroundColor);
                Styles.Instance.colliderBgStyle.normal.background.Apply();
            }

            float aspectRatio = Tileset.TilePxSize.x / Tileset.TilePxSize.y;
            float padding = 2; // pixel size of the border around the tile
            //Rect rCollArea = GUILayoutUtility.GetRect(1, 1, GUILayout.Width(EditorGUIUtility.currentViewWidth), GUILayout.Height(EditorGUIUtility.currentViewWidth / aspectRatio));
            Rect rCollArea = GUILayoutUtility.GetRect(1, 1, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.BeginGroup(rCollArea, Styles.Instance.colliderBgStyle);
            if (e.type == EventType.Repaint)
            {
                float pixelSize = rCollArea.width / (Tileset.TilePxSize.x + 2 * padding);
                m_mousePos = e.mousePosition;
                m_rTile = new Rect(padding * pixelSize, padding * pixelSize, rCollArea.width - 2 * padding * pixelSize, (rCollArea.width / aspectRatio) - 2 * padding * pixelSize);
                m_rTile.height = Mathf.Min(m_rTile.height, rCollArea.height - 2 * padding * pixelSize);
                m_rTile.width = (m_rTile.height * aspectRatio);
            }
            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.DrawTexture(m_rTile, EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;
            if (isMultiselection)
            {
                foreach (uint tileData in Tileset.TileSelection.selectionData)
                {
                    int tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
                    Tile tile = Tileset.GetTile(tileId);
                    if (tile != null)
                    {
                        GUI.color = new Color(1f, 1f, 1f, 1f / Tileset.TileSelection.selectionData.Count);
                        GUI.DrawTextureWithTexCoords(m_rTile, Tileset.AtlasTexture, tile.uv);
                    }
                }
                GUI.color = Color.white;
            }
            else
            {
                GUI.DrawTextureWithTexCoords(m_rTile, Tileset.AtlasTexture, selectedTile.uv);
            }

            Color savedHandleColor = Handles.color;
            if (selectedTile.collData.type != eTileCollider.None)
            {
                Vector2[] collVertices = selectedTile.collData.type == eTileCollider.Full ? s_fullCollTileVertices : selectedTile.collData.vertices;
                if ( collVertices == null || collVertices.Length == 0)
                {
                    collVertices = selectedTile.collData.vertices = new Vector2[s_fullCollTileVertices.Length];
                    Array.Copy(s_fullCollTileVertices, collVertices, s_fullCollTileVertices.Length);
                    EditorUtility.SetDirty(Tileset);
                }

                // Fix and snap positions
                for (int i = 0; i < collVertices.Length; ++i)
                {
                    Vector2 s0 = collVertices[i];
                    s0.x = Mathf.Clamp01(Mathf.Round(s0.x * Tileset.TilePxSize.x) / Tileset.TilePxSize.x);
                    s0.y = Mathf.Clamp01(Mathf.Round(s0.y * Tileset.TilePxSize.y) / Tileset.TilePxSize.y);
                    collVertices[i] = s0;
                }

                // Draw edges
                Vector3[] polyEdges = new Vector3[collVertices.Length + 1];
                for (int i = 0; i < collVertices.Length; ++i)
                {
                    Vector2 s0 = collVertices[i];
                    s0.x = m_rTile.x + m_rTile.width * s0.x; s0.y = m_rTile.yMax - m_rTile.height * s0.y;
                    Vector2 s1 = collVertices[(i + 1) % collVertices.Length];
                    s1.x = m_rTile.x + m_rTile.width * s1.x; s1.y = m_rTile.yMax - m_rTile.height * s1.y;

                    polyEdges[i] = s0;
                    polyEdges[i + 1] = s1;

                    Handles.color = Color.green;
                    Handles.DrawLine(s0, s1);
                    //Draw normals
                    {
                        Handles.color = Color.white;
                        Vector3 normPos = (s0 + s1) / 2f;
                        Handles.DrawLine(normPos, normPos + Vector3.Cross(s1 - s0, Vector3.forward).normalized * m_rTile.yMin);
                    }

                    Handles.color = savedHandleColor;
                }

                float pixelSize = m_rTile.width / Tileset.TilePxSize.x;
                if (selectedTile.collData.type == eTileCollider.Polygon)
                {
                    bool isAddingVertexOn = !m_isDragging && e.shift && m_activeVertexIdx == -1;
                    bool isRemovingVertexOn = !m_isDragging && ((Application.platform == RuntimePlatform.OSXEditor)? e.command : e.control) && collVertices.Length > 3;
                    if (isRemovingVertexOn && m_activeVertexIdx != -1 && e.type == EventType.MouseUp)
                    {
                        selectedTile.collData.vertices = new Vector2[collVertices.Length - 1];
                        for (int i = 0, j = 0; i < collVertices.Length; ++i)
                        {
                            if (i == m_activeVertexIdx) continue;
                            selectedTile.collData.vertices[j] = collVertices[i];
                            ++j;
                        }
                        collVertices = selectedTile.collData.vertices;
                        m_activeVertexIdx = -1;
                    }

                    float minDist = float.MaxValue;
                    if (!m_isDragging)
                    {
                        m_activeVertexIdx = -1;
                    }
                    for (int i = 0; i < collVertices.Length; ++i)
                    {
                        Vector2 s0 = collVertices[i];
                        s0.x = m_rTile.x + m_rTile.width * s0.x;
                        s0.y = m_rTile.yMax - m_rTile.height * s0.y;

                        if (m_isDragging)
                        {
                            if (i == m_activeVertexIdx)
                            {
                                s0 = m_mousePos;
                                s0 -= m_rTile.position;
                                s0.x = Mathf.Clamp(Mathf.Round(s0.x / pixelSize) * pixelSize, 0, m_rTile.width);
                                s0.y = Mathf.Clamp(Mathf.Round(s0.y / pixelSize) * pixelSize, 0, m_rTile.height);
                                s0 += m_rTile.position;
                                collVertices[i].x = Mathf.Clamp01((s0.x - m_rTile.x) / m_rTile.width);
                                collVertices[i].y = Mathf.Clamp01((s0.y - m_rTile.yMax) / -m_rTile.height);
                            }
                        }
                        else
                        {
                            float dist = Vector2.Distance(m_mousePos, s0);
                            if (dist <= minDist && dist < Styles.Instance.collVertexHandleStyle.normal.background.width)
                            {
                                minDist = dist;
                                m_activeVertexIdx = i;
                            }
                        }

                        if (e.type == EventType.Repaint)
                        {
                            if (i == m_activeVertexIdx)
                            {
                                Styles.Instance.vertexCoordStyle.fontSize = (int)(Mathf.Min( 10f, m_rTile.yMin / 2f));
                                GUI.Label(new Rect(0, 0, m_rTile.width, m_rTile.yMin), Vector2.Scale(collVertices[i], Tileset.TilePxSize).ToString(), Styles.Instance.vertexCoordStyle);
                            }
                            GUI.color = m_activeVertexIdx == i ? (isRemovingVertexOn ? Color.red : Color.cyan) : new Color(0.7f, 0.7f, 0.7f, 0.8f);
                            Styles.Instance.collVertexHandleStyle.Draw(new Rect(s0.x - Styles.Instance.collVertexHandleStyle.normal.background.width / 2, s0.y - Styles.Instance.collVertexHandleStyle.normal.background.height / 2, 1, 1), i.ToString(), false, false, false, false);
                            GUI.color = Color.white;
                        }
                    }

                    if (isAddingVertexOn)
                    {
                        int segmentIdx;
                        Vector2 newVertexPos = ClosestPointToPolyLine(polyEdges, out segmentIdx);

                        if (e.type == EventType.MouseUp)
                        {
                            selectedTile.collData.vertices = new Vector2[collVertices.Length + 1];
                            segmentIdx = (segmentIdx + 1) % selectedTile.collData.vertices.Length;
                            for (int i = 0, j = 0; i < selectedTile.collData.vertices.Length; ++i)
                            {
                                if (segmentIdx == i)
                                {
                                    newVertexPos.x = Mathf.Clamp(Mathf.Round(newVertexPos.x / pixelSize) * pixelSize, m_rTile.x, m_rTile.xMax);
                                    newVertexPos.y = Mathf.Clamp(Mathf.Round(newVertexPos.y / pixelSize) * pixelSize, m_rTile.y, m_rTile.yMax);
                                    selectedTile.collData.vertices[i].x = Mathf.Clamp01((newVertexPos.x - m_rTile.x) / m_rTile.width);
                                    selectedTile.collData.vertices[i].y = Mathf.Clamp01((newVertexPos.y - m_rTile.yMax) / -m_rTile.height);
                                }
                                else
                                {
                                    selectedTile.collData.vertices[i] = collVertices[j];
                                    ++j;
                                }
                            }
                            collVertices = selectedTile.collData.vertices;
                            m_activeVertexIdx = -1;
                        }
                        else if (e.type == EventType.Repaint)
                        {
                            GUI.color = new Color(0.7f, 0.7f, 0.7f, 0.8f);
                            Styles.Instance.collVertexHandleStyle.Draw(new Rect(newVertexPos.x - Styles.Instance.collVertexHandleStyle.normal.background.width / 2, newVertexPos.y - Styles.Instance.collVertexHandleStyle.normal.background.height / 2, 1, 1), segmentIdx.ToString(), false, false, false, false);
                            GUI.color = Color.white;
                        }

                    }
                }

                if (e.type == EventType.MouseUp)
                {
                    saveChanges = true;
                    //remove duplicated vertex
                    selectedTile.collData.vertices = selectedTile.collData.vertices.Distinct().ToArray();                    
                    if(selectedTile.collData.vertices.Length <= 2)
                    {
                        selectedTile.collData.vertices = m_savedVertexData;
                    }
                    //snap vertex positions
                    selectedTile.collData.SnapVertices(Tileset);
                }
            }

            GUI.EndGroup();

            if( GUILayout.Button("Reverse Normals") )
            {
                selectedTile.collData.vertices = selectedTile.collData.vertices.Reverse().ToArray();
            }

            EditorGUILayout.Space();

            string helpInfo =
                "  - Click and drag over a vertex to move it" + "\n" +
                "  - Hold Shift + Click for adding a new vertex" + "\n" +
                "  - Hold "+((Application.platform == RuntimePlatform.OSXEditor)? "Command" : "Ctrl")+" + Click for removing a vertex. (should be more than 3)" + "\n" +
                "  - Check the normals for each edge. The normal is displayed in the collision side" + "\n" +
                "";
            s_showHelp = EditorGUILayout.Foldout(s_showHelp, "Help");
            if (s_showHelp)
            {
                EditorGUILayout.HelpBox(helpInfo, MessageType.Info);
            }

            //+++ Collider Settings
            float savedLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 40;
            if (isMultiselection)
            {
                EditorGUILayout.LabelField("* Multi-selection Edition", EditorStyles.boldLabel);
            }
            EditorGUILayout.BeginHorizontal(GUILayout.MinWidth(140));
            {
                EditorGUILayout.LabelField("Collider Data", EditorStyles.boldLabel);
                if (GUILayout.Button("Copy", GUILayout.Width(50)))
                {
                    s_copiedColliderData = selectedTile.collData.Clone();
                }
                if (GUILayout.Button("Paste", GUILayout.Width(50)))
                {
                    selectedTile.collData = s_copiedColliderData.Clone();
                    saveChanges = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = 100;
            EditorGUI.BeginChangeCheck();
            
            //selectedTile.collData.type = (eTileCollider)EditorGUILayout.EnumPopup("Collider Type", selectedTile.collData.type);
            EditorGUILayout.LabelField("Collider Type:", EditorStyles.boldLabel);
            EditorGUI.indentLevel += 2;
            string[] tileColliderNames = System.Enum.GetNames(typeof(eTileCollider));

            selectedTile.collData.type = (eTileCollider)GUILayout.Toolbar((int)selectedTile.collData.type, tileColliderNames);
            EditorGUI.indentLevel -= 2;

            saveChanges |= EditorGUI.EndChangeCheck();
            EditorGUIUtility.labelWidth = savedLabelWidth;
            //---

            //Save changes
            if (saveChanges)
            {
                if (isMultiselection)
                {
                    for (int i = 0; i < Tileset.TileSelection.selectionData.Count; ++i)
                    {
                        Tileset.Tiles[(int)(Tileset.TileSelection.selectionData[i] & Tileset.k_TileDataMask_TileId)].collData = selectedTile.collData.Clone();
                    }
                }
                EditorUtility.SetDirty(Tileset);
                //Refresh selected tilemap
                STETilemap selectedTilemap = Selection.activeGameObject? Selection.activeGameObject.GetComponent<STETilemap>() : null;
                if(selectedTilemap)
                    selectedTilemap.Refresh(false, true);
            }
        }

        /// <summary>
        //     Get the point on a polyline (in 3D space) which is closest to the current
        //     mouse position. And returns the segment index ( the vertex index of the segment with the closest point )
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="closestSegmentIdx"></param>
        /// <returns></returns>
        Vector3 ClosestPointToPolyLine(Vector3[] vertices, out int closestSegmentIdx)
        {
            float minDist = float.MaxValue;
            closestSegmentIdx = 0;
            for (int i = 0; i < vertices.Length - 1; ++i)
            {
                float dist = HandleUtility.DistanceToLine(vertices[i], vertices[i + 1]);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestSegmentIdx = i;
                }
            }
            return HandleUtility.ClosestPointToPolyLine(vertices);
        }

        private static float s_maxLabelNameSize = 0f;
        public static ReorderableList CreateParameterReorderableList( ParameterContainer paramContainer )
        {
            ReorderableList reordList = new ReorderableList(paramContainer.ParameterList, typeof(Parameter), true, true, true, true);
            reordList.onAddDropdownCallback = (Rect buttonRect, ReorderableList l) =>
            {
                GenericMenu menu = new GenericMenu();
                GenericMenu.MenuFunction addBoolParamFunc = () =>
                {
                    paramContainer.AddNewParam(new Parameter("new bool", false));
                };
                GenericMenu.MenuFunction addIntParamFunc = () =>
                {
                    paramContainer.AddNewParam(new Parameter("new int", 0));
                };
                GenericMenu.MenuFunction addFloatParamFunc = () =>
                {
                    paramContainer.AddNewParam(new Parameter("new float", 0f));
                };
                GenericMenu.MenuFunction addStringParamFunc = () =>
                {
                    paramContainer.AddNewParam(new Parameter("new string", ""));
                };
                GenericMenu.MenuFunction addObjectParamFunc = () =>
                {
                    paramContainer.AddNewParam(new Parameter("new object", (UnityEngine.Object)null));
                };
                GenericMenu.MenuFunction removeAllFunc = () =>
                {
                    if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete all items?", "Yes", "No"))
                    {
                        paramContainer.RemoveAll();
                    }
                };
                menu.AddItem(new GUIContent("Add Bool"), false, addBoolParamFunc);
                menu.AddItem(new GUIContent("Add Int"), false, addIntParamFunc);
                menu.AddItem(new GUIContent("Add Float"), false, addFloatParamFunc);
                menu.AddItem(new GUIContent("Add String"), false, addStringParamFunc);
                menu.AddItem(new GUIContent("Add Object"), false, addObjectParamFunc);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Remove All"), false, removeAllFunc);
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Sort By Name"), false, paramContainer.SortByName);
                menu.AddItem(new GUIContent("Sort By Type"), false, paramContainer.SortByType);
                menu.ShowAsContext();
            };
            reordList.onRemoveCallback = (ReorderableList list) =>
            {
                if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete this item?", "Yes", "No"))
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                }
            };
            reordList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Parameters", EditorStyles.boldLabel);
                Texture2D btnTexture = reordList.elementHeight == 0f ? EditorGUIUtility.FindTexture("winbtn_win_max_h") : EditorGUIUtility.FindTexture("winbtn_win_min_h");
                if (GUI.Button(new Rect(rect.width - rect.height, rect.y, rect.height, rect.height), btnTexture, EditorStyles.label))
                {
                    reordList.elementHeight = reordList.elementHeight == 0f ? EditorGUIUtility.singleLineHeight : 0f;
                    reordList.draggable = reordList.elementHeight > 0f;
                }
            };
            reordList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (reordList.elementHeight == 0f)
                    return;

                Parameter param = reordList.list[index] as Parameter;
                if (index == 0)
                {
                    s_maxLabelNameSize = 0f;
                    foreach (Parameter p in paramContainer.ParameterList)
                    {
                        s_maxLabelNameSize = Mathf.Max(s_maxLabelNameSize, EditorStyles.label.CalcSize(new GUIContent(p.name + ".")).x); //NOTE: adding '.' to make sure ending spaces are not skipped
                    }
                }
                Rect rLabel = new Rect(rect.x, rect.y, s_maxLabelNameSize, EditorGUIUtility.singleLineHeight);
                if (index == reordList.index)
                {
                    string newName = EditorGUI.TextField(rLabel, param.name);
                    if (newName != param.name)
                    {
                        paramContainer.RenameParam(param.name, newName);
                    }
                }
                else
                {
                    EditorGUI.LabelField(rLabel, param.name);
                }
                Rect rParamValue = new Rect(rLabel.x + s_maxLabelNameSize, rLabel.y, rect.width - s_maxLabelNameSize, rLabel.height);
                if (param.GetParamType() == eParameterType.Bool)
                {
                    param.SetValue(EditorGUI.Toggle(rParamValue, param.GetAsBool()));
                }
                else if (param.GetParamType() == eParameterType.Int)
                {
                    param.SetValue(EditorGUI.IntField(rParamValue, param.GetAsInt()));
                }
                else if (param.GetParamType() == eParameterType.Float)
                {
                    param.SetValue(EditorGUI.FloatField(rParamValue, param.GetAsFloat()));
                }
                else if (param.GetParamType() == eParameterType.String)
                {
                    param.SetValue(EditorGUI.TextField(rParamValue, param.GetAsString()));
                }
                else if (param.GetParamType() == eParameterType.Object)
                {
                    param.SetValue(EditorGUI.ObjectField(rParamValue, param.GetAsObject(), typeof(UnityEngine.Object), false));
                }
            };

            return reordList;
        }        
    }
}
