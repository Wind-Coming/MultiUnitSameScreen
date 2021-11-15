using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBrowser
{
    public class CopyToOutput : BasicPipeline
    {
        public CopyToOutput()
        {
            name = "CopyToOutput";
            tip = @"拷贝资源到输出目录，删除临时文件夹";
            canDisable = true;
        }


        public override int Process(Dictionary<string, object> objectInPipeline)
        {
            string tempPath = Path.Combine(Utils.OutsideAbFolder, Utils.GetPlatformFolder());
            string output = objectInPipeline["buildOutputPath"] as string;

            var source = Path.Combine(System.Environment.CurrentDirectory, tempPath);
            if (!System.IO.Directory.Exists(source))
                UnityEngine.Debug.Log("No assetBundle output folder, try to build the assetBundles first.");

            // Setup the destination folder for assetbundles.
            //var destination = System.IO.Path.Combine(System.Environment.CurrentDirectory, output);

            DirectoryCopy(tempPath, output);

            //del temp folder
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);

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
