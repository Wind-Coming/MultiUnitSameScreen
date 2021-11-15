using Spenve;
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spenve
{
    public class ResourceSystem : SystemSingleton<ResourceSystem>
    {
        private AsyncOperation loadSceneOperation;
        private float loadSceneProgress;
        private LoaderInterface m_loader;

        private Dictionary<string, Dictionary<string, List<UnityEngine.Object>>> dynamicPool = new Dictionary<string, Dictionary<string, List<UnityEngine.Object>>>();

        protected override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            if (!EditorConfig.SimulateAssetBundleInEditor) {
                m_loader = new AssetDatabaseLoader();

            }
            else
#endif
        {
                m_loader = new AssetBundleLoader();
            }
            m_loader.Init();

        }

        public BundleConfig LoadConfig()
        {
            return m_loader.LoadConfig();
        }

        //同步加载assetbundle中的资源
        public T LoadAsset<T>(string resName) where T : UnityEngine.Object
        {
            BundleAttribute ba = null;
            if (!BundleConfig.NameToBundle.TryGetValue(resName, out ba)) {
                Debug.LogError("没有配置文件bundle， resName:" + resName);
                return null;
            }

            if (ba.usePool) {//从内存池里找
                Dictionary<string, List<UnityEngine.Object>> dpool = null;
                if (dynamicPool.TryGetValue(ba.bundleName, out dpool)) {
                    List<UnityEngine.Object> objList = null;
                    if (dpool.TryGetValue(resName, out objList)) {
                        if (objList.Count > 0) {
                            T t = objList[0] as T;
                            GameObject g = t as GameObject;
                            if (g != null) {
                                g.SetActive(true);
                            }
                            objList.RemoveAt(0);
                            return t;
                        }
                    }
                }
            }

            T obj = m_loader.LoadAsset<T>(resName, ba);

            if (obj != null) {
                obj.name = resName;

                //引用计数
                if (ba.resRefCount.ContainsKey(resName)) {
                    ba.resRefCount[resName]++;
                }
                else {
                    ba.resRefCount.Add(resName, 1);
                }

                Debug.LogWarning("加载" + resName + "的引用计数为" + ba.resRefCount[resName]);
            }

            return obj;
        }

        //卸载(加载后手动改了名字的请用下面的接口)
        public void UnLoad(UnityEngine.Object obj)
        {
            UnLoad(obj.name, obj);
        }

        public void UnLoad(string resName, UnityEngine.Object obj)
        {
            BundleAttribute ba = null;
            if (!BundleConfig.NameToBundle.TryGetValue(resName, out ba)) {
                Debug.LogError("没有配置文件bundle， resName:" + resName);
                return;
            }

            UnLoad(resName, obj, ba);
        }


        private void UnLoad(string resName, UnityEngine.Object obj, BundleAttribute bundleAttribute)
        {
            if (bundleAttribute.usePool) {
                Dictionary<string, List<UnityEngine.Object>> dpool = null;
                if (!dynamicPool.TryGetValue(resName, out dpool)) {
                    dpool = new Dictionary<string, List<UnityEngine.Object>>();
                    dynamicPool.Add(resName, dpool);
                }

                List<UnityEngine.Object> objList = null;
                if (!dpool.TryGetValue(resName, out objList)) {
                    objList = new List<UnityEngine.Object>();
                    dpool.Add(resName, objList);
                }

                objList.Add(obj);

                GameObject g = obj as GameObject;
                if (g != null)
                    g.SetActive(false);
            }
            else {
                //如果是prefab直接destroy
                if (GlobalFunc.CanInstantiate(BundleConfig.NameToSuffixIndex[resName])) {
                    GameObject.Destroy(obj);
                }

                //引用计数减一
                if (bundleAttribute.resRefCount.ContainsKey(resName)) {
                    bundleAttribute.resRefCount[resName]--;

                    Debug.LogWarning("卸载" + resName + "的引用计数为" + bundleAttribute.resRefCount[resName]);

                    if (bundleAttribute.resRefCount[resName] == 0) {
                        bundleAttribute.resRefCount.Remove(resName);

                        if (bundleAttribute.split) {
                            if(bundleAttribute.split) {
                                m_loader.UnloadBundle(resName.ToLower());
                            }
                            else {
                                if (bundleAttribute.resRefCount.Count == 0) {

                                    m_loader.UnloadBundle(bundleAttribute.bundleName);
                                }
                            }
                        }
                    }
                }
                else {
                    Debug.LogError("Bundle引用计数错误！ resName:" + resName);
                }
            }
        }

        //同步加载场景
        public void LoadScene(string sceneName)
        {
            m_loader.LoadSceneRes(sceneName);
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        }

        //异步加载场景
        public void LoadSceneAsync(string sceneName, Action complete = null)
        {
            m_loader.LoadSceneRes(sceneName);
            loadSceneOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            StartCoroutine(LoadSceneProgress(complete));
        }

        IEnumerator LoadSceneProgress(Action complete)
        {
            while (loadSceneOperation.progress < 1) {
                loadSceneProgress = loadSceneOperation.progress;
                yield return new WaitForEndOfFrame();
            }
            loadSceneProgress = 1;
            if (complete != null) {
                complete();
            }
        }
    }
}

//外部使用类
public class ResLoader: IReset
{
    public Dictionary<string, List<UnityEngine.Object>> refObj = new Dictionary<string, List<UnityEngine.Object>>();

    public T LoadAsset<T>(string resName) where T : UnityEngine.Object
    {
        T obj = ResourceSystem.Instance.LoadAsset<T>(resName);
        if(obj == null) {
            return null;
        }

        if (refObj.ContainsKey(resName)) {
            refObj[resName].Add(obj);
        }
        else {
            List<UnityEngine.Object> pool = ListPool<UnityEngine.Object>.Get();
            pool.Add(obj);
            refObj.Add(resName, pool);
        }
        return obj;
    }

    public void LoadSceneAsync(string sceneName)
    {
        ResourceSystem.Instance.LoadScene(sceneName);
    }

    public void Reset()
    {
        foreach(var v in refObj) {
            for(int i = 0; i < v.Value.Count; i++) {
                ResourceSystem.Instance.UnLoad(v.Key, v.Value[i]);
                ListPool<UnityEngine.Object>.Release(v.Value);
            }
        }

        refObj.Clear();
    }
}
