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
    public class CreateABConfig : BasicPipeline
    {

        private bool firstPackgeConfig = false;
        public CreateABConfig()
        {
            name = "CreateABConfig";
            tip = "第一次生成AB包文件信息，包含hashcode，size，用于后续对比制作热更新，输出目录为MD5File";
            canDisable = false;
            configable = true;
            showConfig = true;
        }

        protected override void DrawGUI()
        {
            firstPackgeConfig = EditorGUILayout.ToggleLeft("第一次整包，勾选备份文件md5", firstPackgeConfig);
        }

        public override int Process(Dictionary<string, object> objectInPipeline)
        {
            try
            {
                
                if (!objectInPipeline.ContainsKey("manifest"))
                {
                    Debug.Log("no mainfiest info,just skip");
                    return 0;
                }

                AssetBundleManifest mainfiest = objectInPipeline["manifest"] as AssetBundleManifest;

                //save config
                CreateConfigFile(mainfiest);

               return 0;
            }
            catch( Exception e )
            {
                Debug.Log(e.Message);
                return -1;
            }
        }

        private void CreateConfigFile(AssetBundleManifest assetbm)
        {
            string outputPath = Path.Combine(Utils.OutsideAbFolder, Utils.GetPlatformFolder());

            string xmlPath = Path.Combine(outputPath, "config.xml");

            if (!Directory.Exists(Application.dataPath.Replace("Assets", Utils.OutsideMd5Folder))) {
                Directory.CreateDirectory(Application.dataPath.Replace("Assets", Utils.OutsideMd5Folder));
            }

            XmlDocument xml = new XmlDocument();
            XmlElement root = xml.CreateElement("Assets");
            root.SetAttribute("version",BuildPlayer.GetVersion());//PlayerSettings.bundleVersion);
            string[] allassets = assetbm.GetAllAssetBundles();
            for (int i = 0; i < allassets.Length; i++)
            {
                XmlElement element = xml.CreateElement(allassets[i]);

                string localPath = outputPath + "/" + allassets[i];
                //write size of file
                FileInfo fi = new FileInfo(localPath);
                element.SetAttribute("size", fi.Length.ToString());

                //md5
                string allpath = Utils.OutsideRootFolder + localPath;
                element.SetAttribute("hashcode", Utils.md5file(allpath));


                //end
                root.AppendChild(element);
            }

            xml.AppendChild(root);
            xml.Save(xmlPath);

            if(firstPackgeConfig) {
                xml.Save(Path.Combine(Utils.OutsideMd5Folder, "config.xml"));
            }
        }

    }
}
