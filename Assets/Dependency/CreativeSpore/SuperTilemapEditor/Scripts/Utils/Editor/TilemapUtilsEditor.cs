using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace CreativeSpore.SuperTilemapEditor
{
    public static class TilemapUtilsEditor
    {        
        [System.Obsolete("use CreateTilemapGroupPreviewTexture instead.")]
        static public Texture2D CreateTexture2DFromTilemap(STETilemap tilemap)
        {
            MakeTextureReadable(tilemap.Tileset.AtlasTexture);
            int tilePxSizeX = (int)tilemap.Tileset.TilePxSize.x;
            int tilePxSizeY = (int)tilemap.Tileset.TilePxSize.y;
            Texture2D output = new Texture2D(tilemap.GridWidth * tilePxSizeX, tilemap.GridHeight * tilePxSizeY, TextureFormat.ARGB32, false);
            output.filterMode = FilterMode.Point;
            output.SetPixels32(new Color32[output.width * output.height]);
            output.Apply();
            System.Action<STETilemap, int, int, uint> action = (STETilemap source, int gridX, int gridY, uint tileData) =>
            {
                gridX -= source.MinGridX;
                gridY -= source.MinGridY;
                Tile tile = tilemap.Tileset.GetTile(Tileset.GetTileIdFromTileData(tileData));
                if (tile != null)
                {
                    Texture2D atlasTexture = tilemap.Tileset.AtlasTexture;
                    int tx = Mathf.RoundToInt(tile.uv.x * atlasTexture.width);
                    int ty = Mathf.RoundToInt(tile.uv.y * atlasTexture.height);
                    int tw = tilePxSizeX;
                    int th = tilePxSizeY;
                    Sprite prefabSprite = null;
                    if (tile.prefabData.prefab)
                    {
                        SpriteRenderer spriteRenderer = tile.prefabData.prefab.GetComponent<SpriteRenderer>();
                        if (spriteRenderer && spriteRenderer.sprite)
                        {
                            prefabSprite = spriteRenderer.sprite;
                            MakeTextureReadable(spriteRenderer.sprite.texture);
                            atlasTexture = spriteRenderer.sprite.texture;
                            tx = Mathf.RoundToInt(spriteRenderer.sprite.textureRect.x);
                            ty = Mathf.RoundToInt(spriteRenderer.sprite.textureRect.y);
                            tw = Mathf.RoundToInt(spriteRenderer.sprite.textureRect.width);
                            th = Mathf.RoundToInt(spriteRenderer.sprite.textureRect.height);
                        }
                    }
                    bool flipH = (tileData & Tileset.k_TileFlag_FlipH) != 0;
                    bool flipV = (tileData & Tileset.k_TileFlag_FlipV) != 0;
                    bool rot90 = (tileData & Tileset.k_TileFlag_Rot90) != 0;
                    Color[] srcTileColors = atlasTexture.GetPixels(tx, ty, tw, th);
                    if (flipH)
                    {
                        Color[] tempArr = new Color[0];
                        for (int i = 0; i < th; ++i)
                            tempArr = tempArr.Concat(srcTileColors.Skip(tw * i).Take(tw).Reverse()).ToArray();
                        srcTileColors = tempArr;
                    }
                    if (flipV)
                    {
                        Color[] tempArr = new Color[0];
                        for (int i = th - 1; i >= 0; --i)
                            tempArr = tempArr.Concat(srcTileColors.Skip(tw * i).Take(tw)).ToArray();
                        srcTileColors = tempArr;
                    }
                    if (rot90)
                    {
                        Color[] tempArr = new Color[tw * th];
                        for (int x = tw - 1, i = 0; x >= 0; --x)
                            for (int y = 0; y < th; ++y, ++i)
                                tempArr[i] = srcTileColors[y * tw + x];
                        srcTileColors = tempArr;
                        int temp = tw;
                        tw = th;
                        th = temp;
                    }
                    if (prefabSprite)
                    {
                        Vector2 tileSize = prefabSprite.textureRect.size;
                        Vector2 pivot = prefabSprite.pivot - prefabSprite.textureRectOffset;
                        if (flipV) pivot.y = -pivot.y + prefabSprite.textureRect.height;
                        if (flipH) pivot.x = -pivot.x + prefabSprite.textureRect.width;
                        if (rot90)
                        {
                            pivot = new Vector2(pivot.y, tileSize.x - pivot.x);
                            tileSize.x = prefabSprite.textureRect.size.y;
                            tileSize.y = prefabSprite.textureRect.size.x;
                        }
                        Vector2 offset = pivot - tilemap.Tileset.TilePxSize / 2;// sprite.pivot + sprite.textureRect.position - sprite.textureRectOffset;            
                        BlitPixels(output, gridX * tilePxSizeX - Mathf.RoundToInt(offset.x), gridY * tilePxSizeY - Mathf.RoundToInt(offset.y), Mathf.RoundToInt(tileSize.x), Mathf.RoundToInt(tileSize.y), srcTileColors);
                    }
                    else
                    {
                        output.SetPixels(gridX * tilePxSizeX, gridY * tilePxSizeY, tw, th, srcTileColors);
                    }
                }
            };
            TilemapUtils.IterateTilemapWithAction(tilemap, action);
            output.Apply();
            return output;
        }

        // From: http://answers.unity3d.com/questions/24929/assetdatabase-replacing-an-asset-but-leaving-refer.html
        public static T CreateOrReplaceAsset<T>(T asset, string path) where T : UnityEngine.Object
        {
            T existingAsset = (T)AssetDatabase.LoadAssetAtPath(path, typeof(T));

            if (existingAsset == null)
            {
                AssetDatabase.CreateAsset(asset, path);
                existingAsset = asset;
            }
            else
            {
                EditorUtility.CopySerialized(asset, existingAsset);
            }

            return existingAsset;
        }

        public static void MakeTextureReadable(Texture2D texture2D)
        {
            if (texture2D != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(texture2D);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
                    if (textureImporter != null)
                    {
                        if (!textureImporter.isReadable)
                        {
                            textureImporter.isReadable = true;
                            AssetDatabase.ImportAsset(assetPath);
                        }
                    }
                }
            }
        }

        public static void BlitPixels(Texture2D texture, int x, int y, int blockWidth, int blockHeight, Color[] blitColors)
        {
            Debug.Assert(x >= 0 && y >= 0 && (x + blockWidth) <= texture.width && (y + blockHeight) <= texture.height, "BlitPixels destination rectangle is out of bounds!");
            Color[] dstColors = texture.GetPixels(x, y, blockWidth, blockHeight);
            for (int i = 0; i < dstColors.Length; ++i)
            {
                Color srcColor = blitColors[i];
                Color dstColor = dstColors[i];
                float outA = srcColor.a + dstColor.a * (1f - srcColor.a);
                if (outA > 0f)
                {
                    dstColor = (srcColor * srcColor.a + dstColor * (1f - srcColor.a)) / outA;
                }
                dstColor.a = outA;
                dstColors[i] = dstColor;
            }
            texture.SetPixels(x, y, blockWidth, blockHeight, dstColors);
        }
    }
}
