
using System.IO;
using Spenve;
using UnityEngine;
using System;

[Serializable]
[CreateAssetMenu(menuName = "Config/Create GameConfig Asset")]
public class GameConfig : ScriptableObject
{
    private static GameConfig _instance;

    public static GameConfig Instance
    {
        get
        {
            if (null == _instance) {
                Roload();
            }

            return _instance;
        }
    }

    public static void Roload()
    {
        ResLoader kLoader = ClassPool<ResLoader>.Get();
        _instance = kLoader.LoadAsset<GameConfig>("GameConfig");
    }

    public string CdnURL;

    public string ResourceServerUrl;

    public string LoginServerUrl;

    public string GameServerUrl;



    public static string GetUpdateResourceUrl(string url)
    {
        return Path.Combine(Instance.CdnURL, url);
    }
}
