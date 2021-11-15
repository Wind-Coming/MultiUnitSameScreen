using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spenve;
using System.IO;

namespace Spenve
{
    internal class AssetBundleLoader : LoaderInterface
    {

        private Dictionary<string, AssetBundle> m_LoadedAssetBundles = new Dictionary<string, AssetBundle>();
        private Dictionary<string, string[]> m_Dependencies = new Dictionary<string, string[]>();
        private Dictionary<string, int> m_BundleReferencddCount = new Dictionary<string, int>();

        private AssetBundleManifest m_AssetBundleManifest = null;

        public void Init()
        {
            LoadAssetBundleManifest();
        }

        //加载全局依赖文件
        void LoadAssetBundleManifest()
        {
            AssetBundle assetb = LoadAssetBundle(Utils.GetPlatformFolder());
            m_AssetBundleManifest = assetb.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            //UnloadBundle(Utils.GetPlatformFolder());
        }


        //同步加载assetbundle
        private AssetBundle LoadAssetBundle(string assetbundleName, bool dependence = false)
        {
            AssetBundle bundle = null;
            m_LoadedAssetBundles.TryGetValue(assetbundleName, out bundle);
            if (bundle != null)
            {
                if (dependence)
                {
                    if (m_BundleReferencddCount.ContainsKey(assetbundleName))
                    {
                        m_BundleReferencddCount[assetbundleName]++;
                    }
                    else
                    {
                        m_BundleReferencddCount.Add(assetbundleName, 1);
                    }
                }
                return bundle;
            }

            string mPath = Utils.GetExternalPath(false, false) + assetbundleName;
            GLog.Log("Load in outStore : " + mPath, GameLogType.LOG_RES);

            AssetBundle asb = null;

            if (File.Exists(mPath))
            {
                asb = AssetBundle.LoadFromFile(mPath);
            }

            if (asb == null)
            {
                mPath = Utils.GetInnerPath(false) + assetbundleName;
                GLog.Log("Load in InnerStore : " + mPath, GameLogType.LOG_RES);

                asb = AssetBundle.LoadFromFile(mPath);
            }

            //没有找到资源
            if (asb == null)
            {
                GLog.Warning("资源" + assetbundleName + "不存在!", GameLogType.LOG_RES);
                return null;
            }
            else
            {
                Debug.LogWarning("Bundle加载：" + assetbundleName);

                m_LoadedAssetBundles.Add(assetbundleName, asb);

                LoadDependencies(assetbundleName);
                return asb;
            }
        }

        //加载依赖
        private void LoadDependencies(string assetBundleName)
        {
            if (m_AssetBundleManifest == null)
            {
                return;
            }

            string[] dependencies = m_AssetBundleManifest.GetAllDependencies(assetBundleName);
            if (dependencies.Length == 0)
                return;

            m_Dependencies.Add(assetBundleName, dependencies);
            for (int i = 0; i < dependencies.Length; i++)
            {
                LoadAssetBundle(dependencies[i], true);
            }
        }

        //卸载ab
        public void UnloadBundle(string assetBundleName)
        {
            AssetBundle bundle = null;
            m_LoadedAssetBundles.TryGetValue(assetBundleName, out bundle);
            if (bundle == null)
                return;

            if (!m_BundleReferencddCount.ContainsKey(assetBundleName) || --m_BundleReferencddCount[assetBundleName] == 0)
            {
                Debug.LogWarning("Bundle卸载：" + assetBundleName);
                bundle.Unload(true);
                m_LoadedAssetBundles.Remove(assetBundleName);

                string[] dependencies = null;
                if (!m_Dependencies.TryGetValue(assetBundleName, out dependencies))
                    return;

                foreach (string depen in dependencies)
                {
                    UnloadBundle(depen);
                }

                m_Dependencies.Remove(assetBundleName);
                m_BundleReferencddCount.Remove(assetBundleName);
            }

            GLog.Log("Unload : " + assetBundleName, GameLogType.LOG_RES);
        }


        //加载资源
        public T LoadAsset<T>(string resName, BundleAttribute bundleAttribute) where T : UnityEngine.Object
        {
            AssetBundle ab = null;
            if (bundleAttribute.split) {
                ab = LoadAssetBundle(resName.ToLower(), false);
            }
            else {
                ab = LoadAssetBundle(bundleAttribute.bundleName, false);
            }
            T obj = ab.LoadAsset<T>(resName);
            if(obj == null) {
                Debug.LogError("Cant find: " + resName);
                return null;
            }
            
            

            if(GlobalFunc.CanInstantiate(BundleConfig.NameToSuffixIndex[resName]) && bundleAttribute.needInstantiate) {
                return GameObject.Instantiate(obj);
            }
            else {
                return obj;
            }
        }

        public void LoadSceneRes(string sceneName)
        {
            LoadAssetBundle(sceneName);
        }

        public BundleConfig LoadConfig()
        {
            AssetBundle ab = LoadAssetBundle("config");
            return ab.LoadAsset<BundleConfig>("BundleConfig");
        }
    }
}