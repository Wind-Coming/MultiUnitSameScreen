using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using DG.Tweening;


public class GlobalFunc
{
    public static string[] Suffix = new string[] { ".prefab", ".asset", ".mat", ".png", ".ogg", ".tga",  ".mp3", ".wav", ".shader", ".anim", ".unity" };


    public static string GetFullPath(string localPath)
    {
        var fullPath = Application.dataPath.Replace("Assets", "") + localPath;
        return fullPath;
    }

    public static string GetLocalPath(string fullPath)
    {
        var localPath = fullPath.Replace(Application.dataPath, "Assets");
        return localPath;
    }

    public static string GetFileName(string path)
    {
        string[] ppp = path.Split('/');
        string pp = ppp[ppp.Length - 1];
        return pp;

    }

    public static string GetFileNameWithoutExtend(string path)
    {
        string[] ppp = path.Split('/');
        string pp = ppp[ppp.Length - 1];
        string p = pp.Remove(pp.IndexOf('.'));
        return p;
    }

    public static string GetFilePathNameWithoutExtend(string path)
    {
        string p = path.Remove(path.IndexOf('.'));
        return p;
    }


    public static string GetFolderName(string path)
    {
        int lastindex = path.LastIndexOf('/');
        string ppp = path.Substring(0, lastindex);
        return ppp;
    }

    public static string GetFileExtend(string fileName)
    {
        int lastindex = fileName.LastIndexOf('.');
        string ppp = fileName.Remove(0, lastindex);
        return ppp;
    }

    public static bool SupportSuffix(string suffix)
    {
        for (int n = 0; n < Suffix.Length; n++) {
            if (Suffix[n] == GlobalFunc.GetFileExtend(suffix)) {
                return true;
            }
        }
        return false;
    }

    public static int GetSuffixIndex(string suffix)
    {
        for (int n = 0; n < Suffix.Length; n++) {
            if (Suffix[n] == GlobalFunc.GetFileExtend(suffix)) {
                return n;
            }
        }
        return -1;
    }

    public static string GetSuffix(int index)
    {
        return Suffix[index];
    }

    public static bool CanInstantiate(int suffixIndex)
    {
        return suffixIndex == 0;
    }

    public static Transform GetTransform(Transform father, string name)
    {
        foreach (Transform t in father) {
            if (t.name.Equals(name)) {
                return t;
            }
            else {
                Transform tt = GetTransform(t, name);
                if (tt != null) {
                    return tt;
                }
            }
        }
        return null;
    }

    public static float NearestFloat(float v, float f)
    {
        float m = v % f;
        if (m < f / 2) {
            return v - m;
        }
        else {
            return v - m + f;
        }
    }

    //点到直线的距离
    public static float DisOfPointToLine(Vector3 point, Vector3 linePoint1, Vector3 linePoint2)
    {
        Vector3 vec1 = point - linePoint1;
        Vector3 vec2 = linePoint2 - linePoint1;
        Vector3 vecProj = Vector3.Project(vec1, vec2);
        float dis = Mathf.Sqrt(Mathf.Pow(Vector3.Magnitude(vec1), 2) - Mathf.Pow(Vector3.Magnitude(vecProj), 2));
        return dis;
    }

    /// <summary>
    /// 在animator中获取动画片段
    /// </summary>
    /// <param name="animator"></param>
    /// <param name="clipName"></param>
    /// <returns></returns>
    public static AnimationClip GetAnimationClip(Animator animator, string clipName)
    {
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        for (int i = 0; i < clips.Length; i++) {
            if (clips[i].name.Equals(clipName)) {
                return clips[i];
            }
        }
        return null;
    }

    public static void DelayFunc(Action action, float delayTime)
    {
        float v = 0;
        DOTween.To(() => v, x => v = x, 1, delayTime).OnComplete(() => action());
    }

    public static string GetNum(int num)
    {
        if (num < 10000)
            return num.ToString();
        else {
            return (num * 1.0f / 10000).ToString() + "万";
        }
    }

    public static string GetNum(long num)
    {
        if (num < 10000)
            return num.ToString();
        else {
            return (num * 1.0f / 10000).ToString() + "万";
        }
    }

