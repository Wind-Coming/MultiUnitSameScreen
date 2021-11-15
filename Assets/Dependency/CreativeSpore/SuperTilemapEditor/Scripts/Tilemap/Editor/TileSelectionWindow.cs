using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{
    public class TileSelectionWindow : EditorWindow
    {

        #region Static Methods & Fields
        public static TileSelectionWindow Instance
        {
            get 
            {
                if (!s_instance) Show();
                return s_instance; 
            }
        }
        private static TileSelectionWindow s_instance;

        [MenuItem("SuperTilemapEditor/Window/Tile Palette Window")]
        static void Init()
        {
            Show(null);
        }

        public static void Show(Tileset tileset = null)
        {
            s_instance = (TileSelectionWindow)EditorWindow.GetWindow(typeof(TileSelectionWindow), false, "Tile Palette", true);
            s_instance.m_tilesetControl.Tileset = tileset;
            if (tileset == null)
            {
                s_instance.OnSelectionChange();
            }
            s_instance.wantsMouseMove = true;
        }

        public static void RefreshIfVisible()
        {
            if(s_instance)
            {
                s_instance.Refresh();
            }
        }

        #endregion

        public void Ping()
        {
            m_pingFramesLeft = s_pingFrameNb;
        }

        public void Refresh()
        {
            m_tilesetControl.Tileset = TilesetEditor.GetSelectedTileset();
            Repaint();   
        }

        public TilesetControl TilesetControl { get { return m_tilesetControl; } }

        [SerializeField]
        TilesetControl m_tilesetControl = new TilesetControl();

        private int m_pingFramesLeft = 0;
        private static int s_pingFrameNb = 15;

        void OnEnable()
        {
            s_instance = this;
        }

        void OnSelectionChange()
        {
            Refresh();        
        }

        private static Vector2 s_scrollPos;
        void OnGUI()
        {
            Event e = Event.current;
            if (e.type == EventType.Repaint)
            {
                if (m_pingFramesLeft > 0)
                {
                    --m_pingFramesLeft;
                }
            }            

            if (m_pingFramesLeft > 0)
            {
                float alpha = 1f - Mathf.Abs(2f * ((float)m_pingFramesLeft / s_pingFrameNb) - 1f);
                GUI.color = new Color(1f, 1f, 0f, alpha);
                GUI.DrawTexture(new Rect(0, 0, maxSize.x, maxSize.y), EditorGUIUtility.whiteTexture, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
            }

            if (m_tilesetControl.Tileset == null)
            {
                EditorGUILayout.HelpBox("Select a tileset to edit.", MessageType.Info);
                if (Event.current.type == EventType.Repaint)
                {
                    OnSelectionChange();
                }
                Repaint();
                return;
            }
            STETilemap selectedTilemap = Selection.activeGameObject? Selection.activeGameObject.GetComponent<STETilemap>() : null;
            if (selectedTilemap && selectedTilemap.Tileset != m_tilesetControl.Tileset)
            {
                Refresh();
            }

            s_scrollPos = EditorGUILayout.BeginScrollView(s_scrollPos);
            if (m_tilesetControl.Display())
                Repaint();
            EditorGUILayout.EndScrollView();

        }
    }
}