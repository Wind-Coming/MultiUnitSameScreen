using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spenve;
using Unity.Mathematics;
using Unity.Collections;

public class MapMgr : SingletonDestory<MapMgr>
{
    private const int cellSize = 300;

    private float threshold = 5;
    private Vector3 oldLeftDown = new Vector3(-10000, 0, -10000);
    private Vector3 center = Vector3.zero;

    List<Vector3Int> leftCells = new List<Vector3Int>();
    List<Vector3Int> rightCells = new List<Vector3Int>();
    
    public NativeList<int2> allCells;
    HashSet<int2> allOldCells = new HashSet<int2>();

    void OnEnable()
    {
        allCells = new NativeList<int2>(Allocator.Persistent);
        MsgSystem.Instance.AddListener<Trapezium>("CameraViewPosChange", OnCameraChange);
        MsgSystem.Instance.AddListener<int>("OnLodChanged", OnLodChanged);
    }

    void OnDisable()
    {
        allCells.Dispose();
        MsgSystem.Instance.RemoveListener<Trapezium>("CameraViewPosChange", OnCameraChange);
        MsgSystem.Instance.RemoveListener<int>("OnLodChanged", OnLodChanged);
    }

    int GetCell(Vector3Int v)
    {
        return v.x  * 10000 + v.z;
    }

    int GetCell(int x, int z)
    {
        return x  * 10000 + z;
    }

    int GetX(int hash)
    {
        return hash / 10000;
    }

    int GetZ(int hash)
    {
        return hash % 10000;
    }

    public Vector3 GetCenter()
    {
        return center;
    }
    
    void ClearSameZCell(List<Vector3Int> cells)
    {
        for(int i = 0; i < cells.Count; i++)
        {
            int curz = cells[i].z ;
            if( i + 1 < cells.Count )
            {
                int nextz = cells[i + 1].z;
                if(curz == nextz)
                {
                    cells.RemoveAt(i);
                    i--;
                }
            }
        }
    }

    void OnCameraChange(Trapezium trapezium)
    {
        center = trapezium.center;

        if( (oldLeftDown - trapezium.leftDown).sqrMagnitude < threshold * threshold )
            return;

        oldLeftDown = trapezium.leftDown;

        //根据当前阈值进行延申
        trapezium.leftDown += new Vector3(-threshold, 0, -threshold);
        trapezium.rightDown += new Vector3(threshold, 0, -threshold);
        trapezium.leftTop += new Vector3(-threshold, 0, threshold);
        trapezium.rightTop += new Vector3(threshold, 0, threshold);

        float wid = trapezium.rightDown.x - trapezium.leftTop.x;
        threshold = wid * 0.1f;

        GlobalFunc.BeginSample();

        //计算屏幕两边穿过的格子
        GameUtil.GetLineCells(trapezium.leftDown / cellSize, trapezium.leftTop / cellSize, leftCells);
        GameUtil.GetLineCells(trapezium.rightDown / cellSize, trapezium.rightTop / cellSize, rightCells);

        //排除同一条线上的两个点
        ClearSameZCell(leftCells);
        ClearSameZCell(rightCells);

        if(leftCells.Count != rightCells.Count)
        {
            Debug.LogError("地图计算有误!!!!!!!!!");
            return;
        }

        //摄像机范围内的所有格子
        allCells.Clear();
        for(int i = 0; i < leftCells.Count; i++)
        {
            if(leftCells[i].z != rightCells[i].z)
            {
                Debug.LogError("z值不在同一条水平线上！！！");
                return;
            }

            for(int x = leftCells[i].x; x <= rightCells[i].x; x++)
            {
                if(x >= 0 && leftCells[i].z >= 0)
                    allCells.Add(new int2(x, leftCells[i].z));
            }
        }
        
        //增加的格子
        foreach(var v in allCells)
        {
            if(!allOldCells.Contains(v))
            {
                //Debug.Log("add:" + v);
                LoadTile(v);
            }
        }

        //删除的格子
        foreach(var v in allOldCells)
        {
            if(!allCells.Contains(v))
            {
                //Debug.LogWarning("sub:" + v);
                UnloadTile(v);
            }
        }

        //重新赋值
        allOldCells.Clear();
        foreach(var v in allCells)
        {
            allOldCells.Add(v);
        }

        // GlobalFunc.EndSample();
        // Debug.LogError(allCells.Count);
        //Debug.LogError(trapezium.leftDown + "    " + trapezium.leftTop + "    " + trapezium.rightDown + "     " + trapezium.rightTop);

    }

    void OnLodChanged(int lod)
    {
        foreach(var v in allCells)
        {
            TileUnitMgr.Instance.CreateTile(v);
        }
    }

    void LoadTile(int2 cell)
    {
        if(cell.x < 0 || cell.y < 0)
            return;

        TileUnitMgr.Instance.CreateTile(cell);
    }

    void UnloadTile(int2 cell)
    {
        
    }

    public bool Contains(int2 cell)
    {
        return allCells.Contains(cell);
    }
    // public Vector3 startPos;
    // public Vector3 endPos;
    // void OnDrawGizmos()
    // {
    //     //GameUtil.GetLineCells(startPos / cellSize, endPos / cellSize, leftCells);

    //     Gizmos.color = Color.black;

    //     foreach(var v in allCells)
    //     {
    //         Gizmos.DrawCube(new Vector3(v.x, 0, v.y) * cellSize + new Vector3(0.5f, 0, 0.5f) * cellSize, new Vector3(1, 0, 1) * cellSize);
    //     }

    //     // for(int i = 0; i < rightCells.Count; i++)
    //     // {
    //     //     Gizmos.DrawCube(rightCells[i] * cellSize + new Vector3(0.5f, 0, 0.5f) * cellSize, new Vector3(1, 0, 1) * cellSize);
    //     // }

    //     Gizmos.color = Color.red;
    //     Gizmos.DrawLine(startPos, endPos);
    // }
}
