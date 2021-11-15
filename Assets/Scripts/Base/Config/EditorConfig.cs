using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class EditorConfig
{
    static bool m_SimulateAssetBundleInEditor = true;
    const string kSimulateAssetBundles = "SimulateAssetBundles";
    public static bool SimulateAssetBundleInEditor
    {
        get
        {
            m_SimulateAssetBundleInEditor = EditorPrefs.GetBool(kSimulateAssetBundles, true);
            return m_SimulateAssetBundleInEditor;
        }
        set
        {
            if (value != m_SimulateAssetBundleInEditor)
            {
                m_SimulateAssetBundleInEditor = value;
                EditorPrefs.SetBool(kSimulateAssetBundles, value);
            }
        }
    }

}

#endif