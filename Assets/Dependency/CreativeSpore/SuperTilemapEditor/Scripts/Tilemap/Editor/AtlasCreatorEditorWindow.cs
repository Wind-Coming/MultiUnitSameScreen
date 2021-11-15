using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using System.IO;

namespace CreativeSpore.SuperTilemapEditor
{
    public class AtlasCreatorEditorWindow : EditorWindow 
    {
        enum eMaxTextureSize
        {                     
            _32 = 32,
            _64 = 64,
            _128 = 128,
            _256 = 256,
            _512 = 512,
            _1024 = 1024,
            _2048 = 2048,
            _4096 = 4096,
            _8192 = 8192,
        }
        private Tileset m_tileset;
        private Texture2D m_atlasTexture;
        [SerializeField]
        private Texture2D[] m_tileTextures = null;
        private int m_padding;
        private eMaxTextureSize m_maxTextureSize = eMaxTextureSize._2048;
        private SerializedObject m_serializedObject;

        [MenuItem("SuperTilemapEditor/Window/Atlas Creator Window")]
        public static void Display()
        {
            AtlasCreatorEditorWindow wnd = (AtlasCreatorEditorWindow)EditorWindow.GetWindow(typeof(AtlasCreatorEditorWindow), false, "Atlas Creator", true);
            wnd.minSize = new Vector2(337f, 314f);
        }

        void OnEnable()
        {
            m_serializedObject = new SerializedObject(this);
        }

        private Vector2 m_scrollPos;
        void OnGUI()
        {
            m_serializedObject.Update();
            m_scrollPos = EditorGUILayout.BeginScrollView(m_scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("Atlas Texture:", EditorStyles.boldLabel);
                    EditorGUI.indentLevel += 1;
                    m_tileset = (Tileset)EditorGUILayout.ObjectField("Tileset (optional)", m_tileset, typeof(Tileset), false);   
                    if(!m_tileset)
                    {
                        if( GUILayout.Button("Create Tileset") )
                        {
                            m_tileset = TilesetEditor.CreateTileset();
                        }
                    }
                    // Read Data From Tileset
                    if (m_tileset)
                    {
                        m_atlasTexture = m_tileset.AtlasTexture;                        
                    }
                    m_atlasTexture = (Texture2D)EditorGUILayout.ObjectField("Atlas texture", m_atlasTexture, typeof(Texture2D), false);
                    EditorGUI.indentLevel -= 1;
                }
                EditorGUILayout.EndHorizontal();

                //GUI.enabled = m_atlasTexture != null;
                EditorGUILayout.Space();                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("Atlas Settings:", EditorStyles.boldLabel);
                    EditorGUI.indentLevel += 1;
                    EditorGUILayout.PropertyField(m_serializedObject.FindProperty("m_tileTextures"), true);
                    if(GUILayout.Button("Sort by Name"))
                    {
                        m_tileTextures = m_tileTextures.Where(x => x != null).OrderBy(x => x.name).ToArray();
                    }
                    if (m_tileTextures == null)
                        m_tileTextures = new Texture2D[0];
                    m_padding = EditorGUILayout.IntField("Padding", m_padding);
                    m_maxTextureSize = (eMaxTextureSize)EditorGUILayout.EnumPopup("Max. Texture Size", m_maxTextureSize);
                    if (m_tileset && GUILayout.Button("Create Atlas & Update Tileset") || !m_tileset && GUILayout.Button("Create Atlas"))
                    {
                        if (!m_atlasTexture)
                            m_atlasTexture = new Texture2D((int)m_maxTextureSize, (int)m_maxTextureSize, TextureFormat.ARGB32, false);
                        foreach (Texture2D tileTexture in m_tileTextures)
                            TilemapUtilsEditor.MakeTextureReadable(tileTexture);
                        TilemapUtilsEditor.MakeTextureReadable(m_atlasTexture);
                        Rect[] tileRects = m_atlasTexture.PackTextures(m_tileTextures, m_padding, (int)m_maxTextureSize, false);
                        //EncodeToPNG doesn't work if texture is compressed and this happens if any tile texture is compressed
                        if (m_atlasTexture.format != TextureFormat.ARGB32)
                        {
                            Texture2D tempAtlas = new Texture2D(m_atlasTexture.width, m_atlasTexture.height, TextureFormat.ARGB32, false);
                            tempAtlas.SetPixels32(m_atlasTexture.GetPixels32());
                            tempAtlas.Apply();
                            m_atlasTexture = tempAtlas;
                        }
                        string atlasAssetPath = AssetDatabase.GetAssetPath(m_atlasTexture);
                        if(string.IsNullOrEmpty(atlasAssetPath))
                        {
                            string name = m_tileset ? m_tileset.name : "New ";
                            atlasAssetPath = EditorUtility.SaveFilePanelInProject("Save atlas texture", name + "Atlas", "png", "Save the atlas texture");
                        }
                        if (!string.IsNullOrEmpty(atlasAssetPath))
                        {
                            File.WriteAllBytes(atlasAssetPath, m_atlasTexture.EncodeToPNG());
                            AssetDatabase.ImportAsset(atlasAssetPath, ImportAssetOptions.ForceSynchronousImport);
                            TilesetEditor.OptimizeTextureImportSettings(m_atlasTexture);
                            AssetDatabase.Refresh();
                            m_atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasAssetPath);
                            if (m_tileset && tileRects.Length > 0)
                            {
                                m_tileset.AtlasTexture = m_atlasTexture;
                                m_tileset.TilePxSize = new Vector2(tileRects[0].size.x * m_atlasTexture.width, tileRects[0].size.y * m_atlasTexture.height);
                                int idx;
                                for (idx = 0; idx < tileRects.Length; ++idx)
                                {
                                    Rect uv = tileRects[idx];
                                    if (idx < m_tileset.Tiles.Count)
                                        m_tileset.Tiles[idx].uv = uv;
                                    else
                                        m_tileset.Tiles.Add(new Tile() { uv = uv });
                                }
                                m_tileset.Tiles.RemoveRange(idx, m_tileset.Tiles.Count - idx);
                            }
                        }
                    }
                    EditorGUI.indentLevel -= 1;
                }
                EditorGUILayout.EndVertical();
                const string sHelpInfo ="1. Create a tileset or drag a tileset into the Tileset field.\n"+
                                        "2. Drag the tile textures into the Tile Textures field.\n"+
                                        "3. Optionally sort by name the textures, to make sure the tile order will be the same if later you add new tile textures.\n"+
                                        "4. Press Create Atlas and Update Tileset";
                EditorGUILayout.HelpBox(sHelpInfo, MessageType.Info);

                GUI.enabled = true;
            }
            EditorGUILayout.EndScrollView();

            if (GUI.changed)
            {
                if (m_tileset) EditorUtility.SetDirty(m_tileset);
                m_serializedObject.ApplyModifiedProperties();
            }
        }

       
    }    
}
