using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using System.Xml;
using UnityEditor;


namespace AssetBundleBrowser
{
    public class CreateUpdateRes : BasicPipeline
    {
        public CreateUpdateRes()
        {
            name = "CreateUpdateRes";
            tip = @"制作热更新资源（需存在第一次打包时的文件Md5列表，且不要和步骤4一起开）";
        }

        private string key = "Goodgoodstudy,daydayup!";

        public override int Process(Dictionary<string, object> objectInPipeline)
        {

            AssetBundleManifest mainfiest = objectInPipeline["manifest"] as AssetBundleManifest;

            //第一次打包保存的md5
            XmlDocument firstXml = new XmlDocument();

            string firstPath = Path.Combine(Utils.OutsideMd5Folder, "config.xml");

            firstPath = Application.dataPath.Replace("Assets", firstPath);
            string lstreamConfig = File.ReadAllText(firstPath);
            if (!string.IsNullOrEmpty(lstreamConfig))
                firstXml.LoadXml(lstreamConfig);

            XmlNode firstRoot = firstXml.SelectSingleNode("Assets");


            //-----
            string outputPath = Path.Combine(Utils.OutsideAbFolder, Utils.GetPlatformFolder());

            string hotXmlPath = Path.Combine(Utils.OutsideHotUpdateFolder, "update.xml");

            string hotPath = Application.dataPath.Replace("Assets", Utils.OutsideHotUpdateFolder);
            if (!Directory.Exists(hotPath) ){
                Directory.CreateDirectory(hotPath);
            }
            DirectoryInfo dir = new DirectoryInfo(hotPath);
            FileInfo[] files = dir.GetFiles();
            for(int i = 0; i < files.Length; i++) {
                files[i].Delete();
            }

            XmlDocument xml = new XmlDocument();
            XmlElement root = xml.CreateElement("Assets");
            root.SetAttribute("version", BuildPlayer.GetVersion());//PlayerSettings.bundleVersion);
            string[] allassets = mainfiest.GetAllAssetBundles();
            for (int i = 0; i < allassets.Length; i++) {

                bool needAddToUpdate = false;

                string localPath = outputPath + "/" + allassets[i];
                string allpath = Utils.OutsideRootFolder + localPath;

                string currentHash = Utils.md5file(allpath);

                //如果第一次打包的文件列表里没有或者有改变则更新
                XmlNode firstNode = firstRoot.SelectSingleNode(allassets[i]);
                if(firstNode == null) {
                    needAddToUpdate = true;
                }
                else {
                    string firstHash = firstNode.Attributes["hashcode"].Value;
                    if(!firstHash.Equals(currentHash)) {
                        needAddToUpdate = true;
                    }
                }

                if (needAddToUpdate) {
                    XmlElement element = xml.CreateElement(allassets[i]);

                    //TBC: change to md5 later
                    element.SetAttribute("hashcode", currentHash);

                    //write size of file
                    FileInfo fi = new FileInfo(localPath);
                    element.SetAttribute("size", fi.Length.ToString());
                    fi.CopyTo(hotPath + "/" + fi.Name);

                    //end
                    root.AppendChild(element);
                }
            }

            xml.AppendChild(root);
            xml.Save(hotXmlPath);

            return 0;
        }
    }
}
