using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.SuperTilemapEditor
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
#if UNITY_2018_3_OR_NEWER
    [ExecuteAlways]
#else
    [ExecuteInEditMode] //fix ShouldRunBehaviour warning when using OnTilePrefabCreation
#endif
    public class TileObjMesh : MonoBehaviour
    {

        [SerializeField]
        protected STETilemap m_parentTilemap;
        [SerializeField]
        protected MeshRenderer m_meshRenderer;
        [SerializeField]
        protected MeshFilter m_meshFilter;

        void OnValidate()
        {
            Start();
        }

        void Start()
        {
            m_meshRenderer = GetComponent<MeshRenderer>();
            m_meshFilter = GetComponent<MeshFilter>();
            if (!m_parentTilemap) m_parentTilemap = GetComponentInParent<STETilemap>();
        }

#if UNITY_EDITOR
        void Reset()
        {
            Start();
            m_meshRenderer.material = TilemapUtils.FindDefaultSpriteMaterial();
            if (!m_meshFilter.sharedMesh) m_meshFilter.sharedMesh = new Mesh();
            m_meshFilter.sharedMesh.name = "Quad";
        }
#endif

        private MaterialPropertyBlock m_matPropBlock;
        void UpdateMaterialPropertyBlock()
        {            
            if (m_matPropBlock == null)
                m_matPropBlock = new MaterialPropertyBlock();
            m_meshRenderer.GetPropertyBlock(m_matPropBlock);
            m_matPropBlock.SetColor("_Color", m_parentTilemap.TintColor);
            if (m_parentTilemap.Tileset && m_parentTilemap.Tileset.AtlasTexture != null)
                m_matPropBlock.SetTexture("_MainTex", m_parentTilemap.Tileset.AtlasTexture);
            m_meshRenderer.SetPropertyBlock(m_matPropBlock);
        }

        protected virtual void OnWillRenderObject()
        {
            if(m_parentTilemap)
            {
                UpdateMaterialPropertyBlock();
            }
        }

        public bool SetRenderTile(STETilemap tilemap, uint tileData)
        {
            m_parentTilemap = tilemap;
            Tile tile = tilemap.Tileset.GetTile(Tileset.GetTileIdFromTileData(tileData));
            if (tile != null)
            {
                m_meshRenderer.material = tilemap.Material;
                m_meshRenderer.sortingLayerID = tilemap.SortingLayerID;
                m_meshRenderer.sortingOrder = tilemap.OrderInLayer;
                Vector2 cellSizeDiv2 = tilemap.CellSize / 2f;
                Vector3[] vertices = new Vector3[4]
                {
                    new Vector3(-cellSizeDiv2.x, -cellSizeDiv2.y, 0),
                    new Vector3(cellSizeDiv2.x, -cellSizeDiv2.y, 0),
                    new Vector3(-cellSizeDiv2.x, cellSizeDiv2.y, 0),
                    new Vector3(cellSizeDiv2.x, cellSizeDiv2.y, 0),
                };
                int[] triangles = new int[] { 3, 0, 2, 0, 3, 1 };
                Vector2[] uvs = new Vector2[]
                {
                    new Vector2(tile.uv.xMin, tile.uv.yMin),
                    new Vector2(tile.uv.xMax, tile.uv.yMin),
                    new Vector2(tile.uv.xMin, tile.uv.yMax),
                    new Vector2(tile.uv.xMax, tile.uv.yMax),
                };

                if (!m_meshFilter.sharedMesh) m_meshFilter.sharedMesh = new Mesh();
                m_meshFilter.sharedMesh.name = "Quad";
                Mesh mesh = m_meshFilter.sharedMesh;
                mesh.Clear();
                mesh.vertices = vertices;
                mesh.triangles = triangles;
                mesh.uv = uvs;
                mesh.RecalculateNormals();
                return true;
            }
            return false;
        }

        protected virtual void OnTilePrefabCreation(TilemapChunk.OnTilePrefabCreationData data)
        {
            SetRenderTile(data.ParentTilemap, data.ParentTilemap.GetTileData(data.GridX, data.GridY));
        }        
    }
}
