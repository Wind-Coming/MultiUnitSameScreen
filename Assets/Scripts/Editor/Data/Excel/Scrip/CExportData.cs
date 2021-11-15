using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Runtime.InteropServices;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEngine;
using UnityEditor;

namespace Spenve
{

    //enum EXPORT_DDTP : byte
    //{
    //    DDTP_BYTE = 1,
    //    DDTP_SHORT = 2,
    //    DDTP_INT = 3,
    //    DDTP_BOOL = 4,
    //    DDTP_STRING = 5
    //}

    //导出数据结构
    class rowInfDataEdit
    {
        public UInt16 size;
        public Byte[] value;
        public EXPORT_DDTP dType;
        public rowInfDataEdit(UInt16 sz)
        {
            dType = 0;
            size  = sz;
            value = null;
            if(sz != 0)
                value = new Byte[sz];
        }
    }

    class groupInfData
    {
        public UInt16 size;
        public Byte rowNum;     //总数
        public Byte baseNum;
        public Byte rptNum;     //重复数
        public List<rowInfDataEdit> rowList;
        public groupInfData(Byte sz, Byte rpt)
        {
            size = 0;
            rowNum = sz;
            rptNum = rpt;
            baseNum = (Byte)(sz / rpt);
            rowList = new List<rowInfDataEdit>();
        }


        public void Trim()
        {
            int rows = rowList.Count;
            if (rows % baseNum != 0)
                throw new Exception("Miss repeat value ");


            rptNum = (byte)(rows/baseNum);
            rowNum = (byte)rows;
        }
    }

    class itemInfData
    {
        public groupInfData[] grpList;
        public itemInfData(Byte gsz)
        {
            grpList = new groupInfData[gsz];
        }
    }

    class xmlInfData
    {
        public UInt16 flag;
        public UInt16 lineNum;
        public UInt16 colNum;
        public UInt16 grpNum;
        public itemInfData[] itemList;
        public xmlInfData(UInt16 line)
        {
            flag = 1001;
            grpNum = 1;
            lineNum = line;
            colNum = 0;
            itemList = new itemInfData[line];
        }
    }

    struct rowItemData
    {
        public string r_id;
        public string r_name;
        public string r_type;
        public string r_value;
        public string r_format;
    }

    class CExportData
    {
        String excelFile;
        String fpath;
        int totalCols   = 0;      //总的属性列数
        int totalGrpNum = 0;
        ushort totalLine = 0;    //总的记录行数
        Dictionary<String, String> exptFilePair;
        Dictionary<String, String> xmlFilePair;
        xmlConfig[] cfgList;
        String[] grpIndex;
        xmlInfData myExportData;
        List<String> excelList;

        IWorkbook myBook = null;

        //List[]  grpItem;

        public CExportData(string path)
        {
            excelFile = String.Empty;
            fpath = path;// Directory.GetCurrentDirectory();
            excelList = new List<string>();
            exptFilePair    = new Dictionary<String, String>();
            xmlFilePair     = new Dictionary<String, String>();
            Debug.Log("---------------------------------------------------------------");
            Debug.Log("*                       数据导出程序                          *");
            Debug.Log("---------------------------------------------------------------");
            Debug.Log("[运行]   导出程序已经启动.");
        }

        public CExportData(String[] xfile)
        {
            excelFile = "have";
            fpath = Path.GetDirectoryName(xfile[0]);
            excelList = new List<string>();
            for (int i = 0; i < xfile.Length; i++ )
                excelList.Add(xfile[i]);
            exptFilePair = new Dictionary<String, String>();
            xmlFilePair = new Dictionary<String, String>();
            Debug.Log("---------------------------------------------------------------");
            Debug.Log("*                       数据导出程序                          *");
            Debug.Log("---------------------------------------------------------------");
            Debug.Log("[运行]   导出程序已经启动.");
        }

        public void resetParameters()
        {
            totalCols = 0;      //总的属性列数
            totalGrpNum = 0;
            totalLine = 0;    //总的记录行数
            exptFilePair = new Dictionary<String, String>();
            xmlFilePair = new Dictionary<String, String>();

        }




