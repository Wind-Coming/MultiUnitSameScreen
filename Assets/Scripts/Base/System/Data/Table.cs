using Spenve;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Spenve
{
    public enum EXPORT_DDTP : byte
    {
        DDTP_BYTE = 1,
        DDTP_SHORT = 2,
        DDTP_INT = 3,
        DDTP_BOOL = 4,
        DDTP_STRING = 5,
        DDTP_FLOAT = 6,
        DDTP_ARRAY = 7,
    }

    public class Table
    {
        private int tid;
        public int tableid
        {
            get
            {
                return tid;
            }
        }
        public DateTime crtTime;
        tableData fileData;
        Dictionary<String, int> nameMapping;
        
        public void UpdateTableTime()
        {
            crtTime = DateTime.Now;
        }

        public Boolean IsClean()
        {
            TimeSpan ts = DateTime.Now - crtTime;

            if (ts.Minutes > 3)
            {
                return true;
            }
            return false;
        }

        public Table(int id)
        {
            int row1, col1;
            TextAsset tasset = null;
            //Stream fs = new FileStream(Application.dataPath + "/Resources/ExcelExportData/" + id + ".bytes", FileMode.Open, FileAccess.Read);
            //TextAsset tasset = ResourceSystem.Instance.LoadAsset<TextAsset>("data", id + ".bytes");

            Stream fs = new MemoryStream(tasset.bytes);
            BinaryReader rd = null;
            nameMapping = new Dictionary<string, int>();
            try
            {
                //switch (GlobleDefine.Language)
                //{
                //    case ELanguage.SimpleChinese:
                //        fs = Tools.GetResFileFromBundle(id + ".bytes", "TableCN.assetbundle");
                //        break;
                //    case ELanguage.TraditionalChinese:
                //        fs = Tools.GetResFileFromBundle(id + ".bytes", "TableHK.assetbundle");
                //        break;
                //    default:
                //        fs = Tools.GetResFileFromBundle(id + ".bytes", "TableEN.assetbundle");
                //        break;
                //}

                rd = new BinaryReader(fs);

                fileData = new tableData();
                fileData.flag = rd.ReadUInt16();
                if (fileData.flag != 1001)
                    return;

                fileData.lineNum = rd.ReadUInt16();
                fileData.colNum = rd.ReadUInt16();
                fileData.grpNum = rd.ReadUInt16();

                //读取组索引数据
                for(int i=0;i<fileData.grpNum;i++)
                {
                    string val = readUTF8String(rd);
                    nameMapping.Add(val, i);

                    //读取组属性索引数据
                    ushort num = rd.ReadUInt16();
                    for (int j = 0; j < num; j++)
                    {
                        val = readUTF8String(rd);
                        if(nameMapping.ContainsKey(val) && nameMapping[val]==j)
                            continue;
                        nameMapping.Add(val, j);
                    }
                }
                
                fileData.itemList = new TableItem[fileData.lineNum];

                for (int i = 0; i < fileData.lineNum; i++)
                {
                    row1 = i;
                    //行
                    fileData.itemList[i] = new TableItem(this,(Byte)fileData.grpNum);
                    for (int j = 0; j < fileData.grpNum; j++)
                    {
                        col1 = j;
                        //组
                        ItemGroup grpInf = new ItemGroup();
                        grpInf.size = rd.ReadUInt16();
                        grpInf.rowNum = rd.ReadByte();
                        grpInf.baseNum = rd.ReadByte();
                        grpInf.rptNum = (Byte)(grpInf.rowNum / grpInf.baseNum);
                        grpInf.rowList = new rowInfData[grpInf.rowNum];

                        for (int n = 0; n < grpInf.rowNum; n++)
                        {
                            EXPORT_DDTP dType = (EXPORT_DDTP)rd.ReadByte();
                            if (dType == EXPORT_DDTP.DDTP_STRING)
                            {
                                UInt16 size = rd.ReadUInt16();
                                rd.ReadUInt16();
                                Byte[] tmpstr = rd.ReadBytes(size);
                                grpInf.rowList[n] = new rowInfData(size);
                                grpInf.rowList[n].size = size;
                                grpInf.rowList[n].value = tmpstr;
                            }
                            else if (dType == EXPORT_DDTP.DDTP_SHORT)
                            {
                                grpInf.rowList[n] = new rowInfData(2);
                                grpInf.rowList[n].size = 2;
                                grpInf.rowList[n].raw_value = rd.ReadInt16();
                                grpInf.rowList[n].value = BitConverter.GetBytes(grpInf.rowList[n].raw_value);
                            }
                            else if (dType == EXPORT_DDTP.DDTP_INT)
                            {
                                grpInf.rowList[n] = new rowInfData(4);
                                grpInf.rowList[n].size = 4;
                                grpInf.rowList[n].raw_value = rd.ReadInt32();
                                grpInf.rowList[n].value = BitConverter.GetBytes(grpInf.rowList[n].raw_value);
                            }
                            else if (dType == EXPORT_DDTP.DDTP_FLOAT)
                            {
                                grpInf.rowList[n] = new rowInfData(4);
                                grpInf.rowList[n].size = 4;
                                grpInf.rowList[n].raw_valueFloat = rd.ReadSingle();
                                grpInf.rowList[n].value = BitConverter.GetBytes(grpInf.rowList[n].raw_valueFloat);
                            }
                            else if (dType == EXPORT_DDTP.DDTP_ARRAY)
                            {
                                UInt16 size = rd.ReadUInt16();
                                rd.ReadUInt16();
                                Byte[] tmpstr = rd.ReadBytes(size);
                                grpInf.rowList[n] = new rowInfData(size);
                                grpInf.rowList[n].size = size;
                                grpInf.rowList[n].value = tmpstr;

                                String value = Encoding.UTF8.GetString(tmpstr);
                                string[] tmp = value.Split(',');
                                int[] rtn = new int[tmp.Length];
                                for (int kk = 0; kk < tmp.Length; kk++)
                                {
                                    rtn[kk] = int.Parse(tmp[kk]);
                                }
                                grpInf.rowList[n].raw_valueIntArr = rtn;
                            }
                            else //if (dType == EXPORT_DDTP.DDTP_BYTE)  //if (dType == EXPORT_DDTP.DDTP_BOOL)
                            {
                                grpInf.rowList[n] = new rowInfData(1);
                                grpInf.rowList[n].size = 1;
                                grpInf.rowList[n].raw_value = rd.ReadByte();
                                grpInf.rowList[n].value = BitConverter.GetBytes(grpInf.rowList[n].raw_value);
                            }
                            
                        }
                        fileData.itemList[i].grpList[j] = grpInf;
                    }
                }
                tid = id;
                crtTime = DateTime.Now;
            }
            catch (Exception er)
            {
                string msg = er.Message;
                return;
            }
            finally
            {
                rd.Close();
                fs.Close();
            }
        }

        public String readUTF8String(BinaryReader rd)
        {
            UInt16 size = rd.ReadUInt16();
            rd.ReadUInt16(); 
            Byte[] tmp = rd.ReadBytes(size);
            return Encoding.UTF8.GetString(tmp);
        }

        public TableItem GetTableItem(int id)
        {
            id -= 1;

            if ((fileData == null) || (id + 1 > fileData.lineNum) || (fileData.itemList[id] == null))
                return null;

            return fileData.itemList[id];
        }

        public int GetIndexValue(String name)
        {
            if (!nameMapping.ContainsKey(name))
            {
                throw new Exception(String.Format("there is no such name[{0}] in this table.",name));
            }
            
            return nameMapping[name];
        }

        public int GetSize() 
        {
            return fileData.itemList.Length;
        }

        public TableItem[] GetTableItems()
        {
            return fileData.itemList;
        }
    }

}
