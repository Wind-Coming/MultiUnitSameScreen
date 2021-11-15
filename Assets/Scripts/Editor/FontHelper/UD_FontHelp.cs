using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

public class UD_FontHelp : EditorWindow
{
    private string FontForgePath = null;
    private string SourceFontPath = "";
    private string DesktopPath = null;
    private Assembly assembly = null;

    [MenuItem("Tools/字体/生成字体")]
    
    static void Init()
    {
        EditorWindow.GetWindow(typeof(UD_FontHelp));
    }
     void OnGUI()
     {
         GUILayout.Label("字体打包",EditorStyles.boldLabel);
         GUILayout.BeginHorizontal();
         GUILayout.Label(@"原始字库地址:",GUILayout.Width(80));
         SourceFontPath = GUILayout.TextField(SourceFontPath);
         if (GUILayout.Button("+", GUILayout.Width(20)))
         {
             SourceFontPath = _getPath();
         }
         GUILayout.EndHorizontal();
         if (GUILayout.Button("生成字体"))
         {
            if ((FontForgePath = _checkEnvironment()) == null)
             {
                 Debug.LogError("没有安装FontForge");
                 return;
             }
             assembly = Assembly.Load(@"Assembly-CSharp");
             if (assembly == null)
             {
                 Debug.LogError("加载程序集失败！");
                 return; 
             }
             if (SourceFontPath=="")
             {
                 Debug.LogError("源字体文件路径不能为空");
                 return;
             }
             FileInfo checkSource = new FileInfo(SourceFontPath);
             if (!checkSource.Exists) 
             { 
                 Debug.LogError("源字体文件路径错误");
                 return;
             }
             if (!(checkSource.Extension.Trim().ToLower() == ".ttf"))
             {

                 Debug.LogError("源字体文件格式错误");
                 return;
             }
             _getWord();
             _creatBat();
             _runCmd(DesktopPath + "tempbat.bat");
             Debug.Log("字体生成完毕！若桌面没有NEW.TTF请手动点击tempbat.bat");
         }
     }

