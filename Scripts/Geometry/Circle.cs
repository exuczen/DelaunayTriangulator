using System;

namespace Triangulation
{
    public struct Circle
    {
        public static float MaxRadius { get; set; } = 1E05F;
        public static float MinRadiusForSqrt { get; set; } = float.MaxValue;

        public bool SizeValid { get; }
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
            Bounds = new Bounds2()
            {
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

        public bool ContainsPointWithSqrt(Vector2 point, float sqrOffset)
        {
            bool result = ContainsPoint(point, out _, out float sqrDist, sqrOffset);

            if (result && Radius >= MinRadiusForSqrt)
            {
                result = MathF.Sqrt(sqrDist) <= Radius + sqrOffset;
            }
            return result;
        }

        public bool ContainsPoint(Vector2 point, out Vector2 dr, out float sqrDist, float sqrOffset)
        {
            dr = point - Center;
            sqrDist = dr.SqrLength;

            return sqrDist <= SqrRadius + sqrOffset;
        }

        public override string ToString()
        {
            //return string.Format("Circle: ({0}, {1}) | {2}", Center, Radius.ToString("f2"), Bounds);
            return string.Format("Circle: ({0}, {1})", Center, Radius.ToString("f2"));
        }
    }
}