        public Boolean CheckExcelFileNum()
        {
            //检查excel文件数目
            int fnum1 = 0;
            DirectoryInfo dirInfo = new DirectoryInfo(fpath);
            Debug.Log("[运行]   检查文件夹中的Excel文件.");

            FileInfo[] mFiles = dirInfo.GetFiles("*.xlsx", SearchOption.TopDirectoryOnly);
            fnum1 = 0;
            foreach (FileInfo f in mFiles)
            {
                if ((f.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                {
                    excelFile = f.FullName;
                    fnum1 += 1;
                    excelList.Add(f.FullName);
                }
            }
            if (fnum1 == 0)
            {
                Debug.Log("[提示]   请确认当前目录下面有一个需要导出数据的Excel!");
                return false;
            }
                //if ( fnum1 > 1)
                //{
                //    Debug.Log("[提示]   当前目录有多个excel文件，请只保留一个文件做导出!");
                //    return false;
                //}

            return true;
        }

        public int CheckXMLFileNum(String excelFile)
        {
            //检查XML文件数目
            //DirectoryInfo dirInfo = new DirectoryInfo(fpath);
            //FileInfo[] fInf = dirInfo.GetFiles("*.xlsx", SearchOption.TopDirectoryOnly);
            //excelFile = fInf[0].FullName;
            string sheetName,xmlFile,outFile;
            int num, count = 0;
            Dictionary<String, String> misXmlFile = new Dictionary<String, String>();

            Debug.Log("[运行]   检查文件夹中的XML配置文件.");
            try{
                //object missing = System.Reflection.Missing.Value;
                FileStream stream = File.Open(excelFile, FileMode.Open, FileAccess.Read);
                myBook = new XSSFWorkbook(stream);
                count = myBook.NumberOfSheets;

                //fInf[2] = new FileInfo("hello");

                for (int i = 0; i < count; i++)
                {
                    sheetName = myBook.GetSheetAt(i).SheetName;
                    if (sheetName.IndexOf('(') != -1)
                    {
                        string substr = sheetName.Substring(0, sheetName.IndexOf('('));
                        if (int.TryParse(substr, out num) && (num >= 1))
                        {
                            int Index1 = sheetName.IndexOf('(');
                            int Index2 = sheetName.IndexOf(')');
                            xmlFile = fpath + "\\" +sheetName.Substring(Index1 + 1, Index2 - Index1 - 1) + ".xml";

                            if (File.Exists(xmlFile))
                            {
                                string defPath = EditorPrefs.GetString("exportExcelPath");
                                if (defPath != "")
                                {
                                    outFile = defPath + substr + ".bytes";
                                }
                                else
                                {
                                    outFile = fpath + "\\output\\" + substr + ".bytes";
                                }
                                xmlFilePair.Add(sheetName, xmlFile);
                                exptFilePair.Add(sheetName, outFile);
                            }
                            else
                            {
                                misXmlFile.Add(sheetName, Path.GetFileName(xmlFile));
                            }
                        }
                    }
                }

                if (misXmlFile.Count() > 0)
                {
                    Debug.Log("[提示]   Excel文件的XML配置文件有缺失,如下:");
                    foreach (var files in misXmlFile)
                    {
                        Debug.Log("           " + files.Value);
                    }
                    if (xmlFilePair.Count() <= 0)
                    {
                        Debug.Log(String.Format("[提示]   [{0}]没有对应的XML配置文件,不能导出数据", Path.GetFileName(excelFile)));
                        return 0;
                        //return false;
                    }
                    else
                    {
                        Debug.Log("[提示]   将对Excel文件中的以下表单进行数据导出操作:");
                        foreach (var files in xmlFilePair)
                        {
                            Debug.Log("         对 [ " + files.Key + " ] 导出文件 [  "+ Path.GetFileName(exptFilePair[files.Key]));
                        }
                    }
                }

                stream.Close();
                stream.Dispose();
            }
            catch(Exception Exc)
            {
                string ermsg = Exc.Message;
                Debug.Log("[错误]   检查XML文件出错，错误信息" + ermsg);
                return -1;
            }
            finally
            {
                //myBook.Close();

                // 9.释放资源
                //System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                //System.Runtime.InteropServices.Marshal.ReleaseComObject(myBook);

                // 10.调用GC的垃圾收集方法  
                GC.Collect();
                GC.WaitForPendingFinalizers();  
            }

            return xmlFilePair.Count();
        }

        Boolean AnalyseXML(String xmlfile)
        {
            //int ckItemNum=0;
            String pGroupName=String.Empty,pGroupID=String.Empty;
            String pRowName = String.Empty, pRowID = String.Empty;
            List<String> nameIndexCheck = new List<String>();
            String[] fmtList = { "byte", "short", "int", "string", "float","array" };

            int idnum = 0,grpCount = 0;
            try
            {
                XElement element = XElement.Load(xmlfile);
                totalGrpNum = element.Descendants("Group").Count();
                cfgList = new xmlConfig[totalGrpNum];
                grpIndex = new String[totalGrpNum];

                foreach (var group in element.Descendants("Group"))
                {
                    int allItemNum = 0;
                    int itemNum = group.Descendants("Row").Count();
                    
                    //ckItemNum = 0;

                    if (group.Attribute("repeat") != null)
                    {
                        allItemNum = itemNum * (int.Parse(group.Attribute("repeat").Value));
                        totalCols += allItemNum;
                    }
                    else
                    {
                        allItemNum = itemNum;
                        totalCols += allItemNum;
                    }
                    xmlConfig xmlCfg = new xmlConfig(allItemNum);
                    xmlCfg.grpName = group.Attribute("name").Value;
                    xmlCfg.grpID = group.Attribute("id").Value;
                    xmlCfg.grpItmNum = allItemNum;
                    xmlCfg.grpBaseNum   = itemNum;
  
                    //xmlCfg.nameIndex = new String[itemNum];

                    //检查组id
                    if (!(int.TryParse(xmlCfg.grpID, out idnum) && idnum == grpCount))
                    {
                        throw new Exception(String.Format("组id不是连续的序列号,Group \"{0}\" 的ID应为[{1:00}]", xmlCfg.grpName, grpCount));
                    }
                    grpIndex[grpCount] = xmlCfg.grpName;    //记录组索引
                    pGroupName = xmlCfg.grpName;            //查错信息
                    pGroupID = xmlCfg.grpID;
                    
                    if (nameIndexCheck.Contains(xmlCfg.grpName))
                    {
                        throw new Exception(String.Format("XML配置文件中包含相同的字符串属性名称\"{0}\"", xmlCfg.grpName));
                    }
                    nameIndexCheck.Add(xmlCfg.grpName);


                    if (group.Attribute("repeat") != null)
                    {
                        xmlCfg.grpRptNum = int.Parse(group.Attribute("repeat").Value);
                    }
                    else
                    {
                        xmlCfg.grpRptNum = 1;
                    }

                    //ckItemNum   = 0;
                    int j = 0;
                    for (int i = 0; i < xmlCfg.grpRptNum; i++)
                    {
                        foreach (var row in group.Descendants("Row"))
                        {
                            //ckItemNum = j;
                            xmlCfg.rItem[j].r_id = row.Attribute("id").Value;
                            xmlCfg.rItem[j].r_name = row.Attribute("name").Value;
                            xmlCfg.rItem[j].r_type = row.Attribute("type").Value;
                            xmlCfg.rItem[j].r_value = row.Attribute("value").Value;
                            xmlCfg.rItem[j].r_format = row.Attribute("format").Value;
                            pRowName = xmlCfg.rItem[j].r_name;  //查错信息
                            pRowID = xmlCfg.rItem[j].r_id;

                            if (String.IsNullOrEmpty(xmlCfg.rItem[j].r_value))
                            {
                                if (xmlCfg.rItem[j].r_format.Equals("int") || xmlCfg.rItem[j].r_format.Equals("short") || xmlCfg.rItem[j].r_format.Equals("float") ||
                                    (xmlCfg.rItem[j].r_format.Equals("byte") && xmlCfg.rItem[j].r_type.Equals("text")))
                                {
                                    xmlCfg.rItem[j].r_value = "0";
                                }
                            }

                            if (i == 0)
                            {
                                //检查类型是否匹配
                                if (xmlCfg.rItem[j].r_type.Equals("list") && !xmlCfg.rItem[j].r_format.Equals("byte"))
                                {
                                    throw new Exception("预设值'list'的格式必须设置为'byte'类型");
                                }

                                if (!fmtList.Contains(xmlCfg.rItem[j].r_format))
                                {
                                    throw new Exception("数据类型\"format\"填写错误，必须为byte,short,int,string,float或array");
                                }

                                //检查Row ID
                                if (!(int.TryParse(row.Attribute("id").Value, out idnum) && idnum == j))
                                {
                                    throw new Exception(String.Format("Row的id属性不是连续的序列号,属性\"{0}\"的ID应为[{1:00}]", row.Attribute("name").Value, j));
                                }
                                if (xmlCfg.nameIndex.Contains(row.Attribute("name").Value))
                                {
                                    throw new Exception(String.Format("组中包含有相同的属性名称\"{0}\"", row.Attribute("name").Value));
                                }
                                if (nameIndexCheck.Contains(row.Attribute("name").Value))
                                {
                                    throw new Exception(String.Format("XML配置文件中包含相同的字符串属性名称\"{0}\"", row.Attribute("name").Value));
                                }
                                xmlCfg.nameIndex.Add(row.Attribute("name").Value);
                                nameIndexCheck.Add(row.Attribute("name").Value);
                            }
                            j++;
                        }
                    }
                    cfgList[grpCount] = xmlCfg;
                    grpCount++;
                    pGroupName = String.Empty;
                    pGroupID = String.Empty;
                }
            }
            catch (Exception Exc)
            {
                string ermsg = Exc.Message;
                Debug.Log("[错误]   解析文件 [ " + Path.GetFileName(xmlfile) + " ] 出错." );
                if (pGroupName != String.Empty)
                {
                    Debug.Log("         组名 [ " + pGroupName + " ] ，组ID [ " + pGroupID + " ]");
                }
                if(pRowName != String.Empty)
                {
                    Debug.Log("         行名 [ " + pRowName + " ] ，行ID [ " + pRowID + " ]");
                }
                Debug.Log("         错误信息: " + ermsg);
                return false;
            }
            return true;
        }

        String GetValue(ICell ic)
        {
            try
            {
                return ic.NumericCellValue.ToString();
            }
             catch (System.InvalidOperationException validErr)
            {
                Debug.Log(validErr.Message);
            }
            try
            {
                return ic.StringCellValue.ToString();
            }
            catch (System.InvalidOperationException validErr)
            {
                Debug.Log(validErr.Message);
            }

            return "";
        }

        public Boolean ImportExcelData(String excelFile,String sheetName)
        {
            //根据配置XML文件，读取Excel文件数据，建立文件数据结构
            int rows = 2;//,
            int cols = 0;

            //TODO：LW:需要修改，不能讲写死的数据放在公共的库里。

            //如果是word表(表名写死251) 检查是否有重复的name或者空项
            bool bWordTable = false;
            List<string> listWordKey = null;
            if (sheetName.Equals("251(word)"))
            {
                bWordTable = true;
                listWordKey = new List<string>();
            }
            try
            {
                //object missing = System.Reflection.Missing.Value;
                //FileStream stream = File.Open(excelFile, FileMode.Open, FileAccess.Read);
                //myBook = new XSSFWorkbook(stream);

                ICell cell;
                ISheet worksheet = myBook.GetSheet(sheetName);
                List<IDataValidation> allValidation = worksheet.GetDataValidations();

                totalLine = 0;
                for (int i = 2; i < worksheet.PhysicalNumberOfRows; i++)
                {
                    IRow ir = worksheet.GetRow(i);
                    if (ir == null || ir.Cells[0].ToString() == "")
                    {
                        break;
                    }

                    totalLine++;
                }

                myExportData = new xmlInfData(totalLine);
                myExportData.grpNum = (ushort)totalGrpNum;
                myExportData.colNum = (ushort)totalCols;

                rows = 2;
                //cols = 0;
                while (true)
                {
                    if (totalLine <= rows - 2)
                    {
                        break;
                    }

                    

                    int startCol = 0;
                    myExportData.itemList[rows - 2] = new itemInfData((Byte)totalGrpNum);
                    for (int i = 0; i < totalGrpNum; i++)
                    {
                        groupInfData group = new groupInfData((Byte)cfgList[i].grpItmNum, (Byte)cfgList[i].grpRptNum);
                        group.size = 0;
                        group.baseNum = (Byte)cfgList[i].grpBaseNum;//基本组结构;

                        myExportData.itemList[rows - 2].grpList[i] = group;

                        

                        rowInfDataEdit rowData = null;

                        int repeatSet = cfgList[i].grpRptNum;

                        int repeatRows = 0;

                        IRow row = null;
                      //  bool askBreak = false;

                        for (int j = 0; j < cfgList[i].grpItmNum; j++)
                        {
                            
                           cols = startCol + j;
  
                            //超过了实际长度
                            if (j >= group.rowNum)
                                throw new Exception("Row " + i + " is not as long as expect");

                            row = worksheet.GetRow(rows);

                            cell = row.GetCell(cols);                           
                         
                            //LW: 需要修改！！！！
                            if (bWordTable)
                            {
                                if(null == cell || string.IsNullOrEmpty(cell.ToString()))
                                {
                                    Debug.LogError(String.Format("[word表空值]   [{0}]行[{1}]列的", rows, cols));
                                    continue;
                                }
                                string value = cell.ToString();  
                                if (cfgList[i].nameIndex[j].Equals("name"))
                                {
                                    if (listWordKey.Contains(value))
                                    {
                                        Debug.LogError(String.Format("[word表错误]   [{0}]行[{1}]列的name重复", rows, cols));
                                        continue;
                                    }
                                    listWordKey.Add(value);
                                }
                            }


                            //quick jump repeat set
                            if (repeatSet > 1 && j % group.baseNum == 0)
                            {
                                string value = cell.ToString();
                                Debug.Log("value " + value + " for " + i + "," + j);

                                if (string.IsNullOrEmpty(value))
                                    break;
                            }


                            if (cfgList[i].rItem[j].r_format == "byte")
                            {
                                rowData = ExportByte(rows, cols, cell, allValidation, i, rowData, j);

                            }
                            else if (cfgList[i].rItem[j].r_format == "short")
                            {
                                rowData = ExportShort(cell, i, rowData, j);
                            }
                            else if (cfgList[i].rItem[j].r_format == "int")
                            {
                                rowData = ExportInt(cell, i, rowData, j);
                            }
                            else if (cfgList[i].rItem[j].r_format == "string"
                                || cfgList[i].rItem[j].r_format == "array")
                            {
                                rowData = ExportString(cell, i, rowData, j);

                            }
                            else if (cfgList[i].rItem[j].r_format == "float")
                            {
                                rowData = ExportFloat(cell, i, rowData, j);
                            }
                            else
                            {
                                throw new Exception("unvalid type");
                            }

                            group.rowList.Add(rowData);

                            if (cfgList[i].rItem[j].r_format == "string" 
                                || cfgList[i].rItem[j].r_format == "array")
                            {
                                group.size += (UInt16)(rowData.size + 4);//正序和反序的short，表示字符串长度
                            }
                            else
                            {
                                group.size += rowData.size;
                            }

                            repeatRows++;

                        }//for

                        if ( repeatSet > 1 && group.rowList.Count != group.rowNum )
                        {
                            group.Trim();
                        }

                        startCol += cfgList[i].grpItmNum;

                    }

                    rows += 1;
                }//while

                //计算组数据的参数
                for (int i = 0; i < myExportData.lineNum; i++)
                {
                    for (int j = 0; j < myExportData.grpNum; j++)
                    {
                        UInt16 dataNum = (UInt16)(myExportData.itemList[i].grpList[j].rowNum);
                        myExportData.itemList[i].grpList[j].size += dataNum;
                    }
                }
            }
            catch (Exception er1)
            {
                string ermsg = er1.Message;
                Debug.Log("[错误]   导入Excel文件数据出错");
                Debug.Log("         错误信息 " + ermsg);
                Debug.Log("         " + rows + "行[" + cols + "]列导出数据出错");
                return false;
            }
            finally
            {
                myBook.Close();
                // 9.释放资源
                //System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                //System.Runtime.InteropServices.Marshal.ReleaseComObject(myBook);

                // 10.调用GC的垃圾收集方法  
                GC.Collect();  
                GC.WaitForPendingFinalizers();  

            }
            return true;
        }

        private rowInfDataEdit ExportFloat(ICell cell, int i, rowInfDataEdit rowData, int j)
        {
            float result;
            rowData = new rowInfDataEdit(4);
            rowData.dType = EXPORT_DDTP.DDTP_FLOAT;
            if (cell != null)
            {
                string vvv = GetValue(cell);
                result = float.Parse(vvv);
            }
            else
            {
                result = float.Parse(cfgList[i].rItem[j].r_value);
            }
            rowData.size = 4;
            byte[] tmp = BitConverter.GetBytes(result);
            Array.Copy(tmp, rowData.value, tmp.Count());
            return rowData;
        }

        private rowInfDataEdit ExportString(ICell cell, int i, rowInfDataEdit rowData, int j)
        {
            Byte[] tmpstr = null;
            UInt16 lenth;

            if (GetValue(cell).ToString() == String.Empty)
            {
                tmpstr = null;
                lenth = 0;
            }
            else
            {
                tmpstr = Encoding.UTF8.GetBytes(GetValue(cell).ToString());
                lenth = (UInt16)tmpstr.Count();
            }
            rowData = new rowInfDataEdit(lenth);
            rowData.dType = EXPORT_DDTP.DDTP_STRING;
            if (cfgList[i].rItem[j].r_format == "array")
            {
                rowData.dType = EXPORT_DDTP.DDTP_ARRAY;
            }
            rowData.size = lenth;
            if (lenth > 0)
            {
                Array.Copy(tmpstr, rowData.value, tmpstr.Count());
            }
            return rowData;
        }

        private rowInfDataEdit ExportInt(ICell cell, int i, rowInfDataEdit rowData, int j)
        {
            Int32 result;
            rowData = new rowInfDataEdit(4);
            rowData.dType = EXPORT_DDTP.DDTP_INT;
            if (cell != null)
            {
                string vvv = GetValue(cell);
                if (vvv.Contains("."))
                {
                    vvv = vvv.Remove(vvv.IndexOf("."));
                }
                result = Int32.Parse(vvv);
            }
            else
            {
                result = Int32.Parse(cfgList[i].rItem[j].r_value);
            }
            rowData.size = 4;
            byte[] tmp = BitConverter.GetBytes(result);
            Array.Copy(tmp, rowData.value, tmp.Count());
            return rowData;
        }

        private rowInfDataEdit ExportShort(ICell cell, int i, rowInfDataEdit rowData, int j)
        {
            Int16 result;
            rowData = new rowInfDataEdit(2);
            rowData.dType = EXPORT_DDTP.DDTP_SHORT;
            if (cell != null)
            {
                string vvv = GetValue(cell);
                if (vvv.Contains("."))
                {
                    vvv = vvv.Remove(vvv.IndexOf("."));
                }
                result = Int16.Parse(vvv);
            }
            else
            {
                result = Int16.Parse(cfgList[i].rItem[j].r_value);
            }

            rowData.size = 2;
            byte[] tmp = BitConverter.GetBytes(result);
            Array.Copy(tmp, rowData.value, tmp.Count());
            return rowData;
        }

        private rowInfDataEdit ExportByte(int rows, int cols, ICell cell, List<IDataValidation> allValidation, int i, rowInfDataEdit rowData, int j)
        {
            rowData = new rowInfDataEdit(1);
            rowData.dType = EXPORT_DDTP.DDTP_BYTE;
            Byte result = 0;
            if (cfgList[i].rItem[j].r_type == "list")
            {
                try
                {
                    for (int aj = 0; aj < allValidation.Count; aj++)
                    {
                        for (int m = 0; m < allValidation[aj].Regions.CellRangeAddresses.Length; m++)
                        {
                            if (allValidation[aj].Regions.CellRangeAddresses[m].IsInRange(2, i))
                            {
                                string all = allValidation[aj].ValidationConstraint.Formula1;
                                all = all.Substring(1, all.Length - 2);
                                string[] items = all.Split(',');
                                string val = GetValue(cell).ToString();
                                result = 0;
                                for (int n = 0; n < items.Length; n++)
                                {
                                    if (val == items[n])
                                    {
                                        result = Byte.Parse((n + 1).ToString());
                                        break;
                                    }
                                }
                            }
                        }
                    }

                }
                catch (COMException validErr)
                {
                    string ermsg = validErr.Message;
                    //Debug.LogError("[错误]   导入Excel文件数据出错，错误信息 " + ermsg);
                    Debug.LogError(string.Format("[错误]   请检查[{0}]行[{1}]列的数据类型是否为list.确保Excel中填写了序列值.", rows, cols));
                    throw validErr;
                }
            }
            else
            {
                if (cell != null)
                {
                    result = Byte.Parse(GetValue(cell).ToString());
                }
                else
                {
                    result = Byte.Parse(cfgList[i].rItem[j].r_value);
                }
            }
            rowData.size = 1;
            rowData.value[0] = result;
            return rowData;
        }

        void WriteUTF8String(BinaryWriter bw, String text)
        {
            Byte[] tmpstr = Encoding.UTF8.GetBytes(text);
            Byte[] tmpsize = BitConverter.GetBytes((UInt16)tmpstr.Length);         //反序,Java服务器使用
            Array.Reverse(tmpsize);
            bw.Write((UInt16)tmpstr.Length);
            bw.Write(tmpsize);
            bw.Write(tmpstr);
        }

        Boolean WriteDataFile(String outFile)
        {

            //建立导出文件
            string path1 = Path.GetDirectoryName(outFile);
            if (!Directory.Exists(path1))
            {
                Directory.CreateDirectory(path1);
            }
            if (File.Exists(outFile))
            {
                File.Delete(outFile);
            }

            //写入数据文件
            FileStream fs = File.Create(outFile);
            BinaryWriter bw = new BinaryWriter(fs);

            try
            {
                //写入行头信息
                bw.Write(myExportData.flag);
                bw.Write(myExportData.lineNum);
                bw.Write(myExportData.colNum);
                bw.Write(myExportData.grpNum);

                //写入索引信息
                for (int i = 0; i < myExportData.grpNum; i++)
                {
                    //组索引
                    WriteUTF8String(bw, grpIndex[i]);

                    //属性索引
                    bw.Write((UInt16)cfgList[i].nameIndex.Count());
                    foreach (String name in cfgList[i].nameIndex)
                    {
                        WriteUTF8String(bw, name);
                    }
                }

                //写入组和属性的数据
                for (int i = 0; i < myExportData.lineNum; i++)
                {
                    for (int j = 0; j < myExportData.grpNum; j++)
                    {
                        UInt16 valSize;
                        EXPORT_DDTP dtype;
                        Byte[] vals;

                        groupInfData group = myExportData.itemList[i].grpList[j];

                        bw.Write(group.size);
                        bw.Write(group.rowNum);
                        bw.Write(group.baseNum);
                        UInt16 allItemNum = group.rowNum;//myExportData.itemList[i].grpList[j].rowNum;
                        for (int n = 0; n < allItemNum; n++)
                        {
                            if (group.rowList.Count <= n)
                            {
                                Debug.LogError("导出" + outFile + "出错，可能与有空格有关");
                                return false;
                            }
                            valSize = group.rowList[n].size;
                            dtype = group.rowList[n].dType;
                            bw.Write((Byte)dtype); //写入类型
                            if (dtype == EXPORT_DDTP.DDTP_STRING
                                || dtype == EXPORT_DDTP.DDTP_ARRAY)
                            {
                                //写入字符串长度
                                bw.Write(valSize);                                      //正序,C#客户端使用
                                Byte[] tmpstr = BitConverter.GetBytes(valSize);         //反序,Java服务器使用
                                Array.Reverse(tmpstr);
                                bw.Write(tmpstr);

                                //写入二进制数值
                                if (valSize > 0)
                                {
                                    vals = group.rowList[n].value;
                                    bw.Write(vals);
                                }

                            }
                            else
                            {
                                vals = group.rowList[n].value;
                                bw.Write(vals); //写入二进制数值
                            }


                        }
                    }
                    if ((i + 1) == myExportData.lineNum / 3 || (i + 1) == myExportData.lineNum / 2 || (i + 1) == 2 * myExportData.lineNum / 3 || (i + 1) == myExportData.lineNum)
                    {
                        //Debug.Log("[运行]   写入数据文件已完成" + 100 * (i + 1) / myExportData.lineNum + "%");
                    }
                }

                //bw.Flush();
                //bw.Close();
                //fs.Close();

             
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                bw.Flush();
                bw.Close();
                fs.Close();
            }

            string serverPath = EditorPrefs.GetString("exportServerPath");
            if (serverPath != "")
            {
                File.Copy(outFile, serverPath + outFile, true);
            }
          
            return true;
        }


        public Boolean ExportingProcess()
        {
            //检查Excel文件个数
            if (excelFile == String.Empty)
            {
                if (!CheckExcelFileNum())
                {
                    return false;
                }
            }


            try
            {
                for (int exIndex = 0; exIndex < excelList.Count; ++exIndex )
                {
                    string exportFile = excelList[exIndex];

                    EditorUtility.DisplayProgressBar("窗口", "正在导出Excel...", 1f * (exIndex+1) / excelList.Count);

                    resetParameters();

                    //检查XML配置文件
                    int rslt = CheckXMLFileNum(exportFile);
                    if (rslt == 0)
                    {
                        continue;
                    }
                    else if (rslt < 0)
                    {
                        return false;
                    }

                    Debug.Log("\n");
                    Debug.Log("[运行]   执行表单数据导出操作.");


                    foreach (var files in xmlFilePair)
                    {
                        Debug.Log("[运行]   导出表单 [ " + files.Key + " ] 的数据到文件 [ " + Path.GetFileName(exptFilePair[files.Key]) + " ].\n");

                        //解析xml配置文件
                        Debug.Log("[运行]   正在解析XML配置文件 " + Path.GetFileName(files.Value));
                        if (!AnalyseXML(files.Value))
                        {
                            Debug.LogError("[提示]   解析XML配置文件出错！");
                            return false;
                        }
                        Debug.Log("[运行]   解析XML配置文件完毕" + Path.GetFileName(files.Value));

                        //导入excel数据
                        Debug.Log("[运行]   正在导入Excel表单 [ " + Path.GetFileName(files.Key) + " ] 数据...");
                        if (!ImportExcelData(exportFile, files.Key))
                        {
                            Debug.LogError("[提示]   导入Excel表单数据出错！");
                            return false;
                        }
                        Debug.Log("[运行]   导入Excel表单 [ " + Path.GetFileName(files.Key) + " ] 数据完毕.\n");

                        //写入数据文件
                        Debug.Log("[运行]   正在写入数据文件 [ " + Path.GetFileName(exptFilePair[files.Key]) + " ] ...");

                        if (!WriteDataFile(exptFilePair[files.Key]))
                        {
                            Debug.LogError("[提示]   写入二进制数据文件出错！");
                            return false;
                        }

                        Debug.Log("[运行]   写入数据文件 [ " + Path.GetFileName(exptFilePair[files.Key]) + " ] 完毕.\n");


                        Debug.Log("[运行]   导出表单 [ " + files.Key + " ] 的数据，操作完毕");


                        Debug.Log("[提示]   对Excel文件 [ " + Path.GetFileName(exportFile) + " ] 的数据导出完毕");
                    }
                }
             }
            catch (Exception e)
            {
                Exception ex = e;
                string error = ex.Message;
                Debug.LogError(error);
                return false;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            return true;
        }
    }

    class xmlConfig
    {
        public string grpName;
        public string grpID;
        public int grpRptNum;
        public int grpItmNum;
        public int grpBaseNum;

        public rowItemData[] rItem;
        public List<String> nameIndex;
        public xmlConfig(int num)
        {
            grpName = String.Empty;
            grpID = String.Empty;
            grpRptNum = 0;
            grpItmNum = 0;
            grpBaseNum = 0;
            rItem = new rowItemData[num];
            nameIndex = new List<String>();
            
        }
    }
}
