using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshRenderer))]
public class MeshRenderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        MeshRenderer mr = ((MeshRenderer)target);
        mr.sortingLayerName = EditorGUILayout.TextArea(mr.sortingLayerName);
        mr.sortingOrder = EditorGUILayout.IntField(mr.sortingOrder);
    }
}
