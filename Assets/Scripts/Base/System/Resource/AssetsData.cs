using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "AssetsData", menuName = "Config/BuildAssetsData", order = 2)]
public class AssetsData : ScriptableObject
{
    public List<AssetsPool> assetsPools = new List<AssetsPool>();

    [System.NonSerialized]
    public Dictionary<AssetType, AssetsPool> poolDic = new Dictionary<AssetType, AssetsPool>();

    public void Init()
    {
        if (poolDic.Count > 0)
            return;

        for (int i = 0; i < assetsPools.Count; i++) {
            assetsPools[i].Init();
            poolDic.Add(assetsPools[i].assetType, assetsPools[i]);
        }
    }
}

[System.Serializable]
public class AssetsPool
{
    public AssetType assetType;
    public string extend = "";
    public string abName = "";
    public bool useInstance = true;
    public List<string> assetPathList = new List<string>();
    public List<AssetUnit> assetsList = new List<AssetUnit>();

    [System.NonSerialized]
    public Dictionary<string, AssetUnit> assetDic = new Dictionary<string, AssetUnit>();

    public void Init()
    {
        if (assetDic.Count > 0)
            return;

        for (int j = 0; j < assetsList.Count; j++) {
            assetsList[j].m_kResourcesPathName = assetsList[j].m_kResPathName.Replace("Assets/ABResources/Resources/", "");
            assetDic.Add(assetsList[j].m_kResName, assetsList[j]);
        }
    }
}

[System.Serializable]
public class AssetUnit
{
    //资源名称
    public string m_kResName;
    //资源完整路径
    public string m_kResPathName;

    //Resources路径
    [System.NonSerialized]
    public string m_kResourcesPathName;
}

[System.Serializable]
public enum AssetType
{
    PREFAB = 0,
    GFX,
    UI,
    AUDIO,
    TEXTURE,
    MATERIAL,
    SPRITE,
    CONFIG,
}