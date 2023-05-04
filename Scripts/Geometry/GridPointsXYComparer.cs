﻿//#define SKIP_Y_COMPARE

using System;
using System.Collections.Generic;

namespace Triangulation
{
    public struct GridPointsXYComparer : IComparer<Vector2Int>
    {
        public int Compare(Vector2Int p1, Vector2Int p2)
        {
            int delta = p1.x - p2.x;
            if (delta != 0)
            {
                return Math.Sign(delta);
            }
#if !SKIP_Y_COMPARE
            delta = p1.y - p2.y;
            if (delta != 0)
            {
                return Math.Sign(delta);
            }
#endif
            return 0;
        }
    }
}
