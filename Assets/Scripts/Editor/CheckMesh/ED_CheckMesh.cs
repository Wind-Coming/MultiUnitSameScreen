using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

public class ED_CheckMesh
{
    /// <summary>
    /// 得到模型片断
    /// </summary>
    /// <param name="fbxPath"></param>
    /// <returns></returns>
    [MenuItem("Tools/美术工具/检查FBX面数")]
    public static void FbxCheck()
    {
        string[] strs = Selection.assetGUIDs;
        for(int i = 0; i < strs.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(strs[i]);
            string pathtoFileName = path.Replace('/', '_');
            string filePath = Directory.GetCurrentDirectory() + "\\" + pathtoFileName + ".txt";
            if (File.Exists(filePath))
                File.Delete(filePath);
            StreamWriter sw = null;
            FileInfo myFile = null;
            DirectoryInfo info = new DirectoryInfo(path);

            foreach (FileInfo file in info.GetFiles("*.fbx"))
            {
                string prefabPath = path + '/' + file.Name;
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (obj != null)
                {
                    MeshFilter[] mfs = obj.GetComponentsInChildren<MeshFilter>();
                    foreach (MeshFilter mf in mfs)
                    {
                        ReadMesh(mf.sharedMesh, prefabPath, filePath, ref myFile, ref sw);
                    }

                    SkinnedMeshRenderer[] mrs = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
                    foreach(SkinnedMeshRenderer mr in mrs)
                    {
                        ReadMesh(mr.sharedMesh, prefabPath, filePath, ref myFile, ref sw);
                    }
                }
            }
            if (sw != null)
                sw.Close();

            if (myFile != null)
            {
                EditorUtility.DisplayDialog("检查完成", filePath, "ok");
            }

        }
    }

    static void ReadMesh(Mesh mesh, string prefabPath, string filePath, ref FileInfo myFile, ref StreamWriter sw)
    {
        if (mesh != null)
        {
            if (mesh.triangles.Length / 3 < 280)
                return;

            if (myFile == null)
            {
                myFile = new FileInfo(filePath);
                sw = myFile.CreateText();
            }
            string str = string.Format("vertex: {0}, triangles: {1}, path: {2}", mesh.vertexCount, mesh.triangles.Length / 3, prefabPath);
            sw.WriteLine(str);
        }
    }
}


