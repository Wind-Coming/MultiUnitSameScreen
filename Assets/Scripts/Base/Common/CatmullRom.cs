using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PointTangent
{
    public Vector3 pos;
    public Vector3 tan;
}

public class CatmullRom
{
    public static Vector3 CatmullRomPoint(Vector3 P0, Vector3 P1, Vector3 P2, Vector3 P3, float t)
    {
        float factor = 0.5f;
        Vector3 c0 = P1;
        Vector3 c1 = (P2 - P0) * factor;
        Vector3 c2 = (P2 - P1) * 3f - (P3 - P1) * factor - (P2 - P0) * 2f * factor;
        Vector3 c3 = (P2 - P1) * -2f + (P3 - P1) * factor + (P2 - P0) * factor;

        Vector3 curvePoint = c3 * t * t * t + c2 * t * t + c1 * t + c0;

        return curvePoint;
    }
    
    public static Vector3 FindSplinePoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 ret = new Vector3();

        float t2 = t * t;
        float t3 = t2 * t;

        ret.x = 0.5f * ((2.0f * p1.x) +
            (-p0.x + p2.x) * t +
            (2.0f * p0.x - 5.0f * p1.x + 4 * p2.x - p3.x) * t2 +
            (-p0.x + 3.0f * p1.x - 3.0f * p2.x + p3.x) * t3);

        ret.y = 0.5f * ((2.0f * p1.y) +
            (-p0.y + p2.y) * t +
            (2.0f * p0.y - 5.0f * p1.y + 4 * p2.y - p3.y) * t2 +
            (-p0.y + 3.0f * p1.y - 3.0f * p2.y + p3.y) * t3);

        ret.z = 0.5f * ((2.0f * p1.z) +
            (-p0.z + p2.z) * t +
            (2.0f * p0.z - 5.0f * p1.z + 4 * p2.z - p3.z) * t2 +
            (-p0.z + 3.0f * p1.z - 3.0f * p2.z + p3.z) * t3);

