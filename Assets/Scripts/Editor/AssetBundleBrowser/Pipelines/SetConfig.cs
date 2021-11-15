using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBrowser
{
    public class SetConfig : BasicPipeline
    {
        private string[] configFiles = null;

        private int select = 0;

        private const string despath = "Assets/Resources/GameConfig.asset";
        public SetConfig()
        {
            name = "SetConfig";
            tip = @"设置配置文件";

            configable = true;
        }

        public override void Refresh()
        {
            configFiles = AssetDatabase.FindAssets("l:GameConfig");
        }


        protected override void DrawGUI()
        {
            if (configFiles == null || configFiles.Length <= 0)
            {
                EditorGUILayout.LabelField("Not has any GameConfig file,Please check!");
                return;
            }

            for (int i = 0; i < configFiles.Length; ++i )
            {
                bool db = EditorGUILayout.ToggleLeft("Config File = "+ AssetDatabase.GUIDToAssetPath(configFiles[i]), i == select);
                if (db)
                {
                    select = i;
                }
            }
        }

        public override int Process(Dictionary<string, object> objectInPipeline)
        {
            if (configFiles == null)
            {
                UnityEngine.Debug.LogError("Not has any GameConfig file,Please check!");
                return -2;
            }

            AssetDatabase.DeleteAsset("Assets/Resources/GameConfig.asset");

            string path = AssetDatabase.GUIDToAssetPath(configFiles[select]);
            AssetDatabase.CopyAsset(path, despath);
            AssetDatabase.SaveAssets();

           UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(despath);
           AssetDatabase.ClearLabels(obj);

            return 0;
        }
    }

}
