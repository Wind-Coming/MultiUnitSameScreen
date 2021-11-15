
// Enable a cache dictionary to improve the access to tilechunks, but it keeps the max distance of a tilechunk to 2^16 tilechunks of distance from origin
// Fair enough unless you have a quantum computer
#define ENABLE_TILECHUNK_CACHE_DIC

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace CreativeSpore.SuperTilemapEditor
{

    public enum eColliderType
    {
        None,
        _2D,
        _3D
    };

    public enum e2DColliderType
    {
        EdgeCollider2D,
        PolygonCollider2D
    };

    [System.Flags]
    public enum eTileFlags
    {
        None = 0,
        Updated = 1,
        Rot90 = 2,
        FlipV = 4,
        FlipH = 8,
    }

    public enum eBlendMode
    {
        None = -1,
        AlphaBlending = 0,
        Additive,
        Subtractive,
        Multiply,
        Divide,
    }

    public enum eTileColorPaintMode
    {
        Tile,
        Vertex,
    }

    [AddComponentMenu("SuperTilemapEditor/STETilemap", 10)]
    [DisallowMultipleComponent]
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode] //NOTE: this is needed so OnDestroy is called and there is no memory leaks
#endif
    public class STETilemap : MonoBehaviour
    {
        /// <summary>
        /// In the worst case scenario, each tile subdivided in 4 sub tiles, the maximum value should be <= 62.
        /// The mesh vertices have a limit of 65535 62(width)*62(height)*4(subtiles)*4(vertices) = 61504
        /// Warning! changing this value will break the tilemaps made so far. Change this before creating them.
        /// For performance, you can use a value of 8 to enable dynamic batching (chunks will have less than 300 vertices allowing that)
        /// </summary>
        public const int k_chunkSize = 60; 
        public const string k_UndoOpName = "Paint Op. ";        

        /// <summary>
        /// Disable the instantiation of prefabs attached to tiles. Usefull when creating procedural maps and you want
        /// to wait until the final map is done before instantiate.
        /// Remember to set this to true and call Refresh(false, false, true, false) for each tilemap to instantiate the prefabs
        /// </summary>
        public static bool DisableTilePrefabCreation = false;

#region Public Events
        public delegate void OnMeshUpdatedDelegate(STETilemap source);
        public OnMeshUpdatedDelegate OnMeshUpdated;
        public delegate void OnTileChangedDelegate(STETilemap tilemap, int gridX, int gridY, uint tileData);
        public OnTileChangedDelegate OnTileChanged;
#endregion

#region Public Properties
        public Tileset Tileset
        {
            get { return m_tileset; }
            set
            {
                bool hasChanged = m_tileset != value;
                m_tileset = value;
                if (hasChanged)
                {
                    if (Tileset != null && CellSize == default(Vector2))
                    {
                        CellSize = m_tileset.TilePxSize / m_tileset.PixelsPerUnit;
                    }
                }
            }
        }

        public Material Material
        {
            get
            {
                if (m_material == null)
                {
                    m_material = TilemapUtils.FindDefaultSpriteMaterial();
                }
                return m_material;
            }

            set
            {
                if (value != null && m_material != value)
                {
                    m_material = value;
                    Refresh();
                }
            }
        }


        /// <summary>
        /// Color applied to the material before rendering
        /// </summary>
        public Color TintColor 
        { 
            get { return m_tintColor; }
            set { m_tintColor = value;} 
        }

        /// <summary>
        /// Show the tilemap grid
        /// </summary>
        public bool ShowGrid = true;        

        //NOTE: the inner padding fix the zoom imperfection, but as long as zoom is bigger, the bigger this value has to be        
        /// <summary>
        /// The size, in pixels, the tile UV will be stretched. Use this to fix pixel precision artifacts when tiles have no padding border in the atlas.
        /// </summary>
        public float InnerPadding = 0f;
        /// <summary>
        /// The depth size of the collider. You need to call Refresh(false, true) after changing this value to refresh the collider.
        /// </summary>
        public float ColliderDepth = 0.1f;
        /// <summary>
        /// Set the colliders for this tilemap. You need to call the Refresh method to update all the tilemap chunks after changing this parameter.
        /// </summary>
        public eColliderType ColliderType = eColliderType.None;
        /// <summary>
        /// The type of collider used when ColliderType is eColliderType._2D
        /// </summary>
        public e2DColliderType Collider2DType = e2DColliderType.EdgeCollider2D;
        /// <summary>
        /// Sets the isTrigger property of the collider. You need call Refresh to update the colliders after changing it.
        /// </summary>
        public bool IsTrigger { get { return m_isTrigger; } set { m_isTrigger = value; } }
        /// <summary>
        /// The PhysicsMaterial that is applied to this tilemap colliders.
        /// </summary>
        public PhysicMaterial PhysicMaterial { get { return m_physicMaterial; } set { m_physicMaterial = value; } }
        /// <summary>
        /// The PhysicsMaterial2D that is applied to this tilemap colliders.
        /// </summary>
        public PhysicsMaterial2D PhysicMaterial2D { get { return m_physicMaterial2D; } set { m_physicMaterial2D = value; } }
        /// <summary>
        /// Show the collider normals
        /// </summary>
        public bool ShowColliderNormals = true;
        /// <summary>
        /// The size of the cell containing the tiles. You should call Refresh() after changing this value to apply the effect.
        /// </summary>
        public Vector2 CellSize { get { return m_cellSize; } set { m_cellSize = value; } }
        /// <summary>
        /// Returns the size of the map in units
        /// </summary>
        public Bounds MapBounds { get { return m_mapBounds; } }
        /// <summary>
        /// Lock painting tiles only inside the map bounds
        /// </summary>
        public bool AllowPaintingOutOfBounds { get { return m_allowPaintingOutOfBounds; } set { m_allowPaintingOutOfBounds = value; } }
        /// <summary>
        /// Enables auto trim, making the tilemap to shrink to the visible are when calling UpdateMesh
        /// </summary>
        public bool AutoTrim { get { return m_autoShrink; } set { m_autoShrink = value; } }        
        /// <summary>
        /// If true, undo will be registered when painting to be able to undo any change. But activating this would become a performance killer in big maps.
        /// For these cases, disabling the undo would be a good option to improve the performance while painting
        /// </summary>      
        public bool EnableUndoWhilePainting { get { return m_enableUndoWhilePainting; } set { m_enableUndoWhilePainting = value; } }
        /// <summary>
        /// Returns the minimum horizontal grid position of the tilemap area
        /// </summary>
        public int MinGridX { get { return m_minGridX; } /*set { m_minGridX = Mathf.Min(0, value); }*/ }
        /// <summary>
        /// Returns the minimum vertical grid position of the tilemap area
        /// </summary>
        public int MinGridY { get { return m_minGridY; } /*set { m_minGridY = Mathf.Min(0, value); }*/ }
        /// <summary>
        /// Returns the maximum horizontal grid position of the tilemap area
        /// </summary>
        public int MaxGridX { get { return m_maxGridX; } /*set { m_maxGridX = Mathf.Max(0, value); }*/ }
        /// <summary>
        /// Returns the maximum vertical grid position of the tilemap area
        /// </summary>
        public int MaxGridY { get { return m_maxGridY; } /*set { m_maxGridY = Mathf.Max(0, value); }*/ }
        /// <summary>
        /// Returns the horizontal size of the grid in tiles
        /// </summary>
        public int GridWidth { get { return m_maxGridX - m_minGridX + 1; } }
        /// <summary>
        /// Returns the vertical size of the grid in tiles
        /// </summary>
        public int GridHeight { get { return m_maxGridY - m_minGridY + 1; } }
        /// <summary>
        /// Returns the parent tilemap group the tilemap is children of
        /// </summary>
        public TilemapGroup ParentTilemapGroup { get { return m_parentTilemapGroup; } }
        /// <summary>
        /// Sets/Gets the parallax factor applied to the tilemap when it's rendered by a camera
        /// </summary>
        public Vector2 ParallaxFactor { get { return m_parallaxFactor; } set { m_parallaxFactor = value; } }
        public enum eCollDisplayMode
        {
            Selected,
            ParentSelected,
            Always
        }
        public eCollDisplayMode ColliderDisplayMode = eCollDisplayMode.Selected;
        public ChunkRendererPropertiesData ChunkRendererProperties { get { return m_chunkRendererProperties; } }
        public bool IsUndoEnabled = false;
        public enum eSortOrder
        {
            BottomLeft, //default
            //BottomRight,
            //TopLeft,
            TopRight //reverse triangles (working only locally to a tilemap chunk)
        }
        [Tooltip("Sort order for all tiles rendered by the Tilemap Chunk")]
        public eSortOrder SortOrder = eSortOrder.BottomLeft;

        public int SortingLayerID
        {
            get { return m_sortingLayer; }
            set
            {
                int prevValue = m_sortingLayer;
                m_sortingLayer = value;
#if UNITY_EDITOR
                m_sortingLayerName = EditorUtils.GetSortingLayerNameById(m_sortingLayer);
#endif
                if (m_sortingLayer != prevValue) 
                    RefreshChunksSortingAttributes();
            }
        }

        public string SortingLayerName
        {
            get { return m_sortingLayerName; }
            set
            {
                m_sortingLayerName = value;
#if UNITY_EDITOR
                m_sortingLayer = EditorUtils.GetSortingLayerIdByName(m_sortingLayerName);                
#endif
                RefreshChunksSortingAttributes();
            }
        }        

        public int OrderInLayer
        {
            get { return m_orderInLayer; }
            set
            {
                int prevValue = m_orderInLayer;
                m_orderInLayer = (value << 16) >> 16; // convert from int32 to int16 keeping sign
                if (m_orderInLayer != prevValue)
                    RefreshChunksSortingAttributes();
            }
        }

        public void RefreshChunksSortingAttributes()
        {
            var valueIter = m_dicChunkCache.Values.GetEnumerator(); 
            while (valueIter.MoveNext())
            {
                TilemapChunk chunk = valueIter.Current;
                if (chunk)
                {
                    chunk.SortingLayerID = m_sortingLayer;
                    chunk.OrderInLayer = m_orderInLayer;
                }
            }
        }

        public bool IsVisible
        {
            get
            {
                return m_isVisible;
            }
            set
            {
                bool prevValue = m_isVisible;
                m_isVisible = value;
                if (m_isVisible != prevValue)
                {
                    UpdateMesh();
                }
            }
        }

        /// <summary>
        /// Enables the PixelSnap is the material has the PixelSnap property
        /// </summary>
        public bool PixelSnap { get { return m_pixelSnap; } set { m_pixelSnap = value; } }

#endregion

#region Private Fields

        [SerializeField, SortingLayer]
        private int m_sortingLayer = 0;
        [SerializeField]
        private string m_sortingLayerName = "Default";
        [SerializeField]
        private int m_orderInLayer = 0;
        [SerializeField]
        private Material m_material;
        [SerializeField]
        private Color m_tintColor;
        [SerializeField]
        private bool m_pixelSnap;
        [SerializeField]
        private bool m_isVisible = true;
        [SerializeField]
        private bool m_allowPaintingOutOfBounds = false;
        [SerializeField]
        private bool m_autoShrink = false;
        [SerializeField, Tooltip("Set to false when painting on big maps to improve performance.")]
        private bool m_enableUndoWhilePainting = true;
        [SerializeField]
        private bool m_isTrigger = false;
        [SerializeField]
        private PhysicMaterial m_physicMaterial;
        [SerializeField]
        private PhysicsMaterial2D m_physicMaterial2D;

        [SerializeField]
        Vector2 m_cellSize = Vector2.one;
        [SerializeField]
        private Bounds m_mapBounds;
        [SerializeField]
        private Tileset m_tileset;
        [SerializeField]
        private int m_minGridX = 0;
        [SerializeField]
        private int m_minGridY = 0;
        [SerializeField]
        private int m_maxGridX = 29;
        [SerializeField]
        private int m_maxGridY = 29;
        [SerializeField]
        private TilemapGroup m_parentTilemapGroup;
        [SerializeField]
        private Vector2 m_parallaxFactor = Vector2.one;        
        [System.Serializable]
        public class ChunkRendererPropertiesData
        {
            public ShadowCastingMode castShadows = ShadowCastingMode.Off;
            public bool receiveShadows = false;
#if UNITY_5_4_OR_NEWER
            public LightProbeUsage useLightProbes = LightProbeUsage.Off;
#else
            public bool useLightProbes = false;
#endif
            public ReflectionProbeUsage reflectionProbeUsage = ReflectionProbeUsage.Off;
            public Transform anchorOverride;
        }
        [SerializeField]
        private ChunkRendererPropertiesData m_chunkRendererProperties = new ChunkRendererPropertiesData();


        private bool m_markForUpdateMesh = false;
#if UNITY_EDITOR
        private bool m_markSceneDirtyOnNextUpdateMesh = false;
#endif
#endregion

#region Monobehaviour Methods

        void Awake()
        {
            BuildTilechunkDictionary();
            var valueIter = m_dicChunkCache.Values.GetEnumerator();
            while (valueIter.MoveNext())
            {
                TilemapChunk chunk = valueIter.Current;
                chunk.gameObject.hideFlags |= HideFlags.HideInHierarchy;
            }

            // NOTE: in game Reset is not called
            if (!m_material)
            {
                m_material = TilemapUtils.FindDefaultSpriteMaterial();
                m_tintColor = Color.white;
            }
        }

        private bool m_applyContactsEmptyFix = false;
        void Update()
        {
            if (Application.isPlaying && m_applyContactsEmptyFix)
            {
                m_applyContactsEmptyFix = false;
                var valueIter = m_dicChunkCache.Values.GetEnumerator(); 
                while (valueIter.MoveNext())
                {
                    TilemapChunk chunk = valueIter.Current;
                    if (chunk)
                    {
                        chunk.ApplyContactsEmptyFix();
                    }
                }
            }

            if (m_markForUpdateMesh)
            {
                m_markForUpdateMesh = false;
                m_applyContactsEmptyFix = ColliderType == eColliderType._3D;
                UpdateMeshImmediate();
            }

        }

        void OnEnable()
        {
            Camera.onPreCull += _OnPreCull;
            Camera.onPostRender += _OnPostRender;
        }

        void OnDisable()
        {
            Camera.onPreCull -= _OnPreCull;
            Camera.onPostRender -= _OnPostRender;
        }

        bool m_preRenderPosSet = false;
        Vector3 m_preRenderPos;
        //NOTE: _OnPreCull could be called more than one time, without calling _OnPostRender. For example by selecting a Canvas Text object clicking in the Scene
        private void _OnPreCull(Camera cam)
        {
            if (!m_preRenderPosSet)
            {
                m_preRenderPosSet = true;
                m_preRenderPos = transform.localPosition;
            }
            //if(!cam.name.Equals("SceneCamera")) TODO: add an option to disable parallax in SceveView
            //NOTE: tilemap.transform.position shouldn't be modified. When the parent has a rotation, it leads to float precision, changing slowly the position.
            transform.localPosition = m_preRenderPos + (Vector3)(Vector2.Scale(cam.transform.position, (Vector2.one - m_parallaxFactor)));
        }

        private void _OnPostRender(Camera cam)
        {
            transform.localPosition = m_preRenderPos;
            m_preRenderPosSet = false;
        }

        // NOTE: OnDestroy is not called in editor without [ExecuteInEditMode]
        void OnDestroy()
        {
#if UNITY_EDITOR
            if (m_material && m_material.hideFlags == HideFlags.DontSave && !AssetDatabase.Contains(m_material))
            {
                //avoid memory leak
                DestroyImmediate(m_material);
            }
#endif
            //Destroy all the tilechunks
            var valueIter = m_dicChunkCache.Values.GetEnumerator();
            while (valueIter.MoveNext())
            {
                TilemapChunk chunk = valueIter.Current;
                if (chunk)
                {
                    DestroyImmediate(chunk.gameObject);
                }
            }
        }        

        void OnValidate()
        {
            BuildTilechunkDictionary();
            m_parentTilemapGroup = GetComponentInParent<TilemapGroup>();
#if UNITY_EDITOR
            // fix: for tilemaps created with version 1.3.5 or below
            if(m_tintColor == default(Color))
            {
                Debug.Log("Fixing tilemap made with version below 1.3.5: " + name);
                if (m_material)
                {
                    m_tintColor = m_material.color; //take the color from the material
                    m_pixelSnap = Material.HasProperty("PixelSnap") && Material.IsKeywordEnabled("PIXELSNAP_ON");
                    bool fixMaterial = string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(m_material));
                    if (fixMaterial)
                        m_material = TilemapUtils.FindDefaultSpriteMaterial();
                }
            }
            //---
#endif
            PixelSnap = m_pixelSnap;
        }

        void OnTransformParentChanged()
        {
            m_parentTilemapGroup = GetComponentInParent<TilemapGroup>();
        }

        void Reset()
        {
            ClearMap();
            m_material = TilemapUtils.FindDefaultSpriteMaterial();
            m_tintColor = Color.white;
        }