    public static string GetCoinText(long coin)
    {
        return string.Format("{0:N0}", coin);
    }

    public static string GetZh_NTex(long coin)
    {
        if (coin < 1000000) {
            return coin.ToString();
        }
        else if (coin < 100000000) {
            return ((long)((coin / 10000.0f) * 1000) * 1.0f / 1000.0f).ToString() + "w";
        }
        else {
            return ((long)((coin / 100000000.0f) * 1000) * 1.0f / 1000.0f).ToString() + "y";
        }
    }

    public static string GetZh_NTex2(long coin)
    {
        if (coin < 1000000) {
            return coin.ToString();
        }
        else if (coin < 100000000) {
            return ((long)((coin / 10000.0f) * 1000) * 1.0f / 1000.0f).ToString() + "万";
        }
        else {
            return ((long)((coin / 100000000.0f) * 1000) * 1.0f / 1000.0f).ToString() + "亿";
        }
    }

    public static string GetZh_NTex(int coin)
    {
        return GetZh_NTex((long)coin);
    }

    public static string GetStringPrefs(string key, string df = "")
    {
        return PlayerPrefs.GetString(key, df);
    }

    public static int GetIntPrefs(string key, int df)
    {
        return PlayerPrefs.GetInt(key, df);
    }

    public static float GetFloatPrefs(string key, float df)
    {
        return PlayerPrefs.GetFloat(key, df);
    }




    public static void SetStringPrefs(string key, string df = "")
    {
        PlayerPrefs.SetString(key, df);
    }

    public static void SetIntPrefs(string key, int df)
    {
        PlayerPrefs.SetInt(key, df);
    }

    public static void SetFloatPrefs(string key, float df)
    {
        PlayerPrefs.SetFloat(key, df);
    }


    private static String[] Ls_ShZ = { "零", "壹", "贰", "叁", "肆", "伍", "陆", "柒", "捌", "玖", "拾" };
    private static String[] Ls_DW_Zh = { "", "拾", "佰", "仟", "万", "拾", "佰", "仟", "亿", "拾", "佰", "仟", "万" };
    private static String[] Num_DW = { "", "拾", "佰", "仟", "万", "拾", "佰", "仟", "亿", "拾", "佰", "仟", "万" };


    public static string Num2Zh_Hans(long Num)
    {

        string NumStr;//整个数字字符串
        string NumStr_Zh;//整数部分
        string NumStr_DQ;//当前的数字字符
        string NumStr_R = "";//返回的字符串

        if (Num == 0)
            return Ls_ShZ[0];

        NumStr = Num.ToString();

        NumStr_Zh = NumStr;//默认只有整数部分


        NumStr_Zh = new string(NumStr_Zh.ToCharArray().Reverse<char>().ToArray<char>());//反转字符串

        for (int a = 0; a < NumStr_Zh.Length; a++) {//整数部分转换
            NumStr_DQ = NumStr_Zh.Substring(a, 1);
            if (int.Parse(NumStr_DQ) != 0)
                NumStr_R = Ls_ShZ[int.Parse(NumStr_DQ)] + Ls_DW_Zh[a] + NumStr_R;
            else if (a == 0 || a == 4 || a == 8) {
                if (NumStr_Zh.Length > 8 && a == 4)
                    continue;
                NumStr_R = Ls_DW_Zh[a] + NumStr_R;
            }
            else if (int.Parse(NumStr_Zh.Substring(a - 1, 1)) != 0)
                NumStr_R = Ls_ShZ[int.Parse(NumStr_DQ)] + NumStr_R;

        }

        return NumStr_R;
    }

    private static float startSampleTime = 0;
    public static void BeginSample()
    {
        startSampleTime = Time.realtimeSinceStartup * 1000;
    }

    public static void EndSample()
    {
        Debug.Log( "耗时 ：" + (Time.realtimeSinceStartup * 1000 - startSampleTime) + "毫秒");
    }

}


public class PrefabsKeys
{
    public const string guestPasswd = "guestPasswd";
    public const string guestUserId = "guestUserId";

    public const string PrefabData = "PrefabData";
}

public enum ENM_URL
{
    Login,
    Register,
    ResetPsw,
    GetCode,
}