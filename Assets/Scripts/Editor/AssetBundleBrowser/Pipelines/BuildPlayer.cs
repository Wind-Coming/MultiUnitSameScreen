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
    public class BuildPlayer : BasicPipeline
    {
        private List<bool> buildList = new List<bool>();
        //private List<EditorBuildSettingsScene> levels = new List<EditorBuildSettingsScene>();
        private bool developBuild = true;

        private bool showScene = false;

        private bool updateVersion = true;

        private string buildPath = "";

        private int oldVersionCode = 0;
        private string oldVersionString = "";

        private int targetVersionCode = 0;
        private string targetVersionString = "";

        private string[] versions = null;

        private BuildTarget currentTarget = BuildTarget.StandaloneWindows64;

        public BuildPlayer()
        {
            name = "BuildPlayer";
            tip = @"生成项目文件或XCode项目";

            configable = true;
            showConfig = true;
        }

        public override void Refresh()
        {
            currentTarget = BuildPipelineManager.buildTarget;

            buildList.Clear();

            for (int i = 0; i < EditorBuildSettings.scenes.Length; ++i) {
                EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];

                //only the first scene will build in default
                buildList.Add(i == 0);
            }

            developBuild = EditorUserBuildSettings.development;

            buildPath = Path.Combine(Utils.OutsideApkFolder, developBuild ? "Debug" : "Release");
            buildPath = Path.Combine(buildPath, Utils.GetPlatformFolder());

            oldVersionString = PlayerSettings.bundleVersion;

            oldVersionCode = GetVersionCode(currentTarget);

            targetVersionCode = oldVersionCode;//+1;

            versions = oldVersionString.Split('.');

            bool changed = false;
            if (versions.Length < BuildPipelineManager.config.versionUnit )
            {
                int oldL = versions.Length;

                string[] nversions = new string[BuildPipelineManager.config.versionUnit];

                int add = BuildPipelineManager.config.versionUnit - oldL;
                for (int i = 0; i < oldL; ++i )
                {
                    nversions[i] = versions[i];
                }

                for (int i = 0; i < add; ++i)
                {
                    nversions[i+oldL] = "0";
                }

                versions = nversions;
                changed = true;
            }

            targetVersionString = GetVersionString();

            if (changed)
            {
                PlayerSettings.bundleVersion = targetVersionString;
                oldVersionString = targetVersionString;
            }

        }

        private string GetVersionString() 
        {
            string temp = "";
            for (int i = 0; i < versions.Length-1; ++i )
            {
                temp += versions[i];
                temp += ".";  
            }

            if (this.updateVersion){
               temp += ( int.Parse(versions[versions.Length - 1])).ToString();
            }
            else
                temp += (versions[versions.Length - 1]).ToString();

            return temp;
        }


        public static string GetVersion()
        {
           BasicPipeline o = null;
            if(BuildPipelineManager.all.TryGetValue((int)BuildPipelineStep.BuildPlayer , out o))
            {
               return (o as BuildPlayer).targetVersionString;
            }

            return PlayerSettings.bundleVersion;
        }
        protected override void DrawGUI()
        {
            if (buildList.Count != EditorBuildSettings.scenes.Length)
                Refresh();

            BuildTarget t = BuildPipelineManager.buildTarget;
            if (t != currentTarget)
            {
                currentTarget = t;

                //refresh
                oldVersionCode = GetVersionCode(currentTarget);
                versions = oldVersionString.Split('.');

                targetVersionCode = oldVersionCode;// + 1;

                this.targetVersionString = GetVersionString();

                buildPath = Path.Combine(Utils.OutsideApkFolder, developBuild ? "Debug" : "Release");
                buildPath = Path.Combine(buildPath, Utils.GetPlatformFolder());   
            }


            //build path
            bool db = EditorGUILayout.ToggleLeft("DevelopBuild", developBuild);
            if (db != developBuild)
            {
                developBuild = db;

                buildPath = Path.Combine(Utils.OutsideApkFolder, developBuild ? "Debug" : "Release");
                buildPath = Path.Combine(buildPath, Utils.GetPlatformFolder());
            }
            EditorGUILayout.LabelField("Build Path: " + buildPath);
            EditorGUILayout.Space();

            //build version
            this.targetVersionString = EditorGUILayout.TextField("BundleVersion: ", this.targetVersionString);
            this.targetVersionCode = EditorGUILayout.IntField("BuildNumber: ",this.targetVersionCode);

            //bool update = EditorGUILayout.ToggleLeft("自动升级版本号", updateVersion);
            //if (update != updateVersion)
            //{
            //    updateVersion = update;

            //    //refresh version code
            //    if (!updateVersion)
            //    {
            //        versions = oldVersionString.Split('.');
            //    }
            //    this.targetVersionString = GetVersionString();               
            //}
            EditorGUILayout.Space();

            showScene = EditorGUILayout.Foldout(showScene, "Scenes");
            if (showScene)
            {
                for (int i = 0; i < EditorBuildSettings.scenes.Length; ++i)
                {
                    EditorBuildSettingsScene scene = EditorBuildSettings.scenes[i];
                    if (!scene.enabled)
                        continue;

                    buildList[i] = EditorGUILayout.ToggleLeft(scene.path, buildList[i]);
                }
            }
        }

        public override int Process(Dictionary<string, object> objectInPipeline)
        {
            BuildTarget target = (BuildTarget)objectInPipeline["buildTarget"];
            if (target != this.currentTarget)
            {
                UnityEngine.Debug.LogError("Target changed while buiding");
                return -2;
            }

            var name = Application.productName + (this.developBuild ? "Debug" : "") + oldVersionCode + GetBuildTargetName(target);
            var outputPath = Path.Combine(buildPath, name);

            string[] levels = GetLevelsFromBuildSettings();
            if (levels.Length == 0)
            {
                UnityEngine.Debug.LogError("Nothing to build.");
                return -1;
            }

            if (target == BuildTarget.iOS)
            {
                if (Directory.Exists(outputPath))
                {
                    Directory.Delete(outputPath, true);
                }
                Directory.CreateDirectory(outputPath);
            }

            // Build and copy AssetBundles.
            try
            {
                //set version data
                PlayerSettings.bundleVersion = targetVersionString;

                SetVersionCode(target,targetVersionCode);


                BuildOptions option = developBuild ? BuildOptions.Development : BuildOptions.None;
                UnityEditor.Build.Reporting.BuildReport error = BuildPipeline.BuildPlayer(levels, outputPath, target, option);

                //if (!string.IsNullOrEmpty(error.name))
                //{
                //    throw new Exception("Build Error "+error);
                //}

                EditorUtility.RevealInFinder(outputPath);

                EditorApplication.delayCall += Refresh;

            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("Has exception in build player, rollback the version config");

                PlayerSettings.bundleVersion = oldVersionString;
                SetVersionCode(target, oldVersionCode);

                return -3;
            }

            return 0;
        }

        private void SetVersionCode(BuildTarget target, int targetVersionCode)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    PlayerSettings.Android.bundleVersionCode = targetVersionCode;
                    break;
                case BuildTarget.iOS:
                    PlayerSettings.iOS.buildNumber = targetVersionCode.ToString();
                    break;
                default:
                    EditorPrefs.SetInt("bundleVersionCode",targetVersionCode);
                    break;
            }
        }


        private int GetVersionCode(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.Android:
                    return PlayerSettings.Android.bundleVersionCode;
                case BuildTarget.iOS:
                    return int.Parse(PlayerSettings.iOS.buildNumber);
                default:
                    return EditorPrefs.HasKey("bundleVersionCode") ? EditorPrefs.GetInt("bundleVersionCode") : 0;
            }
        }

        public string GetBuildTargetName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return ".apk";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return ".exe";
                case BuildTarget.StandaloneOSX:
                    return ".app";
                case BuildTarget.iOS:
                    return "";
                // Add more build targets for your own.
                default:
                    UnityEngine.Debug.Log("Target not implemented.");
                    return "";
            }
        }

        string[] GetLevelsFromBuildSettings()
        {
            List<string> levels = new List<string>();
            for (int i = 0; i < EditorBuildSettings.scenes.Length; ++i)
            {
                if ( this.buildList[i] )
                    levels.Add(EditorBuildSettings.scenes[i].path);
            }

            return levels.ToArray();
        }

        public override void SetProperties()
        {
            SetProperty<bool>("DevelopBuild" , developBuild);
            SetProperty<bool>("AutoUpdate" , updateVersion);
        }

        public override void GetProperties()
        {
            GetProperty("DevelopBuild",ref developBuild);
            GetProperty("AutoUpdate" ,ref updateVersion);
        }
    }

}
