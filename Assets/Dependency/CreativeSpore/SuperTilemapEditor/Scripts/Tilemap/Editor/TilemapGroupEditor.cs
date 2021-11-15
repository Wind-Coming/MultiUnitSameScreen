using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;

namespace CreativeSpore.SuperTilemapEditor
{
    [CustomEditor(typeof(TilemapGroup))]
    public class TilemapGroupEditor : Editor
    {
        [MenuItem("GameObject/SuperTilemapEditor/TilemapGroup", false, 10)]
        static void CreateTilemapGroup(MenuCommand menuCommand)
        {
            GameObject obj = new GameObject("TilemapGroup");
            obj.AddComponent<TilemapGroup>();
            if (menuCommand.context is GameObject)
                obj.transform.SetParent((menuCommand.context as GameObject).transform);
            Selection.activeGameObject = obj;
        }

        private ReorderableList m_tilemapReordList;
        private ReorderableList m_sceneViewTilemapRList;
        private TilemapEditor m_tilemapEditor;
        private TilemapGroup m_target;
        private List<GameObject> m_tilemapRemovingList = new List<GameObject>();
        private SerializedProperty m_displayTilemapRList;
        private SerializedProperty m_defaultTilemapWindowVisibleProperty;

        private SerializedProperty m_selectedIndexProp;
        private void OnEnable()
        {
            m_target = target as TilemapGroup;
            m_target.Refresh();
            m_defaultTilemapWindowVisibleProperty = serializedObject.FindProperty("m_defaultTilemapWindowVisible");
            m_selectedIndexProp = serializedObject.FindProperty("m_selectedIndex");
            m_displayTilemapRList = serializedObject.FindProperty("m_displayTilemapRList");
            m_tilemapReordList = CreateTilemapReorderableList();
            m_tilemapReordList.index = m_selectedIndexProp.intValue;
            m_sceneViewTilemapRList = CreateTilemapReorderableList();
            m_sceneViewTilemapRList.index = m_selectedIndexProp.intValue;
            if(!m_target.DisplayTilemapRList)
            {
                m_sceneViewTilemapRList.elementHeight = 0f;
                m_sceneViewTilemapRList.draggable = m_sceneViewTilemapRList.elementHeight > 0f;
                m_sceneViewTilemapRList.displayAdd = m_sceneViewTilemapRList.draggable;
                m_sceneViewTilemapRList.displayRemove = m_sceneViewTilemapRList.draggable;
            }
        }

        private void OnDisable()
        {
            if (m_tilemapEditor)
                TilemapEditor.DestroyImmediate(m_tilemapEditor);
        }

        public override void OnInspectorGUI()
        {
            // NOTE: this happens during undo/redo
            if( m_target.transform.childCount != m_target.Tilemaps.Count) 
            {
                m_target.Refresh();
            }
            serializedObject.Update();

            if (m_tilemapReordList.HasKeyboardControl())
                DoKeyboardChecks();

            // clamp index to valid value
            m_selectedIndexProp.intValue = Mathf.Clamp(m_selectedIndexProp.intValue, -1, m_target.Tilemaps.Count - 1);

            // Draw Tilemap List
            m_tilemapReordList.index = m_selectedIndexProp.intValue;
            m_tilemapReordList.DoLayoutList();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_unselectedColorMultiplier"), new GUIContent("Highlight Alpha"));
            EditorGUIUtility.labelWidth = 200f;       
            EditorGUILayout.PropertyField(m_defaultTilemapWindowVisibleProperty, new GUIContent("Display Default Tilemap Window"));
            EditorGUIUtility.labelWidth = 0f;
            EditorGUILayout.Space();

            // Draw Tilemap Inspector
            TilemapEditor tilemapEditor = GetTilemapEditor();
            if (tilemapEditor)
            {
                tilemapEditor.OnInspectorGUI();
            }

            m_displayTilemapRList.boolValue = m_sceneViewTilemapRList.elementHeight > 0f;
            serializedObject.ApplyModifiedProperties();

            //fix: argument exception when removing the last tilemap in the list
            if (Event.current.type == EventType.Layout)
            {
                for (int i = 0; i < m_tilemapRemovingList.Count; ++i)
                    Undo.DestroyObjectImmediate(m_tilemapRemovingList[i]);
                m_tilemapRemovingList.Clear();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
                Repaint();
            }
        }

