using System;
using UnityEngine;

namespace Triangulation
{
    public struct Bounds2Int : IEquatable<Bounds2Int>
    {
        public Vector2Int Size => max - min + Vector2Int.One;

        public Vector2Int min;
        public Vector2Int max;

        public static Bounds2Int MinMax(Vector2Int min, Vector2Int max)
        {
            return new Bounds2Int
            {
                min = min,
                max = max
            };
        }

        public static Bounds2Int GetBounds(Vector2Int[] pointsXY, int beg, int end)
        {
            if (beg < 0 || end < 0)
            {
                throw new IndexOutOfRangeException();
            }
            var min = pointsXY[beg];
            var max = min;

            for (int i = beg + 1; i <= end; ++i)
            {
                Vector2Int v = pointsXY[i];
                max = Vector2Int.Max(max, v);
                min = Vector2Int.Min(min, v);
            }
            return MinMax(min, max);
        }

        public Bounds2Int(Vector2Int min, Vector2Int max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Contains(Vector2Int pos)
        {
            return pos.x >= min.x && pos.x <= max.x && pos.y >= min.y && pos.y <= max.y;
        }

        public bool Contains(Vector2 pos)
        {
            return pos.x >= min.x && pos.x <= max.x && pos.y >= min.y && pos.y <= max.y;
        }

        public bool Equals(Bounds2Int other)
        {
            return min == other.min && max == other.max;
        }
    }
}
