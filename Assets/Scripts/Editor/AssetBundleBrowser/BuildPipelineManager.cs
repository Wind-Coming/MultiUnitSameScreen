using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace AssetBundleBrowser
{
    public enum BuildPipelineStep
    {
        None = 0,

        PreBuild,
        StandarBuildAssetBundle,
        CreateAssetBundleConfig,
        CreateAssetMap,
        CopyToStream,
        BuildPlayer,
        EncryptResource,
        CreateUpdateRes,

        BuildLua = 100,
        
    }

    public class BuildPipelineManager
    {
        public static List<BasicPipeline> pipelines = new List<BasicPipeline>();

        public static Dictionary<int, BasicPipeline> all = new Dictionary<int, BasicPipeline>();

        private static bool loaded = false;

        public static bool inProcess = false;
        public static int step = 0;
        public static int total = 0;
        private static int remain = 0;

        public static FraxniusBuildEditData config = null;

        private static Dictionary<string, object> buildArgs = null;

        public static BuildTarget buildTarget = BuildTarget.StandaloneWindows64;

        public static void Init()
        {
            if (all.Count > 0)
                return;

            all[(int)BuildPipelineStep.PreBuild] = new PreBuild();
            all[(int)BuildPipelineStep.StandarBuildAssetBundle] = new StandarBuildResource();
            all[(int)BuildPipelineStep.CreateAssetBundleConfig] = new CreateABConfig();
            all[(int)BuildPipelineStep.CreateAssetMap] = new CreateAssetMap();
            all[(int)BuildPipelineStep.BuildPlayer] = new BuildPlayer();
            all[(int)BuildPipelineStep.BuildLua] = new BuildLua();
            all[(int)BuildPipelineStep.CopyToStream] = new CopyToStream();
            all[(int)BuildPipelineStep.EncryptResource] = new  EncryptResources();
            all[(int)BuildPipelineStep.CreateUpdateRes] = new CreateUpdateRes();
        }

        public static void AddPipeline(int id, BasicPipeline pipeline) 
        {
            all[id] = pipeline;
        }

        public static void LoadPipeline(bool force = false) 
        {
            Init();

            inProcess = false;

            if (loaded && !force)
                return;

            string[] guids = AssetDatabase.FindAssets("BuildEditData");
            if (guids.Length <= 0)
            {
                config = ScriptableObject.CreateInstance<FraxniusBuildEditData>();
                AssetDatabase.CreateAsset(config, "Assets/BuildEditData.asset");
            }
            else
            {
                config = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), typeof(FraxniusBuildEditData)) as FraxniusBuildEditData;
            }

            pipelines.Clear();

            


            for (int i = 0; i < config.pipelines.Count; ++i)
            {
                int id = (int)config.pipelines[i];

                if (all.ContainsKey(id))
                {
                    pipelines.Add(all[id]);
                }
            }

            loaded = true;

        }

        public static void Refresh() 
        {
            foreach ( BasicPipeline bp in pipelines )
            {
                bp.Refresh();
            }

        }

        public static void StartBuild() 
        {
            total = 0;
            for (int i = 0; i < pipelines.Count; ++i)
            {
                BasicPipeline bp = pipelines[i];
                if (bp.enable)
                {
                    total++;
                }
            }

            step = 0;
            inProcess = total > 0;

            remain = total;
            buildArgs = new Dictionary<string, object>();
        }


        public static void BuildStep() 
        {
            if (step >= pipelines.Count)
                return;

            BasicPipeline bp = pipelines[step];
            if (!bp.enable)
            {
                step++;
                return;
            }

            bool ok = true;
            try
            {
                EditorUtility.DisplayProgressBar("Build ", "Building..." + (total - remain + 1) + "/" + total, (float)(total - remain) / (float)total);

                int result = bp.Process(buildArgs);
                if (result == 0)
                {
                    step++;
                    remain--;

                    if (remain <= 0)
                    {
                        inProcess = false;

                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Success","Build Finish","OK");
                    
                    }
                }
                else
                {
                    ok = false;
                }
            }
            catch(Exception e )
            {
                ok = false;
                Debug.LogException(e);

            }
            
            if ( !ok)
            {
                inProcess = false;
                EditorUtility.ClearProgressBar();
                EditorApplication.delayCall += () => { EditorUtility.DisplayDialog("Build Fail", "Build Step " + bp.name + ", is failed with exception,please check!", "OK"); };
            }
        }

        public static string GetPlatformFolder(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                default:
                    return target.ToString();
            }
        }
    }
}