        private Rect m_defaultTilemapsWindowRect = new Rect(0f, 0f, 340f, 160f);
        private bool m_defaultTilemapWindowMinimized = false;        
        public void OnSceneGUI()
        {
            serializedObject.Update();

            Handles.BeginGUI();
            // clamp index to valid value
            m_selectedIndexProp.intValue = Mathf.Clamp(m_selectedIndexProp.intValue, -1, m_target.Tilemaps.Count - 1);
            m_sceneViewTilemapRList.index = m_selectedIndexProp.intValue;
            GUI.color = new Color(.75f, .75f, 0.8f, 0.85f);
            float hOff = m_displayTilemapRList.boolValue? 40f : 20f;
            m_sceneViewTilemapRList.DoList(new Rect(Screen.width - 300f - 10f, Screen.height - m_sceneViewTilemapRList.GetHeight() - hOff, 300f, 1f));
            GUI.color = Color.white;
            Handles.EndGUI();

            TilemapEditor tilemapEditor = GetTilemapEditor();
            if (tilemapEditor)
            {
                (tilemapEditor as TilemapEditor).OnSceneGUI();                
            }


            DoKeyboardChecks();
            serializedObject.ApplyModifiedProperties();

            if (m_defaultTilemapWindowVisibleProperty.boolValue)
            {
                Rect rDefaultTilemapWindow = new Rect(5f, Screen.height - m_defaultTilemapsWindowRect.height - 20f, m_defaultTilemapsWindowRect.width, m_defaultTilemapsWindowRect.height);
                if (m_defaultTilemapWindowMinimized)
                {
                    rDefaultTilemapWindow.y = Screen.height - 55f;
                }
                GUI.Window(0, rDefaultTilemapWindow, DoWindow, "Tile/Brush Default Tilemap", STEditorStyles.Instance.boldWindow);
            }
        }

        private void DoWindow(int id)
        {
            Event e = Event.current;            
            STETilemap defaultTilemap = m_target.GetDefaultTilemapForCurrentTileSelection();
            GUILayout.BeginArea(new Rect(0f, EditorGUIUtility.singleLineHeight, m_defaultTilemapsWindowRect.width, m_defaultTilemapsWindowRect.height));
            GUILayout.Label("Default Tilemap: " + (defaultTilemap? "<b>" + defaultTilemap.name + "</b>" : "<none>"), STEditorStyles.Instance.richLabel);
            if (GUILayout.Button("Set Default Tilemap <b>(For the Palette Selection)</b>", STEditorStyles.Instance.richButton))
            {
                if (m_target.SelectedTilemap && m_target.SelectedTilemap.Tileset && m_target.SelectedTilemap.Tileset.TileSelection != null)
                {
                    foreach (uint tileData in m_target.SelectedTilemap.Tileset.TileSelection.selectionData)
                        SetDefaultTilemapFromTileData(m_target.SelectedTilemap, 0, 0, tileData);
                }
                else
                {
                    m_target.SetDefaultTilemapForCurrentSelectedTileOrBrush();
                }
            }
            if (GUILayout.Button("Remove Default Tilemap <b>(For the Palette Selection)</b>", STEditorStyles.Instance.richButton))
                m_target.ClearDefaultTilemapForCurrentSelectedTileOrBrush();
            EditorGUILayout.Space();
            if (GUILayout.Button("Set Default Tilemap <b>(For the Tiles in the Tilemap)</b>", STEditorStyles.Instance.richButton))
                TilemapUtils.IterateTilemapWithAction(m_target.SelectedTilemap, SetDefaultTilemapFromTileData);
            if (GUILayout.Button("Set Default Tilemap <b>(For all Tilemaps)</b>", STEditorStyles.Instance.richButton))
            {
                m_target.IterateTilemapWithAction((STETilemap tilemap) => {
                    TilemapUtils.IterateTilemapWithAction(tilemap, SetDefaultTilemapFromTileData);
                });
            }
            EditorGUILayout.Space();
            /*
            if (GUILayout.Button("Clear Default Tilemap <b>(All Tiles in the Tilemap)</b>", STEditorStyles.Instance.richButton))
                TilemapUtils.IterateTilemapWithAction(m_target.SelectedTilemap, (STETilemap tilemap, int gx, int gy, uint tileData) =>
                {
                    int brushId = Tileset.GetBrushIdFromTileData(tileData);
                    if (brushId != Tileset.k_BrushId_Default)
                        m_target.ClearBrushDefaultTilemap(brushId);
                    else
                    {
                        int tileId = Tileset.GetTileIdFromTileData(tileData);
                        if (tileId != Tileset.k_TileId_Empty)
                            m_target.ClearTileDefaultTilemap(tileId);
                    }
                });
            */
            if (GUILayout.Button("Clear Default Tilemap Data <b>(For all Tiles)</b>", STEditorStyles.Instance.richButton))
                m_target.ClearAllDefaultTilemapData();
            GUILayout.EndArea();
            if (e.isMouse && m_defaultTilemapsWindowRect.Contains(e.mousePosition))
            {
                if(e.type == EventType.MouseDown && e.mousePosition.y <= EditorGUIUtility.singleLineHeight)
                    m_defaultTilemapWindowMinimized = !m_defaultTilemapWindowMinimized;
                e.Use();
            }
        }

