using UnityEngine;
using System.Collections;
using UnityEditor;

namespace CreativeSpore.SuperTilemapEditor
{

    public class TilePropertiesWindow : EditorWindow
    {

        public static TilePropertiesWindow Instance
        {
            get 
            {
                if (!s_instance) Show();
                return s_instance; 
            }
        }
        private static TilePropertiesWindow s_instance;

        [MenuItem("SuperTilemapEditor/Window/Tile Properties Window")]
        static void Init()
        {
            Show(null);
        }

        public static void Show(Tileset tileset = null)
        {
            s_instance = (TilePropertiesWindow)EditorWindow.GetWindow(typeof(TilePropertiesWindow), false, "Tile Properties", true);            
            s_instance.m_tilePropertiesControl.Tileset = tileset;
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

        public void Refresh()
        {
            m_tilePropertiesControl.Tileset = TilesetEditor.GetSelectedTileset();            
            Repaint();
        }

        public TilePropertiesControl TilePropertiesControl { get { return m_tilePropertiesControl; } }

        [SerializeField]
        TilePropertiesControl m_tilePropertiesControl = new TilePropertiesControl();

        void OnEnable()
        {
            s_instance = this;
            minSize = new Vector2(200f, 200f);
        }

        void OnSelectionChange()
        {
            Refresh();
        }

        void OnGUI()
        {
            if (m_tilePropertiesControl.Tileset == null)
            {
                EditorGUILayout.HelpBox("Select a tileset to edit.", MessageType.Info);
                if (Event.current.type == EventType.Repaint)
                {
                    OnSelectionChange();
                }
                Repaint();
                return;
            }

            STETilemap selectedTilemap = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<STETilemap>() : null;
            if (selectedTilemap && selectedTilemap.Tileset != m_tilePropertiesControl.Tileset)
            {
                Refresh();
            }
            m_tilePropertiesControl.Display();

            Repaint();
        }
    }
}