#if UNITY_EDITOR

        public void OnDrawGizmosSelected()
        {
            if (ColliderDisplayMode == eCollDisplayMode.Selected)
            {
                if (Selection.activeGameObject == this.gameObject)
                {
                    DoDrawGizmos();
                }
            }
        }

        public void OnDrawGizmos()
        {
            if (ColliderDisplayMode == eCollDisplayMode.Always ||
                (ColliderDisplayMode == eCollDisplayMode.ParentSelected && Selection.activeGameObject && this.gameObject.transform.IsChildOf(Selection.activeGameObject.transform))
                )
            {
                DoDrawGizmos();
            }
        }

        void DoDrawGizmos()
        {
            Vector3 savedPos = transform.localPosition;
            transform.localPosition += (Vector3)(Vector2.Scale(Camera.current.transform.position, (Vector2.one - m_parallaxFactor))); //apply parallax
            Rect rBound = new Rect(MapBounds.min, MapBounds.size);
            HandlesEx.DrawRectWithOutline(transform, rBound, new Color(0, 0, 0, 0), new Color(1, 1, 1, 0.5f));

            if ( ShowGrid )
            {
                Plane tilemapPlane = new Plane(this.transform.forward, this.transform.position);
                float distCamToTilemap = 0f;
                Ray rayCamToPlane = new Ray(Camera.current.transform.position, Camera.current.transform.forward);
                tilemapPlane.Raycast(rayCamToPlane, out distCamToTilemap);
                if (HandleUtility.GetHandleSize(rayCamToPlane.GetPoint(distCamToTilemap)) <= 13f)
                {

                    // draw tile cells
                    Gizmos.color = EditorGlobalSettings.TilemapGridColor;

                    // Horizontal lines
                    for (float i = 1; i < GridWidth; i++)
                    {
                        Gizmos.DrawLine(
                            this.transform.TransformPoint(new Vector3(MapBounds.min.x + i * CellSize.x, MapBounds.min.y)),
                            this.transform.TransformPoint(new Vector3(MapBounds.min.x + i * CellSize.x, MapBounds.max.y))
                            );
                    }

                    // Vertical lines
                    for (float i = 1; i < GridHeight; i++)
                    {
                        Gizmos.DrawLine(
                            this.transform.TransformPoint(new Vector3(MapBounds.min.x, MapBounds.min.y + i * CellSize.y, 0)),
                            this.transform.TransformPoint(new Vector3(MapBounds.max.x, MapBounds.min.y + i * CellSize.y, 0))
                            );
                    }
                }                
                Gizmos.color = Color.white;
            }

            //Draw Chunk Colliders
            var valueIter = m_dicChunkCache.Values.GetEnumerator();
            while (valueIter.MoveNext())
            {
                TilemapChunk chunk = valueIter.Current;
                if (chunk)
                {
                    int chunkX = Mathf.RoundToInt(chunk.GridPosX / k_chunkSize);
                    int chunkY = Mathf.RoundToInt(chunk.GridPosY / k_chunkSize);
                    rBound = new Rect(chunkX * k_chunkSize * CellSize.x, chunkY * k_chunkSize * CellSize.y, k_chunkSize * CellSize.x, k_chunkSize * CellSize.y);
                    HandlesEx.DrawRectWithOutline(transform, rBound, new Color(0, 0, 0, 0), new Color(1, 0, 0, 0.2f));
                    chunk.DrawColliders();
                }
            }
            //
            transform.localPosition = savedPos; // restore position
        }
