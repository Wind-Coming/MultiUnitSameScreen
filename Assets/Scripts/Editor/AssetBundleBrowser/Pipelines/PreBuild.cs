using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBrowser
{
    public class PreBuild : BasicPipeline
    {
        private BuildTarget buildTarget;
        private GUIContent m_TargetContent;

        private string outputPath = "";

        private bool clearOld = true;

        public PreBuild()
        {
            name = "preBuild";
            tip = @"预处理，清理文件夹";

            canDisable = false;
            configable = true;
            showConfig = true;

        }

        public override void Refresh()
        {
            buildTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildPipelineManager.buildTarget = buildTarget;

            m_TargetContent = new GUIContent(@"目标平台", "Choose target platform to build for.");

            outputPath = Path.Combine(Utils.OutsideAbFolder, Utils.GetPlatformFolder());
        }


        public override int Process(Dictionary<string, object> objectInPipeline)
        {            
            if (clearOld && Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);


            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            objectInPipeline["buildTarget"] = buildTarget;
            objectInPipeline["buildOutputPath"] = outputPath;
            return 0;
        }

        protected override void DrawGUI()
        {
            BuildTarget tgt = (BuildTarget)EditorGUILayout.EnumPopup(m_TargetContent, buildTarget);

            if (tgt != buildTarget)
            {
                buildTarget = tgt;
                outputPath = Path.Combine(Utils.OutsideAbFolder, Utils.GetPlatformFolder());

                BuildPipelineManager.buildTarget = buildTarget;
            }

            EditorGUILayout.LabelField(@"输出路径: " + outputPath);
            EditorGUILayout.Space();

            clearOld = EditorGUILayout.ToggleLeft(@"删除目标文件夹内容", clearOld);
        }

        public override void SetProperties()
        {
            SetProperty<bool>("DeleteDoc" , clearOld);
        }

        public override void GetProperties()
        {
             GetProperty("DeleteDoc", ref clearOld);
        }
    }
}
