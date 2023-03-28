using System;

namespace Triangulation
{
    public struct Circle
    {
        public static float MaxRadius = 1E05F;

        public bool SizeValid { get; private set; }
        public Vector2 Center { get; }
        public float SqrRadius { get; }
        public float Radius { get; }
        public Bounds2 Bounds { get; }

        public bool Filled { get; set; }

        public Circle(Vector2 center, float sqrRadius) : this()
        {
            Center = center;
            SqrRadius = sqrRadius;
            Radius = MathF.Sqrt(SqrRadius);
            Bounds = new Bounds2() {
                min = center - Radius * Vector2.One,
                max = center + Radius * Vector2.One,
            };
            Filled = false;
            SizeValid = Radius > 0f && Radius < MaxRadius;
        }

        public bool ContainsPoint(Vector2 point, float sqrOffset)
        {
            return ContainsPoint(point, out _, out _, sqrOffset);
        }

        public bool ContainsPoint(Vector2 point, out Vector2 dr, out Vector2 sqrDr, float sqrOffset)
        {
            dr = point - Center;
            sqrDr.x = dr.x * dr.x;
            sqrDr.y = dr.y * dr.y;
            return sqrDr.x + sqrDr.y <= SqrRadius + sqrOffset;
        }

        public bool ContainsPoint(Vector2 point, float sqrOffset, out float sqrDelta)
        {
            var dr = point - Center;
            float sqrDx = dr.x * dr.x;
            float sqrDy = dr.y * dr.y;
            sqrDelta = SqrRadius - (sqrDx + sqrDy);
            return sqrDelta + sqrOffset >= 0f;
        }

        public override string ToString()
        {
            //return string.Format("Circle: ({0}, {1}) | {2}", Center, Radius.ToString("f2"), Bounds);
            return string.Format("Circle: ({0}, {1})", Center, Radius.ToString("f2"));
        }
    }
}
