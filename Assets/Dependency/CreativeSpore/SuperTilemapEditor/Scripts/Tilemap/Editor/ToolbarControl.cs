using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace CreativeSpore.SuperTilemapEditor
{
    public class ToolbarControl
    {
        public delegate void OnToolSelectedDelegate(ToolbarControl source, int selectedToolIdx, int prevSelectedToolIdx);
        public OnToolSelectedDelegate OnToolSelected;

        public int SelectedIdx 
        { 
            get { return m_selectedIdx; } 
            set 
            {
                m_selectedIdx = value; 
            } 
        }

        private List<GUIContent> m_buttonGuiContentList;
        private int m_selectedIdx = -1;
        private bool[] m_isHighlighted;

        public ToolbarControl(List<GUIContent> buttonGuiContentList)
        {
            m_buttonGuiContentList = new List<GUIContent>(buttonGuiContentList);
            m_isHighlighted = new bool[m_buttonGuiContentList.Count];
        }

        public void SetHighlight(int toolIdx, bool value)
        {
            if (toolIdx >= 0 && toolIdx < m_isHighlighted.Length)
                m_isHighlighted[toolIdx] = value;
        }

        public void DoGUI(Vector2 position, Vector2 buttonSize, Color bgColor, Color outlineColor)
        {
            Color savedColor = GUI.color;
            int buttonNb = m_buttonGuiContentList.Count;
            Rect rToolBar = new Rect(position.x, position.y, buttonNb * buttonSize.x, buttonSize.y);
            GUILayout.BeginArea(rToolBar);
            HandlesEx.DrawRectWithOutline(new Rect(Vector2.zero, rToolBar.size), bgColor, outlineColor);
            GUILayout.BeginHorizontal();

            if (m_isHighlighted.Length != m_buttonGuiContentList.Count)
                System.Array.Resize(ref m_isHighlighted, m_buttonGuiContentList.Count);

            int buttonPadding = 4;
            Rect rToolBtn = new Rect(buttonPadding, buttonPadding, rToolBar.size.y - 2 * buttonPadding, rToolBar.size.y - 2 * buttonPadding);
            for (int idx = 0; idx < m_buttonGuiContentList.Count; ++idx )
            {
                _DoToolbarButton(rToolBtn, idx);
                rToolBtn.x = rToolBtn.xMax + 2 * buttonPadding;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            GUI.color = savedColor;
        }

        public void TriggerButton(int idx)
        {
            int prevIdx = m_selectedIdx;
            m_selectedIdx = idx;
            if (OnToolSelected != null) OnToolSelected(this, m_selectedIdx, prevIdx);
        }

        private void _DoToolbarButton(Rect rToolBtn, int idx)
        {
            int iconPadding = 6;
            Rect rToolIcon = new Rect(rToolBtn.x + iconPadding, rToolBtn.y + iconPadding, rToolBtn.size.y - 2 * iconPadding, rToolBtn.size.y - 2 * iconPadding);
            Color activeColor = new Color(1f, 1f, 1f, 0.8f);
            Color disableColor = new Color(1f, 1f, 1f, 0.4f);
            Color highlithColor = new Color(1f, 1f, 0f, 0.8f);
            if (m_isHighlighted[idx])
                GUI.color = highlithColor;
            else
                GUI.color = m_selectedIdx == idx ? activeColor : disableColor;
            if (GUI.Button(rToolBtn, m_buttonGuiContentList[idx]))
            {
                TriggerButton(idx);
            }
            GUI.color = Color.white;
            if (m_buttonGuiContentList[idx].image)
                GUI.DrawTexture(rToolIcon, m_buttonGuiContentList[idx].image); 
        }
    }
}
