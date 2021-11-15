using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spenve
{
    internal class AssetDatabaseLoader : LoaderInterface
    {
        StringBuilder m_sStringBuilder = new StringBuilder();
        public void Init()
        {
        }

        //同步加载assetbundle中的资源
        public T LoadAsset<T>(string resName, BundleAttribute bundleAttribute) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            m_sStringBuilder.Clear();
            m_sStringBuilder.Append("/");
            m_sStringBuilder.Append(resName);
            int suffixIndex = BundleConfig.NameToSuffixIndex[resName];//只有返回0（prefab）可以实例化
            m_sStringBuilder.Append(GlobalFunc.GetSuffix(suffixIndex));

            string fileName = m_sStringBuilder.ToString();

            for (int i = 0; i < bundleAttribute.paths.Count; i++) {
                UnityEngine.Object n = AssetDatabase.LoadAssetAtPath(bundleAttribute.paths[i] + fileName, typeof(T));
                if (n != null) {
                    //如果后缀是prefab，并且bundle使用实例化
                    if (GlobalFunc.CanInstantiate(suffixIndex) && bundleAttribute.needInstantiate) {
                        return GameObject.Instantiate(n) as T;
                    }
                    else {
                        return n as T;
                    }
                }
            }

            Debug.LogError("Cant find res : " + fileName);
#endif
            return null;
        }

        public void LoadSceneRes(string sceneName)
        {
            throw new NotImplementedException();
        }

        public BundleConfig LoadConfig()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<BundleConfig>("Assets/Res/Config/BundleConfig.asset");
#endif
            return null;
        }

        public void UnloadBundle(string assetBundleName)
        {

        }
    }
}