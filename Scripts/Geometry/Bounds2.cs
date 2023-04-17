using System;
using System.Collections.Generic;
using System.Numerics;

namespace Triangulation
{
    public struct Bounds2
    {
        public Vector2 min;
        public Vector2 max;

        public Vector2 Size => max - min;
        public Vector2 Center => (min + max) * 0.5f;

        public static Bounds2 MinMax(Vector2 min, Vector2 max)
        {
            return new Bounds2 {
                min = min,
                max = max
            };
        }

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
                if (v.X > max.X)
                {
                    max.X = v.X;
                }
                else if (v.X < min.X)
                {
                    min.X = v.X;
                }
                if (v.Y > max.Y)
                {
                    max.Y = v.Y;
                }
                else if (v.Y < min.Y)
                {
                    min.Y = v.Y;
                }
            }
            return MinMax(min, max);
        }

        public bool Overlap(Bounds2 other)
        {
            return other.min.X <= max.X && other.max.X >= min.X
                && other.min.Y <= max.Y && other.max.Y >= min.Y;
        }

        public bool Contains(Bounds2 other)
        {
            return other.min.X >= min.X && other.max.X <= max.X
                && other.min.Y >= min.Y && other.max.Y <= max.Y;
        }

        public bool Contains(Vector2 point)
        {
            return point.X >= min.X && point.X <= max.X
                && point.Y >= min.Y && point.Y <= max.Y;
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
