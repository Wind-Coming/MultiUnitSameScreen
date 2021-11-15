using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Spenve
{
    public interface LoaderInterface
    {

        void Init();
        BundleConfig LoadConfig();
        T LoadAsset<T>(string resName, BundleAttribute bundleAttribute) where T : UnityEngine.Object;
        void LoadSceneRes(string sceneName);//场景的ab名字和场景名字要一样
        void UnloadBundle(string bundleName);
    }
}
