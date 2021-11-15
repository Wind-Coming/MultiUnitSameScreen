using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssetBundleBrowser
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "BuildData", menuName = "Config/BuildEditData", order = 2)]
    public class FraxniusBuildEditData : ScriptableObject
    {
        public List<BuildPipelineStep> pipelines = new List<BuildPipelineStep>();

        public int versionUnit = 4;

    }
}