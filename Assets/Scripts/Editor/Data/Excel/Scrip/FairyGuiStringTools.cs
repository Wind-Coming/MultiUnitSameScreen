using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using System.Xml.Linq;
using System;
using System.Collections.Generic;
using NPOI.SS.Util;
//using XmlDocument;
//using 
namespace Spenve
{
    /// <summary>
    /// 根据模板文件生成excel文件 从excel文件生成对应语言的xml文件
    /// </summary>
    public class FairyGuiStringTools : EditorWindow
    {
        [MenuItem("Export/clearAllPlayerPre")]
        public static void ClearAllPlayerPre()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }       
    }
}
