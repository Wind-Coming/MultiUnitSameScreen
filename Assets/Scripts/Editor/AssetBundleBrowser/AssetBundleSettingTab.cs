using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using AssetBundleBrowser.AssetBundleDataSource;
using Spenve;
using UnityEditor;

namespace AssetBundleBrowser
{
    [System.Serializable]
    internal class AssetBundleSettingTab
    {
        AssetBundleSettingTab m_InspectTab;
        Vector2 m_ScorllPosition;
        BundleConfig m_kBundleConfig;

        internal AssetBundleSettingTab()
        {
        }

        internal void OnEnable(Rect subPos, EditorWindow parent)
        {

        }

        internal void OnEnable(EditorWindow parent)
        {
            m_InspectTab = (parent as AssetBundleBrowserMain).m_BundleSettingTab;
            m_kBundleConfig = BundleConfig.Instance;
        }

        internal void OnDisable()
        {

        }

        public void OnGUI(Rect rect)
        {
            m_ScorllPosition = EditorGUILayout.BeginScrollView(m_ScorllPosition, false, true);

            GUILayout.Space(5);

            int _height = 20;
            int _butW = 20;
            int _toggleW = 120; 
            int _floderW = 160;
            int _pathfW = 300;

            float y = rect.y;

            Undo.RecordObject(m_kBundleConfig, "");
            List<BundleAttribute> bundles = m_kBundleConfig.allBundle;

            for (int i = 0; i < bundles.Count; i++) {
                BundleAttribute info = bundles[i];
                EditorGUILayout.BeginHorizontal();


                info.bundleName = GUILayout.TextArea(info.bundleName, GUILayout.Width(_floderW), GUILayout.Height(_height));

                GUILayout.Space(_butW);
                if (GUILayout.Button("+添加文件夹", GUILayout.Width(80), GUILayout.Height(_height))) {
                    string newPath = EditorUtility.OpenFolderPanel("Select a  folder .", "Assets/", "");
                    if (!string.IsNullOrEmpty(newPath)) {
                        int x2 = newPath.IndexOf("Assets");

                        string realPath = newPath.Substring(x2);
                        info.paths.Add(realPath);
                    }
                }

                GUILayout.Space(_butW);
                info.split = GUILayout.Toggle(info.split, "每个文件独立打包",GUILayout.Width(_toggleW), GUILayout.Height(_height));

                GUILayout.Space(_butW);
                info.needInstantiate = GUILayout.Toggle(info.needInstantiate, "使用Instantiate", GUILayout.Width(_toggleW), GUILayout.Height(_height));

                GUILayout.Space(_butW);
                info.usePool = GUILayout.Toggle(info.usePool, "使用内存池", GUILayout.Width(_toggleW), GUILayout.Height(_height)); 


                if (GUILayout.Button("-", GUILayout.Width(_butW), GUILayout.Height(_height))) { 
                    info.ClearAllBundleName();
                    bundles.RemoveAt(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();


                for (int j = 0; j < info.paths.Count; j++) {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(_floderW);
                    GUILayout.Space(_butW);

                    info.paths[j] = GUILayout.TextArea(info.paths[j], GUILayout.Width(_floderW), GUILayout.Height(_height));

                    if (GUILayout.Button("-", GUILayout.Width(_butW), GUILayout.Height(_height))) {
                        info.ClearPathBundleName(info.paths[j]);
                        info.paths.RemoveAt(j);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }


                GUILayout.Space(10);
                GUIStyle colorStyle = new GUIStyle();
                colorStyle.normal.textColor = new Color(0, 0.6f, 0);
                GUILayout.Label("____________________________________________________________________________________________________________________________", colorStyle);
            }



            GUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();

            int addButW = 100;
            int saveXmlButW = 100;
            int resetAllButW = 100;

            int rExpand = 40;

            if (GUILayout.Button(" + ", GUILayout.Width(addButW), GUILayout.Height(_height))) {
                BundleAttribute ba = new BundleAttribute();
                bundles.Add(ba);
            }

            GUILayout.Space(200);

            if (GUILayout.Button("Save", GUILayout.Width(saveXmlButW), GUILayout.Height(_height))) {
                EditorUtility.SetDirty(BundleConfig.Instance);
                AssetDatabase.SaveAssets();
            }

            GUILayout.Space(rExpand);

            if (GUILayout.Button("UpdateFiles", GUILayout.Width(resetAllButW), GUILayout.Height(_height))) {
                RefreshFiles();
            }

            GUILayout.Space(rExpand);

            if (GUILayout.Button("SetFileAbName", GUILayout.Width(resetAllButW), GUILayout.Height(_height))) {
                SetBundleName();
            }


            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

        }


        public static void RefreshFiles()
        {
            List<BundleAttribute> bundles = BundleConfig.Instance.allBundle;
            for (int i = 0; i < bundles.Count; i++) {
                BundleAttribute info = bundles[i];
                info.RefreshFiles();
            }
            Debug.Log("刷新文件成功！");
        }

        public static void SetBundleName()
        {
            List<BundleAttribute> bundles = BundleConfig.Instance.allBundle;
            for (int i = 0; i < bundles.Count; i++) {
                BundleAttribute info = bundles[i];
                info.SetBundleName();
            }
            Debug.Log("设置文件BundleName成功！");
        }

        [MenuItem("资源工具/刷新设置x _%_q", priority = 2051)]
        static void ShowWindow()
        {
            RefreshFiles();
            SetBundleName();
        }

    }
}