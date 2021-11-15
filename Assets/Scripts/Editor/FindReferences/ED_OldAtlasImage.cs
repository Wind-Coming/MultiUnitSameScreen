using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class ED_OldAtalsImage : Editor
{
    private static string m_kUIPrefabsPath = Application.dataPath + "/C4Project/Bundle/UI";
    private string m_kAtlasPath = "Atlas/";
    private static string m_kAtlasAssetPath = "Assets/C4Project/Bundle/Atlas/";
    private Dictionary<string, Dictionary<string, Sprite>> m_kAtlasMap;//图集的集合      
    private List<string> uiAllImageList = new List<string>();
    private List<string> uiNotHaveImageList = new List<string>();
    private List<string> suspectedInUseImageList = new List<string>();

    //只是用于查找什么图片被哪些cs脚本，lua，csv引用
    private List<string> suspectedUseByExcludePrefab = new List<string>();
    private List<string> referenceLogList = new List<string>();

    private static string m_kLuaScriptPath = Application.dataPath + "/C4Project/Script/LuaFile";
    private static string m_kCSScriptPath = Application.dataPath + "/C4Project/Script";

    [MenuItem("Tools/图集/查找没有用到的atlas资源")]
    static void Init()
    {
        ED_OldAtalsImage eo = new ED_OldAtalsImage();
        eo.GetAllUIImage();
        eo.GetAllAtlasSprite();

        eo.GetCSScripteImage();
        eo.GetLuaScripteImage();
    }

    public void GetAllAtlasSprite()
    {    
        string[] atlas = { "UI3_1Atlas", "UI3_Map", "UI3Atlas", "IconAtlas" };
        for (int j = 0; j < atlas.Length; j++)
        {
            Object[] kSpriteObjects = kSpriteObjects = AssetDatabase.LoadAllAssetsAtPath(m_kAtlasAssetPath + atlas[j] + ".png");
            if (kSpriteObjects.Length == 0)
            {
                Debug.LogError("Atlas Load Error " + atlas[j]);
                return;
            }
            Dictionary<string, Sprite> kAtlas = new Dictionary<string, Sprite>();

            for (int i = 0; i < kSpriteObjects.Length; i++)
            {
                if (kSpriteObjects[i].GetType() == typeof(UnityEngine.Sprite))
                {
                    Sprite kSprite = (Sprite)kSpriteObjects[i];
                    string name = atlas[j] + "/" + kSprite.name;
                    if (!uiAllImageList.Contains(name))
                    {
                        if (!uiNotHaveImageList.Contains(name))
                        {
                            uiNotHaveImageList.Add(name);
                        }
                    }
                    else
                    {
                        int a = 1;
                        a += 1;
                    }
                }
            }
        }
        Debug.Log("------------------------------UI Not Used--------------------------------------------"+uiNotHaveImageList.Count);       
    }

    public void GetAllUIImage()
    {
        List<string> kFiles = new List<string>(Directory.GetFiles(m_kUIPrefabsPath, "*.prefab", SearchOption.AllDirectories));
        Debug.Log("----------------------UI Count---------------------------------" + kFiles.Count);        

        for (int iIndex = 0; iIndex < kFiles.Count; iIndex++)
        {
            string kPath = kFiles[iIndex];
            int nPos = kPath.IndexOf("Assets");
            string kAssetPath = kPath.Substring(nPos);
            GameObject kPrefabs = AssetDatabase.LoadAssetAtPath(kAssetPath, typeof(GameObject)) as GameObject;
            if (kPrefabs == null)
                continue;

            Image[] children = kPrefabs.GetComponentsInChildren<Image>(true);
            foreach (Image child in children)
            {
                if (child == null)
                    continue;
                if (child.mainTexture == null)
                    continue;
                if (child.sprite == null)
                    continue;
                string name = child.mainTexture.name + "/" + child.sprite.name;
                if (!uiAllImageList.Contains(name))
                    uiAllImageList.Add(name);
            }

            SpriteRenderer[] childrenRender = kPrefabs.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer child in childrenRender)
            {
                if (child == null)
                    continue;
                if (child.sprite == null)
                    continue;
                if (child.sprite.texture == null)
                    continue;
                string name = child.sprite.texture.name + "/" + child.sprite.name;
                if (!uiAllImageList.Contains(name))
                    uiAllImageList.Add(name);
            }
        }
        Debug.Log("----------------------UI Used---------------------------------" + uiAllImageList.Count);
    }

    public void GetCSScripteImage(bool bLogReferece = false)
    {
        List<string> kFiles = new List<string>(Directory.GetFiles(m_kCSScriptPath, "*.cs", SearchOption.AllDirectories));
        Debug.Log("----------------------CS File Count---------------------------------" + kFiles.Count);
  
        for (int iIndex = 0; iIndex < kFiles.Count; iIndex++)
        {
            string kPath = kFiles[iIndex];
            int nPos = kPath.IndexOf("Assets");
            string kAssetPath = kPath.Substring(nPos);
            TextAsset kCSScript = AssetDatabase.LoadAssetAtPath(kAssetPath, typeof(TextAsset)) as TextAsset;
            if (kCSScript == null)
                continue;
            string CSContent = kCSScript.text.ToString();
            for (int i = uiNotHaveImageList.Count - 1; i >= 0; --i)
            {
                if (bContainSprite(CSContent, uiNotHaveImageList[i]))
                {
                    if (!bLogReferece)
                    {
                        uiNotHaveImageList.RemoveAt(i);
                    }

                    if (bLogReferece)
                    {
                        if (!suspectedUseByExcludePrefab.Contains(uiNotHaveImageList[i]))
                        {
                            suspectedUseByExcludePrefab.Add(uiNotHaveImageList[i]);
                        }

                        referenceLogList.Add("CS 脚本名称： " + kAssetPath + " 引用到的atlas图片为： " + uiNotHaveImageList[i]);
                    }
                }
            }
        }

        Debug.Log("----------------------CS Used Rest---------------------------------" + uiNotHaveImageList.Count);
    }

    public void GetLuaScripteImage(bool bLogReferece = false)
    {
        List<string> kFiles = new List<string>(Directory.GetFiles(m_kLuaScriptPath, "*.lua", SearchOption.AllDirectories));
        Debug.Log("----------------------LUA File Count---------------------------------" + kFiles.Count);
        
        for (int iIndex = 0; iIndex < kFiles.Count; iIndex++)
        {
            string luaFilePath = kFiles[iIndex];

            string CSContent = File.ReadAllText(luaFilePath);
            for (int i = uiNotHaveImageList.Count - 1; i >= 0; --i)
            {
                if (bContainSprite(CSContent, uiNotHaveImageList[i]) || bContainSprite(CSContent, uiNotHaveImageList[i], "\'"))
                {
                    if (!bLogReferece)
                    {
                        uiNotHaveImageList.RemoveAt(i);
                    }

                    if (bLogReferece)
                    {
                        if (!suspectedUseByExcludePrefab.Contains(uiNotHaveImageList[i]))
                        {
                            suspectedUseByExcludePrefab.Add(uiNotHaveImageList[i]);
                        }

                        referenceLogList.Add("Lua 脚本名称： " + Path.GetFileName(luaFilePath) + " 引用到的atlas图片为： " + uiNotHaveImageList[i]);
                    }
                }
            }
        }

        Debug.Log("----------------------LUA Used Rest---------------------------------" + uiNotHaveImageList.Count);
    }

    private bool bContainSprite(string content, string iImageName, string quoteStr = "\"", bool bCsv = false)
    {
        string[] imageNameArray = iImageName.Split('/');
        if (imageNameArray.Length != 2)
        {
            return false;
        }

        string tmpName = imageNameArray[1];

        if (!bCsv)
        {
            tmpName = quoteStr + tmpName + quoteStr;
        }

        if (content.Contains(tmpName))
        {
            return true;
        }

        if (!bCsv)
        {
            int idx = tmpName.LastIndexOf("_", StringComparison.Ordinal);

            if (idx > 0)
            {
                string subStr = tmpName.Substring(0, idx + 1) + quoteStr;

                if (content.Contains(subStr))
                {
                    if (!suspectedInUseImageList.Contains(tmpName))
                    {
                        suspectedInUseImageList.Add(iImageName);
                    }
                    return true;
                }
            }
        }

        return false;
    }

    [MenuItem("Tools/图集/手动复查/除Prefab外没有用到的atlas资源")]
    public static void ManualCheckImage(MenuCommand menuComman)
    {
        string[] assetGuid = Selection.assetGUIDs;//需要一个txt记录需要对应的资源。格式是 "atlas/资源名称 ……"
        ED_OldAtalsImage eo = new ED_OldAtalsImage();

        for (int idx = 0; idx < assetGuid.Length; ++idx)
        {
            string kAssetPath = AssetDatabase.GUIDToAssetPath(assetGuid[idx]);
            TextAsset kCSScript = AssetDatabase.LoadAssetAtPath(kAssetPath, typeof(TextAsset)) as TextAsset;

            if (kCSScript == null)
                continue;
            string CSContent = kCSScript.text.ToString();
            string[] imageNameArray = CSContent.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            eo.CheckImageForManualCustom(imageNameArray);
        }
    }

    [MenuItem("Tools/图集/手动复查/查找在refab中的引用")]
    public static void FindInAllPrefabUI(MenuCommand menuComman)
    {
        atlasSpirieList.Clear();

        string[] assetGuid = Selection.assetGUIDs;//需要一个txt记录需要对应的资源。格式是 "atlas/资源名称 ……"
        ED_OldAtalsImage eo = new ED_OldAtalsImage();

        for (int idx = 0; idx < assetGuid.Length; ++idx)
        {
            string kAssetPath = AssetDatabase.GUIDToAssetPath(assetGuid[idx]);
            TextAsset kCSScript = AssetDatabase.LoadAssetAtPath(kAssetPath, typeof(TextAsset)) as TextAsset;

            if (kCSScript == null)
                continue;
            string CSContent = kCSScript.text.ToString();
            string[] imageNameArray = CSContent.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < imageNameArray.Length; ++i)
            {
                string[] tmpArray = imageNameArray[i].Split('|');
                atlasSpirieList.Add(tmpArray[0]);

                if (tmpArray.Length > 1)
                {
                    targetAtlasSpirieList.Add(tmpArray[1]);
                }
            }
        }

        List<string> bReplacedPrefabList = new List<string>();
        List<string> kFiles = new List<string>(Directory.GetFiles(m_kUIPrefabsPath, "*.prefab", SearchOption.AllDirectories));

        for (int idx = 0; idx < kFiles.Count; ++idx)
        {
            string kPath = kFiles[idx];
            int nPos = kPath.IndexOf("Assets");

            string kAssetPath = kPath.Substring(nPos);
            string outStr;
            if (_replacePrefabByPath(kAssetPath, out outStr, true))
            {
                bReplacedPrefabList.Add(outStr);
                bReplacedPrefabList.Add("上面Obj引用到的Prefab：" + kPath);
            }
        }

        _outputResult(bReplacedPrefabList);
        Debug.Log(" ------All Done -");
    }


    public void CheckImageForManualCustom(string[] imageNameArray)
    {
        //检查一下有没有通用前缀的版本
        uiNotHaveImageList.AddRange(imageNameArray);
        GetCSScripteImage(true);
        GetLuaScripteImage(true);
    }

    [MenuItem("Tools/图集/查找单一prefab资源/各obj详细清单")]
    public static void CheckSpecialUISpritesObjName(MenuCommand menuComman)
    {
        _checkSpecialUISprite(true);
    }

    private static void _checkSpecialUISprite(bool bShowName = false)
    {
        GameObject[] objArray = Selection.gameObjects;
        ED_OldAtalsImage eo = new ED_OldAtalsImage();
        List<string> useImageList = new List<string>();
        List<string> useTexture = new List<string>();

        for (int idx = 0; idx < objArray.Length; ++idx)
        {
            GameObject obj = objArray[idx];

            Image[] children = obj.GetComponentsInChildren<Image>(true);
            foreach (Image child in children)
            {
                if (child == null)
                    continue;
                if (child.mainTexture == null)
                    continue;

                string name = child.sprite == null ? child.mainTexture.name : child.mainTexture.name + "/" + child.sprite.name;
                if (bShowName)
                {
                    name += "|" + child.name;
                }

                if (child.sprite == null)
                {
                    if (!useTexture.Contains(name))
                        useTexture.Add(name);
                }
                else
                {
                    if (!useImageList.Contains(name))
                        useImageList.Add(name);
                }

            }
        }

        string m_kLog = "";
        for (int i = 0; i < useImageList.Count; i++)
        {
            m_kLog += useImageList[i] + "\n";
        }

        for (int i = 0; i < useTexture.Count; i++)
        {
            m_kLog += useTexture[i] + "\n";
        }

        Debug.Log(m_kLog);
    }

    [MenuItem("Tools/图集/查找单一prefab资源/atlas资源汇总")]
    public static void CheckSpecialUISprites(MenuCommand menuComman)
    {
        _checkSpecialUISprite(false);
    }

    [MenuItem("Tools/图集/从原来的Atlas里移出查找出来的图")]
    public static void MoveImages(MenuCommand menuComman)
    {
        string[] assetGuid = Selection.assetGUIDs;//需要一个txt记录需要对应的资源。格式是 "atlas/资源名称 ……"
        ED_OldAtalsImage eo = new ED_OldAtalsImage();

        for (int idx = 0; idx < assetGuid.Length; ++idx)
        {
            string kAssetPath = AssetDatabase.GUIDToAssetPath(assetGuid[idx]);
            TextAsset kCSScript = AssetDatabase.LoadAssetAtPath(kAssetPath, typeof(TextAsset)) as TextAsset;

            if (kCSScript == null)
                continue;
            string CSContent = kCSScript.text.ToString();
            string[] imageNameArray = CSContent.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            string targetFilePath = Application.dataPath.Replace("Client/Assets", "Misc/tmp/sprites");
            string filePath = Application.dataPath.Replace("Client/Assets", "Misc/UI");
            string[] allfiles = Directory.GetFiles(filePath, "*.png", SearchOption.AllDirectories);

            List<string> allFileList = new List<string>();
            allFileList.AddRange(allfiles);

            Debug.Log("File Count : " + allFileList.Count + " and imageCount is : " + imageNameArray.Length);

            for (int i = imageNameArray.Length - 1; i >= 0; --i)
            {
                for (int k = allFileList.Count - 1; k >= 0; --k)
                {
                    string kPath = allFileList[k];
                    string fileName = Path.GetFileNameWithoutExtension(kPath);
                    string[] nameArray = imageNameArray[i].Split('/');

                    if (kPath.Contains(nameArray[0]) && fileName.Equals(nameArray[1]))
                    {
                        string targetFile = targetFilePath + "/" + fileName + ".png";
                        File.Move(kPath, targetFile);
                        allFileList.RemoveAt(k);
                        break;
                    }
                }
            }

            Debug.Log("All Move Done.");
        }
    }


    [MenuItem("Tools/图集/DeleteImages")]
    public static void DeleteImages(MenuCommand menuComman)
    {
        string[] assetGuid = Selection.assetGUIDs; //需要一个txt记录需要对应的资源。格式是 "atlas/资源名称 ……"
        ED_OldAtalsImage eo = new ED_OldAtalsImage();

        for (int idx = 0; idx < assetGuid.Length; ++idx)
        {
            string kAssetPath = AssetDatabase.GUIDToAssetPath(assetGuid[idx]);
            TextAsset kCSScript = AssetDatabase.LoadAssetAtPath(kAssetPath, typeof(TextAsset)) as TextAsset;

            if (kCSScript == null)
                continue;
            string CSContent = kCSScript.text.ToString();
            string[] imageNameArray = CSContent.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            string filePath = Application.dataPath.Replace("Client/Assets", "Misc/UI");
            string[] allfiles = Directory.GetFiles(filePath, "*.png", SearchOption.AllDirectories);

            List<string> allFileList = new List<string>();
            allFileList.AddRange(allfiles);

            filePath = Application.dataPath.Replace("Client/Assets", "UI_Atlas");
            allfiles = Directory.GetFiles(filePath, "*.png", SearchOption.AllDirectories);
            allFileList.AddRange(allfiles);

            Debug.Log("File Count : " + allFileList.Count + " and imageCount is : " + imageNameArray.Length);

            for (int i = imageNameArray.Length - 1; i >= 0; --i)
            {
                for (int k = allFileList.Count - 1; k >= 0; --k)
                {
                    string kPath = allFileList[k];
                    string fileName = Path.GetFileNameWithoutExtension(kPath);
                    string[] nameArray = imageNameArray[i].Split('/');

                    if (kPath.Contains(nameArray[0]) && fileName.Equals(nameArray[1]))
                    {
                        File.Delete(kPath);
                        allFileList.RemoveAt(k);
                        break;
                    }
                }
            }

            Debug.Log("All Delete Done. Left Count : " + allFileList.Count);
        }
    }

    private static List<string> atlasSpirieList = new List<string>();
    private static List<string> targetAtlasSpirieList = new List<string>();

    [MenuItem("Tools/图集/替换所有的Prefab")]
    public static void ReplaceAllPrefabUI(MenuCommand menuComman)
    {
        atlasSpirieList.Clear();

        string[] assetGuid = Selection.assetGUIDs;//需要一个txt记录需要对应的资源。格式是 "atlas/资源名称|替换的atlas/替换的资源名称 ……"
        ED_OldAtalsImage eo = new ED_OldAtalsImage();

        for (int idx = 0; idx < assetGuid.Length; ++idx)
        {
            string kAssetPath = AssetDatabase.GUIDToAssetPath(assetGuid[idx]);
            TextAsset kCSScript = AssetDatabase.LoadAssetAtPath(kAssetPath, typeof(TextAsset)) as TextAsset;

            if (kCSScript == null)
                continue;
            string CSContent = kCSScript.text.ToString();
            string[] imageNameArray = CSContent.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < imageNameArray.Length; ++i)
            {
                string[] tmpArray = imageNameArray[i].Split('|');
                atlasSpirieList.Add(tmpArray[0]);

                if (tmpArray.Length > 1)
                {
                    targetAtlasSpirieList.Add(tmpArray[1]);
                }
            }
        }

        List<string> bReplacedPrefabList = new List<string>();
        List<string> kFiles = new List<string>(Directory.GetFiles(m_kUIPrefabsPath, "*.prefab", SearchOption.AllDirectories));

        for (int idx = 0; idx < kFiles.Count; ++idx)
        {
            string kPath = kFiles[idx];
            int nPos = kPath.IndexOf("Assets");

            string kAssetPath = kPath.Substring(nPos);
            string outStr;
            if (_replacePrefabByPath(kAssetPath, out outStr))
            {
                bReplacedPrefabList.Add("被替换的Prefab：" + kPath);
            }
        }

        _outputResult(bReplacedPrefabList);
        Debug.Log(" ------All Done -");
    }

    [MenuItem("Tools/图集/检查atlas之间是否有同名的贴图")]
    public static void CheckRepeatBetweenAtals(MenuCommand menuComman)
    {
        List<string> repeatTextureAtlasName = new List<string>();
        List<string> repeatTexture = new List<string>();
        List<string> allImageList = new List<string>();

        string[] atlas = { "UI3_1Atlas", "UI3_Map", "UI3Atlas", "IconAtlas" };
        for (int j = 0; j < atlas.Length; j++)
        {
            Object[] kSpriteObjects = AssetDatabase.LoadAllAssetsAtPath(m_kAtlasAssetPath + atlas[j] + ".png");
            if (kSpriteObjects.Length == 0)
            {
                Debug.LogError("Atlas Load Error " + atlas[j]);
                return;
            }

            for (int i = 0; i < kSpriteObjects.Length; i++)
            {
                if (kSpriteObjects[i].GetType() == typeof(UnityEngine.Sprite))
                {
                    Sprite kSprite = (Sprite)kSpriteObjects[i];
                    string name = kSprite.name;
                    if (allImageList.Contains(name))
                    {
                        repeatTexture.Add(name);
                        repeatTextureAtlasName.Add(atlas[j] + "/" + name);
                    }
                    else
                    {
                        allImageList.Add(name);
                    }
                }
            }
        }

        _outputResult(repeatTextureAtlasName);

        Debug.Log("------------Check repeat done----------");
    }

    private static bool _replacePrefabByPath(string prefabPath, out string objSpritePaireStr, bool bJustForFind = false)
    {
        GameObject obj = PrefabUtility.LoadPrefabContents(prefabPath);
        objSpritePaireStr = "";

        if (obj == null)
        {
            return false;
        }

        bool bReplaced = false;
        
        Image[] children = obj.GetComponentsInChildren<Image>(true);
        foreach (Image child in children)
        {
            if (child == null)
                continue;
            if (child.mainTexture == null)
                continue;
            if (child.sprite == null)
                continue;
            string name = child.mainTexture.name + "/" + child.sprite.name;

            if (atlasSpirieList.Contains(name))
            {
                if (!bJustForFind)
                {
                    int idx = atlasSpirieList.IndexOf(name);

                    if (targetAtlasSpirieList.Count > idx)
                    {
                        string[] targetInfo = targetAtlasSpirieList[idx].Split('/');

                        if (targetInfo.Length == 2)
                        {
                            //需要被替换
                            //Sprite s = UI_SpriteManager.LoadSpriteInEditor(targetInfo[0], targetInfo[1]);

                            //if (s == null)
                            //{
                            //    Debug.LogError(targetInfo[0] + " atlas  中没有找到 " + targetInfo[1]);
                            //}
                            //else
                            //{
                            //    child.sprite = s;
                            //}
                        }
                    }
                }
                else
                {
                    objSpritePaireStr += ("\n" + child.name + "|" + name);
                }

                bReplaced = true;
            }
        }

        if (bReplaced && !bJustForFind)
        {
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
        }
        
        // Clean up
        PrefabUtility.UnloadPrefabContents(obj);

        return bReplaced;
    }

    [MenuItem("Tools/图集/Texture/检查单边大于128大小的图片")]
    public static void CheckLargeSpriteInAtals(MenuCommand menuComman)
    {
        List<string> allImageList = new List<string>();
        List<string> allBorderLargeList = new List<string>();

        string[] atlas = { "UI3_1Atlas", "UI3Atlas"};
        for (int j = 0; j < atlas.Length; j++)
        {
            Object[] kSpriteObjects = AssetDatabase.LoadAllAssetsAtPath(m_kAtlasAssetPath + atlas[j] + ".png");
            if (kSpriteObjects.Length == 0)
            {
                Debug.LogError("Atlas Load Error " + atlas[j]);
                return;
            }

            for (int i = 0; i < kSpriteObjects.Length; i++)
            {
                if (kSpriteObjects[i].GetType() == typeof(UnityEngine.Sprite))
                {
                    Sprite kSprite = (Sprite)kSpriteObjects[i];
                    string name = atlas[j] + "/" + kSprite.name;

                    if ((kSprite.border.x > 0 || kSprite.border.y > 0 || kSprite.border.z > 0 || kSprite.border.w > 0) &&
                        (kSprite.rect.width >= 100 || kSprite.rect.height >= 100))
                    {
                        allBorderLargeList.Add("border large sprite : " + name);
                    }
                    else if ((kSprite.rect.width >= 128 && kSprite.rect.height >= 30) || (kSprite.rect.width >= 30 && kSprite.rect.height >= 128))
                    {
                        allImageList.Add(name);
                    }
                }
            }
        }

        allImageList.AddRange(allBorderLargeList);
        _outputResult(allImageList);

        Debug.Log("------------Check CheckLargeSpriteInAtals done----------");
    }

    [MenuItem("Tools/图集/Texture/替换所有的Prefab，替换成对应的Texture")]
    public static void ReplaceAllPrefabUIToTexture(MenuCommand menuComman)
    {
        atlasSpirieList.Clear();

        string[] assetGuid = Selection.assetGUIDs;//需要一个txt记录需要对应的资源。格式是 "atlas/资源名称|替换的atlas/替换的资源名称 ……"
        ED_OldAtalsImage eo = new ED_OldAtalsImage();

        for (int idx = 0; idx < assetGuid.Length; ++idx)
        {
            string kAssetPath = AssetDatabase.GUIDToAssetPath(assetGuid[idx]);
            TextAsset kCSScript = AssetDatabase.LoadAssetAtPath(kAssetPath, typeof(TextAsset)) as TextAsset;

            if (kCSScript == null)
                continue;
            string CSContent = kCSScript.text.ToString();
            string[] imageNameArray = CSContent.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < imageNameArray.Length; ++i)
            {
                string[] tmpArray = imageNameArray[i].Split('|');
                atlasSpirieList.Add(tmpArray[0]);
            }
        }

        List<string> bReplacedPrefabList = new List<string>();
        List<string> kFiles = new List<string>(Directory.GetFiles(m_kUIPrefabsPath, "*.prefab", SearchOption.AllDirectories));

        for (int idx = 0; idx < kFiles.Count; ++idx)
        {
            string kPath = kFiles[idx];
            int nPos = kPath.IndexOf("Assets");

            string kAssetPath = kPath.Substring(nPos);
            string outStr;
            if (_replacePrefabByPathToTexture(kAssetPath, out outStr))
            {
                bReplacedPrefabList.Add("被替换的Prefab：" + kPath + "， 详情如下：");
                bReplacedPrefabList.Add(outStr);
            }
        }

        _outputResult(bReplacedPrefabList);
        Debug.Log(" ------All Done -");
    }

    private static bool _replacePrefabByPathToTexture(string prefabPath, out string objSpritePaireStr)
    {
        GameObject obj = PrefabUtility.LoadPrefabContents(prefabPath);
        objSpritePaireStr = "";

        if (obj == null)
        {
            return false;
        }

        bool bReplaced = false;

        Image[] children = obj.GetComponentsInChildren<Image>(true);
        foreach (Image child in children)
        {
            if (child == null)
                continue;
            if (child.mainTexture == null)
                continue;
            if (child.sprite == null)
                continue;
            string spriteName = child.sprite.name;
            string name = child.mainTexture.name + "/" + spriteName;

            if (atlasSpirieList.Contains(name))
            {
                Sprite targetSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/C4Project/Bundle/Textures/UI/TextureInAtlas/" + spriteName + ".png");
                if (targetSprite != null)
                {
                    child.sprite = targetSprite;
                    objSpritePaireStr += child.name + "|" + spriteName + " 被替换成Sprite\n";
                }
                else
                {
                    Texture targetTex = AssetDatabase.LoadAssetAtPath<Texture>("Assets/C4Project/Bundle/Textures/UI/TextureInAtlas/" + spriteName + ".png");

                    if (targetTex != null)
                    {
                        Color previewColor = child.color;
                        GameObject tmpObj = child.gameObject;
                        DestroyImmediate(child);
                        RawImage rawImg = tmpObj.AddComponent<RawImage>();

                        if (rawImg != null)
                        {
                            rawImg.color = previewColor;
                            rawImg.texture = targetTex;
                            objSpritePaireStr += tmpObj.name + "|" + spriteName + " 被替换成Texture\n";

                        }
                    }
                    else
                    {
                        Debug.LogError(child.name + "|没有找到贴图" + spriteName);
                    }
                }

                bReplaced = true;
            }
        }

        if (bReplaced)
        {
            PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
        }

        // Clean up
        PrefabUtility.UnloadPrefabContents(obj);

        return bReplaced;
    }

    private static void _outputResult(List<string> result)
    {
        if (result.Count > 0)
        {
            string targetFilePath = Application.dataPath + "/C4Project/ReferenceOutPut.txt";

            using (StreamWriter sw = new StreamWriter(targetFilePath))
            {
                foreach (var str in result)
                {
                    sw.WriteLine(str);
                }
            }
        }
    }
}

