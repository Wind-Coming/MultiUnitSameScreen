using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spenve;

[ExecuteInEditMode]
public class LodMgr : ScriptSingletonWithEditor<LodMgr>
{
    public AnimationCurve m_ScaleCurve;
    public Vector2Int[] LodNums;
    public int m_iCurrentLod = 0; 
    public float m_fCurrentScale = 1;
    public bool m_bScaleChanged = false;

    void OnEnable()
    {
        MsgSystem.Instance.AddListener<int>("OnZoomChanged", OnCameraZoomChanged);
    }

    void OnDisable()
    {
        MsgSystem.Instance.RemoveListener<int>("OnZoomChanged", OnCameraZoomChanged);
    }


    void OnCameraZoomChanged(int high)
    {
        int oldLod = m_iCurrentLod;
        for(int i = 0; i < LodNums.Length; i++)
        {
            if(high >= LodNums[i].x && high <LodNums[i].y)
            {
                if(m_iCurrentLod != i){
                    m_iCurrentLod = i;
                    MsgSystem.Instance.PostMessage("OnLodChanged", m_iCurrentLod);
                    //Debug.Log("lod变化："+ m_iCurrentLod);

                    if(m_iCurrentLod == 0)
                    {
                        Shader.globalMaximumLOD = 300;
                    }
                    else
                    {
                        Shader.globalMaximumLOD = 100;
                    }

                }
                break;
            }
        }

        if(m_iCurrentLod < 1 && high < 150)
        {
            m_fCurrentScale = m_ScaleCurve.Evaluate( ((high - 50) * 1.0f / 100 ) );

            MsgSystem.Instance.PostMessage("OnScaleChanged", m_fCurrentScale);
        }

        if(m_iCurrentLod == 1 && oldLod == 0)
        {
            m_fCurrentScale = 1;
            MsgSystem.Instance.PostMessage("OnScaleChanged", m_fCurrentScale);
        }
    }
}
