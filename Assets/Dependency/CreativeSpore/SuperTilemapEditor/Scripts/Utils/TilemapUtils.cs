using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CreativeSpore.SuperTilemapEditor
{

    public class TileData
    {
        public bool flipVertical;
        public bool flipHorizontal;
        public bool rot90;
        public int brushId;
        public int tileId;

        public uint Value { get { return BuildData(); } } // for debugging

        /// <summary>
        /// This is true when tileData has the special value 0xFFFFFFFF, meaning the tile will not be drawn
        /// </summary>
        public bool IsEmpty { get { return brushId == Tileset.k_BrushId_Default && tileId == Tileset.k_TileId_Empty; } }

        public TileData()
        {
            SetData(0x0000FFFF);
        }

        public TileData(uint tileData)
        {
            SetData(tileData);
        }

        /// <summary>
        /// Set data by providing a tileData value ( ex: SetData( Tilemap.GetTileData(12, 35) ) )
        /// </summary>
        /// <param name="tileData"></param>
        public void SetData(uint tileData)
        {
            flipVertical = (tileData & Tileset.k_TileFlag_FlipV) != 0;
            flipHorizontal = (tileData & Tileset.k_TileFlag_FlipH) != 0;
            rot90 = (tileData & Tileset.k_TileFlag_Rot90) != 0;
            brushId = tileData != Tileset.k_TileData_Empty ? (int)((tileData & Tileset.k_TileDataMask_BrushId) >> 16) : 0;
            tileId = (int)(tileData & Tileset.k_TileDataMask_TileId);
        }

        /// <summary>
        /// Build the tile data using current parameters
        /// </summary>
        /// <returns></returns>
        public uint BuildData()
        {
            if( IsEmpty )
            {
                return Tileset.k_TileData_Empty;
            }
            uint tileData = 0;
            if(flipVertical) tileData |= Tileset.k_TileFlag_FlipV;
            if (flipHorizontal) tileData |= Tileset.k_TileFlag_FlipH;
            if (rot90) tileData |= Tileset.k_TileFlag_Rot90;
            tileData |= ( (uint)brushId << 16 ) & Tileset.k_TileDataMask_BrushId;
            tileData |= (uint)tileId & Tileset.k_TileDataMask_TileId;
            return tileData;
        }
    }

    public static class TilemapUtils
    {
        public static Material FindDefaultSpriteMaterial()
        {
#if UNITY_EDITOR && (UNITY_5_4 || UNITY_5_5_OR_NEWER)
            return UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat"); //fix: Unity 5.4.0f3 is not finding the material using Resources
#else
            return Resources.GetBuiltinResource<Material>("Sprites-Default.mat");
#endif
        }

        /// <summary>
        /// Get the world position for the center of a given grid cell position.
        /// </summary>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        static public Vector3 GetGridWorldPos( STETilemap tilemap, int gridX, int gridY)
        {
            return tilemap.transform.TransformPoint(new Vector2((gridX + .5f) * tilemap.CellSize.x, (gridY + .5f) * tilemap.CellSize.y));
        }

        static public Vector3 GetGridWorldPos(int gridX, int gridY, Vector2 cellSize)
        {
            return new Vector2((gridX + .5f) * cellSize.x, (gridY + .5f) * cellSize.y);
        }

        /// <summary>
        /// Get the local position of the center of a tile given the grid position
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="gridX"></param>
        /// <param name="gridY"></param>
        /// <returns></returns>
        static public Vector2 GetTileCenterPosition(STETilemap tilemap, int gridX, int gridY)
        {
            return new Vector2((gridX + .5f) * tilemap.CellSize.x, (gridY + .5f) * tilemap.CellSize.y);
        }

        /// <summary>
        /// Get the local position of the center of a tile given the local tilemap position
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="locPosition"></param>
        /// <returns></returns>
        static public Vector2 GetTileCenterPosition(STETilemap tilemap, Vector2 locPosition)
        {
            int gridX = GetGridX(tilemap, locPosition);
            int gridY = GetGridY(tilemap, locPosition);
            return GetTileCenterPosition(tilemap, gridX, gridY);
        }

        /// <summary>
        /// Gets the grid X position for a given tilemap and local position. To convert from world to local position use tilemap.transform.InverseTransformPoint(worldPosition).
        /// Avoid using positions multiple of cellSize like 0.32f if cellSize = 0.16f because due float imprecisions the return value could be wrong.
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="locPosition"></param>
        /// <returns></returns>
        static public int GetGridX( STETilemap tilemap, Vector2 locPosition)
        {
            return BrushUtil.GetGridX(locPosition, tilemap.CellSize);
        }

        /// <summary>
        /// Gets the grid X and Y position for a given tilemap and local tilemap position and return a vector2 with the result.
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="locPosition"></param>
        /// <returns></returns>
        static public Vector2 GetGridPosition(STETilemap tilemap, Vector2 locPosition)
        {
            return new Vector2(GetGridX(tilemap, locPosition), GetGridY(tilemap, locPosition));
        }

        /// <summary>
        /// Gets the grid Y position for a given tilemap and local position. To convert from world to local position use tilemap.transform.InverseTransformPoint(worldPosition).
        /// Avoid using positions multiple of cellSize like 0.32f if cellSize = 0.16f because due float imprecisions the return value could be wrong.
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="locPosition"></param>
        /// <returns></returns>
        static public int GetGridY(STETilemap tilemap, Vector2 locPosition)
        {
            return BrushUtil.GetGridY(locPosition, tilemap.CellSize);
        }

        /// <summary>
        /// Gets the grid X position for a given tilemap and camera where the mouse is over.
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        static public int GetMouseGridX(STETilemap tilemap, Camera camera)
        {            
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(-tilemap.transform.forward, tilemap.transform.position);
            float dist;
            plane.Raycast(ray, out dist);
            return GetGridX(tilemap, tilemap.transform.InverseTransformPoint(ray.GetPoint(dist)));
        }

        /// <summary>
        /// /// Gets the grid X position for a given tilemap and camera where the mouse is over.
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        static public int GetMouseGridY(STETilemap tilemap, Camera camera)
        {
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(-tilemap.transform.forward, tilemap.transform.position);
            float dist;
            plane.Raycast(ray, out dist);
            return GetGridY(tilemap, tilemap.transform.InverseTransformPoint(ray.GetPoint(dist)));
        }

        /// <summary>
        /// Get the parameter container from tileData if tileData contains a tile with parameters or Null in other case
        /// </summary>
        static public ParameterContainer GetParamsFromTileData(STETilemap tilemap, uint tileData)
        {
            return GetParamsFromTileData(tilemap.Tileset, tileData);
        }

        /// <summary>
        /// Get the parameter container from tileData if tileData contains a tile with parameters or Null in other case
        /// </summary>
        static public ParameterContainer GetParamsFromTileData(Tileset tileset, uint tileData)
        {
            int brushId = Tileset.GetBrushIdFromTileData(tileData);
            TilesetBrush brush = tileset.FindBrush(brushId);
            if(brush)
            {
                return brush.Params;
            }
            else
            {
                int tileId = Tileset.GetTileIdFromTileData(tileData);
                Tile tile = tileset.GetTile(tileId);
                if(tile != null)
                {
                    return tile.paramContainer;
                }
            }
            return null;
        }

        /// <summary>
        /// Translates a screen position into a tilemap local 2D position.
        /// </summary>
        /// <param name="screenPos"></param>
        /// <param name="tilemap"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Vector2 ScreenToLocalPosition(Vector2 screenPos, STETilemap tilemap, Camera camera)
        {
            Ray ray = camera.ScreenPointToRay(screenPos);
            Plane plane = new Plane(-tilemap.transform.forward, tilemap.transform.position);
            float dist;
            plane.Raycast(ray, out dist);
            return tilemap.transform.InverseTransformPoint(ray.GetPoint(dist));
        }

        /// <summary>
        /// Translates a screen position into a world 2D position over the XY plane.
        /// </summary>
        /// <param name="screenPos"></param>
        /// <param name="camera"></param>
        /// <returns></returns>
        public static Vector2 ScreenToWorldPosition(Vector2 screenPos, Camera camera)
        {
            Ray ray = camera.ScreenPointToRay(screenPos);
            Plane plane = new Plane(-Vector3.forward, Vector3.zero);
            float dist;
            plane.Raycast(ray, out dist);
            return ray.GetPoint(dist);
        }

        /// <summary>
        /// Iterate through all the tilemap cells and calls an action for each cell
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="action"></param>
        static public void IterateTilemapWithAction( STETilemap tilemap, System.Action<STETilemap, int, int> action )
        {
            if (tilemap)
            for(int gy = tilemap.MinGridY; gy <= tilemap.MaxGridY; ++gy)
                for(int gx = tilemap.MinGridX; gx <= tilemap.MaxGridX; ++gx)
                    if (action != null) action(tilemap, gx, gy);
        }

        /// <summary>
        /// Iterate through all the tilemap cells and calls an action for each cell.
        /// Ex:
        /// void EraseTilesFromTilemap(Tilemap tilemap)
        /// {
        ///    IterateTilemapWithAction(tilemap, EraseTilesAction);
        /// }
        /// void EraseTilesAction(Tilemap tilemap, int gx, int gy)
        /// {
        ///    tilemap.Erase(gx, gy);
        /// }
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="action"></param>
        static public void IterateTilemapWithAction(STETilemap tilemap, System.Action<STETilemap, int, int, uint> action)
        {
            if (tilemap)
                for (int gy = tilemap.MinGridY; gy <= tilemap.MaxGridY; ++gy)
                    for (int gx = tilemap.MinGridX; gx <= tilemap.MaxGridX; ++gx)
                        if (action != null) action(tilemap, gx, gy, tilemap.GetTileData(gx, gy));
        }

        /// <summary>
        /// Checks if a rect overlaps any tile with colliders
        /// </summary>
        /// <returns></returns>
        static public bool OverlapRect(STETilemap tilemap, Rect rect)
        {
            int gridX0 = GetGridX(tilemap, rect.min);
            int gridY0 = GetGridY(tilemap, rect.min);
            int gridX1 = GetGridX(tilemap, rect.max);
            int gridY1 = GetGridY(tilemap, rect.max);
            for(int x = gridX0; x <= gridX1; ++x)
            {
                for(int y = gridY0; y <= gridY1; ++y)
                {
                    Tile tile = tilemap.GetTile(x, y);
                    if (tile != null && tile.collData.type != eTileCollider.None)
                        return true;
                }
            }
            return false;
        }

        public static Camera GetCurrentPreviewCamera() { return s_previewCamera; }
        private static Camera s_previewCamera = null;

        //ref: http://answers.unity3d.com/questions/1193700/what-is-the-proper-way-to-draw-previews-for-custom.html
        /// <summary>
        /// Creates a Texture2D with a preview of a tilemap or tilemapGroup. In case you use a tilemapGroup, the tilemap will be used as reference for the PixelsPerUnits.
        /// </summary>
        /// <param name="tilemap"></param>
        /// <param name="tilemapGroup"></param>
        /// <returns></returns>
        public static Texture2D CreateTilemapGroupPreviewTexture(STETilemap tilemap, TilemapGroup tilemapGroup = null)
        {
            float pixelToUnits = tilemap.Tileset.TilePxSize.x / tilemap.CellSize.x;
            Bounds tilemapGroupBounds = tilemap.MapBounds;
            if (tilemapGroup)
                tilemapGroup.IterateTilemapWithAction((STETilemap tmap) => tilemapGroupBounds.Encapsulate(tmap.MapBounds));
            Vector2 size = tilemapGroupBounds.size * pixelToUnits;

            Camera previewCamera = Camera.main ? Camera.main : Object.FindObjectOfType<Camera>();
            previewCamera = GameObject.Instantiate(previewCamera);
            s_previewCamera = previewCamera;
            RenderTexture rendTextr = new RenderTexture((int)size.x, (int)size.y, 32, RenderTextureFormat.ARGB32);
            rendTextr.Create();
            previewCamera.transform.position = tilemap.transform.TransformPoint(new Vector3(tilemapGroupBounds.center.x, tilemapGroupBounds.center.y, -10));

            RenderTexture savedActiveRT = RenderTexture.active;
            RenderTexture savedCamTargetTexture = previewCamera.targetTexture;
            RenderTexture.active = rendTextr;
            previewCamera.targetTexture = rendTextr;
            previewCamera.orthographicSize = (previewCamera.pixelRect.height) / (2f * pixelToUnits);
            previewCamera.Render();
            Texture2D outputTexture = new Texture2D((int)size.x, (int)size.y, TextureFormat.ARGB32, false);
            outputTexture.ReadPixels(new Rect(0, 0, (int)size.x, (int)size.y), 0, 0);
            outputTexture.Apply();
            previewCamera.targetTexture = savedCamTargetTexture;
            RenderTexture.active = savedActiveRT;

            Object.DestroyImmediate(rendTextr);
            Object.DestroyImmediate(previewCamera.gameObject);
            s_previewCamera = null;

            return outputTexture;
        }
        
        private static Dictionary<string, Sprite> s_spriteCache = new Dictionary<string, Sprite>();
        /// <summary>
        /// Returns a sprite for the tileId in the tileset and using the pixelsPerUnit specified (or the tileset pixels per units if the values is <= 0)
        /// </summary>
        public static Sprite GetOrCreateTileSprite(Tileset tileset, int tileId, float pixelsPerUnit = 0)
        {
            Sprite sprite = null;
            if(pixelsPerUnit <= 0)
                pixelsPerUnit = tileset.PixelsPerUnit;
            Tile tile = tileset.GetTile(tileId);
            if (tile != null)
            {
                Vector2 atlasSize = new Vector2(tileset.AtlasTexture.width, tileset.AtlasTexture.height);
                Rect spriteUV = new Rect(Vector2.Scale(tile.uv.position, atlasSize), Vector2.Scale(tile.uv.size, atlasSize));
                string spriteName = tileset.name + "_" + tileId + "_" + pixelsPerUnit;
                if (!s_spriteCache.TryGetValue(spriteName, out sprite) || !sprite)
                {
                    sprite = Sprite.Create(tileset.AtlasTexture, spriteUV, new Vector2(.5f, .5f), pixelsPerUnit);
                    sprite.name = spriteName;
                    s_spriteCache[spriteName] = sprite;
                }
            }
            return sprite;
        }
    }
}
