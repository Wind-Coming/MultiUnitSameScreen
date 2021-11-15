using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CreativeSpore.SuperTilemapEditor
{

    public static class TilemapDrawingUtils
    {
        public const float k_timeToAbortFloodFill = 5f;

        struct Point
        {
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }
            public int X;
            public int Y;
        }

        public static void FloodFill(STETilemap tilemap, Vector2 vLocalPos, uint[,] tileData)
        {
            int gridX = BrushUtil.GetGridX(vLocalPos, tilemap.CellSize);
            int gridY = BrushUtil.GetGridY(vLocalPos, tilemap.CellSize);
            FloodFill(tilemap, gridX, gridY, tileData);
        }

        //https://social.msdn.microsoft.com/Forums/en-US/9d926a16-0051-4ca3-b77c-8095fb489ae2/flood-fill-c?forum=csharplanguage
        public static void FloodFill(STETilemap tilemap, int gridX, int gridY, uint[,] tileData, bool randomize = false)
        {
            float timeStamp;
            timeStamp = Time.realtimeSinceStartup;
            //float callTimeStamp = timeStamp;

            int patternW = tileData.GetLength(0);
            int patternH = tileData.GetLength(1);
            LinkedList<Point> check = new LinkedList<Point>();
            uint floodFrom = tilemap.GetTileData(gridX, gridY);
            int dataIdx0 = randomize ? Random.Range(0, patternW) : (gridX % patternW + patternW) % patternW;
            int dataIdx1 = randomize ? Random.Range(0, patternH) : (gridY % patternH + patternH) % patternH;
            tilemap.SetTileData(gridX, gridY, tileData[dataIdx0, dataIdx1]);
            bool isBrush = Tileset.GetBrushIdFromTileData(floodFrom) != 0;
            //Debug.Log(" Flood Fill Starts +++++++++++++++ ");
            if (
                (patternW > 0 && patternH > 0) &&
                isBrush? 
                Tileset.GetBrushIdFromTileData(floodFrom) != Tileset.GetBrushIdFromTileData(tileData[0, 0])
                :
                floodFrom != tileData[0, 0]
            )
            {
                check.AddLast(new Point(gridX, gridY));
                while (check.Count > 0)
                {
                    Point cur = check.First.Value;
                    check.RemoveFirst();

                    foreach (Point off in new Point[] {
                        new Point(0, -1), new Point(0, 1), 
                        new Point(-1, 0), new Point(1, 0)})
                    {
                        Point next = new Point(cur.X + off.X, cur.Y + off.Y);
                        uint nextTileData = tilemap.GetTileData(next.X, next.Y);
                        if (
                            next.X >= tilemap.MinGridX && next.X <= tilemap.MaxGridX
                            && next.Y >= tilemap.MinGridY && next.Y <= tilemap.MaxGridY
                        )
                        {
                            if(
                                isBrush? 
                                Tileset.GetBrushIdFromTileData(floodFrom) == Tileset.GetBrushIdFromTileData(nextTileData)
                                :
                                floodFrom == nextTileData
                            )
                            {
                                check.AddLast(next);
                                dataIdx0 = randomize ? Random.Range(0, patternW) : (next.X % patternW + patternW) % patternW;
                                dataIdx1 = randomize ? Random.Range(0, patternH) : (next.Y % patternH + patternH) % patternH;
                                tilemap.SetTileData(next.X, next.Y, tileData[dataIdx0, dataIdx1]);
                            }
                        }
                    }

                    float timePast = Time.realtimeSinceStartup - timeStamp;
                    if (timePast > k_timeToAbortFloodFill)
                    {
#if UNITY_EDITOR
                        int result = UnityEditor.EditorUtility.DisplayDialogComplex("FloodFill is taking too much time", "Do you want to continue for another " + k_timeToAbortFloodFill + " seconds?", "Wait", "Cancel", "Wait and Don't ask again");
                        if (result == 0)
                        {
                            timeStamp = Time.realtimeSinceStartup;
                        }
                        else if (result == 1)
                        {
                            break;
                        }
                        else if (result == 2)
                        {
                            timeStamp = float.MaxValue;
                        }
#else
                    check.Clear();
#endif
                    }
                }
            }

            //Debug.Log("FloodFill Time " + (int)((Time.realtimeSinceStartup - callTimeStamp) * 1000) + "ms");
        }

        public static void FloodFillPreview(STETilemap tilemap, Vector2 vLocalPos, uint tileData, List<Vector2> outFilledPoints, uint maxPoints = uint.MaxValue)
        {
            int gridX = BrushUtil.GetGridX(vLocalPos, tilemap.CellSize);
            int gridY = BrushUtil.GetGridY(vLocalPos, tilemap.CellSize);
            FloodFillPreview(tilemap, gridX, gridY, tileData, outFilledPoints, maxPoints);
        }

        //Note: this is doing the same as FloodFill but not saving data in the tilemap, only saving the filled points and returning a list
        public static void FloodFillPreview(STETilemap tilemap, int gridX, int gridY, uint tileData, List<Vector2> outFilledPoints, uint maxPoints = uint.MaxValue)
        {
            if (
                gridX >= tilemap.MinGridX && gridX <= tilemap.MaxGridX
                && gridY >= tilemap.MinGridY && gridY <= tilemap.MaxGridY
            )
            {
                bool[] filledPoints = new bool[tilemap.GridWidth * tilemap.GridHeight];
                LinkedList<Point> check = new LinkedList<Point>();
                uint floodFrom = tilemap.GetTileData(gridX, gridY);
                outFilledPoints.Add(Vector2.Scale(new Vector2(gridX, gridY), tilemap.CellSize));
                filledPoints[(gridY - tilemap.MinGridY) * tilemap.GridWidth + gridX - tilemap.MinGridX] = true;
                bool isBrush = Tileset.GetBrushIdFromTileData(floodFrom) != 0;
                if (
                    isBrush ?
                    Tileset.GetBrushIdFromTileData(floodFrom) != Tileset.GetBrushIdFromTileData(tileData)
                    :
                    floodFrom != tileData
                )
                {
                    check.AddLast(new Point(gridX, gridY));
                    while (check.Count > 0)
                    {
                        Point cur = check.First.Value;
                        check.RemoveFirst();

                        foreach (Point off in new Point[] {
                        new Point(0, -1), new Point(0, 1), 
                        new Point(-1, 0), new Point(1, 0)})
                        {
                            Point next = new Point(cur.X + off.X, cur.Y + off.Y);

                            if (
                                next.X >= tilemap.MinGridX && next.X <= tilemap.MaxGridX
                                && next.Y >= tilemap.MinGridY && next.Y <= tilemap.MaxGridY
                            )
                            {
                                if (filledPoints[(next.Y - tilemap.MinGridY) * tilemap.GridWidth + next.X - tilemap.MinGridX]) continue; // skip already filled points
                                uint nextTileData = tilemap.GetTileData(next.X, next.Y);
                                if (
                                    isBrush ?
                                    Tileset.GetBrushIdFromTileData(floodFrom) == Tileset.GetBrushIdFromTileData(nextTileData)
                                    :
                                    floodFrom == nextTileData
                                )
                                {
                                    check.AddLast(next);
                                    filledPoints[(next.Y - tilemap.MinGridY) * tilemap.GridWidth + next.X - tilemap.MinGridX] = true;
                                    outFilledPoints.Add(Vector2.Scale(new Vector2(next.X, next.Y), tilemap.CellSize));
                                    if (outFilledPoints.Count >= maxPoints)
                                    {
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }

        public delegate bool PlotFunction(int x, int y);

        public static void DrawDot(STETilemap tilemap, Vector2 locPos, uint[,] tileData, bool randomize = false)
        {
            int x0 = TilemapUtils.GetGridX(tilemap, locPos);
            int y0 = TilemapUtils.GetGridY(tilemap, locPos);
            DrawDot(tilemap, x0, y0, tileData, randomize);
        }

        public static void DrawDot(STETilemap tilemap, int x0, int y0, uint[,] tileData, bool randomize = false)
        {
            int w = tileData.GetLength(0);
            int h = tileData.GetLength(1);
            int dataIdx0 = randomize ? Random.Range(0, w) : (x0 % w + w) % w;
            int dataIdx1 = randomize ? Random.Range(0, h) : (y0 % h + h) % h;
            tilemap.SetTileData(x0, y0, tileData[dataIdx0, dataIdx1]);
        }

        //ref: http://www.roguebasin.com/index.php?title=Bresenham%27s_Line_Algorithm
        public static void Line(int x0, int y0, int x1, int y1, PlotFunction plot)
        {
            bool steep = Mathf.Abs(y1 - y0) > Mathf.Abs(x1 - x0);
            if (steep) { Swap<int>(ref x0, ref y0); Swap<int>(ref x1, ref y1); }
            if (x0 > x1) { Swap<int>(ref x0, ref x1); Swap<int>(ref y0, ref y1); }
            int dX = (x1 - x0), dY = Mathf.Abs(y1 - y0), err = (dX / 2), ystep = (y0 < y1 ? 1 : -1), y = y0;

            for (int x = x0; x <= x1; ++x)
            {
                if (!(steep ? plot(y, x) : plot(x, y))) return;
                err = err - dY;
                if (err < 0) { y += ystep; err += dX; }
            }
        }

        public static void DrawLine(STETilemap tilemap, Vector2 locPosA, Vector2 locPosB, uint[,] tileData, bool randomize = false)
        {
            int x0 = TilemapUtils.GetGridX(tilemap, locPosA);
            int y0 = TilemapUtils.GetGridY(tilemap, locPosA);
            int x1 = TilemapUtils.GetGridX(tilemap, locPosB);
            int y1 = TilemapUtils.GetGridY(tilemap, locPosB);
            DrawLine(tilemap, x0, y0, x1, y1, tileData, randomize);
        }

        public static void DrawLine(STETilemap tilemap, int x0, int y0, int x1, int y1, uint[,] tileData, bool randomize = false)
        {
            int w = tileData.GetLength(0);
            int h = tileData.GetLength(1);
            TilemapDrawingUtils.Line(x0, y0, x1, y1,
                (x, y) =>
                {
                    int dataIdx0 = randomize ? Random.Range(0, w) : (x % w + w) % w;
                    int dataIdx1 = randomize ? Random.Range(0, h) : (y % h + h) % h;
                    tilemap.SetTileData(x, y, tileData[dataIdx0, dataIdx1]);
                    return true;
                }
                );
        }

        public static void DrawLineMirrored(STETilemap tilemap, Vector2 locPosA, Vector2 locPosB, uint[,] tileData, bool randomize = false)
        {
            int x0 = TilemapUtils.GetGridX(tilemap, locPosA);
            int y0 = TilemapUtils.GetGridY(tilemap, locPosA);
            int x1 = TilemapUtils.GetGridX(tilemap, locPosB);
            int y1 = TilemapUtils.GetGridY(tilemap, locPosB);
            DrawLineMirrored(tilemap, x0, y0, x1, y1, tileData, randomize);
        }

        public static void DrawLineMirrored(STETilemap tilemap, int x0, int y0, int x1, int y1, uint[,] tileData, bool randomize = false)
        {
            int w = tileData.GetLength(0);
            int h = tileData.GetLength(1);

            TilemapDrawingUtils.Line(x0, y0, x1, y1,
                (x, y) =>
                {
                    int dataIdx0 = randomize ? Random.Range(0, w) : (x % w + w) % w;
                    int dataIdx1 = randomize ? Random.Range(0, h) : (y % h + h) % h;
                    tilemap.SetTileData(x, y, tileData[dataIdx0, dataIdx1]);
                    dataIdx0 = randomize ? Random.Range(0, w) : ((x0 - x) % w + w) % w;
                    dataIdx1 = randomize ? Random.Range(0, h) : ((y0 - y) % h + h) % h;
                    tilemap.SetTileData((x0<<1) - x, (y0<<1) - y, tileData[dataIdx0, dataIdx1]);
                    return true;
                }
                );
        }

        public static void Rect(int x0, int y0, int x1, int y1, bool isFilled, PlotFunction plot)
        {
            if (x0 > x1) Swap<int>(ref x0, ref x1);
            if (y0 > y1) Swap<int>(ref y0, ref y1);

            if (isFilled)
            {
                for (int y = y0; y <= y1; ++y)
                    for (int x = x0; x <= x1; plot(x, y), ++x) ;
            }
            else
            {
                for (int y = y0; y <= y1; ++y){ plot(x0, y); plot(x1, y); }
                for (int x = x0; x <= x1; ++x) { plot(x, y0); plot(x, y1); }
            }
        }

        public static void DrawRect(STETilemap tilemap, Vector2 locPosA, Vector2 locPosB, uint[,] tileData, bool isFilled, bool is9Sliced = false, bool randomize = false)
        {
            int x0 = TilemapUtils.GetGridX(tilemap, locPosA);
            int y0 = TilemapUtils.GetGridY(tilemap, locPosA);
            int x1 = TilemapUtils.GetGridX(tilemap, locPosB);
            int y1 = TilemapUtils.GetGridY(tilemap, locPosB);
            DrawRect(tilemap, x0, y0, x1, y1, tileData, isFilled, is9Sliced, randomize);
        }

        public static void DrawRect(STETilemap tilemap, int x0, int y0, int x1, int y1, uint[,] tileData, bool isFilled, bool is9Sliced = false, bool randomize = false)
        {
            int w = tileData.GetLength(0);
            int h = tileData.GetLength(1);
            if (x0 > x1) Swap<int>( ref x0, ref x1);
            if (y0 > y1) Swap<int>(ref y0, ref y1);
            TilemapDrawingUtils.Rect(x0, y0, x1, y1, isFilled,
            (x, y) =>
            {
                if (is9Sliced)
                {
                    if(x == x0 && y == y0)
                        tilemap.SetTileData(x, y, tileData[0, 0]);
                    else if(x == x0 && y == y1)
                        tilemap.SetTileData(x, y, tileData[0, h - 1]);
                    else if(x == x1 && y == y0)
                        tilemap.SetTileData(x, y, tileData[w - 1, 0]);
                    else if (x == x1 && y == y1)
                        tilemap.SetTileData(x, y, tileData[w - 1, h - 1]);
                    else
                    {
                        int cw = w - 2;
                        int ch = h - 2;
                        int cx = cw >= 1 ? 1 + (x % cw + cw) % cw : (x % w + w) % w;
                        int cy = ch >= 1 ? 1 + (y % ch + ch) % ch : (y % h + h) % h;
                        if (x == x0)
                            tilemap.SetTileData(x, y, tileData[0, cy]);
                        else if (x == x1)
                            tilemap.SetTileData(x, y, tileData[w - 1, cy]);
                        else if (y == y0)
                            tilemap.SetTileData(x, y, tileData[cx, 0]);
                        else if (y == y1)
                            tilemap.SetTileData(x, y, tileData[cx, h - 1]);
                        else
                        {
                            if(randomize)
                                tilemap.SetTileData(x, y, tileData[w > 2 ? Random.Range(1, w - 1) : cx, h > 2 ? Random.Range(1, h - 1) : cy]);
                            else
                                tilemap.SetTileData(x, y, tileData[cx, cy]);
                        }
                    }
                }
                else
                {
                    int dataIdx0 = randomize ? Random.Range(0, w) : (x % w + w) % w;
                    int dataIdx1 = randomize ? Random.Range(0, h) : (y % h + h) % h;
                    tilemap.SetTileData(x, y, tileData[dataIdx0, dataIdx1]);
                }
                return true;
            }
            );
        }

        //ref: https://github.com/Skiles/aseprite/blob/a5056e15512b219d7dbbb902feb89362e614891e/src/raster/algo.cpp
        //NOTE: this is not working for cases where x1 x2 y1 or y2 are negative or x1 > x2 or y1 > y2 
        public static void Ellipse(int x1, int y1, int x2, int y2, bool isFilled, PlotFunction plot)
        {
            int mx, my, rx, ry;

            int err;
            int xx, yy;
            int xa, ya;
            int x, y;

            int mx2, my2;            

            mx = (x1 + x2) / 2;
            mx2 = (x1 + x2 + 1) / 2;
            my = (y1 + y2) / 2;
            my2 = (y1 + y2 + 1) / 2;
            rx = Mathf.Abs(x1 - x2);
            ry = Mathf.Abs(y1 - y2);

            if (isFilled)
            {
                int xmin = Mathf.Min(x1, x2);
                int xmax = Mathf.Max(x1, x2);
                int ymin = Mathf.Min(y1, y2);
                int ymax = Mathf.Max(y1, y2);
                if (rx == 1) { for (int c = ymin; c <= ymax; ++c) plot(x2, c); rx--; }
                if (rx == 0) { for (int c = ymin; c <= ymax; ++c) plot(x1, c); return; }

                if (ry == 1) { for (int c = xmin; c <= xmax; ++c ) plot(c, y2); ry--; }
                if (ry == 0) { for (int c = xmin; c <= xmax; ++c) plot(c, y1); return; }
            }
            else
            {
                if (rx == 1) { Line(x2, y1, x2, y2, plot); rx--; }
                if (rx == 0) { Line(x1, y1, x1, y2, plot); return; }

                if (ry == 1) { Line(x1, y2, x2, y2, plot); ry--; }
                if (ry == 0) { Line(x1, y1, x2, y1, plot); return; }
            }

            rx /= 2;
            ry /= 2;

            /* Draw the 4 poles. */
            plot(mx, my2 + ry);
            plot(mx, my - ry);
            if (isFilled)
            {
                for (int c = mx - rx; c <= mx2 + rx; ++c) plot(c, my);
            }
            else
            {
                plot(mx2 + rx, my);
                plot(mx - rx, my);
            }

            /* For even diameter axis, double the poles. */
            if (mx != mx2)
            {
                plot(mx2, my2 + ry);
                plot(mx2, my - ry);
            }

            if (my != my2)
            {
                if (isFilled)
                {
                    for (int c = mx - rx; c <= mx2 + rx; ++c) plot(c, my2);
                }
                else
                {
                    plot(mx2 + rx, my2);
                    plot(mx - rx, my2);
                }
            }

            xx = rx * rx;
            yy = ry * ry;

            /* Do the 'x direction' part of the arc. */

            x = 0;
            y = ry;
            xa = 0;
            ya = xx * 2 * ry;
            err = xx / 4 - xx * ry;

            for (; ; )
            {
                err += xa + yy;
                if (err >= 0)
                {
                    ya -= xx * 2;
                    err -= ya;
                    y--;
                }
                xa += yy * 2;
                x++;
                if (xa >= ya)
                    break;

                if (isFilled)
                {
                    for (int c = mx - x; c <= mx2 + x; ++c) plot(c, my - y);
                    for (int c = mx - x; c <= mx2 + x; ++c) plot(c, my2 + y);
                }
                else
                {
                    plot(mx2 + x, my - y);
                    plot(mx - x, my - y);
                    plot(mx2 + x, my2 + y);
                    plot(mx - x, my2 + y);
                }
            }

            /* Fill in missing pixels for very thin ellipses. (This is caused because
                * we always take 1-pixel steps above, and thus might jump past the actual
                * ellipse line.)
                */
            if (y == 0)
                while (x < rx)
                {
                    if (isFilled)
                    {
                        for (int c = mx - x; c <= mx2 + x; ++c) plot(c, my - 1);
                        for (int c = mx - x; c <= mx2 + x; ++c) plot(c, my2 + 1);
                    }
                    else
                    {
                        plot(mx2 + x, my - 1);
                        plot(mx2 + x, my2 + 1);
                        plot(mx - x, my - 1);
                        plot(mx - x, my2 + 1);
                    }
                    x++;
                }

            /* Do the 'y direction' part of the arc. */

            x = rx;
            y = 0;
            xa = yy * 2 * rx;
            ya = 0;
            err = yy / 4 - yy * rx;

            for (; ; )
            {
                err += ya + xx;
                if (err >= 0)
                {
                    xa -= yy * 2;
                    err -= xa;
                    x--;
                }
                ya += xx * 2;
                y++;
                if (ya > xa)
                    break;
                if (isFilled)
                {
                    for (int c = mx - x; c <= mx2 + x; ++c) plot(c, my - y);
                    for (int c = mx - x; c <= mx2 + x; ++c) plot(c, my2 + y);
                }
                else
                {
                    plot(mx2 + x, my - y);
                    plot(mx - x, my - y);
                    plot(mx2 + x, my2 + y);
                    plot(mx - x, my2 + y);
                }
            }

            /* See comment above. */
            if (x == 0)
                while (y < ry)
                {
                    if (isFilled)
                    {
                        for (int c = mx - 1; c <= mx2 + 1; ++c) plot(c, my - y);
                        for (int c = mx - 1; c <= mx2 + 1; ++c) plot(c, my2 + y);
                    }
                    else
                    {
                        plot(mx - 1, my - y);
                        plot(mx2 + 1, my - y);
                        plot(mx - 1, my2 + y);
                        plot(mx2 + 1, my2 + y);
                    }
                    y++;
                }
        }

        public static void DrawEllipse(STETilemap tilemap, Vector2 locPosA, Vector2 locPosB, uint[,] tileData, bool isFilled, bool randomize = false)
        {
            int x0 = TilemapUtils.GetGridX(tilemap, locPosA);
            int y0 = TilemapUtils.GetGridY(tilemap, locPosA);
            int x1 = TilemapUtils.GetGridX(tilemap, locPosB);
            int y1 = TilemapUtils.GetGridY(tilemap, locPosB);
            DrawEllipse(tilemap, x0, y0, x1, y1, tileData, isFilled, randomize);
        }

        public static void DrawEllipse(STETilemap tilemap, int x0, int y0, int x1, int y1, uint[,] tileData, bool isFilled, bool randomize = false)
        {
            int w = tileData.GetLength(0);
            int h = tileData.GetLength(1);
            int xf = 0;
            int yf = 0;

            //fix for cases where x1 x2 y1 or y2 are negative or x1 > x2 or y1 > y2 
            // NOTE: I tested this only for case x1 == y1 == 0
            if (x0 > x1) Swap<int>(ref x0, ref x1);
            if (y0 > y1) Swap<int>(ref y0, ref y1);
            if (x0 < 0) { xf = x0; x0 = 0; x1 -= xf; }
            if (y0 < 0) { yf = y0; y0 = 0; y1 -= yf; }
            //
            TilemapDrawingUtils.Ellipse(x0, y0, x1, y1, isFilled,
            (x, y) =>
            {
                int dataIdx0 = randomize ? Random.Range(0, w) : ((x + xf) % w + w) % w;
                int dataIdx1 = randomize ? Random.Range(0, h) : ((y + yf) % h + h) % h;
                tilemap.SetTileData(x + xf, y + yf, tileData[dataIdx0, dataIdx1]);
                return true;
            }
            );
        }
    }
}