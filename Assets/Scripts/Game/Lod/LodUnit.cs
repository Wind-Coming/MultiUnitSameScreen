using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Spenve;

[ExecuteInEditMode]
public class LodUnit : MonoBehaviour
{
    private Vector3 orgScale = Vector3.one;
    
    void Start()
    {
        orgScale = transform.localScale;
    }

    void OnEnable()
    {
        MsgSystem.Instance.AddListener<int>("OnScaleChanged", OnLodScaleChanged);
    }

    void OnDisable()
    {
        MsgSystem.Instance.RemoveListener<int>("OnScaleChanged", OnLodScaleChanged);
    }

    void OnLodScaleChanged(int scale)
    {
        transform.localScale = orgScale * scale;
    }
}
