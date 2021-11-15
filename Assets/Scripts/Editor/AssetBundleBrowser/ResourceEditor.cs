using Spenve;
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;

namespace Spenve
{
    public class ResourceEditor : Editor
    {
        const string kSimulateAssetBundlesMenu = "资源工具/模拟Bundle模式";

        [MenuItem(kSimulateAssetBundlesMenu)]
        public static void ToggleSimulateAssetBundle()
        {
            EditorConfig.SimulateAssetBundleInEditor = !EditorConfig.SimulateAssetBundleInEditor;
        }

        [MenuItem(kSimulateAssetBundlesMenu, true)]
        public static bool ToggleSimulateAssetBundleValidate()
        {
            Menu.SetChecked(kSimulateAssetBundlesMenu, EditorConfig.SimulateAssetBundleInEditor);
            return true;
        }
    }
}