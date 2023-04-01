using System;
using System.Drawing;

namespace Triangulation
{
    public struct GridUtils
    {
        private static readonly Color[] DebugColors = { Color.Red, Color.Green, Color.Blue, Color.Yellow };

        public static bool FindClosestCellWithPredicate(Vector2Int cXY, Vector2Int xyCount, out Vector2Int cellXY,
#if DEBUG_CLOSEST_CELLS
            Func<int, int, Color, string, bool> predicate)
#else
            Func<int, int, bool> predicate)
#endif
        {
            return FindClosestCellWithPredicate(cXY.x, cXY.y, xyCount.x, xyCount.y, out cellXY, predicate);
        }

        public static bool FindClosestCellWithPredicate(int cX, int cY, int xCount, int yCount, out Vector2Int cellXY,
#if DEBUG_CLOSEST_CELLS
            Func<int, int, Color, string, bool> predicate)
#else
            Func<int, int, bool> predicate)
#endif
        {
#if DEBUG_CLOSEST_CELLS
            static string deltaString(int dr, int dl) => string.Format("({0},{1})", dr, dl);
#endif
            cellXY = new Vector2Int(cX, cY);
#if DEBUG_CLOSEST_CELLS
            if (predicate(cX, cY, Color.White, deltaString(0, 0)))
#else
            if (predicate(cX, cY))
#endif
            {
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
#if DEBUG_CLOSEST_CELLS
                                if (predicate(x, y, debugColor, deltaString(dl, dr)))
#else
                                if (predicate(x, y))
#endif
                                {
                                    cellXY = new Vector2Int(x, y);
                                    return true;
                                }
                            }
                            if (xInBounds[i] && dr >= dyMin && dr <= dyMax)
                            {
                                int y = cY + dr;
                                int x = cX + dl;
#if DEBUG_CLOSEST_CELLS
                                if (predicate(x, y, debugColor, deltaString(dr, dl)))
#else
                                if (predicate(x, y))
#endif
                                {
                                    cellXY = new Vector2Int(x, y);
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
