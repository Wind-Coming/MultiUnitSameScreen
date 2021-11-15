using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

namespace Spenve
{
    public class ExportExcel : EditorWindow
    {

        [MenuItem("Export/导出选中Excel")]
        public static void ExpFiles()
        {
            Object[] selected = Selection.GetFiltered(typeof(object), SelectionMode.TopLevel);
            string[] files = new string[selected.Length];  
            for (int i = 0; i < selected.Length; i++ )
            {
                files[i] = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/" + AssetDatabase.GetAssetPath(selected[i]);
            }
            
            CExportData Operation;
            Operation = new CExportData(files);
            Operation.ExportingProcess();

            AssetDatabase.Refresh();
            Debug.Log("导出完成");
        }

        [MenuItem("Export/导出选中Excel到指定目录")]
        public static void ExpToFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
            Object[] selected = Selection.GetFiltered(typeof(object), SelectionMode.TopLevel);
            string[] files = new string[selected.Length];
            for (int i = 0; i < selected.Length; i++)
            {
                files[i] = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/" + AssetDatabase.GetAssetPath(selected[i]);
            }

            EditorPrefs.SetString("exportExcelPath", path + "/");

            CExportData Operation;
            Operation = new CExportData(files);
            Operation.ExportingProcess();

            //恢复默认路径
            EditorPrefs.SetString("exportExcelPath", "");

            AssetDatabase.Refresh();
            Debug.Log("导出完成");
        }


        [MenuItem("Export/导出文件夹中所有Excel")]
        public static void ExpFolder()
        {
            CExportData Operation;
            if (EditorPrefs.GetString("excelPath") != "")
            {
                Operation = new CExportData(EditorPrefs.GetString("excelPath"));
            }
            else
            {
                Operation = new CExportData(Application.dataPath + "/GamePlay/Data/excel");
            }
            Operation.ExportingProcess();

            AssetDatabase.Refresh();
            Debug.Log("导出完成");
        }

        [MenuItem("Export/设置默认Excel文件夹路径")]
        public static void SetFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
            EditorPrefs.SetString("excelPath", path);

            Debug.Log("Excel路径设置成功：" + path);
        }

        [MenuItem("Export/设置默认导出路径")]
        public static void SetExportFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
            EditorPrefs.SetString("exportExcelPath", path + "/");

            Debug.Log("导出路径设置成功：" + path);
        }

        [MenuItem("Export/设置服务器数据路径")]
        public static void SetExportServerFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
            EditorPrefs.SetString("exportServerPath", path);

            Debug.Log("导出服务器路径设置成功：" + path);
        }

        [MenuItem("Export/恢复默认设置")]
        public static void RestoreSetting()
        {
            EditorPrefs.SetString("exportServerPath", "");
            EditorPrefs.SetString("exportExcelPath", "");
            EditorPrefs.SetString("excelPath", "");

            Debug.Log("恢复成功");
        }
    }

}