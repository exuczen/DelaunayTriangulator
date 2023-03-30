using System;
using System.Drawing;

namespace Triangulation
{
    public struct GridUtils
    {
        private static readonly Color[] DebugColors = { Color.Red, Color.Green, Color.Blue, Color.Yellow };

        public static bool FindClosestCellWithPredicate(int cX, int cY, int xCount, int yCount,
            /*out TriangleCell cell, Predicate<TriangleCell> predicate*/
            out Vector2Int cellXY, Func<int, int, Color, bool> predicate)
        {
            cellXY = new Vector2Int(cX, cY);
            /*if (GetCell(cX, cY, out cell, out _) && predicate(cell))*/
            if (predicate(cX, cY, Color.White))
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

                        for (int j = -1; j <= 1; j += 2)
                        {
                            int dr = j * absDr;
                            if (yInBounds[i] && dr >= dxMin && dr <= dxMax)
                            {
                                int y = cY + radius * boundsSign;
                                int x = cX + dr;
                                /*if (GetCell(x, y, out cell, out _) && predicate(cell))*/
                                if (predicate(x, y, debugColor))
                                {
                                    cellXY = new Vector2Int(x, y);
                                    return true;
                                }
                            }
                            if (xInBounds[i] && dr >= dyMin && dr <= dyMax)
                            {
                                int x = cX + radius * boundsSign;
                                int y = cY + dr;
                                /*if (GetCell(x, y, out cell, out _) && predicate(cell))*/
                                if (predicate(x, y, debugColor))
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