#endif
#endregion

#region Public Methods

        /// <summary>
        /// Force an update of all tilechunks. This is called after changing sensitive data like CellSize, Collider depth, etc
        /// </summary>
        /// <param name="refreshMesh"></param>
        /// <param name="refreshMeshCollider"></param>
        public void Refresh(bool refreshMesh = true, bool refreshMeshCollider = true, bool refreshTileObjects = false, bool invalidateBrushes = false)
        {
            BuildTilechunkDictionary();
            var valueIter = m_dicChunkCache.Values.GetEnumerator();
            while (valueIter.MoveNext())
            {
                TilemapChunk chunk = valueIter.Current;
                if (chunk)
                {
                    if (refreshMesh) chunk.InvalidateMesh();
                    if (refreshMeshCollider) chunk.InvalidateMeshCollider();
                    if (refreshTileObjects) chunk.RefreshTileObjects();
                    if (invalidateBrushes) chunk.InvalidateBrushes();
                }
            }
            UpdateMesh();
        }

        /// <summary>
        /// Call this methods after changing any render property to update all created tilechunks.
        /// </summary>
        public void UpdateChunkRendererProperties()
        {
            var valueIter = m_dicChunkCache.Values.GetEnumerator();
            while (valueIter.MoveNext())
            {
                TilemapChunk chunk = valueIter.Current;
                if (chunk)
                {
                    chunk.UpdateRendererProperties();
                }
            }
        }

        /// <summary>
        /// Shrink the map bounds to the minimum area enclosing all visible tiles
        /// </summary>
        public void Trim()
        {
            Bounds mapBounds = new Bounds();
            Vector2 halfCellSize = CellSize / 2f; // used to avoid precission errors
            m_maxGridX = m_maxGridY = m_minGridX = m_minGridY = 0;
            var valueIter = m_dicChunkCache.Values.GetEnumerator();
            while (valueIter.MoveNext())
            {
                TilemapChunk chunk = valueIter.Current;
                if (chunk)
                {
                    Bounds tilechunkBounds = chunk.GetBounds();
                    Vector2 min = transform.InverseTransformPoint(chunk.transform.TransformPoint(tilechunkBounds.min));
                    Vector2 max = transform.InverseTransformPoint(chunk.transform.TransformPoint(tilechunkBounds.max));
                    mapBounds.Encapsulate(min + halfCellSize);
                    mapBounds.Encapsulate(max - halfCellSize);
                }
            }
            m_minGridX = BrushUtil.GetGridX(mapBounds.min, CellSize);
            m_minGridY = BrushUtil.GetGridY(mapBounds.min, CellSize);
            m_maxGridX = BrushUtil.GetGridX(mapBounds.max, CellSize);
            m_maxGridY = BrushUtil.GetGridY(mapBounds.max, CellSize);
            RecalculateMapBounds();
        }

        /// <summary>
        /// Clear the tilemap from all tilechunks and also remove all objects in the hierarchy
        /// </summary>
        [ContextMenu("Clear Map")]        
        public void ClearMap(bool keepBounds = false)
        {
            if (!keepBounds)
            {
                m_mapBounds = new Bounds();
                m_maxGridX = m_maxGridY;
            }
            while (transform.childCount > 0)
            {
#if UNITY_EDITOR
                if (IsUndoEnabled)
                {
                    Undo.DestroyObjectImmediate(transform.GetChild(0).gameObject);
                }
                else
                {
                    DestroyImmediate(transform.GetChild(0).gameObject);
                }
#else
                DestroyImmediate(transform.GetChild(0).gameObject);
#endif
            }
        }

        /// <summary>
        /// Remove color channel from all tilemap chunks
        /// </summary>        
        public void RemoveColorChannel()
        {
            var valueIter = m_dicChunkCache.Values.GetEnumerator();
            while (valueIter.MoveNext())
            {
                TilemapChunk chunk = valueIter.Current;
                if (chunk)
                {
                    chunk.RemoveColorChannel();
                }
            }
        }

        /// <summary>
        /// Clear color channel from all tilemap chunks
        /// </summary>
        public void ClearColorChannel(Color clearColor)
        {
            var valueIter = m_dicChunkCache.Values.GetEnumerator();
            while (valueIter.MoveNext())
            {
                TilemapChunk chunk = valueIter.Current;
                if (chunk)
                {
                    chunk.ClearColorChannel(clearColor);
                }
            }
        }

        /// <summary>
        /// Set a tile color using a tilemap local position
        /// </summary>
        /// <param name="vLocalPos"></param>
        /// <param name="color"></param>
        public void SetTileColor(Vector2 vLocalPos, Color32 color, eBlendMode blendMode = eBlendMode.AlphaBlending)
        {
            SetTileColor(vLocalPos, new TileColor32(color), blendMode);
        }

        /// <summary>
        /// Set a tile color in the grid position
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <param name="color"></param>
        public void SetTileColor(int gridX, int gridY, Color32 color, eBlendMode blendMode = eBlendMode.AlphaBlending)
        {
            SetTileColor(gridX, gridY, new TileColor32(color), blendMode);
        }

        /// <summary>
        /// Set a tile color using a tilemap local position
        /// </summary>
        /// <param name="vLocalPos"></param>
        /// <param name="c0">Bottom left corner</param>
        /// <param name="c1">Bottom right corner</param>
        /// <param name="c2">Top left corner</param>
        /// <param name="c3">Top right corner</param>
        public void SetTileColor(Vector2 vLocalPos, TileColor32 tileColor, eBlendMode blendMode = eBlendMode.AlphaBlending)
        {
            int gridX = BrushUtil.GetGridX(vLocalPos, CellSize);
            int gridY = BrushUtil.GetGridY(vLocalPos, CellSize);
            SetTileColor(gridX, gridY, tileColor, blendMode);
        }

        /// <summary>
        /// Set a tile color in the grid position
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <param name="c0">Bottom left corner</param>
        /// <param name="c1">Bottom right corner</param>
        /// <param name="c2">Top left corner</param>
        /// <param name="c3">Top right corner</param>
        public void SetTileColor(int gridX, int gridY, TileColor32 tileColor, eBlendMode blendMode = eBlendMode.AlphaBlending)
        {
            TilemapChunk chunk = GetOrCreateTileChunk(gridX, gridY, true);
            int chunkGridX = (gridX < 0 ? -gridX - 1 : gridX) % k_chunkSize;
            int chunkGridY = (gridY < 0 ? -gridY - 1 : gridY) % k_chunkSize;
            if (gridX < 0) chunkGridX = k_chunkSize - 1 - chunkGridX;
            if (gridY < 0) chunkGridY = k_chunkSize - 1 - chunkGridY;
            if (m_allowPaintingOutOfBounds || (gridX >= m_minGridX && gridX <= m_maxGridX && gridY >= m_minGridY && gridY <= m_maxGridY))
            {
                chunk.SetTileColor(chunkGridX, chunkGridY, tileColor, blendMode);
            }
        }

        /// <summary>
        /// Gets the tile colors set for the tile at local position
        /// </summary>
        /// <param name="vLocalPos"></param>
        /// <returns></returns>
        public TileColor32 GetTileColor(Vector2 vLocalPos)
        {
            int gridX = BrushUtil.GetGridX(vLocalPos, CellSize);
            int gridY = BrushUtil.GetGridY(vLocalPos, CellSize);
            return GetTileColor(gridX, gridY);
        }

        /// <summary>
        /// Gets the tile colors for the tile at grid position
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        public TileColor32 GetTileColor(int gridX, int gridY)
        {
            TilemapChunk chunk = GetOrCreateTileChunk(gridX, gridY, true);
            int chunkGridX = (gridX < 0 ? -gridX - 1 : gridX) % k_chunkSize;
            int chunkGridY = (gridY < 0 ? -gridY - 1 : gridY) % k_chunkSize;
            if (gridX < 0) chunkGridX = k_chunkSize - 1 - chunkGridX;
            if (gridY < 0) chunkGridY = k_chunkSize - 1 - chunkGridY;
            if (m_allowPaintingOutOfBounds || (gridX >= m_minGridX && gridX <= m_maxGridX && gridY >= m_minGridY && gridY <= m_maxGridY))
            {
                return chunk.GetTileColor(chunkGridX, chunkGridY);
            }
            return default(TileColor32);
        }

        /// <summary>
        /// Set a tile data using a tilemap local position
        /// </summary>
        /// <param name="vLocalPos"></param>
        /// <param name="tileData"></param>
        public void SetTileData(Vector2 vLocalPos, uint tileData)
        {
            int gridX = BrushUtil.GetGridX(vLocalPos, CellSize);
            int gridY = BrushUtil.GetGridY(vLocalPos, CellSize);
            SetTileData(gridX, gridY, tileData);
        }

        /// <summary>
        /// Set a tile data in the grid position
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <param name="tileData"></param>
        public void SetTileData(int gridX, int gridY, uint tileData)
        {
            TilemapChunk chunk = GetOrCreateTileChunk(gridX, gridY, true);
            int chunkGridX = (gridX < 0 ? -gridX - 1 : gridX) % k_chunkSize;
            int chunkGridY = (gridY < 0 ? -gridY - 1 : gridY) % k_chunkSize;
            if (gridX < 0) chunkGridX = k_chunkSize - 1 - chunkGridX;
            if (gridY < 0) chunkGridY = k_chunkSize - 1 - chunkGridY;
            if (m_allowPaintingOutOfBounds || (gridX >= m_minGridX && gridX <= m_maxGridX && gridY >= m_minGridY && gridY <= m_maxGridY))
            {
                chunk.SetTileData(chunkGridX, chunkGridY, tileData);
                if (OnTileChanged != null) OnTileChanged(this, gridX, gridY, tileData);
#if UNITY_EDITOR
                m_markSceneDirtyOnNextUpdateMesh = true;
#endif
                // Update map bounds
                //if (tileData != Tileset.k_TileData_Empty) // commented to update the brush bounds when copying empty tiles
                {
                    m_minGridX = Mathf.Min(m_minGridX, gridX);
                    m_maxGridX = Mathf.Max(m_maxGridX, gridX);
                    m_minGridY = Mathf.Min(m_minGridY, gridY);
                    m_maxGridY = Mathf.Max(m_maxGridY, gridY);
                }
                //--
            }
        }

        /// <summary>
        /// Reset the flag Updated for the tile at local position. 
        /// This makes a brush at that position to call the Refresh method when UpdateMesh is performed.
        /// </summary>
        /// <param name="vLocalPos"></param>
        public void RefreshTile(Vector2 vLocalPos)
        {
            int gridX = BrushUtil.GetGridX(vLocalPos, CellSize);
            int gridY = BrushUtil.GetGridY(vLocalPos, CellSize);
            RefreshTile(gridX, gridY);
        }

        /// <summary>
        /// Reset the flag Updated for the tile at grid position. 
        /// This makes a brush at that position to call the Refresh method when UpdateMesh is performed.
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        public void RefreshTile(int gridX, int gridY)
        {
            TilemapChunk chunk = GetOrCreateTileChunk(gridX, gridY, true);
            int chunkGridX = (gridX < 0 ? -gridX - 1 : gridX) % k_chunkSize;
            int chunkGridY = (gridY < 0 ? -gridY - 1 : gridY) % k_chunkSize;
            if (gridX < 0) chunkGridX = k_chunkSize - 1 - chunkGridX;
            if (gridY < 0) chunkGridY = k_chunkSize - 1 - chunkGridY;
            chunk.RefreshTile(chunkGridX, chunkGridY);
        }

        /// <summary>
        /// Set a tile data at the local position
        /// </summary>
        /// <param name="vLocalPos"></param>
        /// <param name="tileId"></param>
        /// <param name="brushId"></param>
        /// <param name="flags"></param>
        public void SetTile(Vector2 vLocalPos, int tileId, int brushId = Tileset.k_BrushId_Default, eTileFlags flags = eTileFlags.None)
        {
            int gridX = BrushUtil.GetGridX(vLocalPos, CellSize);
            int gridY = BrushUtil.GetGridY(vLocalPos, CellSize);
            SetTile(gridX, gridY, tileId, brushId, flags);
        }

        /// <summary>
        /// Set a tile data at the grid position
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <param name="tileId"></param>
        /// <param name="brushId"></param>
        /// <param name="flags"></param>
        public void SetTile(int gridX, int gridY, int tileId, int brushId = Tileset.k_BrushId_Default, eTileFlags flags = eTileFlags.None)
        {
            uint tileData = ((uint)flags << 28) | (((uint)brushId << 16) & Tileset.k_TileDataMask_BrushId) | ((uint)tileId & Tileset.k_TileDataMask_TileId);
            SetTileData(gridX, gridY, tileData);
        }

        /// <summary>
        /// Erase the tile placed at the local position
        /// </summary>
        /// <param name="vLocalPos"></param>
        public void Erase(Vector2 vLocalPos)
        {
            SetTileData( vLocalPos, Tileset.k_TileData_Empty );
        }

        /// <summary>
        /// Erase the tile placed at the grid position
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        public void Erase(int gridX, int gridY)
        {
            SetTileData(gridX, gridY, Tileset.k_TileData_Empty);
        }

        /// <summary>
        /// Returns the tile data at the local position
        /// </summary>
        /// <param name="vLocalPos"></param>
        /// <returns></returns>
        public uint GetTileData(Vector2 vLocalPos)
        {
            int gridX = BrushUtil.GetGridX(vLocalPos, CellSize);
            int gridY = BrushUtil.GetGridY(vLocalPos, CellSize);
            return GetTileData(gridX, gridY);
        }

        /// <summary>
        /// Returns the tile data at the grid position
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        public uint GetTileData(int gridX, int gridY)
        {
            TilemapChunk chunk = GetOrCreateTileChunk(gridX, gridY);
            if (chunk == null)
            {
                return Tileset.k_TileData_Empty;
            }
            else
            {
                int chunkGridX = (gridX < 0 ? -gridX - 1 : gridX) % k_chunkSize;
                int chunkGridY = (gridY < 0 ? -gridY - 1 : gridY) % k_chunkSize;
                if (gridX < 0) chunkGridX = k_chunkSize - 1 - chunkGridX;
                if (gridY < 0) chunkGridY = k_chunkSize - 1 - chunkGridY;
                return chunk.GetTileData(chunkGridX, chunkGridY);
            }
        }

        /// <summary>
        /// Returns the tile at the local position
        /// </summary>
        /// <param name="vLocalPos"></param>
        /// <returns></returns>
        public Tile GetTile(Vector2 vLocalPos)
        {
            int gridX = BrushUtil.GetGridX(vLocalPos, CellSize);
            int gridY = BrushUtil.GetGridY(vLocalPos, CellSize);
            return GetTile(gridX, gridY);
        }

        /// <summary>
        /// Returns the tile at the grid position
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        public Tile GetTile(int gridX, int gridY)
        {
            uint tileData = GetTileData(gridX, gridY);
            int tileId = Tileset.GetTileIdFromTileData(tileData);
            return Tileset.GetTile(tileId);
        }

        /// <summary>
        /// Returns the brush at the local position
        /// </summary>
        /// <param name="vLocalPos"></param>
        /// <returns></returns>
        public TilesetBrush GetBrush(Vector2 vLocalPos)
        {
            int gridX = BrushUtil.GetGridX(vLocalPos, CellSize);
            int gridY = BrushUtil.GetGridY(vLocalPos, CellSize);
            return GetBrush(gridX, gridY);
        }

        /// <summary>
        /// Returns the brush at the grid position
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        public TilesetBrush GetBrush(int gridX, int gridY)
        {
            uint tileData = GetTileData(gridX, gridY);
            int brushId = Tileset.GetBrushIdFromTileData(tileData);
            return Tileset.FindBrush(brushId);
        }

        /// <summary>
        /// Returns the instances created using the tile prefab property at the local position.
        /// </summary>
        /// <param name="vLocalPos"></param>
        /// <returns></returns>
        public GameObject GetTileObject(Vector2 vLocalPos)
        {
            int gridX = BrushUtil.GetGridX(vLocalPos, CellSize);
            int gridY = BrushUtil.GetGridY(vLocalPos, CellSize);
            return GetTileObject(gridX, gridY);
        }

        /// <summary>
        /// Returns the instances created using the tile prefab property at the grid position.
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        public GameObject GetTileObject(int gridX, int gridY)
        {
            TilemapChunk chunk = GetOrCreateTileChunk(gridX, gridY);
            if (chunk == null)
            {
                return null;
            }
            else
            {
                int chunkGridX = (gridX < 0 ? -gridX - 1 : gridX) % k_chunkSize;
                int chunkGridY = (gridY < 0 ? -gridY - 1 : gridY) % k_chunkSize;
                if (gridX < 0) chunkGridX = k_chunkSize - 1 - chunkGridX;
                if (gridY < 0) chunkGridY = k_chunkSize - 1 - chunkGridY;
                return chunk.GetTileObject(chunkGridX, chunkGridY);
            }
        }

        /// <summary>
        /// Returns true if the local position is inside the tilemap bounds
        /// </summary>
        /// <param name="vLocalPos"></param>
        /// <returns></returns>
        public bool IsPositionInsideTilemap(Vector2 vLocalPos)
        {
            return MapBounds.Contains(vLocalPos);
        }

        /// <summary>
        /// Returns true if the grid position is inside the tilemap bounds
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        public bool IsGridPositionInsideTilemap(int gridX, int gridY)
        {
            return gridX >= m_minGridX && gridX <= m_maxGridX && gridY >= m_minGridY && gridY <= m_maxGridY;
        }

        /// <summary>
        /// Updates the render mesh and mesh collider of all tile chunks. This should be called once after making all modifications to the tilemap with SetTileData.
        /// </summary>
        public void UpdateMesh()
        {
            m_markForUpdateMesh = true;
        }

        static List<TilemapChunk> s_chunkList = new List<TilemapChunk>(50);
        /// <summary>
        /// Update the render mesh and mesh collider of all tile chunks. This should be called once after making all modifications to the tilemap with SetTileData.
        /// </summary>
        public void UpdateMeshImmediate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && m_markSceneDirtyOnNextUpdateMesh)
            {
                m_markSceneDirtyOnNextUpdateMesh = false;
#if UNITY_5_3 || UNITY_5_3_OR_NEWER
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
#else
                EditorApplication.MarkSceneDirty();
#endif
            }
