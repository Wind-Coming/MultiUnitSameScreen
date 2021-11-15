using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spenve
{
    public class ItemGroup
    {
        public UInt16 size;
        public Byte rowNum;     //总数
        public Byte rptNum;     //重复数
        public Byte baseNum;    //组基本属性数
        public rowInfData[] rowList;
        public ItemGroup()
        {
            size = 0;
            rowNum = 0;
            rptNum = 0;
            baseNum = 0;
            rowList = null;
        }

        public Boolean IsRepeat()
        {
            return rptNum > 1;
        }

        //得到有多少组对象
        public int GetRepeatNum()
        {
            return rptNum;
        }

        //每组对象的属性数量
        public int GetPropertyNum()
        {
            if (rptNum != 0)
            {
                return baseNum;
            }
            return 0;
        }

        public float GetRepeatFloatValue(int rptIndex, int offset)
        {
            if (rptNum == 0)
                return 0;
            int num = rptIndex * baseNum + offset;

            return rowList[num].raw_valueFloat;
        }
        public int GetRepeatIntValue(int rptIndex, int offset)
        {
            if (rptNum == 0)
                return 0;

            //int rptnumber = rowNum / rptNum;
            int num = rptIndex * baseNum + offset;
            //Byte[] intArray = new Byte[4] { 0, 0, 0, 0 };
            //int length = rowList[num].value.Count();
            //Array.Copy(rowList[num].value, intArray, length);

            //int value = BitConverter.ToInt32(rowList[num].value, 0);
            //return value;

            return rowList[num].raw_value;
        }

        public String GetRepeatStringValue(int rptIndex, int offset)
        {
            if (rptNum == 0)
                return String.Empty;

            //int rptnumber = rowNum / rptNum;
            int num = rptIndex * baseNum + offset;

            String value = Encoding.UTF8.GetString(rowList[num].value);
            return value;
        }
        public int[] GetRepeatIntArrayValue(int rptIndex, int offset)
        {
            if (rptNum == 0)
                return null;
            int num = rptIndex * baseNum + offset;
            return rowList[num].raw_valueIntArr;
        }
    }
}