        private void SetDefaultTilemapFromTileData(STETilemap tilemap, int gx, int gy, uint tileData) 
        {
            int brushId = Tileset.GetBrushIdFromTileData(tileData);
            if (brushId != Tileset.k_BrushId_Default)
                m_target.SetBrushDefaultTilemap(brushId, tilemap);
            else
            {
                int tileId = Tileset.GetTileIdFromTileData(tileData);
                if(tileId != Tileset.k_TileId_Empty)
                    m_target.SetTileDefaultTilemap(tileId, tilemap);
            }
        }

private void DoKeyboardChecks()
        {
            Event e = Event.current;
            // Cycle over tilemaps
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == ShortcutKeys.k_NextLayer) m_selectedIndexProp.intValue++;
                else if (e.keyCode == ShortcutKeys.k_PrevLayer) m_selectedIndexProp.intValue += m_tilemapReordList.count - 1;
                m_selectedIndexProp.intValue %= m_tilemapReordList.count;
            }
        }

        //NOTE: m_tilemapEditor.target changes when OnSceneGUI is called, so this method makes sure to create it again if target doesn't match
        private TilemapEditor GetTilemapEditor()
        {
            var targetObj = target as TilemapGroup;
            if (!m_tilemapEditor || !m_tilemapEditor.target || m_tilemapEditor.target != targetObj.SelectedTilemap)
            {
                if (targetObj.SelectedTilemap)
                {
                    if (m_tilemapEditor)
                        TilemapEditor.DestroyImmediate(m_tilemapEditor);
                    m_tilemapEditor = TilemapEditor.CreateEditor(targetObj.SelectedTilemap) as TilemapEditor;
                }
                else
                {
                    if (m_tilemapEditor)
                        TilemapEditor.DestroyImmediate(m_tilemapEditor);
                    m_tilemapEditor = null;
                }
            }
            return m_tilemapEditor;
        }

        private ReorderableList CreateTilemapReorderableList()
        {
            ReorderableList reordList = new ReorderableList(serializedObject, serializedObject.FindProperty("m_tilemaps"), true, true, true, true);
            reordList.displayAdd = reordList.displayRemove = true;
            reordList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Tilemaps", EditorStyles.boldLabel);
                Texture2D btnTexture = reordList.elementHeight == 0f ? EditorGUIUtility.FindTexture("winbtn_win_max_h") : EditorGUIUtility.FindTexture("winbtn_win_min_h");
                if (GUI.Button(new Rect( rect.x + rect.width - rect.height, rect.y, rect.height, rect.height), btnTexture, EditorStyles.label))
                {
                    GUI.changed = true;
                    reordList.elementHeight = reordList.elementHeight == 0f ? 21f : 0f;
                    reordList.draggable = reordList.elementHeight > 0f;
                    reordList.displayAdd = reordList.draggable;
                    reordList.displayRemove = reordList.draggable;
                }
            };
            var tilemapTilemapSerialized = new Dictionary<STETilemap, SerializedObject>();
            var tilemapTilemapObjectSerialized = new Dictionary<SerializedObject, SerializedObject>();
            reordList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                if (reordList.elementHeight == 0)
                    return;
                var element = reordList.serializedProperty.GetArrayElementAtIndex(index);               
                rect.y += 2;
                STETilemap tilemap = element.objectReferenceValue as STETilemap;
                if (tilemap)
                {
                    SerializedObject tilemapSerialized;
                    SerializedObject tilemapObjSerialized;
                    if (!tilemapTilemapSerialized.TryGetValue(tilemap, out tilemapSerialized))
                    {
                        tilemapTilemapSerialized[tilemap] = tilemapSerialized = new SerializedObject(tilemap);
                    }

                    if (!tilemapTilemapObjectSerialized.TryGetValue(tilemapSerialized, out tilemapObjSerialized))
                    {
                        tilemapTilemapObjectSerialized[tilemapSerialized] = tilemapObjSerialized = new SerializedObject(tilemapSerialized.FindProperty("m_GameObject").objectReferenceValue);
                    }

                    Rect rToggle = new Rect(rect.x, rect.y, 16f, EditorGUIUtility.singleLineHeight);
                    Rect rName = new Rect(rect.x + 20f, rect.y, rect.width - 130f - 20f, EditorGUIUtility.singleLineHeight);
                    Rect rColliders = new Rect(rect.x + rect.width - 125f, rect.y, 125f, EditorGUIUtility.singleLineHeight);
                    Rect rSortingLayer = new Rect(rect.x + rect.width - 125f, rect.y, 80f, EditorGUIUtility.singleLineHeight);
                    Rect rSortingOrder = new Rect(rect.x + rect.width - 40f, rect.y, 40f, EditorGUIUtility.singleLineHeight);

                    tilemap.IsVisible = EditorGUI.Toggle(rToggle, GUIContent.none, tilemap.IsVisible, STEditorStyles.Instance.visibleToggleStyle);
                    EditorGUI.PropertyField(rName, tilemapObjSerialized.FindProperty("m_Name"), GUIContent.none);
                    if (TilemapEditor.EditMode == TilemapEditor.eEditMode.Collider)
                    {
                        SerializedProperty colliderTypeProperty = tilemapSerialized.FindProperty("ColliderType");
                        string[] colliderTypeNames = new List<string>(System.Enum.GetNames(typeof(eColliderType)).Select(x => x.Replace('_', ' '))).ToArray();
                        EditorGUI.BeginChangeCheck();
                        colliderTypeProperty.intValue = GUI.Toolbar(rColliders, colliderTypeProperty.intValue, colliderTypeNames);
                        if (EditorGUI.EndChangeCheck())
                        {
                            tilemapSerialized.ApplyModifiedProperties();
                            tilemap.Refresh(false, true);
                        }
                    }
                    else
                    {
                        // Sorting Layer and Order in layer            
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(rSortingLayer, tilemapSerialized.FindProperty("m_sortingLayer"), GUIContent.none);
                        EditorGUI.PropertyField(rSortingOrder, tilemapSerialized.FindProperty("m_orderInLayer"), GUIContent.none);
                        tilemapSerialized.FindProperty("m_orderInLayer").intValue = (tilemapSerialized.FindProperty("m_orderInLayer").intValue << 16) >> 16; // convert from int32 to int16 keeping sign
                        if (EditorGUI.EndChangeCheck())
                        {
                            tilemapSerialized.ApplyModifiedProperties();
                            tilemap.RefreshChunksSortingAttributes();
                            SceneView.RepaintAll();
                        }
                        //--- 
                    }

                    if(GUI.changed)
                    {
                        tilemapObjSerialized.ApplyModifiedProperties();
                    }
                }
            };
            reordList.onReorderCallback = (ReorderableList list) =>
            {
                var targetObj = target as TilemapGroup;
                int sibilingIdx = 0;
                foreach (STETilemap tilemap in targetObj.Tilemaps)
                {
                    tilemap.transform.SetSiblingIndex(sibilingIdx++);
                }
                Repaint();
            };
            reordList.onSelectCallback = (ReorderableList list) =>
            {
                m_selectedIndexProp.intValue = reordList.index;
                serializedObject.ApplyModifiedProperties();
                GUI.changed = true;
                TileSelectionWindow.RefreshIfVisible();
                TilePropertiesWindow.RefreshIfVisible();
            };
            reordList.onAddCallback = (ReorderableList list) =>
            {
                var targetObj = target as TilemapGroup;
                Undo.RegisterCompleteObjectUndo(targetObj, "New Tilemap");
                GameObject obj = new GameObject();
                Undo.RegisterCreatedObjectUndo(obj, "New Tilemap");
                STETilemap newTilemap = obj.AddComponent<STETilemap>();
                obj.transform.parent = targetObj.transform;
                obj.name = GameObjectUtility.GetUniqueNameForSibling(obj.transform.parent, "New Tilemap");

                STETilemap copiedTilemap = targetObj.SelectedTilemap;
                if(copiedTilemap)
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(copiedTilemap);
                    UnityEditorInternal.ComponentUtility.PasteComponentValues(newTilemap);
                    obj.name = GameObjectUtility.GetUniqueNameForSibling(obj.transform.parent, copiedTilemap.name);
                }
            };
            reordList.onRemoveCallback = (ReorderableList list) =>
            {
                m_tilemapRemovingList.Add(m_target.SelectedTilemap.gameObject);                
            };

            return reordList;
        }
    }
}
