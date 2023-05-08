using System;
using System.Drawing;
using System.Numerics;

namespace Triangulation
{
    public readonly struct GridUtils
    {
        private static readonly Color[] DebugColors = { Color.Red, Color.Green, Color.Blue, Color.Yellow };

        public static Vector2 SnapToGrid(Vector2 point, Vector2 cellSize, out Vector2Int xy)
        {
            int x = (int)(point.X / cellSize.X + 0.5f);
            int y = (int)(point.Y / cellSize.Y + 0.5f);
            xy = new Vector2Int(x, y);
            return xy * cellSize;
        }

        public static Vector2Int GetXYCount(Vector2 size, out Vector2 cellSize, int minDivsCount)
        {
            int xCount, yCount;
            if (size.X == size.Y)
            {
                xCount = yCount = minDivsCount;
                cellSize = size / xCount;
            }
            else if (size.Y < size.X)
            {
                yCount = minDivsCount;
                cellSize.Y = size.Y / yCount;
                xCount = (int)(size.X / cellSize.Y + 0.5f);
                cellSize.X = size.X / xCount;
            }
            else
            {
                xCount = minDivsCount;
                cellSize.X = size.X / xCount;
                yCount = (int)(size.Y / cellSize.X + 0.5f);
                cellSize.Y = size.Y / yCount;
            }
            return new Vector2Int(xCount, yCount);
        }

        public static bool FindClosestCellWithPredicate(Vector2Int cXY, Vector2Int xyCount, out Vector3Int cellXYI,
#if DEBUG_CLOSEST_CELLS
            Func<Vector3Int, Color, string, bool> predicate)
#else
            Predicate<Vector3Int> predicate)
#endif
        {
            return FindClosestCellWithPredicate(cXY.x, cXY.y, xyCount.x, xyCount.y, out cellXYI, predicate);
        }

        public static bool FindClosestCellWithPredicate(int cX, int cY, int xCount, int yCount, out Vector3Int cellXYI,
#if DEBUG_CLOSEST_CELLS
            Func<Vector3Int, Color, string, bool> predicate)
#else
            Predicate<Vector3Int> predicate)
#endif
        {
#if DEBUG_CLOSEST_CELLS
            static string deltaString(int dr, int dl) => string.Format("({0},{1})", dr, dl);
#endif
            cellXYI = new Vector3Int(cX, cY, cY * xCount + cX);
            var xyi = cellXYI;
#if DEBUG_CLOSEST_CELLS
            if (predicate(xyi, Color.White, deltaString(0, 0)))
#else
            if (predicate(xyi))
#endif
            {
                cellXYI = xyi;
                return true;
            }
            int radius = 1;
            var xInBounds = new bool[2] {
                cX - radius >= 0,
                cX + radius < xCount
            };
            var yInBounds = new bool[2] {
                cY - radius >= 0,
                cY + radius < yCount
            };
            int debugColorIndex = 0;

            while (xInBounds[0] || xInBounds[1] || yInBounds[0] || yInBounds[1])
            {
                int dxMax = Math.Min(radius - 1, xCount - 1 - cX);
                int dxMin = -Math.Min(radius - 1, cX);
                int dyMax = Math.Min(radius, yCount - 1 - cY);
                int dyMin = -Math.Min(radius, cY);

                var debugColor = DebugColors[debugColorIndex++];
                debugColorIndex %= DebugColors.Length;

                for (int absDr = 0; absDr <= radius; absDr++)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        int boundsSign = (i << 1) - 1;
                        int dl = radius * boundsSign;

                        for (int j = -1; j <= 1; j += 2)
                        {
                            int dr = j * absDr;
                            if (yInBounds[i] && dr >= dxMin && dr <= dxMax)
                            {
                                int y = cY + dl;
                                int x = cX + dr;
                                xyi.Set(x, y, y * xCount + x);
#if DEBUG_CLOSEST_CELLS
                                if (predicate(xyi, debugColor, deltaString(dl, dr)))
#else
                                if (predicate(xyi))
#endif
                                {
                                    cellXYI = xyi;
                                    return true;
                                }
                            }
                            if (xInBounds[i] && dr >= dyMin && dr <= dyMax)
                            {
                                int y = cY + dr;
                                int x = cX + dl;
                                xyi.Set(x, y, y * xCount + x);
#if DEBUG_CLOSEST_CELLS
                                if (predicate(xyi, debugColor, deltaString(dl, dr)))
#else
                                if (predicate(xyi))
#endif
                                {
                                    cellXYI = xyi;
                                    return true;
                                }
                            }
                        }
                    }
                }
                radius++;
                xInBounds[0] = cX - radius >= 0;
                xInBounds[1] = cX + radius < xCount;
                yInBounds[0] = cY - radius >= 0;
                yInBounds[1] = cY + radius < yCount;
            }
            return false;
        }
    }
}
