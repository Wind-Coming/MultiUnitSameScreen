using UnityEngine;
using System.Collections;

namespace CreativeSpore.SuperTilemapEditor
{
    public static class TilemapVertexPaintUtils
    {
        /// <summary>
        /// Paint the tiles in a tilemap specifing a center and a radius. The color will be multiplied by the intensity curve along the radius.
        /// </summary>
        /// <param name="tilemap">The target tilemap</param>
        /// <param name="center">The center of the circle</param>
        /// <param name="radius">The radius of the circle</param>
        /// <param name="color">The color to be painted</param>
        /// <param name="blendingMode">The blending mode</param>
        /// <param name="vertexPaint">If the color is changed for the whole tile or by vertex</param>
        /// <param name="intensityCurve">The value multiplied to the color along the radius</param>
        public static void VertexPaintCircle(STETilemap tilemap, Vector2 center, float radius, Color color, eBlendMode blendingMode, bool vertexPaint = false, AnimationCurve intensityCurve = null)
        {
            Vector2 minPos = new Vector2(center.x - radius, center.y - radius);
            Vector2 maxPos = new Vector2(center.x + radius, center.y + radius);
            int minGridX = TilemapUtils.GetGridX(tilemap, minPos);
            int minGridY = TilemapUtils.GetGridY(tilemap, minPos);
            int maxGridX = TilemapUtils.GetGridX(tilemap, maxPos);
            int maxGridY = TilemapUtils.GetGridY(tilemap, maxPos);
            Vector2 tileCenter = TilemapUtils.GetTileCenterPosition(tilemap, minPos);
            float minX = tileCenter.x;
            float sqrRadius = radius * radius;
            Color32 color32 = color;
            for (int y = minGridY; y <= maxGridY; ++y, tileCenter.y += tilemap.CellSize.y)
            {
                tileCenter.x = minX;
                for (int x = minGridX; x <= maxGridX; ++x, tileCenter.x += tilemap.CellSize.x)
                {
                    if (vertexPaint)
                    {
                        _PaintTileVerticesByDist(tilemap, x, y, tileCenter, center, radius, color, blendingMode, intensityCurve);
                    }
                    else
                    {
                        float sqrDist = (center - tileCenter).sqrMagnitude;
                        if (sqrDist <= sqrRadius)
                        {
                            if (intensityCurve != null)
                            {
                                float dist = Mathf.Sqrt(sqrDist);
                                color32.a = (byte)(255 * color.a * intensityCurve.Evaluate(1f - dist / radius));
                            }
                            tilemap.SetTileColor(x, y, color32, blendingMode);
                        }
                    }
                }
            }
        }

        private static Vector2[] s_tileVertexPos = new Vector2[4];
        private static Color32[] s_tileVertexColor = new Color32[4];
        private static void _PaintTileVerticesByDist(STETilemap tilemap, int gridX, int gridY, Vector2 tilePos, Vector2 center, float radius, Color color, eBlendMode blendingMode, AnimationCurve intensityCurve)
        {
            Vector2 cellSizeDiv2 = tilemap.CellSize / 2f;
            s_tileVertexPos[0] = new Vector2(tilePos.x - cellSizeDiv2.x, tilePos.y - cellSizeDiv2.y);
            s_tileVertexPos[1] = new Vector2(tilePos.x + cellSizeDiv2.x, tilePos.y - cellSizeDiv2.y);
            s_tileVertexPos[2] = new Vector2(tilePos.x - cellSizeDiv2.x, tilePos.y + cellSizeDiv2.y);
            s_tileVertexPos[3] = new Vector2(tilePos.x + cellSizeDiv2.x, tilePos.y + cellSizeDiv2.y);
            float sqrRadius = radius * radius;
            Color32 color32 = color;
            for (int i = 0; i < 4; ++i)
            {
                float sqrDist = (center - s_tileVertexPos[i]).sqrMagnitude;
                if (sqrDist <= sqrRadius)
                {
                    if (intensityCurve != null)
                    {
                        float dist = Mathf.Sqrt(sqrDist);
                        s_tileVertexColor[i] = new Color32(color32.r, color32.g, color32.b, (byte)(255 * color.a * intensityCurve.Evaluate(1f - dist / radius)));
                    }
                    else
                    {
                        s_tileVertexColor[i] = color32;
                    }
                }
                else
                {
                    s_tileVertexColor[i] = default(Color32);
                }
            }
            tilemap.SetTileColor(gridX, gridY, new TileColor32(s_tileVertexColor[0], s_tileVertexColor[1], s_tileVertexColor[2], s_tileVertexColor[3]), blendingMode);
        }
    }
}