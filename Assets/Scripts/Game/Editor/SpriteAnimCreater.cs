using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteAnimCreater
{
    
    [MenuItem("Assets/选中atlas创建动画")]
    public static void CreateSpriteAnim()
    {
        if(Selection.activeObject == null)
        {
            Debug.Log("请选中atlas！");
            return;
        }

        string localPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        var asset = AssetDatabase.LoadAllAssetsAtPath(localPath);

        string assetPath = localPath.Replace(".png", ".asset");

        SpriteClip allAtlasMap;
        if(File.Exists(Application.dataPath.Replace("Assets", "") + assetPath))
        {
            allAtlasMap = AssetDatabase.LoadAssetAtPath<SpriteClip>(assetPath);
        }
        else
        {
            allAtlasMap = SpriteClip.CreateInstance<SpriteClip>();
            AssetDatabase.CreateAsset(allAtlasMap, assetPath);
        }
        allAtlasMap.sprites = new SpriteHash[asset.Length - 1];//有一个是贴图自身，减去


        int index = 0;
        foreach (var o in asset)
        {
            if (o is Sprite)
            {
                SpriteHash sh = new SpriteHash();
                int hash = GetHash(o.name);
                sh.sprite = (Sprite)o;
                sh.hash = hash;
                allAtlasMap.sprites[index++] = sh;
            }
        }

        EditorUtility.SetDirty(allAtlasMap);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static int GetHash(string name)
    {
        int angle = GetAngle(name);
        if(angle == -1)
        {
            Debug.Log("sprite没有包含角度！");
            return -1;
        }

        int frame = GetFrame(name);

        int anim = GetAnimNameIndex(name);

        return anim * 100000000 + angle * 1000 + frame;
    }

    public static int GetAnimNameIndex(string name)
    {
        string[] values = System.Enum.GetNames(typeof(SpriteAnimation));
        for(int i = 0; i < values.Length; i++)
        {
            if(name.Contains("_" + values[i] + "_"))
            {
                return i;
            }
        }
        return -1;
    }

    public static int GetAngle(string name)
    {
        string[] values = name.Split('_');
        for(int i = 0; i < values.Length; i++)
        {
            if(values[i].Contains("Degree"))
            {
                string angle = values[i].Replace("Degree", "");
                int an = int.Parse(angle);
                Debug.Log(angle + "*************" + an );
                return an;
            }
        }
        return -1;
    }

    public static int GetFrame(string name)
    {
        string[] values = name.Split('_');
        string angle = values[values.Length - 1];
        int an = int.Parse(angle);
        return an;
    }

}
