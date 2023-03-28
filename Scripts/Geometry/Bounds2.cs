using System;
using System.Collections.Generic;

namespace Triangulation
{
    public struct Bounds2
    {
        public Vector2 min;
        public Vector2 max;

        public Vector2 Size => max - min;
        public Vector2 Center => (min + max) * 0.5f;

        public static Bounds2 GetBounds(List<Vector2> points, int offset)
        {
            return GetBounds(i => points[i], offset, points.Count - 1);
        }

        public static Bounds2 GetBounds(Vector2[] points, int beg, int end)
        {
            return GetBounds(i => points[i], beg, end);
        }

        private static Bounds2 GetBounds(Func<int, Vector2> getPoint, int beg, int end)
        {
            if (beg < 0 || end < 0)
            {
                throw new IndexOutOfRangeException();
            }
            var min = getPoint(beg);
            var max = min;

            for (int i = beg + 1; i <= end; ++i)
            {
                Vector2 v = getPoint(i);
                if (v.x > max.x)
                {
                    max.x = v.x;
                }
                else if (v.x < min.x)
                {
                    min.x = v.x;
                }
                if (v.y > max.y)
                {
                    max.y = v.y;
                }
                else if (v.y < min.y)
                {
                    min.y = v.y;
                }
            }
            return new Bounds2(min, max);
        }

        public Bounds2(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Overlap(Bounds2 other)
        {
            return other.min.x <= max.x && other.max.x >= min.x
                && other.min.y <= max.y && other.max.y >= min.y;
        }

        public bool Contains(Vector2 point)
        {
            return point.x >= min.x && point.x <= max.x
                && point.y >= min.y && point.y <= max.y;
        }

        public string ToString(string format)
        {
            return string.Format("({0}, {1})", min.ToString(format), max.ToString(format));
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", min, max);
        }
    }
}
