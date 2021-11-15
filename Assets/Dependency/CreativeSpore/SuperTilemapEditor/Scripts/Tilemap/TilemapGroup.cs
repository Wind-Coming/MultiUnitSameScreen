using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.SuperTilemapEditor
{
    [AddComponentMenu("SuperTilemapEditor/TilemapGroup", 10)]
    [DisallowMultipleComponent]
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode] // allow OnTransformChildrenChanged to be called
#endif
    public class TilemapGroup : MonoBehaviour, ISerializationCallbackReceiver
    {
        public STETilemap SelectedTilemap {
            get { return m_selectedIndex >= 0 && m_selectedIndex < m_tilemaps.Count ? m_tilemaps[m_selectedIndex] : null; }
            set { m_selectedIndex = m_tilemaps != null ? m_tilemaps.IndexOf(value) : -1; }
        }
        public int SelectedTilemapIndex { get { return m_selectedIndex; } }
        public List<STETilemap> Tilemaps { get { return m_tilemaps; } }
        public float UnselectedColorMultiplier { get { return m_unselectedColorMultiplier; } set { m_unselectedColorMultiplier = value; } }
        public bool DefaultTilemapWindowVisible { get { return m_defaultTilemapWindowVisible; } set { m_defaultTilemapWindowVisible = value; } }
        public bool DisplayTilemapRList { get { return m_displayTilemapRList; } set { m_displayTilemapRList = value; } }
        public STETilemap this[int idx] { get { return m_tilemaps[idx]; } }
        public STETilemap this[string name] { get { return FindTilemapByName(name); } }

        [SerializeField]
        private List<STETilemap> m_tilemaps = new List<STETilemap>();
        [SerializeField]
        private int m_selectedIndex = -1;
        [SerializeField, Range(0f, 1f)]
        private float m_unselectedColorMultiplier = 1f;
        [SerializeField]
        private bool m_defaultTilemapWindowVisible = false;
        [SerializeField]
        private bool m_displayTilemapRList = true;

        void OnValidate()
        {
            if (Tilemaps.Count != transform.childCount)
            {
                Refresh();
            }
        }

        void OnTransformChildrenChanged()
        {
            Refresh();
        }

        void Start()
        {
            Refresh();
        }

        void OnDrawGizmosSelected()
        {
            if (SelectedTilemap)
            {
                SelectedTilemap.SendMessage("DoDrawGizmos", SendMessageOptions.DontRequireReceiver);
            }
        }

        public STETilemap FindTilemapByName(string name)
        {
            return Tilemaps.Find(x => x.name == name);
        }

        public void Refresh()
        {
            m_tilemaps = new List<STETilemap>(GetComponentsInChildren<STETilemap>(true));
            if (m_tilemaps.Count > 0 && m_selectedIndex < 0) m_selectedIndex = 0;
            m_selectedIndex = Mathf.Clamp(m_selectedIndex, -1, m_tilemaps.Count);
        }

        public void IterateTilemapWithAction(System.Action<STETilemap> action)
        {
            for (int i = 0; i < m_tilemaps.Count; ++i)
                if (action != null) action(m_tilemaps[i]);
        }

        private Dictionary<int, STETilemap> m_dicTileDefaultTilemap = new Dictionary<int, STETilemap>();
        private Dictionary<int, STETilemap> m_dicBrushDefaultTilemap = new Dictionary<int, STETilemap>();
        [SerializeField]
        private List<int> m_savedTileIds = new List<int>();
        [SerializeField]
        private List<STETilemap> m_savedTileDefaultTilemaps = new List<STETilemap>();
        [SerializeField]
        private List<int> m_savedBrushIds = new List<int>();
        [SerializeField]
        private List<STETilemap> m_savedBrushDefaultTilemaps = new List<STETilemap>();
        private bool m_invalidateDefaultTilemapDictionaries = false;

        public void OnAfterDeserialize()
        {
            if (m_dicTileDefaultTilemap.Count != m_savedTileIds.Count)
            {
                //Debug.Log("Deserialize Default Tilemap Data");
                m_dicTileDefaultTilemap.Clear();
                for (int i = 0, count = m_savedTileIds.Count; i < count; ++i)
                    m_dicTileDefaultTilemap[m_savedTileIds[i]] = m_savedTileDefaultTilemaps[i];
                m_dicBrushDefaultTilemap.Clear();
                for (int i = 0, count = m_savedBrushIds.Count; i < count; ++i)
                    m_dicBrushDefaultTilemap[m_savedBrushIds[i]] = m_savedBrushDefaultTilemaps[i];
            }
        }

        public void OnBeforeSerialize()
        {
            if (m_invalidateDefaultTilemapDictionaries)
            {
                //Debug.Log("Serialize Default Tilemap Data");
                m_invalidateDefaultTilemapDictionaries = false;
                m_savedTileIds.Clear();
                m_savedTileIds.AddRange(m_dicTileDefaultTilemap.Keys);
                m_savedTileDefaultTilemaps.Clear();
                m_savedTileDefaultTilemaps.AddRange(m_dicTileDefaultTilemap.Values);
                m_savedBrushIds.Clear();
                m_savedBrushIds.AddRange(m_dicBrushDefaultTilemap.Keys);
                m_savedBrushDefaultTilemaps.Clear();
                m_savedBrushDefaultTilemaps.AddRange(m_dicBrushDefaultTilemap.Values);
            }
        }

        public void DoOnTileSelected(int tileId)
        {
            SelectDefaultTilemap(tileId, m_dicTileDefaultTilemap);
        }

        public void DoOnBrushSelected(int brushId)
        {
            SelectDefaultTilemap(brushId, m_dicBrushDefaultTilemap);
        }

        public void SetTileDefaultTilemap(int tileId, STETilemap tilemap)
        {
            m_dicTileDefaultTilemap[tileId] = tilemap;
            m_invalidateDefaultTilemapDictionaries = true;
        }

        public void SetBrushDefaultTilemap(int brushId, STETilemap tilemap)
        {
            m_dicBrushDefaultTilemap[brushId] = tilemap;
            m_invalidateDefaultTilemapDictionaries = true;
        }

        public void ClearTileDefaultTilemap(int tileId)
        {
            m_dicTileDefaultTilemap.Remove(tileId);
            m_invalidateDefaultTilemapDictionaries = true;
        }

        public void ClearBrushDefaultTilemap(int brushId)
        {
            m_dicBrushDefaultTilemap.Remove(brushId);
            m_invalidateDefaultTilemapDictionaries = true;
        }

        public void ClearAllDefaultTilemapData()
        {
            m_dicTileDefaultTilemap.Clear();
            m_dicBrushDefaultTilemap.Clear();
            m_invalidateDefaultTilemapDictionaries = true;
        }

        public void SetDefaultTilemapForCurrentSelectedTileOrBrush()
        {
            if (SelectedTilemap && SelectedTilemap.Tileset)
            {
                if (SelectedTilemap.Tileset.SelectedTileId != Tileset.k_TileId_Empty)
                    SetDefaultTilemapForTileOrBrush(SelectedTilemap.Tileset.SelectedTileId, m_dicTileDefaultTilemap);
                else if (SelectedTilemap.Tileset.SelectedBrushId != Tileset.k_BrushId_Default)
                    SetDefaultTilemapForTileOrBrush(SelectedTilemap.Tileset.SelectedBrushId, m_dicBrushDefaultTilemap);
            }
        }

        public STETilemap GetDefaultTilemapForCurrentTileSelection()
        {
            if (SelectedTilemap && SelectedTilemap.Tileset)
            {
                if (SelectedTilemap.Tileset.SelectedTileId != Tileset.k_TileId_Empty)
                    return GetDefaultTilemapForTileOrBrush(SelectedTilemap.Tileset.SelectedTileId, m_dicTileDefaultTilemap);
                else if (SelectedTilemap.Tileset.SelectedBrushId != Tileset.k_BrushId_Default)
                    return GetDefaultTilemapForTileOrBrush(SelectedTilemap.Tileset.SelectedBrushId, m_dicBrushDefaultTilemap);
                else if(SelectedTilemap.Tileset.TileSelection != null && SelectedTilemap.Tileset.TileSelection.selectionData.Count > 0)
                {
                    uint tileData = SelectedTilemap.Tileset.TileSelection.selectionData[0];
                    int brushId = Tileset.GetBrushIdFromTileData(tileData);
                    if (brushId != Tileset.k_BrushId_Default)
                        return GetDefaultTilemapForTileOrBrush(brushId, m_dicBrushDefaultTilemap);
                    else
                    {
                        int tileId = Tileset.GetTileIdFromTileData(tileData);
                        if (tileId != Tileset.k_TileId_Empty)
                            return GetDefaultTilemapForTileOrBrush(tileId, m_dicTileDefaultTilemap);
                    }
                }
            }
            return null;
        }

        public void ClearDefaultTilemapForCurrentSelectedTileOrBrush()
        {
            if (SelectedTilemap && SelectedTilemap.Tileset)
            {
                if (SelectedTilemap.Tileset.SelectedTileId != Tileset.k_TileId_Empty)
                    ClearDefaultTilemapForTileOrBrush(SelectedTilemap.Tileset.SelectedTileId, m_dicTileDefaultTilemap);
                else if (SelectedTilemap.Tileset.SelectedBrushId != Tileset.k_BrushId_Default)
                    ClearDefaultTilemapForTileOrBrush(SelectedTilemap.Tileset.SelectedBrushId, m_dicBrushDefaultTilemap);
            }
        }

        private void ClearDefaultTilemapForTileOrBrush(int id, Dictionary<int, STETilemap> dic)
        {
            dic.Remove(id);
            m_invalidateDefaultTilemapDictionaries = true;
        }

        private STETilemap GetDefaultTilemapForTileOrBrush(int id, Dictionary<int, STETilemap> dic)
        {
            STETilemap defaultTilemap;
            dic.TryGetValue(id, out defaultTilemap);
            return defaultTilemap;
        }

        private void SetDefaultTilemapForTileOrBrush(int id, Dictionary<int, STETilemap> dic)
        {
            dic[id] = SelectedTilemap;
            m_invalidateDefaultTilemapDictionaries = true;
        }

        private bool SelectDefaultTilemap(int id, Dictionary<int, STETilemap> dic)
        {
            STETilemap defaultTilemap;
            if(dic.TryGetValue(id, out defaultTilemap))
            {
                SelectedTilemap = defaultTilemap;
                return true;
            }
            return false;
        }
    }
}
