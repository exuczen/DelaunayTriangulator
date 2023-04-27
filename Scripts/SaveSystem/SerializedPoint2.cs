using System;
using System.Numerics;

namespace Triangulation
{
    [Serializable]
    public struct SerializedPoint2
    {
#if UNITY
        public float X;
        public float Y;
        public int Index;
#else
        public float X { get; set; }
        public float Y { get; set; }
        public int Index { get; set; }
#endif

        public SerializedPoint2(Vector2 v, int index)
        {
            X = v.X;
            Y = v.Y;
            Index = index;
        }

        public Vector2 ToVector2() => new Vector2(X, Y);

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", X.ToStringF2(), Y.ToStringF2(), Index);
        }
    }
}
