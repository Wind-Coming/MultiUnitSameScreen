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
    public enum CompressOptions
    {
        Uncompressed = 0,
        StandardCompression,
        ChunkBasedCompression,
    }



    public class StandarBuildResource : BasicPipeline
    {
        class ToggleData
        {
            public ToggleData(bool s,
                string title,
                string tooltip,
                List<string> onToggles,
                BuildAssetBundleOptions opt = BuildAssetBundleOptions.None)
            {
                if (onToggles.Contains(title))
                    state = true;
                else
                    state = s;
                content = new GUIContent(title, tooltip);
                option = opt;
            }
            //public string prefsKey
            //{ get { return k_BuildPrefPrefix + content.text; } }
            public bool state;
            public GUIContent content;
            public BuildAssetBundleOptions option;
        }

        GUIContent m_CompressionContent;

        CompressOptions m_Compression = CompressOptions.StandardCompression;
        GUIContent[] m_CompressionOptions =
        {
            new GUIContent("No Compression"),
            new GUIContent("Standard Compression (LZMA)"),
            new GUIContent("Chunk Based Compression (LZ4)")
        };
        int[] m_CompressionValues = { 0, 1, 2 };

        List<ToggleData> m_ToggleData;

        public List<string> m_OnToggles = new List<string>();

        public StandarBuildResource()
        {
            name = "StandarBuildResource";
            tip = "Unity标准资源打包，需要在剪辑其中设置Assetbundle名称";
            configable = true;

 
            m_ToggleData = new List<ToggleData>();
            m_ToggleData.Add(new ToggleData(
                false,
                "Exclude Type Information",
                "Do not include type information within the asset bundle (don't write type tree).",
                m_OnToggles,
                BuildAssetBundleOptions.DisableWriteTypeTree));
            m_ToggleData.Add(new ToggleData(
                false,
                "Force Rebuild",
                "Force rebuild the asset bundles",
                m_OnToggles,
                BuildAssetBundleOptions.ForceRebuildAssetBundle));
            m_ToggleData.Add(new ToggleData(
                false,
                "Ignore Type Tree Changes",
                "Ignore the type tree changes when doing the incremental build check.",
                m_OnToggles,
                BuildAssetBundleOptions.IgnoreTypeTreeChanges));
            m_ToggleData.Add(new ToggleData(
                false,
                "Append Hash",
                "Append the hash to the assetBundle name.",
                m_OnToggles,
                BuildAssetBundleOptions.AppendHashToAssetBundleName));
            m_ToggleData.Add(new ToggleData(
                false,
                "Strict Mode",
                "Do not allow the build to succeed if any errors are reporting during it.",
                m_OnToggles,
                BuildAssetBundleOptions.StrictMode));
            m_ToggleData.Add(new ToggleData(
                false,
                "Dry Run Build",
                "Do a dry run build.",
                m_OnToggles,
                BuildAssetBundleOptions.DryRunBuild));


        }

        public override void Refresh()
        {
            m_CompressionContent = new GUIContent("Compression", "Choose no compress, standard (LZMA), or chunk based (LZ4)");


        }

        public override int Process(Dictionary<string, object> objectInPipeline)
        {
            try
            {
                BuildAssetBundleOptions opt = BuildAssetBundleOptions.None;

                if (m_Compression == CompressOptions.Uncompressed)
                    opt |= BuildAssetBundleOptions.UncompressedAssetBundle;
                else if (m_Compression == CompressOptions.ChunkBasedCompression)
                    opt |= BuildAssetBundleOptions.ChunkBasedCompression;
                foreach (var tog in m_ToggleData)
                {
                    if (tog.state)
                        opt |= tog.option;
                }

                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                if (objectInPipeline.ContainsKey("buildTarget"))
                {
                   BuildTarget sTarget = (BuildTarget)objectInPipeline["buildTarget"]; 
                   if( target != sTarget)
                   {
                      throw new Exception("Please choose the right platform first !!!");
                   }

                   target = sTarget;
                }

                string outputPath = Path.Combine(Utils.OutsideAbFolder, Utils.GetPlatformFolder());

                AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(outputPath, opt, target);

                objectInPipeline["manifest"] = manifest;

                return 0;
            }
            catch( Exception e )
            {
                Debug.LogException(e);
                return -1;
            }
        }


        protected override void DrawGUI()
        {
            
            CompressOptions cmp = (CompressOptions)EditorGUILayout.IntPopup(
                        m_CompressionContent,
                        (int)m_Compression,
                        m_CompressionOptions,
                        m_CompressionValues);

            if (cmp != m_Compression)
            {
                m_Compression = cmp;
            }

            bool newState = false;
            foreach (var tog in m_ToggleData)
            {
                newState = EditorGUILayout.ToggleLeft(
                    tog.content,
                    tog.state);
                if (newState != tog.state)
                {
                    if (newState)
                        m_OnToggles.Add(tog.content.text);
                    else
                        m_OnToggles.Remove(tog.content.text);
                    tog.state = newState;
                }
            }
            EditorGUILayout.Space();
        }

        public override void SetProperties()
        {
            SetProperty<int>("Compression" , (int)m_Compression);
            for (int i = 0; i < m_ToggleData.Count; i++)
            {
                SetProperty<bool>(m_ToggleData[i].content.text , m_ToggleData[i].state);
            }
        }

        public override void GetProperties()
        {
            m_OnToggles.Clear();

            int value = (int)m_Compression ;
            GetProperty("Compression" , ref value);
            m_Compression = (CompressOptions)value;

            for (int i = 0; i < m_ToggleData.Count; i++)
            {
                
              ToggleData data  = m_ToggleData[i];
              bool tag = false;
              GetProperty(data.content.text , ref tag);
              data.state = tag;
              if(data.state)              
              {
                  m_OnToggles.Add(data.content.text);
              }
            }
        }
    }
}