#endif

            RecalculateMapBounds();

            s_chunkList.Clear();
            var valueIter = m_dicChunkCache.Values.GetEnumerator();
            while (valueIter.MoveNext())
            {
                TilemapChunk chunk = valueIter.Current;
                if (chunk)
                {
                    if (!chunk.UpdateMesh())
                    {
#if UNITY_EDITOR
                        if (IsUndoEnabled)
                        {
                            Undo.DestroyObjectImmediate(chunk.gameObject);
                        }
                        else
                        {
                            DestroyImmediate(chunk.gameObject);
                        }
#else
                        DestroyImmediate(chunk.gameObject);
#endif
                    }
                    else
                    {
                        //chunk.UpdateColliderMesh();
                        s_chunkList.Add(chunk);
                    }
                }
            }

            if (m_autoShrink)
                Trim();

            // UpdateColliderMesh is called after calling UpdateMesh of all tilechunks, because UpdateColliderMesh needs the tileId to be updated 
            // ( remember a brush sets neighbours tile id to empty, so UpdateColliderMesh won't be able to know the collider type )
            for (int i = 0; i < s_chunkList.Count; ++i)
            {
                s_chunkList[i].UpdateColliders();
            }

            if (OnMeshUpdated != null) OnMeshUpdated(this);
        }

        /// <summary>
        /// Sets the map limits
        /// </summary>
        /// <param name="minGridX"></param>
        /// <param name="minGridY"></param>
        /// <param name="maxGridX"></param>
        /// <param name="maxGridY"></param>
        public void SetMapBounds(int minGridX, int minGridY, int maxGridX, int maxGridY)
        {
            m_minGridX = Mathf.Min(minGridX, 0);
            m_minGridY = Mathf.Min(minGridY, 0);
            m_maxGridX = Mathf.Max(maxGridX, 0);
            m_maxGridY = Mathf.Max(maxGridY, 0);
            RecalculateMapBounds();
        }

        /// <summary>
        /// Recalculate the bounding volume of the map from the grid limits
        /// </summary>
        public void RecalculateMapBounds()
        {
            // Fix grid limits if necessary
            m_minGridX = Mathf.Min(m_minGridX, 0);
            m_minGridY = Mathf.Min(m_minGridY, 0);
            m_maxGridX = Mathf.Max(m_maxGridX, 0);
            m_maxGridY = Mathf.Max(m_maxGridY, 0);
            
            Vector2 minTilePos = Vector2.Scale(new Vector2(m_minGridX, m_minGridY), CellSize);
            Vector2 maxTilePos = Vector2.Scale(new Vector2(m_maxGridX, m_maxGridY), CellSize);
            Vector3 savedSize = m_mapBounds.size;
            m_mapBounds.min = m_mapBounds.max = Vector2.zero;
            m_mapBounds.Encapsulate(minTilePos);
            m_mapBounds.Encapsulate(minTilePos + CellSize);
            m_mapBounds.Encapsulate(maxTilePos);
            m_mapBounds.Encapsulate(maxTilePos + CellSize);
            if (savedSize != m_mapBounds.size)
            {
                var valueIter = m_dicChunkCache.Values.GetEnumerator();
                while (valueIter.MoveNext())
                {
                    TilemapChunk chunk = valueIter.Current;
                    if (chunk)
                    {
                        chunk.InvalidateBrushes(); //TODO: this is very slow. Find a way to update only the tiles on the edges?
                    }
                }
            }
        }

        /// <summary>
        /// Flip the tilemap vertically
        /// </summary>
        /// <param name="changeFlags"></param>
        public void FlipV(bool changeFlags)
        {
            List<uint> flippedList = new List<uint>(GridWidth * GridHeight);
            for (int gy = MinGridY; gy <= MaxGridY; ++gy)
            {
                for (int gx = MinGridX; gx <= MaxGridX; ++gx)
                {
                    int flippedGy = GridHeight - 1 - gy;
                    flippedList.Add(GetTileData(gx, flippedGy));
                }
            }

            int idx = 0;
            for (int gy = MinGridY; gy <= MaxGridY; ++gy)
            {
                for (int gx = MinGridX; gx <= MaxGridX; ++gx, ++idx)
                {
                    uint flippedTileData = flippedList[idx];
                    if (
                        changeFlags 
                        && (flippedTileData != Tileset.k_TileData_Empty)
                        && (flippedTileData & Tileset.k_TileDataMask_BrushId) == 0 // don't activate flip flags on brushes
                        )
                    {
                        flippedTileData = TilesetBrush.ApplyAndMergeTileFlags(flippedTileData, Tileset.k_TileFlag_FlipV);
                    }
                    SetTileData(gx, gy, flippedTileData);
                }
            }
        }

        /// <summary>
        /// Flip the map horizontally
        /// </summary>
        /// <param name="changeFlags"></param>
        public void FlipH(bool changeFlags)
        {
            List<uint> flippedList = new List<uint>(GridWidth * GridHeight);
            for (int gx = MinGridX; gx <= MaxGridX; ++gx)
            {
                for (int gy = MinGridY; gy <= MaxGridY; ++gy)
                {
                    int flippedGx = GridWidth - 1 - gx;
                    flippedList.Add(GetTileData(flippedGx, gy));
                }
            }

            int idx = 0;
            for (int gx = MinGridX; gx <= MaxGridX; ++gx)
            {
                for (int gy = MinGridY; gy <= MaxGridY; ++gy, ++idx)
                {
                    uint flippedTileData = flippedList[idx];
                    if (
                        changeFlags
                        && (flippedTileData != Tileset.k_TileData_Empty)
                        && (flippedTileData & Tileset.k_TileDataMask_BrushId) == 0 // don't activate flip flags on brushes
                        )
                    {
                        flippedTileData = TilesetBrush.ApplyAndMergeTileFlags(flippedTileData, Tileset.k_TileFlag_FlipH);
                    }
                    SetTileData(gx, gy, flippedTileData);
                }
            }
        }

        /// <summary>
        /// Rotate the map 90 degrees clockwise
        /// </summary>
        /// <param name="changeFlags"></param>
        public void Rot90(bool changeFlags)
        {
            List<uint> flippedList = new List<uint>(GridWidth * GridHeight);
            for (int gy = MinGridY; gy <= MaxGridY; ++gy)
            {
                for (int gx = MinGridX; gx <= MaxGridX; ++gx)
                {
                    flippedList.Add(GetTileData(gx, gy));
                }
            }

            int minGridX = MinGridX;
            int minGridY = MinGridY;
            int maxGridX = MaxGridY;
            int maxGridY = MaxGridX;
            ClearMap();

            int idx = 0;
            for (int gx = minGridX; gx <= maxGridX; ++gx)
            {
                for (int gy = maxGridY; gy >= minGridY; --gy, ++idx)
                {
                    uint flippedTileData = flippedList[idx];
                    if (
                        changeFlags
                        && (flippedTileData != Tileset.k_TileData_Empty)
                        && (flippedTileData & Tileset.k_TileDataMask_BrushId) == 0 // don't activate flip flags on brushes
                        )
                    {
                        flippedTileData = TilesetBrush.ApplyAndMergeTileFlags(flippedTileData, Tileset.k_TileFlag_Rot90);
                    }
                    SetTileData(gx, gy, flippedTileData);
                }
            }
        }

        public bool InvalidateChunkAt(int gridX, int gridY, bool invalidateMesh = true, bool invalidateMeshCollider = true)
        {
            TilemapChunk chunk = GetOrCreateTileChunk(gridX, gridY);
            if (chunk != null)
            {
                chunk.InvalidateMesh();
                chunk.InvalidateMeshCollider();
                return true;
            }
            return false;
        }

