
using UnityEngine;
using System.Collections;


namespace nTools.PrefabPainter
{
    public class PrefabPainterSceneSettings : MonoBehaviour
    {     
#if (UNITY_EDITOR) 
        public Placement placeUnder = Placement.World;
        public GameObject parentForPrefabs;
#endif
    }
}
