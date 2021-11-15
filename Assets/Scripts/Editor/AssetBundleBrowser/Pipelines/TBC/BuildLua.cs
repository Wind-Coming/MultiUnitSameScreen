using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AssetBundleBrowser
{
    public class BuildLua : BasicPipeline
    {
        //static string luaDir = Application.dataPath + "/Lua";                //lua逻辑代码目录
        //static string toluaDir = Application.dataPath + "/ToLua/Lua";        //tolua lua文件目录

       static List<string> luaPath = new List<string>();

        public BuildLua()
        {
            name = "BuildLua";
            tip = @"打包Lua代码，支持加密";

            canDisable = true;
            //showConfig = true;
        }

        protected override void DrawGUI()
        {
            //for (int i = 0; i < luaPath.Count; i++)
            //{
            //  EditorGUILayout.LabelField(luaPath[i]);
            //}

            //EditorGUILayout.BeginHorizontal();
            //if(GUILayout.Button("-"))
            //{
            //    luaPath.RemoveAt(luaPath.Count -1);
            //}

            //if(GUILayout.Button("+"))
            //{
            //   string newPath =  EditorUtility.OpenFolderPanel("Select lua scripts folder ." ,"Assets/" , "" );
            //   if(!string.IsNullOrEmpty(newPath))
            //   {
            //       luaPath.Add(newPath);
            //   }
            //}

            //EditorGUILayout.EndHorizontal();
        }

        public override int Process(Dictionary<string, object> objectInPipeline)
        {

           //if(luaPath.Count <=0)
           //   return 0;

          try 
          {
             SetLuaFile();
              return  0;
          }
          catch( Exception e )
          {
              Debug.Log(e.Message);
              return -2;
          }
        }

        static void SetLuaFile()
        {
            EditorUtility.DisplayProgressBar("窗口", "正在处理lua文件，稍等....", 0.1f);
            string tempDir = Application.dataPath + "/temp/Lua";

            if (!File.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            //CustomEncode(LuaConst.luaDir, tempDir);//自定义加密
            //CustomEncode(LuaConst.toluaDir, tempDir);
            //for (int i = 0; i < luaPath.Count; i++)
            //{
            //   string path = luaPath[i];
            //   if (!string.IsNullOrEmpty(path))
            //       CustomEncode(path, tempDir);
            //}
            //CopyLuaBytesFiles(LuaConst.luaDir, tempDir);//直接复制
            //CopyLuaBytesFiles(LuaConst.toluaDir, tempDir);
            //EncodeAllLuaFile(LuaConst.luaDir, tempDir);//jit编码
            //EncodeAllLuaFile(LuaConst.toluaDir, tempDir);

            AssetDatabase.Refresh();

            List<string> dirs = new List<string>();
            GetAllDirs(tempDir, dirs);

            for (int i = 0; i < dirs.Count; i++)
            {
                string str = dirs[i].Remove(0, tempDir.Length);
                BuildLuaBundle(str.Replace('\\', '/'), "Assets/temp/Lua");
            }

            BuildLuaBundle(null, "Assets/temp/Lua");

            AssetDatabase.SaveAssets();
            EditorUtility.ClearProgressBar();
        }

        static void BuildLuaBundle(string subDir, string sourceDir)
        {
            string[] files = Directory.GetFiles(sourceDir + subDir, "*.bytes");
            string bundleName = "lua.unity3d";// = subDir == null ? "lua.unity3d" : "lua" + subDir.Replace('/', '_') + ".unity3d";
            //bundleName = bundleName.ToLower();

            for (int i = 0; i < files.Length; i++)
            {
                AssetImporter importer = AssetImporter.GetAtPath(files[i]);

                if (importer)
                {
                    importer.assetBundleName = bundleName;
                    importer.assetBundleVariant = null;
                }
            }
        }

        static void GetAllDirs(string dir, List<string> list)
        {
            string[] dirs = Directory.GetDirectories(dir);
            list.AddRange(dirs);

            for (int i = 0; i < dirs.Length; i++)
            {
                GetAllDirs(dirs[i], list);
            }
        }

        static void CustomEncode(string sourceDir, string destDir, bool appendext = true, string searchPattern = "*.lua", SearchOption option = SearchOption.AllDirectories)
        {
            string[] files = Directory.GetFiles(sourceDir, searchPattern, option);
            int len = sourceDir.Length;

            if (sourceDir[len - 1] == '/' || sourceDir[len - 1] == '\\')
            {
                --len;
            }

            for (int i = 0; i < files.Length; i++)
            {
                string str = files[i].Remove(0, len);
                string dest = destDir + "/" + str;
                if (appendext) dest += ".bytes";
                string dir = Path.GetDirectoryName(dest);
                Directory.CreateDirectory(dir);
                string sourceStr = File.ReadAllText(files[i]);
                File.Copy(files[i], dest, true);
                File.WriteAllText(dest, Encrypt.EncodeBase64(sourceStr));
            }
        }

        public override void SetProperties()
        {
            SetProperty<int>("luapathcount" , luaPath.Count);
            for (int i = 0; i < luaPath.Count; i++)
            {
               string key = "luapath" + i;
               SetProperty<string>(key ,luaPath[i])  ;
            }
        }

        public override void GetProperties()
        {
            luaPath.Clear();

            int count = 0;
            GetProperty("luapathcount" ,ref count);
            for (int i = 0; i < count; i++)
            {
                string key = "luapath" + i;
                string path = "";
                GetProperty(key , ref path);
                if(!string.IsNullOrEmpty(path))
                    luaPath.Add(path);
            }
        }
    }
}
