using System;

namespace Triangulation
{
    [Serializable]
    public struct SerializedGridPoint2
    {
#if UNITY
        public int X;
        public int Y;
        public int Index;
#else
        public int X { get; set; }
        public int Y { get; set; }
        public int Index { get; set; }
#endif

        public SerializedGridPoint2(Vector2Int p, int index)
        {
            X = p.x;
            Y = p.y;
            Index = index;
        }

        public Vector2Int GetXY() => new Vector2Int(X, Y);

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", X, Y, Index);
        }
    }
}
