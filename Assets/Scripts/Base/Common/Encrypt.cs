using UnityEngine;
using System.Collections;
using System;
using System.Text;

/// <summary>
/// 加密，解密字符串
/// </summary>
public static class Encrypt {
    private static string EncodeBase64(Encoding encode, string source)
    {
        string enstring = "";
        byte[] bytes = encode.GetBytes(source);
        try
        {
            enstring = Convert.ToBase64String(bytes);
        }
        catch
        {
            enstring = source;
        }
        return enstring;
    }

    /// <summary>
    /// Base64加密，采用utf8编码方式加密
    /// </summary>
    /// <param name="source">待加密的明文</param>
    /// <returns>加密后的字符串</returns>
    public static string EncodeBase64(string source)
    {
        return EncodeBase64(Encoding.UTF8, source);
    }

    /// <summary>
    /// Base64解密
    /// </summary>
    /// <param name="codeName">解密采用的编码方式，注意和加密时采用的方式一致</param>
    /// <param name="result">待解密的密文</param>
    /// <returns>解密后的字符串</returns>
    private static string DecodeBase64(Encoding encode, string result)
    {
        string decode = "";
        byte[] bytes = Convert.FromBase64String(result);
        try
        {
            decode = encode.GetString(bytes);
        }
        catch
        {
            decode = result;
        }
        return decode;
    }

    /// <summary>
    /// Base64解密，采用utf8编码方式解密
    /// </summary>
    /// <param name="result">待解密的密文</param>
    /// <returns>解密后的字符串</returns>
    public static string DecodeBase64(string result)
    {
        return DecodeBase64(Encoding.UTF8, result);
    }
}
