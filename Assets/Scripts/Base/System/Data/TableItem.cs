using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spenve
{
    public class TableItem
    {
        public Table rootTable;
        public ItemGroup[] grpList;
        public TableItem(Table root,Byte gsz)
        {
            rootTable = root;
            grpList = new ItemGroup[gsz];
        }

        public int GetNumValue(int grpIndex, int index, int repeat = 0)
        {
            return grpList[grpIndex].GetRepeatIntValue(repeat, index);
        }

        public int GetNumValue(String grpName, String itemName, int repeat = 0)
        {
            int grpIndex = rootTable.GetIndexValue(grpName);
            int itemIndex = rootTable.GetIndexValue(itemName);
            return GetNumValue(grpIndex, itemIndex, repeat);
        }

        public int GetNumValue(String grpName, int index, int repeat = 0)
        {
            int grpIndex = rootTable.GetIndexValue(grpName);
            return GetNumValue(grpIndex, index, repeat);
        }

        public int GetNumValue(int grpIndex, String itemName, int repeat = 0)
        {
            int itemIndex = rootTable.GetIndexValue(itemName);
            return GetNumValue(grpIndex, itemIndex, repeat);
        }

        public String GetStringValue(int grpIndex, int index, int repeat = 0)
        {
            return grpList[grpIndex].GetRepeatStringValue(repeat, index);
        }

        public String GetStringValue(String grpName, String itemName, int repeat = 0)
        {
            int grpIndex = rootTable.GetIndexValue(grpName);
            int itemIndex = rootTable.GetIndexValue(itemName);
            return GetStringValue(grpIndex, itemIndex, repeat);
        }

        public String GetStringValue(String grpName, int itemIndex, int repeat = 0)
        {
            int grpIndex = rootTable.GetIndexValue(grpName);
            return GetStringValue(grpIndex, itemIndex, repeat);
        }

        public String GetStringValue(int grpIndex, String itemName, int repeat = 0)
        {
            int itemIndex = rootTable.GetIndexValue(itemName);
            return GetStringValue(grpIndex, itemIndex, repeat);
        }


        public int[] GetRepeatIntArrayValue(int grpIndex, String itemName, int repeat = 0)
        {
            int itemIndex = rootTable.GetIndexValue(itemName);
            return GetRepeatIntArrayValue(grpIndex, itemIndex, repeat);
        }
        public int[] GetRepeatIntArrayValue(int grpIndex, int index, int repeat = 0)
        {
            return grpList[grpIndex].GetRepeatIntArrayValue(repeat, index);
        }
        public float GetFloatValue(int grpIndex, int index, int repeat = 0)
        {
            return grpList[grpIndex].GetRepeatFloatValue(repeat, index);
        }
        public float GetFloatValue(int grpIndex, String itemName, int repeat = 0)
        {
            int itemIndex = rootTable.GetIndexValue(itemName);
            return GetFloatValue(grpIndex, itemIndex, repeat);
        }

        public ItemGroup GetGroup(int grpIndex)
        {
            return grpList[grpIndex];
        }

        public ItemGroup GetGroup(String grpName)
        {
            int grpIndex = rootTable.GetIndexValue(grpName);
            return GetGroup(grpIndex);
        }

        public int GetRepeatNum(string grpName)
        {
            int grpIndex = rootTable.GetIndexValue(grpName);
            return grpList[grpIndex].GetRepeatNum();
        }
    }
}
