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
    public class CreateAssetMap : BasicPipeline
    {
        public CreateAssetMap()
        {
            name = "CreateAssetMap";
            tip = @"创建资源映射文件";
        }


        public override int Process(Dictionary<string, object> objectInPipeline)
        {
            XmlDocument xml = new XmlDocument();
            XmlElement root = xml.CreateElement("AssetMap");
            xml.AppendChild(root);

            Dictionary<string,string> ids = new Dictionary<string,string>();

            var names = AssetDatabase.GetAllAssetBundleNames();
            foreach (string name in names)
            {
                var assets = AssetDatabase.GetAssetPathsFromAssetBundle(name);
                foreach (string aname in assets)
                {
                    XmlElement sub = xml.CreateElement("Asset");

                    string id = aname;

                    int index = aname.LastIndexOf(Path.AltDirectorySeparatorChar);
                    id = index > 0 ? aname.Substring(index+1):aname;

                    index = id.LastIndexOf(".");
                    id = index > 0 ? id.Substring(0, index) : id;

                    if (ids.ContainsKey(id))
                    {
                        Debug.LogError("Duplicated asset id " + id + ", between " + aname + " and " + ids[id]);
                        return -1;
                    }

                    ids[id] = aname;

                    sub.SetAttribute("id", id);
                    sub.SetAttribute("ab", name);

                    root.AppendChild(sub);
                }
            }

            string tempPath = Utils.OutsideAbFolder;
            xml.Save( Path.Combine( tempPath, "assetmap.xml"));

            objectInPipeline["useAssetMap"] = true;

            return 0;
        }
    }

}
