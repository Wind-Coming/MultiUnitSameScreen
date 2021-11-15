using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class TileUnit
{
    public bool isSprite;//
    public Vector4 rect;
    public Vector2[] uvs;
    public Mesh mesh;
    public Material material;

    public Vector3 localPosition;
    public Vector3 scale;
    public Quaternion rotation;
}

[CreateAssetMenu(menuName="MapData/TileData")]
public class TileData : ScriptableObject
{
    public List<TileUnit> Lod5Units = new List<TileUnit>();
    public List<TileUnit> Lod4Units = new List<TileUnit>();
    public List<TileUnit> Lod3Units = new List<TileUnit>();
    public List<TileUnit> Lod2Units = new List<TileUnit>();
    public List<TileUnit> Lod1Units = new List<TileUnit>();

    public void Clear()
    {
        Lod1Units.Clear();
        Lod2Units.Clear();
        Lod3Units.Clear();
        Lod4Units.Clear();
        Lod5Units.Clear();
    }

    public List<TileUnit> GetUnits(int lod)
    {
        if(lod == 5)
        {
            return Lod5Units;
        }
        else if(lod == 4)
        {
            return Lod4Units;
        }
        else if (lod == 3)
        {
            return Lod3Units;
        }
        else if (lod == 2)
        {
            return Lod2Units;
        }
        else if (lod == 1)
        {
            return Lod1Units;
        }

        return Lod1Units;
    }
}
