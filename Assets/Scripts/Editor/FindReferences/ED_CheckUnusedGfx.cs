using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
//using Mono.Data.Sqlite;

public class ED_CheckUnusedGfx : EditorWindow
{
    [MenuItem("Tools/检查没有引用到的特效", false, 71)]

    static void Initialize()
    {
        ED_CheckUnusedGfx window = EditorWindow.GetWindow<ED_CheckUnusedGfx>("CheckUnusedGfx");
        window.minSize = new Vector2(100, 800);

        window.Show();
    }

//    private List<UT_PrefabPoolManager.PrefabPoolConfig> gfxTempList = new List<UT_PrefabPoolManager.PrefabPoolConfig>();
//    private List<UnityEngine.Object> unUsedGfx = new List<UnityEngine.Object>();
//    private int orgNum = 0;
//    private Vector2 scrollPos = new Vector2(0, 0);

//    string CutString(string sourceStr, string str)
//    {
//        int index = sourceStr.IndexOf(str);
//        if (index != -1)
//        {
//            return sourceStr.Remove(index);
//        }
//        return sourceStr;
//    }

//    string CutTrailString(string sourceStr, string str)
//    {
//        if (sourceStr.EndsWith(str))
//        {
//            int index = sourceStr.LastIndexOf(str);
//            return sourceStr.Remove(index);
//        }
//        return sourceStr;
//    }

//    private void OnGUI()
//    {
//        if(GUILayout.Button("Check"))
//        {
//            if (UT_PrefabPoolManager.Instance == null)
//            {
//                Debug.Log("请保证场景里有UT_PrefabPoolManager，且点了刷新的!");
//                return;
//            }
//            gfxTempList.Clear();
//            UT_PrefabPoolManager.Instance.Init();
//            for (int i = 0; i < UT_PrefabPoolManager.Instance.m_kGfxPrefabPoolConfigList.Count; i++)
//            {
//                UT_PrefabPoolManager.PrefabPoolConfig np = new UT_PrefabPoolManager.PrefabPoolConfig();
//                np.m_kResPathName = UT_PrefabPoolManager.Instance.m_kGfxPrefabPoolConfigList[i].m_kResPathName;
//                np.m_kResName = UT_PrefabPoolManager.Instance.m_kGfxPrefabPoolConfigList[i].m_kResName;


//                np.m_kResName = CutString(np.m_kResName, "_0");
//                np.m_kResName = CutString(np.m_kResName, "_1");
//                np.m_kResName = CutString(np.m_kResName, "_2");
//                np.m_kResName = CutString(np.m_kResName, "_3");
//                np.m_kResName = CutString(np.m_kResName, "_4");
//                np.m_kResName = CutString(np.m_kResName, "_5");
//                np.m_kResName = CutTrailString(np.m_kResName, "_L_");
//                np.m_kResName = CutTrailString(np.m_kResName, "_M_");
//                np.m_kResName = CutTrailString(np.m_kResName, "_S_");
//                np.m_kResName = CutTrailString(np.m_kResName, "_L");
//                np.m_kResName = CutTrailString(np.m_kResName, "_M");
//                np.m_kResName = CutTrailString(np.m_kResName, "_S");

//                gfxTempList.Add(np);
//            }

//            orgNum = gfxTempList.Count;

//            LC_DBManager.Instance.Init_WithEditor();

//            CheckCsv();

//            Debug.Log("csv引用特效数量:" + (orgNum - gfxTempList.Count));
//            orgNum = gfxTempList.Count;

//            CheckCSharp();

//            Debug.Log("c#引用特效数量:" + (orgNum - gfxTempList.Count));
//            orgNum = gfxTempList.Count;

//            CheckLua();

//            Debug.Log("lua引用特效数量:" + (orgNum - gfxTempList.Count));
//            orgNum = gfxTempList.Count;

//            CheckRD_Visible_Animator();

//            Debug.Log("动画文件引用特效数量:" + (orgNum - gfxTempList.Count));
//            orgNum = gfxTempList.Count;

//            CheckRtsSkill();

//            Debug.Log("rts技能引用特效数量:" + (orgNum - gfxTempList.Count));
//            orgNum = gfxTempList.Count;

//            CheckScene();
//            Debug.Log("场景引用特效数量:" + (orgNum - gfxTempList.Count));
//            orgNum = gfxTempList.Count;

//            Debug.Log("未用到的特效数量:" + (gfxTempList.Count));
//        }

//        if(gfxTempList.Count > 0)
//        {
//            if (GUILayout.Button("显示未使用的特效"))
//            {
//                unUsedGfx.Clear();
//                for (int i = 0; i < gfxTempList.Count; i++)
//                {
//                    string path = "Assets/C4Project/Bundle/" + gfxTempList[i].m_kResPathName + ".prefab";
//                    UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
//                    unUsedGfx.Add(obj);
//                }
//            }

//            if (GUILayout.Button("将未使用的特效全部移动到临时文件夹Assets/GfxTemp"))
//            {
//                if (!Directory.Exists(Application.dataPath + "/GfxTemp"))
//                {
//                    AssetDatabase.CreateFolder("Assets", "GfxTemp");
//                }

//                for (int i = 0; i < gfxTempList.Count; i++)
//                {
//                    string path = "Assets/C4Project/Bundle/" + gfxTempList[i].m_kResPathName + ".prefab";
//                    string[] sss = gfxTempList[i].m_kResPathName.Split('/');
//                    string newPath = "Assets/GfxTemp/" + sss[sss.Length - 1] + ".prefab";
//                    AssetDatabase.MoveAsset(path, newPath);
//                }
//            }

//            //if (GUILayout.Button("删除未使用的特效"))
//            //{
//            //    for (int i = 0; i < gfxTempList.Count; i++)
//            //    {
//            //        string path = "Assets/C4Project/Bundle/" + gfxTempList[i].m_kResPathName + ".prefab";
//            //        AssetDatabase.DeleteAsset(path);
//            //    }
//            //}

//            if (unUsedGfx.Count > 0)
//            {
//                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

//                for (int i = 0; i < unUsedGfx.Count; i++)
//                {
//                    EditorGUILayout.ObjectField(unUsedGfx[i], typeof(UnityEngine.Object));
//                }

//                EditorGUILayout.EndScrollView();
//            }
//        }
//    }

//    private void CheckString(string str)
//    {
//        for (int i = 0; i < gfxTempList.Count; i++)
//        {
//            if (str.Contains(gfxTempList[i].m_kResName))
//            {
//                //Debug.Log(gfxTempList[i].m_kResName);
//                gfxTempList.RemoveAt(i);
//                i--;
//            }
//        }
//    }

//    #region DB部分
//    void CheckCsv()
//    {
//        string filePath = Application.dataPath.Replace("Client/Assets", "Tools/CSV/XLSX");

//        string[] allfiles = Directory.GetFiles(filePath);

//        EditorUtility.ClearProgressBar();

//        for (int i = 0; i < allfiles.Length; i++)
//        {
//            string[] strs = allfiles[i].Split('\\');
//            string fname = strs[strs.Length - 1];
//            fname = fname.Replace(".xlsx", "");

//            if (fname == "Language")//Language表不参与检查
//                continue;

//            string s = LC_DBManager.Instance.GetDbString(fname);

//            CheckString(s);

//            EditorUtility.DisplayProgressBar("正在检查Excel表", fname, i * 1.0f / allfiles.Length);
//        }

//        EditorUtility.ClearProgressBar();
//    }
//    #endregion

//    #region 脚本部分

//    private void CheckCSharp()
//    {
//        EditorUtility.ClearProgressBar();
//        string m_kCSScriptPath = Application.dataPath + "/C4Project/Script";
//        List<string> kFiles = new List<string>(Directory.GetFiles(m_kCSScriptPath, "*.cs", SearchOption.AllDirectories));
//        for (int iIndex = 0; iIndex < kFiles.Count; iIndex++)
//        {
//            string kPath = kFiles[iIndex];
//            int nPos = kPath.IndexOf("Assets");
//            string kAssetPath = kPath.Substring(nPos);
//            TextAsset kCSScript = AssetDatabase.LoadAssetAtPath(kAssetPath, typeof(TextAsset)) as TextAsset;
//            if (kCSScript == null)
//                continue;
//            string CSContent = kCSScript.text.ToString();

//            CheckString(CSContent);

//            EditorUtility.DisplayProgressBar("正在检查C#脚本", kFiles[iIndex], iIndex * 1.0f / kFiles.Count);
//        }
//        EditorUtility.ClearProgressBar();
//    }

//    private void CheckLua()
//    {
//        EditorUtility.ClearProgressBar();
//        string m_kCSScriptPath = Application.dataPath + "/C4Project/Script";
//        List<string> kFiles = new List<string>(Directory.GetFiles(m_kCSScriptPath, "*.lua", SearchOption.AllDirectories));
//        for (int iIndex = 0; iIndex < kFiles.Count; iIndex++)
//        {
//            string kPath = kFiles[iIndex];
//            string str = File.ReadAllText(kPath);

//            CheckString(str);

//            EditorUtility.DisplayProgressBar("正在检查lua脚本", kFiles[iIndex], iIndex * 1.0f / kFiles.Count);
//        }
//        EditorUtility.ClearProgressBar();
//    }

//    #endregion

//    #region 序列化引用部分
//    private void CheckRD_Visible_Animator()
//    {
//        EditorUtility.ClearProgressBar();

//        string[] units = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets/C4Project/Bundle" });
//        for(int i = 0; i < units.Length; i++)
//        {
//            string path = AssetDatabase.GUIDToAssetPath(units[i]);
//            GameObject go = AssetDatabase.LoadAssetAtPath(path, typeof(GameObject)) as GameObject;
//            RD_Visible_Animator rva = go.GetComponentInChildren<RD_Visible_Animator>();
//            if(rva != null)
//            {
//                for(int j = 0; j <  rva.m_kAnimationList_ActorState.Count; j++)
//                {
//                    for(int n = 0; n < rva.m_kAnimationList_ActorState[j].m_kGfxEventList.Count; n ++)
//                    {
//                        CheckString(rva.m_kAnimationList_ActorState[j].m_kGfxEventList[n].m_kAttachedConfig.m_kGfxResName);
//                    }

//                }
//            }

//            EditorUtility.DisplayProgressBar("正在检查RD_Visible_Animator附带特效", path, i * 1.0f / units.Length);
//        }

//        EditorUtility.ClearProgressBar();
//    }
//    #endregion

//    #region rts技能引用部分
//    private void CheckRtsSkill()
//    {
//        //EditorUtility.ClearProgressBar();

//        //Rts_SkillData m_kSkillData = UT_PrefabPoolManager.Instance.Alloc(UT_PrefabPoolManager.PrefabType.PT_AI, "Rts_SkillData", null, true, true) as Rts_SkillData;

//        //for( int i = 0; i <  m_kSkillData.allSkills.Count; i++)
//        //{
//        //    RtsSkill value = m_kSkillData.allSkills[i];
//        //    if (value.bulletGfxName != "")
//        //    {
//        //        CheckString(value.bulletGfxName);
//        //    }
//        //    if (value.fireGfxName != "")
//        //    {
//        //        CheckString(value.fireGfxName);
//        //    }
//        //    if (value.hitGfxName != "")
//        //    {
//        //        CheckString(value.hitGfxName);
//        //    }
//        //    if (value.subHitGfxName != "")
//        //    {
//        //        CheckString(value.subHitGfxName);
//        //    }

//        //    EditorUtility.DisplayProgressBar("rts技能对特效的引用", "rts技能对特效的引用", i * 1.0f / m_kSkillData.allSkills.Count);
//        //}

//        //EditorUtility.ClearProgressBar();
//    }
//    #endregion

//    #region    场景对特效的引用部分
//    public void CheckScene()
//    {
//        EditorUtility.ClearProgressBar();
//        //遍历所有游戏场景
//        int progress = 0;
//        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
//        {
//            if (scene.enabled)
//            {
//                //打开场景
//                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path);

//                //获取场景中的所有游戏对象
//                GameObject[] gos = (GameObject[])FindObjectsOfType(typeof(GameObject));
//                foreach (GameObject go in gos)
//                {
//                    //判断GameObject是否为一个Prefab的引用
//                    if (PrefabUtility.GetPrefabType(go) == PrefabType.PrefabInstance)
//                    {
//                        if (go.GetComponent<RD_Gfx>() != null)
//                            CheckString(go.name);
//                    }
//                }
//                EditorUtility.DisplayProgressBar("场景对特效的引用", "场景对特效的引用", progress * 1.0f / EditorBuildSettings.scenes.Length);
//            }
//            progress++;
//        }
//        EditorUtility.ClearProgressBar();
//    }
//    #endregion
}