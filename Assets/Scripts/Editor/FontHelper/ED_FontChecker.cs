using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.UI;

public class ED_FontChecker : Editor
{
    private static string m_kTargetFontName = "SourceHanSansCN-Medium";   
    [MenuItem("Tools/字体/Font Check", false, 72)]
    static void Check()
    {
        string kUIPath  = Application.dataPath + "/Res/Font";
        string[] kFiles = Directory.GetFiles(kUIPath, "*.prefab",SearchOption.AllDirectories);
        for(int iIdx = 0;iIdx < kFiles.Length;iIdx++)
        {
            string kPath = kFiles[iIdx];
            string kAssetPath = kPath.Substring(kPath.IndexOf("Asset"));
            GameObject kObj = AssetDatabase.LoadAssetAtPath(kAssetPath,typeof(GameObject)) as GameObject;
            if(kObj == null)
            continue;

            Text kText = kObj.GetComponent<Text>();
            if(kText != null)
            {
                if(kText.font.name != m_kTargetFontName)
                {
                    Debug.LogError("Font Check Error,Prefab Name="+ kObj.name +
                        "|Font Name="+ kText.font.name + "|Text Comp="+ kObj.name);
                }
            }

            Text[] kTextArray = kObj.GetComponentsInChildren<Text>();
            for(int iIndex = 0; iIndex < kTextArray.Length; iIndex++)
            {
                if (kTextArray[iIndex] != null)
                {
                    if (kTextArray[iIndex].font.name != m_kTargetFontName)
                    {
                        Debug.LogError("Font Check Error,Prefab Name=" + kObj.name +
                            "|Font Name=" + kTextArray[iIndex].font.name + "|Text Comp=" + kTextArray[iIndex].gameObject.name);
                    }
                }
            }
        }
    }
}
