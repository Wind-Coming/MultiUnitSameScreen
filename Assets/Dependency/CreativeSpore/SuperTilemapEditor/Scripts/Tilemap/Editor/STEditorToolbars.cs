using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{
    public class STEditorToolbars
    {

        static STEditorToolbars s_instance;
        public static STEditorToolbars Instance { get { if (s_instance == null) s_instance = new STEditorToolbars(); return s_instance; } }

        public ToolbarControl brushToolbar;
        public ToolbarControl brushPaintToolbar;

        private STEditorToolbars()
        {
            List<GUIContent> guiContentList = new List<GUIContent>()
            {
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.eToolIcon.Pencil), "Paint"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.eToolIcon.Erase), "Erase (Hold Shift)"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.eToolIcon.Fill), "Fill (Double click)"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.eToolIcon.FlipH), "Flip Horizontal ("+ShortcutKeys.k_FlipH+")"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.eToolIcon.FlipV), "Flip Vertical ("+ShortcutKeys.k_FlipV+")"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.eToolIcon.Rot90), "Rotate 90 clockwise (" + ShortcutKeys.k_Rot90 + "); anticlockwise (" + ShortcutKeys.k_Rot90Back + ")"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.eToolIcon.Info), " Display Help (F1)"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.eToolIcon.Refresh), " Refresh Tilemap (F5)"),
            };
            brushToolbar = new ToolbarControl(guiContentList);
            brushToolbar.OnToolSelected += OnToolSelected_BrushToolbar;

            guiContentList = new List<GUIContent>()
            {
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.ePaintModeIcon.Pencil), "Paint ("+ShortcutKeys.k_PencilTool+")"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.ePaintModeIcon.Line), "Line ("+ShortcutKeys.k_LineTool+")"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.ePaintModeIcon.Rect), "Rectangle ("+ShortcutKeys.k_RectTool+")"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.ePaintModeIcon.FilledRect), "Filled Rectangle ("+ShortcutKeys.k_RectTool+")"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.ePaintModeIcon.Ellipse), "Ellipse ("+ShortcutKeys.k_EllipseTool+")"),
                new GUIContent(ToolIcons.GetToolTexture(ToolIcons.ePaintModeIcon.FilledEllipse), "Filled Ellipse ("+ShortcutKeys.k_EllipseTool+")"),
            };
            brushPaintToolbar = new ToolbarControl(guiContentList);
            brushPaintToolbar.OnToolSelected += OnToolSelected_BrushPaintToolbar;
        }

        private void OnToolSelected_BrushPaintToolbar(ToolbarControl source, int selectedToolIdx, int prevSelectedToolIdx)
        {
            BrushBehaviour.Instance.PaintMode = (BrushBehaviour.eBrushPaintMode)selectedToolIdx;
        }

        private void OnToolSelected_BrushToolbar(ToolbarControl source, int selectedToolIdx, int prevSelectedToolIdx)
        {
            ToolIcons.eToolIcon toolIcon = (ToolIcons.eToolIcon)selectedToolIdx;
            switch (toolIcon)
            {
                case ToolIcons.eToolIcon.Pencil:
                    TilemapEditor.s_brushMode = TilemapEditor.eBrushMode.Paint;
                    Tools.current = Tool.None;
                    break;
                case ToolIcons.eToolIcon.Erase:
                    TilemapEditor.s_brushMode = TilemapEditor.eBrushMode.Erase;
                    brushPaintToolbar.TriggerButton(0);
                    Tools.current = Tool.None;
                    break;
                case ToolIcons.eToolIcon.Fill:
                    TilemapEditor.s_brushMode = TilemapEditor.eBrushMode.Fill;
                    brushPaintToolbar.TriggerButton(0);
                    Tools.current = Tool.None;
                    break;
                case ToolIcons.eToolIcon.FlipV:
                    BrushBehaviour.SFlipV();
                    Tools.current = Tool.None;
                    source.SelectedIdx = prevSelectedToolIdx;
                    break;
                case ToolIcons.eToolIcon.FlipH:
                    BrushBehaviour.SFlipH();
                    Tools.current = Tool.None;
                    source.SelectedIdx = prevSelectedToolIdx;
                    break;
                case ToolIcons.eToolIcon.Rot90:
                    BrushBehaviour.SRot90();
                    Tools.current = Tool.None;
                    source.SelectedIdx = prevSelectedToolIdx;
                    break;
                case ToolIcons.eToolIcon.Info:
                    TilemapEditor.s_displayHelpBox = !TilemapEditor.s_displayHelpBox;
                    Tools.current = Tool.None;
                    source.SelectedIdx = prevSelectedToolIdx;
                    source.SetHighlight(selectedToolIdx, TilemapEditor.s_displayHelpBox);
                    break;
                case ToolIcons.eToolIcon.Refresh:
                    TilemapGroup tilemapGroup = Selection.activeGameObject.GetComponent<TilemapGroup>();
                    if (tilemapGroup)
                    {
                        foreach (STETilemap tilemap in tilemapGroup.Tilemaps)
                        {
                            tilemap.Refresh(true, true, true, true);
                        }
                    }
                    else
                    {
                        STETilemap tilemap = Selection.activeGameObject.GetComponent<STETilemap>();
                        if (tilemap) tilemap.Refresh(true, true, true, true);
                    }
                    Tools.current = Tool.None;
                    source.SelectedIdx = prevSelectedToolIdx;
                    break;
            }
        }
    }
}