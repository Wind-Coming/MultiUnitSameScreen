using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace CreativeSpore.SuperTilemapEditor
{
    public partial class TilemapEditor
    {        
        private class ColorSettings
        {
            public uint colorMask = 0xFFFFFFFFu; // [R|G|B|A]
            public bool toggleColorMaskAll = true;
            public Color color = Color.white;
            public eBlendMode blendMode = eBlendMode.AlphaBlending;
            public eTileColorPaintMode tileColorPaintMode = eTileColorPaintMode.Vertex;
            public bool paintTilemapGroup = false;
            public float radius = 1f;
            public AnimationCurve brushIntensity = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // new AnimationCurve(new Keyframe[] { new Keyframe(0f, 0f, 2f, 2f), new Keyframe(1f, 1f) });
        }
        private static ColorSettings s_colorSettings = new ColorSettings();
        private static bool s_enableUndoColorPainting = false;

        private void OnInspectorGUI_Color()
        {
            s_colorSettings.color = EditorGUILayout.ColorField("Color", s_colorSettings.color);
            s_colorSettings.blendMode = (eBlendMode)EditorGUILayout.EnumPopup("Blend Mode", s_colorSettings.blendMode);
            s_colorSettings.tileColorPaintMode = (eTileColorPaintMode)EditorGUILayout.EnumPopup(new GUIContent("Tile Color Paint Mode", "Tile mode will change the 4 vertices of the tile using the same color; Vertex will take into account each vertex of the tile separately."), s_colorSettings.tileColorPaintMode);
            if (m_tilemap.ParentTilemapGroup)
            {
                s_colorSettings.paintTilemapGroup = EditorGUILayout.Toggle(new GUIContent("Paint Tilemap Group", "Paints all the tilemaps in the tilemap group."), s_colorSettings.paintTilemapGroup);
            }
            s_colorSettings.radius = Mathf.Max(0f, EditorGUILayout.FloatField("Radius", s_colorSettings.radius));
            s_colorSettings.brushIntensity = EditorGUILayout.CurveField(new GUIContent("Brush Intensity", "The alpha of the color will be multiplied by this curve values along the radius."), s_colorSettings.brushIntensity);
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if(GUILayout.Button("Clear to Color"))
            {
                if(s_colorSettings.paintTilemapGroup && m_tilemap.ParentTilemapGroup)
                {
                    m_tilemap.ParentTilemapGroup.IterateTilemapWithAction((STETilemap tilemap) =>
                    {
                        if (tilemap && tilemap.IsVisible)
                        {
                            if (s_enableUndoColorPainting)
                                RegisterTilemapUndo(tilemap);
                            tilemap.ClearColorChannel(s_colorSettings.color);
                            tilemap.UpdateMesh();
                        }
                    });                         
                }
                else
                {
                    if (s_enableUndoColorPainting)
                        RegisterTilemapUndo(m_tilemap);
                    m_tilemap.ClearColorChannel(s_colorSettings.color);
                    m_tilemap.UpdateMesh();
                }
            }
            EditorGUILayout.Space();
            if(GUILayout.Button("Remove Color Channel"))
            {
                if (s_colorSettings.paintTilemapGroup && m_tilemap.ParentTilemapGroup)
                {
                    m_tilemap.ParentTilemapGroup.IterateTilemapWithAction((STETilemap tilemap) => 
                    {
                        if (tilemap && tilemap.IsVisible)
                        {
                            if (s_enableUndoColorPainting)
                                RegisterTilemapUndo(tilemap);
                            tilemap.RemoveColorChannel();
                            tilemap.UpdateMesh();
                        }
                    });                    
                }
                else
                {
                    if (s_enableUndoColorPainting)
                        RegisterTilemapUndo(m_tilemap);
                    m_tilemap.RemoveColorChannel();
                    m_tilemap.UpdateMesh();
                }
            }
            s_enableUndoColorPainting = EditorGUILayout.Toggle(new GUIContent("Enable Undo", "Enables undo/redo for color paint actions. It could slowdown the color painting actions."), s_enableUndoColorPainting);
            EditorGUILayout.Space();
            string helpInfo =
                "- Hold " + EditorCompatibilityUtils.CtrKeyName + " while scrolling up/down to change the brush radius.\n" +
                "- Hold " + EditorCompatibilityUtils.AltKeyName + " while pressing the left mouse button to pick tile color.\n"
                ;
            EditorGUILayout.HelpBox(helpInfo, MessageType.Info);            
        }

        private void DoColorSceneGUI()
        {
            Event e = Event.current;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);
            EventType currentEventType = Event.current.GetTypeForControl(controlID);
            bool skip = false;
            //int saveControl = GUIUtility.hotControl; //FIX: Should not grab hot control with an active capture

            //Shortcuts
            if (e.type == EventType.ScrollWheel && e.control)
            {
                s_colorSettings.radius += e.delta.y * 0.1f;
                e.Use();
            }

            try
            {
                if (currentEventType == EventType.Layout) { skip = true; }
                else if (currentEventType == EventType.ScrollWheel) { skip = true; }

                if (m_tilemap.Tileset == null)
                {
                    return;
                }

                if (!skip)
                {                    
                    EditorGUIUtility.AddCursorRect(new Rect(0f, 0f, (float)Screen.width, (float)Screen.height), MouseCursor.Arrow);
                    //GUIUtility.hotControl = controlID; //FIX: Should not grab hot control with an active capture
                    {
                        Plane chunkPlane = new Plane(m_tilemap.transform.forward, m_tilemap.transform.position);
                        Vector2 mousePos = Event.current.mousePosition; mousePos.y = Screen.height - mousePos.y;
                        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                        float dist;
                        if (chunkPlane.Raycast(ray, out dist))
                        {
                            Vector3 brushWorldPos = ray.GetPoint(dist);
                            // Update brush transform
                            m_localBrushPos = (Vector2)m_tilemap.transform.InverseTransformPoint(brushWorldPos);

                            EditorCompatibilityUtils.CircleCap(0, brushWorldPos, m_tilemap.transform.rotation, s_colorSettings.radius);
                            if (
                                (EditorWindow.focusedWindow == EditorWindow.mouseOverWindow) && // fix painting tiles when closing another window popup over the SceneView like GameObject Selection window
                                (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                            )
                            {
                                if (e.button == 0)
                                {
                                    if(e.alt)
                                    {
                                        //Color Pickup
                                        Color pickedColor = m_tilemap.GetTileColor(m_localBrushPos).c0;
                                        pickedColor.a = s_colorSettings.color.a;
                                        s_colorSettings.color = pickedColor;
                                    }
                                    else if (s_colorSettings.paintTilemapGroup && m_tilemap.ParentTilemapGroup)
                                    {
                                        m_tilemap.ParentTilemapGroup.IterateTilemapWithAction((STETilemap tilemap) =>
                                        {
                                            if (tilemap && tilemap.IsVisible)
                                            {
                                                if (s_enableUndoColorPainting)
                                                    RegisterTilemapUndo(tilemap);
                                                TilemapVertexPaintUtils.VertexPaintCircle(tilemap, m_localBrushPos, s_colorSettings.radius, s_colorSettings.color, s_colorSettings.blendMode, s_colorSettings.tileColorPaintMode == eTileColorPaintMode.Vertex, s_colorSettings.brushIntensity);
                                                tilemap.UpdateMesh();
                                            }
                                        });
                                    }
                                    else
                                    {
                                        if(s_enableUndoColorPainting)
                                            RegisterTilemapUndo(m_tilemap);
                                        TilemapVertexPaintUtils.VertexPaintCircle(m_tilemap, m_localBrushPos, s_colorSettings.radius, s_colorSettings.color, s_colorSettings.blendMode, s_colorSettings.tileColorPaintMode == eTileColorPaintMode.Vertex, s_colorSettings.brushIntensity);
                                        m_tilemap.UpdateMesh();
                                    }
                                }                                
                            }                            
                        }
                    }

                    if (currentEventType == EventType.MouseDrag && Event.current.button < 2) // 2 is for central mouse button
                    {
                        // avoid dragging the map
                        Event.current.Use();
                    }
                }
            }
            // Avoid loosing the hotControl because of a triggered exception
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }

            SceneView.RepaintAll();
            //GUIUtility.hotControl = saveControl; //FIX: Should not grab hot control with an active capture
        }

        void RegisterTilemapUndo(STETilemap tilemap)
        {
            Undo.RecordObject(tilemap, "Tilemap Painting " + tilemap.name);
            Undo.RecordObjects(tilemap.GetComponentsInChildren<TilemapChunk>(), "Tilemap Painting " + tilemap.name);
        }
    }
}
