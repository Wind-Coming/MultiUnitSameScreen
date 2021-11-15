using Spenve;
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spenve
{
    public class DataSystem : SystemSingleton<DataSystem>
    {

        public const int TableFilter = 10000;
        Dictionary<int, Table> tables = new Dictionary<int, Table>();

        public TableItem GetTableItemBySid(int sid)
        {
            int table = sid / TableFilter;
            int index = sid % TableFilter;

            return GetTable(table).GetTableItem(index);
        }

        public int GetItemSID(int table, int index ) 
        {
            return table * TableFilter + index;
        }

        public int GetTableIndex(int sid)
        {
            return sid / TableFilter;
        }
        public int GetTableItemIndex(int sid)
        {
            return sid % TableFilter;
        }
        public Table GetTable(int id)
        {
            Table myTable;

            if (tables.ContainsKey(id))
            {
                myTable = tables[id];
                myTable.UpdateTableTime();
            }
            else
            {
                myTable = new Table(id);
                if (myTable != null)
                {
                    tables.Add(id, myTable);
                }
            }

            //对表进行一个清理操作
            if (tables.Count >= 12)//12
            {
                List<KeyValuePair<int, Table>> lst = new List<KeyValuePair<int, Table>>(tables);
                lst.Sort(delegate(KeyValuePair<int, Table> s1, KeyValuePair<int, Table> s2)
                {
                    return s1.Value.crtTime.CompareTo(s2.Value.crtTime);
                });

                for (int i = 0; i < 11; i++)//11
                {
                    if (i < 8)//8
                    {
                        if (tables[lst[i].Key] != null)
                        {
                            tables[lst[i].Key] = null;
                        }
                        tables.Remove(lst[i].Key);
                    }
                    else
                    {
                        if (tables[lst[i].Key].IsClean())
                        {
                            tables[lst[i].Key] = null;
                            tables.Remove(lst[i].Key);
                        }
                    }
                }
            }

            return myTable;
        }

        public void CloseTable(int id)
        {
            if (tables.ContainsKey(id))
            {
                if (tables[id] != null)
                {
                    tables[id] = null;
                }
                tables.Remove(id);
            }
        }
    }

    //导出数据结构
    public class rowInfData
    {
        public UInt16 size;
        public EXPORT_DDTP type;
        public Byte[] value;
        public int raw_value;
        public float raw_valueFloat;
        public int[] raw_valueIntArr;
        public rowInfData()
        {
            size = 0;
            value = null;
            raw_value = 0;
            raw_valueFloat = 0;
            raw_valueIntArr = new int[0];
        }
        public rowInfData(UInt16 sz)
        {
            size = sz;
            // value = new Byte[sz];
        }
    }


    public class tableData
    {
        public UInt16 flag;
        public UInt16 lineNum;
        public UInt16 colNum;
        public UInt16 grpNum;
        public TableItem[] itemList;
        public tableData()
        {
            flag = 0;
            grpNum = 0;
            lineNum = 0;
            colNum = 0;
            itemList = null;
        }
        public tableData(UInt16 line)
        {
            flag = 1001;
            grpNum = 1;
            lineNum = line;
            colNum = 0;
            itemList = new TableItem[line];
        }
    }
}