        return ret;
    }

    private static Vector3 FindSplineTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        Vector3 ret = new Vector3();

        float t2 = t * t;

        ret.x = 0.5f * (-p0.x + p2.x) +
            (2.0f * p0.x - 5.0f * p1.x + 4 * p2.x - p3.x) * t +
            (-p0.x + 3.0f * p1.x - 3.0f * p2.x + p3.x) * t2 * 1.5f;

        ret.y = 0.5f * (-p0.y + p2.y) +
            (2.0f * p0.y - 5.0f * p1.y + 4 * p2.y - p3.y) * t +
            (-p0.y + 3.0f * p1.y - 3.0f * p2.y + p3.y) * t2 * 1.5f;

        ret.z = 0.5f * (-p0.z + p2.z) +
            (2.0f * p0.z - 5.0f * p1.z + 4 * p2.z - p3.z) * t +
            (-p0.z + 3.0f * p1.z - 3.0f * p2.z + p3.z) * t2 * 1.5f;

        return ret;
    }

    //通过贝塞尔曲线插值
    public static List<Vector3> GetAllPoints(List<Vector3> Points, float inteval, int lNum = 0)
    {
        List<Vector3> allPoints = new List<Vector3>();
        List<Vector3> temPoints = new List<Vector3>();
        for( int i = 0; i < Points.Count; i++ )
        {
            temPoints.Add(Points[i]);
        }

        //前后各插一点
        Vector3 f = temPoints[0] + temPoints[0] - temPoints[1];
        Vector3 b = temPoints[temPoints.Count - 1] + temPoints[temPoints.Count - 1] - temPoints[temPoints.Count - 2];
        temPoints.Insert(0, f);
        temPoints.Add(b);

        for (int i = 0; i < temPoints.Count - 3; i++)
        {
            float disOfTwoPoints = Vector3.Distance(temPoints[i + 1], temPoints[i + 2]);
            int lerpNum = (int)(disOfTwoPoints / inteval) + 1;
            if( lNum != 0 )
            {
                lerpNum = lNum;
            }
            int realNum = lerpNum;
            if( i == temPoints.Count - 4 )
            {
                realNum++;
            }
            for (int n = 0; n < realNum; n++)
            {
                Vector3 ppp = CatmullRom.FindSplinePoint(temPoints[i], temPoints[i + 1], temPoints[i + 2], temPoints[i + 3], n * 1.0f / lerpNum);
                allPoints.Add(ppp);
            }
        }

        return allPoints;
    }

    //通过贝塞尔曲线插值,并返回切线
    public static List<PointTangent> GetAllPointsAndTan(List<Vector3> Points)
    {
        List<PointTangent> allPointsTan = new List<PointTangent>();
        List<Vector3> temPoints = new List<Vector3>();
        for (int i = 0; i < Points.Count; i++)
        {
            temPoints.Add(Points[i]);
        }

        //前后各插一点
        Vector3 f = temPoints[0] + temPoints[0] - temPoints[1];
        Vector3 b = temPoints[temPoints.Count - 1] + temPoints[temPoints.Count - 1] - temPoints[temPoints.Count - 2];
        temPoints.Insert(0, f);
        temPoints.Add(b);

        for (int i = 0; i < temPoints.Count - 3; i++)
        {
            float disOfTwoPoints = Vector3.Distance(temPoints[i + 1], temPoints[i + 2]);
            int lerpNum = (int)(disOfTwoPoints / 0.2f) + 1;
            for (int n = 0; n < lerpNum; n++)
            {
                PointTangent ppp = new PointTangent();
                ppp.pos = CatmullRom.FindSplinePoint(temPoints[i], temPoints[i + 1], temPoints[i + 2], temPoints[i + 3], n * 1.0f / lerpNum);
                ppp.tan = CatmullRom.FindSplineTangent(temPoints[i], temPoints[i + 1], temPoints[i + 2], temPoints[i + 3], n * 1.0f / lerpNum);
                allPointsTan.Add(ppp);
            }
        }

        return allPointsTan;
    }


    //获取总长度（这些点是贝塞尔曲线计算过后的点）
    public static float GetDistance(List<Vector3> Points)
    {
        float distance = 0;
        for( int i = 1; i < Points.Count; i++ )
        {
            distance += Vector3.Distance(Points[i - 1], Points[i]);
        }
        return distance;
    }

    //获取总长度（这些点是贝塞尔曲线计算过后的点）
    public static float GetDistance(List<PointTangent> Points)
    {
        float distance = 0;
        for (int i = 1; i < Points.Count; i++)
        {
            distance += Vector3.Distance(Points[i - 1].pos, Points[i].pos);
        }
        return distance;
    }

    //通过距离计算位置（这些点是贝塞尔曲线计算过后的点）
    public static PointTangent GetPositonWithDistance(List<PointTangent> Points, float dis)
    {
        PointTangent pos = new PointTangent();
        float distance = 0;
        for (int i = 1; i < Points.Count; i++)
        {
            distance += Vector3.Distance(Points[i - 1].pos, Points[i].pos);
            if( dis <= distance )
            {
                pos.pos = Points[i].pos + (Points[i-1].pos - Points[i].pos).normalized * (distance - dis);
                pos.tan = Vector3.Lerp(Points[i - 1].tan, Points[i].tan, Vector3.Distance(pos.pos, Points[i - 1].pos) / Vector3.Distance(Points[i].pos, Points[i - 1].pos));
                return pos;
            }
        }

        return pos;
    }

    //获取点的索引（计算之前的点）
    public static int GetIndex(List<Vector3> Points, Vector3 p)
    {
        List<Vector3> allPoints = new List<Vector3>();
        List<Vector3> temPoints = new List<Vector3>();
        for (int i = 0; i < Points.Count; i++)
        {
            temPoints.Add(Points[i]);
        }

        //前后各插一点
        Vector3 f = temPoints[0] + temPoints[0] - temPoints[1];
        Vector3 b = temPoints[temPoints.Count - 1] + temPoints[temPoints.Count - 1] - temPoints[temPoints.Count - 2];
        temPoints.Insert(0, f);
        temPoints.Add(b);

        for (int i = 0; i < temPoints.Count - 3; i++)
        {
            float disOfTwoPoints = Vector3.Distance(temPoints[i + 1], temPoints[i + 2]);
            int lerpNum = (int)(disOfTwoPoints / 0.2f) + 1;
            for (int n = 0; n < lerpNum; n++)
            {
                Vector3 ppp = CatmullRom.FindSplinePoint(temPoints[i], temPoints[i + 1], temPoints[i + 2], temPoints[i + 3], n * 1.0f / lerpNum);
                if (ppp == p)
                {
                    return i + 1;
                }
            }
        }

        return -1;
    }
}