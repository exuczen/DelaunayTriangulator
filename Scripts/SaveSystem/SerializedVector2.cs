using System;

namespace Triangulation
{
    [Serializable]
    public struct SerializedVector2
    {
#if UNITY
        public float X;
        public float Y;
#else
        public float X { get; set; }
        public float Y { get; set; }
#endif

        public SerializedVector2(Vector2 v)
        {
            X = v.x;
            Y = v.y;
        }

        public static implicit operator SerializedVector2(Vector2 v)
        {
            return new SerializedVector2(v);
        }
    }
}
