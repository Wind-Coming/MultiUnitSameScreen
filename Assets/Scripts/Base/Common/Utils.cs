using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Net;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class Utils
{
    private static DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static long Jan1st1970_tick = Jan1st1970.Ticks;

    //private static DateTime Jan1st1970Local = Jan1st1970.ToLocalTime();

    public static long GetCurrentTime()
    {
        return (long)((DateTime.UtcNow - Jan1st1970).TotalMilliseconds);
    }

    private static long serverTimeFixed = 0L;
    private static long connectTimeDelay = 0;

    public static long GetServerTime()
    {
        return GetCurrentTime() + serverTimeFixed;
    }

    public static int GetServerTimeInSeconds()
    {
        return (int)(GetServerTime() / 1000L);
    }

    public static void SetServerTime(long serverTime)
    {
        long now = GetCurrentTime();
        serverTimeFixed = serverTime + connectTimeDelay - now;
    }

    public static DateTime ParseJavaTime(long time)
    {
        long time_tricks = Jan1st1970_tick + time * 10000;//日志日期刻度
        DateTime dt = new DateTime(time_tricks, DateTimeKind.Utc);//转化为DateTime
        return dt;
    }


    #region Path 

    public static string OutsideAbFolder            = "AssetBundles";
    public static string OutsideMd5Folder           = "MD5File";
    public static string OutsideHotUpdateFolder     = "HotUpdate";
    public static string OutsideApkFolder           = "Build";

    public static string OutsideRootFolder
    {
        get
        {
            return Application.dataPath.Replace("Assets", "");
        }
    }

    public static string GetPlatformFolder()
    {
#if UNITY_EDITOR
        switch (EditorUserBuildSettings.activeBuildTarget) {
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "iOS";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                return "Windows";
            case BuildTarget.StandaloneOSX:
                return "OSX";
            default:
                return null;
        }
#else
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";
                case RuntimePlatform.OSXPlayer:
                    return "OSX";
                default:
                    return null;
            }

#endif
    }

    public static string GetExternalPath(bool write = false, bool streamRead = false)
    {
        string path = "";
        if (write) {
            if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer) {
                path = System.Environment.CurrentDirectory.Replace("\\", "/") + "/" + OutsideHotUpdateFolder;
            }
            else {
                path = Application.persistentDataPath;
            }
        }
        else {
            if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer) {
                if (streamRead) {
                    path = "file://" + System.Environment.CurrentDirectory.Replace("\\", "/") + "/" + OutsideHotUpdateFolder;
                }
                else {
                    path = System.Environment.CurrentDirectory.Replace("\\", "/") + "/" + OutsideHotUpdateFolder;
                }
            }
            else {
                if (streamRead) {
                    path = "file://" + Application.persistentDataPath;
                }
                else {
                    path = Application.persistentDataPath;
                }
            }
        }
        return path + "/";
    }

    public static string GetInnerPath(bool streamRead = false)
    {
        string path = "";
        if (Application.isEditor) {
            if (streamRead) {
                path = "file://" + System.Environment.CurrentDirectory.Replace("\\", "/") + "/AssetBundles/" + GetPlatformFolder();
            }
            else {
                path = System.Environment.CurrentDirectory.Replace("\\", "/") + "/AssetBundles/" + GetPlatformFolder();
            }
        }
        else if (Application.platform == RuntimePlatform.WindowsPlayer) {
            if (streamRead) {
                path = "file://" + Application.streamingAssetsPath + "/AssetBundles/" + GetPlatformFolder();
            }
            else {
                path = Application.streamingAssetsPath + "/AssetBundles/" + GetPlatformFolder();
            }
        }
        else if (Application.platform == RuntimePlatform.Android) {
            if (streamRead) {
                path = Application.streamingAssetsPath + "/AssetBundles/" + GetPlatformFolder();
            }
            else {
                path = Application.dataPath + "!assets" + "/AssetBundles/" + GetPlatformFolder();
            }
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer) {
            if (streamRead) {
                path = "file://" + Application.streamingAssetsPath + "/AssetBundles/" + GetPlatformFolder();
            }
            else {
                path = Application.dataPath + "/Raw" + "/AssetBundles/" + GetPlatformFolder();
            }
        }
        return path + "/";
    }

    #endregion


    public static IPAddress ParseIP(string ipString)
    {
        //Debug.Log(ipString);
        //if (Application.platform == RuntimePlatform.IPhonePlayer)
        //{
        //    IOSAddressItem[] list = IOSIPV6.ResolveIOSAddress(ipString);
        //    return list[0].ip;
        //}
        //else
        //{
        try {
            return IPAddress.Parse(ipString);
        }
        catch (Exception) {
            IPAddress[] IPs = Dns.GetHostAddresses(ipString);
            return IPs[0];
        }
        //}
    }

    /// <summary>
    /// 计算文件的MD5值-------原来ulua的打包方式
    /// </summary>
    public static string md5file(string file)
    {
        try {
            FileStream fs = new FileStream(file, FileMode.Open);
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(fs);
            fs.Close();

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++) {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
        catch (Exception ex) {
            throw new Exception("md5file() fail, error:" + ex.Message);
        }
    }
}