#endregion

#region Private Methods      
  
        Dictionary<uint, TilemapChunk> m_dicChunkCache = new Dictionary<uint, TilemapChunk>();
        private TilemapChunk GetOrCreateTileChunk(int gridX, int gridY, bool createIfDoesntExist = false)
        {
            if (m_dicChunkCache.Count == 0 && transform.childCount > 0)
                BuildTilechunkDictionary();

            int chunkX = (gridX < 0 ? (gridX + 1 - k_chunkSize) : gridX) / k_chunkSize;
            int chunkY = (gridY < 0 ? (gridY + 1 - k_chunkSize) : gridY) / k_chunkSize;

            TilemapChunk tilemapChunk = null;

            uint key = (uint)((chunkY << 16) | (chunkX & 0x0000FFFF));
            m_dicChunkCache.TryGetValue(key, out tilemapChunk);

            if (tilemapChunk == null && createIfDoesntExist)
            {
                string chunkName = chunkX + "_" + chunkY;
                GameObject chunkObj = new GameObject(chunkName);
                if (IsUndoEnabled)
                {
#if UNITY_EDITOR
                    Undo.RegisterCreatedObjectUndo(chunkObj, k_UndoOpName + name);
#endif
                }
                tilemapChunk = chunkObj.AddComponent<TilemapChunk>(); //NOTE: this call TilemapChunk.OnEnable before initializing the TilemapChunk. Make all changes after this.
                chunkObj.transform.parent = transform;
                chunkObj.transform.localPosition = new Vector2(chunkX * k_chunkSize * CellSize.x, chunkY * k_chunkSize * CellSize.y);
                chunkObj.transform.localRotation = Quaternion.identity;
                chunkObj.transform.localScale = Vector3.one;
                chunkObj.hideFlags = gameObject.hideFlags | HideFlags.HideInHierarchy; //NOTE: note the flags inheritance. BrushBehaviour object is not saved, so chunks are left orphans unless this inheritance is done
                // Reset is not called after AddComponent while in play
                if (Application.isPlaying)
                {
                    tilemapChunk.Reset();
                }
                tilemapChunk.ParentTilemap = this;
                tilemapChunk.GridPosX = chunkX * k_chunkSize;
                tilemapChunk.GridPosY = chunkY * k_chunkSize;
                tilemapChunk.SetDimensions(k_chunkSize, k_chunkSize);
                tilemapChunk.SetSharedMaterial(Material);
                tilemapChunk.SortingLayerID = m_sortingLayer;
                tilemapChunk.OrderInLayer = m_orderInLayer;
                tilemapChunk.UpdateRendererProperties();

                m_dicChunkCache[key] = tilemapChunk;
            }

            return tilemapChunk;
        }

        private void BuildTilechunkDictionary()
        {
            m_dicChunkCache.Clear();
            for(int i = 0; i < transform.childCount; ++i)
            {
                TilemapChunk chunk = transform.GetChild(i).GetComponent<TilemapChunk>();
                if (chunk)
                {
                    int chunkX = (chunk.GridPosX < 0 ? (chunk.GridPosX + 1 - k_chunkSize) : chunk.GridPosX) / k_chunkSize;
                    int chunkY = (chunk.GridPosY < 0 ? (chunk.GridPosY + 1 - k_chunkSize) : chunk.GridPosY) / k_chunkSize;
                    uint key = (uint)((chunkY << 16) | (chunkX & 0x0000FFFF));
                    m_dicChunkCache[key] = chunk;
                }
            }
        }        

#endregion
    }
}
