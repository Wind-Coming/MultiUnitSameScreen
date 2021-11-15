using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pao : MonoBehaviour
{
    public LineRenderer line;

    //public Vector3 start;
    //public Vector3 end;
    //public float high;
    public Vector3 p1 = new Vector2(0, 0);
    public Vector3 p2 = new Vector2(5, 10);
    public Vector3 p3 = new Vector2(10, 0);


    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 abc = Get(p1, p2, p3);

        int count = line.positionCount - 1;
        for ( int i = 0; i <= count; i++) {
            Vector2 v;
            v.x = i;
            v.y = abc.x * i * i + abc.y * i + abc.z;
            line.SetPosition(i, v);
        }
        //float dis = Vector3.Distance(start, end);
        //Vector3 dir = (end - start).normalized;

        //float a = -high / (dis / 2) * (dis / 2);
        //float b = high;

        //int count = line.positionCount - 1;

        //float half = count * 0.5f;

        //float inteval = dis / count;
        
        //for (int i = 0; i <= count; i++) {
        //    Vector3 v = start + dir * i * dis * 1.0f / count;
        //    float x = (i * 1.0f - half) * inteval;
        //    v.y = a * x * x + b;
        //    line.SetPosition(i, v);
        //}

    }

    public Vector3 Get(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        Vector3 v3;
        float m1Up = (p2.y - p3.y) * p1.x - (p2.x - p3.x) * p1.y + p2.x * p3.y - p3.x * p2.y;
        float m1Down = (p2.x - p3.x) * (p1.x - p2.x) * (p1.x - p3.x);
        v3.x = - m1Up / m1Down;

        float m2Up = (p2.y - p3.y) * p1.x * p1.x + p2.x * p2.x * p3.y - p3.x * p3.x * p2.y - (p2.x * p2.x - p3.x * p3.x) * p1.y;
        float m2Down = (p2.x - p3.x) * (p1.x - p2.x) * (p1.x - p3.x);
        v3.y = m2Up / m2Down;

        float m3Up = (p2.x * p3.y) * p1.x * p1.x - (p2.x * p2.x * p3.y - p3.x * p3.x * p2.y) * p1.x + (p2.x * p2.x * p3.x - p2.x * p3.x * p3.x) * p1.y;
        float m3Down = (p2.x - p3.x) * (p1.x - p2.x) * (p1.x - p3.x);
        v3.z = m3Up / m3Down;
        return v3;
    }
}
