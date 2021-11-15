using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBrowser
{
    public class CopyToStream : BasicPipeline
    {
        private string m_streamingPath = Application.streamingAssetsPath + @"/AssetBundles";

        public CopyToStream()
        {
            name = "CopyToStream";
            tip = @"拷贝资源到StreamAsset";
            canDisable = true;
        }


        public override int Process(Dictionary<string, object> objectInPipeline)
        {
           // string tempPath = BuildPipelineManager.config.tempFolder;
            string output = objectInPipeline["buildOutputPath"] as string;


            string path = m_streamingPath + "/" + Utils.GetPlatformFolder();
           //var destination = System.IO.Path.Combine(System.Environment.CurrentDirectory, path);
           if (Directory.Exists(path))
               Directory.Delete(path, true);

           DirectoryCopy(output, path);

            AssetDatabase.Refresh();


            return 0;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }


            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            }
        }

    }
}
