using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine.UI;

public class ED_FindReferences {

    [MenuItem("Assets/工具/查找引用", false, 10)]
    static private void Find()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!string.IsNullOrEmpty(path)) {
            string guid = AssetDatabase.AssetPathToGUID(path);
            List<string> withoutExtensions;
            if (path.EndsWith(".mat")) {
                withoutExtensions = new List<string>() { ".prefab", ".unity" };
            }
            else if(path.EndsWith(".shader")) {
                withoutExtensions = new List<string>() { ".mat" };
            }
            else if (path.EndsWith(".prefab")) {
                withoutExtensions = new List<string>() { ".unity", ".asset" };
            }
            else {
                withoutExtensions = new List<string>() { ".prefab", ".unity", ".mat", ".asset" };
            }

            string[] files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
                .Where(s => withoutExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
            int startIndex = 0;

            EditorApplication.update = delegate ()
            {
                string file = files[startIndex];

                bool isCancel = EditorUtility.DisplayCancelableProgressBar("匹配资源中", file, (float)startIndex / (float)files.Length);

                if (Regex.IsMatch(File.ReadAllText(file), guid)) {
                    Debug.Log(file, AssetDatabase.LoadAssetAtPath<Object>(GetRelativeAssetsPath(file)));
                }

                startIndex++;
                if (isCancel || startIndex >= files.Length) {
                    EditorUtility.ClearProgressBar();
                    EditorApplication.update = null;
                    startIndex = 0;
                    Debug.Log("匹配结束");
                }

            };
        }
    }

        [MenuItem("Assets/工具/替换所有prefab对Sprites-Default的引用", false, 10)]
    static private void ReplaceSpriteDefault()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        string guid = "{fileID: 10754, guid: 0000000000000000f000000000000000, type: 0}";
        List<string> withoutExtensions = new List<string>() { ".prefab", ".unity" }; ;

        string[] files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
            .Where(s => withoutExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
        int startIndex = 0;

        EditorApplication.update = delegate ()
        {
            string file = files[startIndex];

            bool isCancel = EditorUtility.DisplayCancelableProgressBar("匹配资源中", file, (float)startIndex / (float)files.Length);

            string fileText = File.ReadAllText(file);
            if (Regex.IsMatch(fileText, guid))
            {
                fileText = fileText.Replace(guid, "{fileID: 2100000, guid: 2adbc25d859ad06458867d99b86715dc, type: 2}");
                File.WriteAllText(file, fileText);
                Debug.Log(file, AssetDatabase.LoadAssetAtPath<Object>(GetRelativeAssetsPath(file)));
            }

            startIndex++;
            if (isCancel || startIndex >= files.Length)
            {
                EditorUtility.ClearProgressBar();
                EditorApplication.update = null;
                startIndex = 0;
                Debug.Log("匹配结束");
            }

        };

        AssetDatabase.Refresh();
    }

    [MenuItem("Assets/工具/查找shader引用", false, 10)]
    static private void FindShaderRef()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        List<string> matGuiIdList = GetRefGuids();
    }

    static private string GetRelativeAssetsPath(string path)
    {
        return "Assets" + Path.GetFullPath(path).Replace(Path.GetFullPath(Application.dataPath), "").Replace('\\', '/');
    }

    static private List<string> GetRefGuids()
    {
        List<string> list = new List<string>();
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!string.IsNullOrEmpty(path)) {
            string guid = AssetDatabase.AssetPathToGUID(path);
            List<string> withoutExtensions;
            if (path.EndsWith(".shader")) {
                withoutExtensions = new List<string>() { ".mat" };
            }
            else {
                Debug.Log("Choose a shader !");
                return list;
            }

            string[] files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
                .Where(s => withoutExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
            int startIndex = 0;

            Shader shader = Shader.Find("Particles/FX_AllInOne");
            if(shader == null) {
                Debug.Log("shader = null");
                return list;
            }

            EditorApplication.update = delegate ()
            {
                string file = files[startIndex];

                bool isCancel = EditorUtility.DisplayCancelableProgressBar("匹配资源中", file, (float)startIndex / (float)files.Length);

                if (Regex.IsMatch(File.ReadAllText(file), guid)) {
                    //list.Add(AssetDatabase.AssetPathToGUID(GetRelativeAssetsPath(file)));
                    //Debug.Log(file, AssetDatabase.LoadAssetAtPath<Object>(GetRelativeAssetsPath(file)));
                    Material mat = AssetDatabase.LoadAssetAtPath<Object>(GetRelativeAssetsPath(file)) as Material;
                    Debug.Log(mat.name, mat);
                    if (mat.shader.name.Contains("Particles/Alpha Blended CloudShadow")) {
                        mat.shader = shader;

                        mat.SetFloat("_Mode", 0);
                        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetFloat("_DestBlend", 10);

                        //mat.SetFloat("_SetColor", 1);
                        //mat.EnableKeyword("_SETCOLOR_ON");

                        //if (mat.GetTexture("_MaskTex") != null) {
                        //    mat.SetFloat("_UseMask", 1);
                        //    mat.EnableKeyword("_USEMASK_ON");
                        //}
                        //else {
                        //    mat.SetFloat("_UseMask", 0);
                        //    mat.DisableKeyword("_USEMASK_ON");
                        //}

                        //if (mat.GetTexture("_NoiesTex") != null) {
                        //    mat.SetFloat("_UseNoise", 1);
                        //    mat.EnableKeyword("_USENOISE_ON");
                        //}
                        //else {
                        //    mat.SetFloat("_UseNoise", 0);
                        //    mat.DisableKeyword("_USENOISE_ON");
                        //}

                        //Vector4 v = mat.GetVector("_MainTexUV_speed");
                        //if(v.x != 0 || v.y != 0 || v.z != 0 || v.w != 0) {
                        //    mat.SetFloat("_MainUvScroll", 1);
                        //    mat.EnableKeyword("_MAINUVSCROLL_ON");
                        //}

                        //if (mat.GetTexture("_DissolveTex") != null) {
                        //    mat.SetFloat("_DissolveFactor", 1);
                        //    mat.EnableKeyword("_DISSOLVEFACTOR_ON");
                        //}
                        //else {
                        //    mat.SetFloat("_DissolveFactor", 0);
                        //    mat.DisableKeyword("_DISSOLVEFACTOR_ON");
                        //}

                        //mat.SetFloat("_UseFresnal", 0);
                        //mat.DisableKeyword("_DISSOLVEFACTOR_ON");
                    }
                }

                startIndex++;
                if (isCancel || startIndex >= files.Length) {
                    EditorUtility.ClearProgressBar();
                    EditorApplication.update = null;
                    startIndex = 0;
                    Debug.Log("材质数量：" + list.Count);

                    AssetDatabase.SaveAssets();
                    //FindMatRef(list);
                }
            };
        }
        return list;
    }

    static void FindMatRef(List<string> list)
    {
        List<string> withoutExtensions;

        withoutExtensions = new List<string>() { ".prefab", ".unity" };

        string[] files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories)
            .Where(s => withoutExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
        int startIndex = 0;

        EditorApplication.update = delegate ()
        {
            string file = files[startIndex];

            bool isCancel = EditorUtility.DisplayCancelableProgressBar("匹配资源中", file, (float)startIndex / (float)files.Length);

            for (int i = 0; i < list.Count; i++) {
                if (Regex.IsMatch(File.ReadAllText(file), list[i])) {
                    Debug.Log(file, AssetDatabase.LoadAssetAtPath<Object>(GetRelativeAssetsPath(file)));
                }
            }
            startIndex++;
            if (isCancel || startIndex >= files.Length) {
                EditorUtility.ClearProgressBar();
                EditorApplication.update = null;
                startIndex = 0;
                Debug.Log("匹配结束");
            }

        };
    }
}
