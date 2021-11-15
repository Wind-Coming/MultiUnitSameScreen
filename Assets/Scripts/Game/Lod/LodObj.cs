using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Spenve;

[Serializable]
public class LodGameObj
{
    public Vector2Int lodRange;
    public GameObject obj;
}

[ExecuteInEditMode]
public class LodObj : MonoBehaviour
{
    public bool isGroup = false;
    public LodGameObj[] LodGameObjs;

    void OnEnable()
    {
        if(LodMgr.Instance == null)
            return;
        OnLodChanged(LodMgr.Instance.m_iCurrentLod);
        MsgSystem.Instance.AddListener<int>("OnLodChanged", OnLodChanged);
    }

    void OnDisable()
    {
        if(LodMgr.Instance == null)
            return;
        MsgSystem.Instance.RemoveListener<int>("OnLodChanged", OnLodChanged);
    }

    public void SetMaxLod()
    {
        int max = 0;
        for (int i = 0; i < LodGameObjs.Length; i++)
        {
            if(LodGameObjs[i].lodRange.y > max)
            {
                max = LodGameObjs[i].lodRange.y;
            }
        }

        OnLodChanged(max);
    }

    void OnLodChanged(int lod)
    {
        if(LodGameObjs == null)
            return;
            
        for(int i = 0; i < LodGameObjs.Length; i++)
        {
            if(LodGameObjs[i].obj == null)
                continue;

            if(lod >= LodGameObjs[i].lodRange.x && lod <= LodGameObjs[i].lodRange.y)
            {
                LodGameObjs[i].obj.SetActive(true);
            }
            else
            {
                LodGameObjs[i].obj.SetActive(false);
            }
        }
    }
}
