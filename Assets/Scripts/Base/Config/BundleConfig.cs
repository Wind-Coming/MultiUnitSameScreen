using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

namespace Spenve
{
    [Serializable]
    public class BundleAttribute
    {
        #region 序列化数据
        public string bundleName = "bundleName";
        public List<string> paths = new List<string>();     //该bundle包含的路径
        public bool split;                                  //是否拆分为每个文件一个bundle
        public bool needInstantiate;                        //是否需要实例化
        public bool usePool;                                //是否使用内存池
        public List<string> files = new List<string>();     //包含的所有文件
        public List<int> fileSuffixIndex = new List<int>(); //文件后缀索引
        public List<int> fileIndex = new List<int>();       //文件的路径索引

        #endregion

        #region 运行时数据

        [NonSerialized]
        public Dictionary<string, int> resRefCount = new Dictionary<string, int>();//资源的引用计数

        #endregion

        #region 编辑器函数
#if UNITY_EDITOR
        public void RefreshFiles()
        {
            files.Clear();
            fileSuffixIndex.Clear();
            fileIndex.Clear();
            for (int k = 0; k < paths.Count; k++) {
                string fullPath = GlobalFunc.GetFullPath(paths[k]);
                var dir = new DirectoryInfo(fullPath);
                var allFiles = dir.GetFiles("*", SearchOption.TopDirectoryOnly);
                for (var i = 0; i < allFiles.Length; ++i) {
                    var fileInfo = allFiles[i];

                    if (!fileInfo.Name.EndsWith(".meta")) {
                        var basePath = fileInfo.FullName.Replace('\\', '/');
                        files.Add(GlobalFunc.GetFileNameWithoutExtend(basePath));
                        fileIndex.Add(k);

                        if (!GlobalFunc.SupportSuffix(GlobalFunc.GetFileExtend(basePath))) {
                            Debug.LogError("暂不支持此文件后缀！" + basePath);
                        }
                        else {
                            fileSuffixIndex.Add(GlobalFunc.GetSuffixIndex(GlobalFunc.GetFileExtend(basePath)));
                        }
                    }
                }
            }
        }

        public void SetBundleName()
        {
            for (int k = 0; k < paths.Count; k++) {
                string fullPath = GlobalFunc.GetFullPath(paths[k]);
                var dir = new DirectoryInfo(fullPath);
                var allFiles = dir.GetFiles("*", SearchOption.TopDirectoryOnly);
                for (var i = 0; i < allFiles.Length; ++i) {
                    EditorUtility.DisplayProgressBar("设置AssetName名称", bundleName, k * 1.0f / allFiles.Length);
                    var fileInfo = allFiles[i];

                    if (!fileInfo.Name.EndsWith(".meta")) {
                        var basePath = fileInfo.FullName.Replace('\\', '/');
                        string path = GlobalFunc.GetLocalPath(basePath);
                        var importer = AssetImporter.GetAtPath(path);
                        if (split || basePath.EndsWith(".unity")) {//设置为单独打包的或者场景文件
                            importer.assetBundleName = GlobalFunc.GetFileNameWithoutExtend(basePath).ToLower();
                        }
                        else {
                            importer.assetBundleName = bundleName;
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }

        public void ClearAllBundleName()
        {
            for (int k = 0; k < paths.Count; k++) {
                ClearPathBundleName(paths[k]);
            }
        }


        public void ClearPathBundleName(string bpath)
        {
            string fullPath = GlobalFunc.GetFullPath(bpath);
            var dir = new DirectoryInfo(fullPath);
            var allFiles = dir.GetFiles("*", SearchOption.TopDirectoryOnly);
            for (var i = 0; i < allFiles.Length; ++i)
            {
                EditorUtility.DisplayProgressBar("设置AssetName名称", bundleName, i * 1.0f / allFiles.Length);
                var fileInfo = allFiles[i];

                if (!fileInfo.Name.EndsWith(".meta"))
                {
                    var basePath = fileInfo.FullName.Replace('\\', '/');
                    string path = GlobalFunc.GetLocalPath(basePath);
                    var importer = AssetImporter.GetAtPath(path);
                    importer.assetBundleName = null;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        public void RefreshAndSetAbName()
        {
            RefreshFiles();
            SetBundleName(); 
        }
#endif

        #endregion 
    }

    [Serializable]
    [CreateAssetMenu(menuName = "Config/Create BundleConfig Asset")]
    public class BundleConfig : ScriptableObject
    {
        private static BundleConfig _instance;

        public static BundleConfig Instance
        {
            get
            {
                if (null == _instance) {
                    Init();
                }

                return _instance;
            }
        }

        public static List<BundleAttribute> Bundles
        {
            get
            {
                return Instance.allBundle;
            }
        }

        private Dictionary<string, BundleAttribute> nameToBundleDic;
        private Dictionary<string, int> nameToSuffixIndex;

        public static Dictionary<string, BundleAttribute> NameToBundle
        {
            get
            {
                return Instance.nameToBundleDic;
            }
        }

        public static Dictionary<string, int> NameToSuffixIndex
        {
            get
            {
                return Instance.nameToSuffixIndex;
            }
        }


        private static void Init()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) {
                string xmlPath = "Assets/Res/Config/BundleConfig.asset";
                _instance = AssetDatabase.LoadAssetAtPath<BundleConfig>(xmlPath);
            }
            else
#endif
        {
                InitConfig();
            }
        }

        private static void InitConfig()
        {
            _instance = ResourceSystem.Instance.LoadConfig();
            _instance.nameToBundleDic = new Dictionary<string, BundleAttribute>();
            _instance.nameToSuffixIndex = new Dictionary<string, int>();
            for (int i = 0; i < _instance.allBundle.Count; i++) {
                for (int j = 0; j < _instance.allBundle[i].files.Count; j++) {
                    string key = _instance.allBundle[i].files[j];
                    if (_instance.nameToBundleDic.ContainsKey(key)) {
                        Debug.LogError("存在重名文件！name:" + key);
                        continue;
                    }
                    _instance.nameToBundleDic.Add(key, _instance.allBundle[i]);
                    _instance.nameToSuffixIndex.Add(key, _instance.allBundle[i].fileSuffixIndex[j]);
                }
            }
        }

        public List<BundleAttribute> allBundle = new List<BundleAttribute>();
    }
}