    private string _getPath()
     {
         UD_OpenLocalFile.FileInfo openfile = new UD_OpenLocalFile.FileInfo();
         openfile.structSize = Marshal.SizeOf(openfile);
         openfile.filter = "All Files\0*.*\0\0";
         openfile.file = new string(new char[256]);
         openfile.maxFile = openfile.file.Length;
         openfile.fileTitle = new string(new char[64]);
         openfile.maxFileTitle = openfile.fileTitle.Length;
         openfile.initialDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);//默认路径
         openfile.title = "选择字体文件";
         openfile.defExt = "TTF";//显示文件的类型
         openfile.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;
         if (UD_OpenLocalFile.GetFileInfo(openfile))
         {
             return openfile.file;
         }
         return "";
     }

     private void _runCmd(string command)
     {
         System.Diagnostics.Process p = new System.Diagnostics.Process();
         p.StartInfo.FileName = "cmd.exe";           
         p.StartInfo.Arguments = "/c " + command;   
         p.StartInfo.UseShellExecute = false;        
         p.StartInfo.RedirectStandardInput = true;  
         p.StartInfo.RedirectStandardOutput = true; 
         p.StartInfo.RedirectStandardError = true;   
         p.StartInfo.CreateNoWindow = true;          
         p.Start();
     }
     private string _getGB2312String()
     {
         var list = new List<byte>();
         for (var 区 = 16; 区 <= 55; 区++)
             for (int 位2 = (区 == 55) ? 89 : 94, 位 = 1; 位 <= 位2; 位++)
             {
                 list.Add((byte)(区 + 0xa0));
                 list.Add((byte)(位 + 0xa0));
             }
         return Encoding.GetEncoding("GB2312").GetString(list.ToArray());
     }

     private string _getCharCode(char character)
     {
         UTF32Encoding encoding = new UTF32Encoding();
         byte[] bytes = encoding.GetBytes(character.ToString().ToCharArray());
         string kCodeStr = System.BitConverter.ToString(bytes, 0);
         string[] kSubCode = kCodeStr.Split('-');
         return kSubCode[1] + kSubCode[0];

     }

     private void _regularFile(FileInfo source, HashSet<string> WordSet)
     {
         StreamReader sr = new StreamReader(source.OpenRead());
         Regex rx = new Regex("[\\][u][0-9A-F]{4}", RegexOptions.Compiled);
         string temp = null;
         while ((temp = sr.ReadLine()) != null)
         {
             MatchCollection mc = rx.Matches(temp);
             for (int i = 0; i < mc.Count; i++)
             {
                 WordSet.Add(mc[i].Value.Substring(1, 4));
             }
         }
         sr.Close();
     }
    /// <summary>
    /// 检查是否安装FontForge
    /// </summary>
    /// <returns>FontForge的安装路径</returns>
    private string _checkEnvironment()
     {
         DesktopPath = "";
         string tempDesktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
         string[] cuts=tempDesktopPath.Split('\\');
         for (int i = 0; i < cuts.Length; i++)
         {
             DesktopPath += cuts[i];
             DesktopPath += "\\\\";
         }
         Microsoft.Win32.RegistryKey uninstallNode = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE", false)
             .OpenSubKey("Wow6432Node", false)
             .OpenSubKey("Microsoft", false)
             .OpenSubKey("Windows", false)
             .OpenSubKey("CurrentVersion", false)
             .OpenSubKey("Uninstall", false);
         foreach (string subKeyName in uninstallNode.GetSubKeyNames())
         {
             Microsoft.Win32.RegistryKey subKey = uninstallNode.OpenSubKey(subKeyName);
             object displayName = subKey.GetValue("DisplayName");
             if (displayName != null)
             {
                 if (displayName.ToString().Contains("FontForge"))
                 {
                     return subKey.GetValue("InstallLocation").ToString();
            
                 }
             }
         }
         return null;  
     }
    /// <summary>
    /// 查询项目中所有的字
    /// </summary>
    private void _getWord()
     {
        //读取CSV中字体

        HashSet<string> WordSet = new HashSet<string>();

        //LC_DBManager DB = LC_DBManager.Instance;
        //DB.Init();
        //FieldInfo[] DBfields = DB.GetType().GetFields();
        //for (int i = 0; i < DBfields.Length; i++) {
        //    if (DBfields[i].Name.IndexOf("CSV") > -1) {
        //        System.Object CSV = DBfields[i].GetValue(DB);
        //        System.Type inner = assembly.GetType(DBfields[i].FieldType.Name + "+DataEntry", false);
        //        if (inner == null) {
        //            Debug.Log(DBfields[i].FieldType.Name + "没有内部类DataEntry");
        //            continue;
        //        }
        //        FieldInfo[] innerfields = inner.GetFields();
        //        FieldInfo[] CSVfields = CSV.GetType().GetFields();
        //        for (int j = 0; j < CSVfields.Length; j++) {
        //            if (CSVfields[j].Name.IndexOf("m_kDataEntryTable") > -1) {
        //                var CSVTable = CSVfields[j].GetValue(CSV);
        //                for (int k = 0; k < innerfields.Length; k++) {
        //                    if (innerfields[k].FieldType.Equals(typeof(string))) {
        //                        if (CSVTable == null) {
        //                            Debug.Log(CSV + "没有Init");
        //                        }
        //                        else {
        //                            foreach (System.Object o in (CSVTable as IEnumerable)) {
        //                                var itemValue = o.GetType().GetProperty("Value").GetValue(o, null);
        //                                var stringfield = innerfields[k].GetValue(itemValue);
        //                                char[] chars = stringfield.ToString().ToCharArray();
        //                                for (int m = 0; m < chars.Length; m++) {
        //                                    WordSet.Add(_getCharCode(chars[m]));
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //添加常用字
        char[] kGbChar = _getGB2312String().ToCharArray();
        Debug.Log(kGbChar.Length);
        for (int n = 0; n < kGbChar.Length; n++) {
            WordSet.Add(_getCharCode(kGbChar[n]));
        }

        //遍历Prefab,查找其中的字 暂时没必要
        //DirectoryInfo theFolder = new DirectoryInfo(Application.dataPath);
        //Queue<DirectoryInfo> theQueue = new Queue<DirectoryInfo>();
        //theQueue.Enqueue(theFolder);
        //int prefab = 0;
        //while (theQueue.Count > 0)
        //{
        //    DirectoryInfo current = theQueue.Dequeue();
        //    DirectoryInfo[] currentchild = current.GetDirectories();
        //    for (int i = 0; i < currentchild.Length; i++)
        //    {
        //        theQueue.Enqueue(currentchild[i]);
        //    }
        //    FileInfo[] currentFiles = current.GetFiles();
        //    for (int i = 0; i < currentFiles.Length; i++)
        //    {
        //        if (currentFiles[i].Extension == ".prefab")
        //        {
        //            prefab++;
        //            _regularFile(currentFiles[i], WordSet);
        //        }
        //    }

        //}

        StreamWriter kWriter = File.CreateText(@"e:\tempScript.pe");
        string[] SourceFontPathCuts = SourceFontPath.Split('\\');
        for (int i = 1; i < SourceFontPathCuts.Length; i++) {
            SourceFontPathCuts[0] += @"\\";
            SourceFontPathCuts[0] += SourceFontPathCuts[i];
        }
        kWriter.WriteLine(@"Open(""" + SourceFontPathCuts[0] + @""");");
        foreach (string code in WordSet) {
            kWriter.WriteLine("SelectMore(0u" + code + ");");
        }
        kWriter.WriteLine("SelectInvert();");
        kWriter.WriteLine("DetachAndRemoveGlyphs();");
        kWriter.WriteLine(@"Generate(""" + DesktopPath + @"NEW.TTF"");");
        kWriter.Flush();
        kWriter.Close();
    }
    /// <summary>
    /// 制作批处理文件
    /// </summary>
    private void _creatBat()
    {
        StreamWriter kWriter = File.CreateText(DesktopPath+@"tempbat.bat");
        kWriter.WriteLine("@echo off");
        kWriter.WriteLine(@"set FF=" + @"""" + FontForgePath + @"""");
        kWriter.WriteLine(@"set ""PYTHONHOME=%FF%""");
        kWriter.WriteLine(@"set ""PYTHONPATH=%FF%lib\python2.7""");
        kWriter.WriteLine(@"set ""PATH=%FF%;%FF%\bin;%PATH%""");
        kWriter.WriteLine(@"set FF_PATH_ADDED=TRUE");
        kWriter.WriteLine(@"fontforge -script e:\tempScript.pe");
        kWriter.Flush();
        kWriter.Close();
    }
}

