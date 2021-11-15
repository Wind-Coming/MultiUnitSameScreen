using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameUtil
{
    public static List<Vector3Int> GetLineCells(Vector3 p0, Vector3 p1, List<Vector3Int> touched)
    {
        touched.Clear();

        var x0 = p0.x;
        var y0 = p0.z;
        var x1 = p1.x;
        var y1 = p1.z;

        var steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
        if (steep)
        {
            x0 = p0.z;
            y0 = p0.x;
            x1 = p1.z;
            y1 = p1.x;
        }

        if (x0 > x1)
        {
            var x0_old = x0;
            var y0_old = y0;
            x0 = x1;
            x1 = x0_old;
            y0 = y1;
            y1 = y0_old;
        }

        var ratio = Mathf.Abs((y1 - y0) / (x1 - x0));
        int mirror= y1 > y0 ? 1 : -1;

        for (var col = Mathf.Floor(x0); col < Mathf.Ceil(x1); col++)
        {
            float currY = y0 + mirror * ratio * (col - x0);

            //第一格不进行延边计算
            bool skip = false;
            if (col == Mathf.Floor(x0))
            {
                skip = Mathf.Floor(currY) != Mathf.Floor(y0);
            }

            if (!skip)
            {
                if (!steep)
                {
                    touched.Add(new Vector3Int((int)col, 0, Mathf.FloorToInt(currY)));
                }
                else
                {
                    touched.Add(new Vector3Int(Mathf.FloorToInt(currY), 0, (int)col));
                }
            }

            //根据斜率计算是否有跨格。0
            if ( (mirror > 0 ? (Mathf.Ceil(currY) - currY) : (currY - Mathf.Floor(currY))) < ratio)
            {
                int crossY = Mathf.FloorToInt(currY) + mirror;

                //判断是否超出范围
                if ( (mirror > 0 && crossY >= Mathf.Ceil(y1)) || (mirror < 0 && crossY < Mathf.Floor(y1))){
                    break;
                }
                    //跨线格子
                    if (!steep)
                    {
                        touched.Add(new Vector3Int((int)col, 0, crossY));
                    }
                    else
                    {
                        touched.Add(new Vector3Int(crossY, 0, (int)col));
                    }
                
            }
        }
        return touched;
    }
}
