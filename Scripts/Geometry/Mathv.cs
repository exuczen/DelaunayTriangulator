using System;
using System.Numerics;

namespace Triangulation
{
    public static class Mathv
    {
        public const float Epsilon = 1E-05F;

        public static bool IsFinite(Vector2 v)
        {
            return float.IsFinite(v.X) && float.IsFinite(v.Y);
        }

        public static bool IsNaN(Vector2 v)
        {
            return float.IsNaN(v.X) || float.IsNaN(v.Y);
        }

        public static float CosAngle(Vector2 v1, Vector2 v2)
        {
            return Vector2.Dot(Vector2.Normalize(v1), Vector2.Normalize(v2));
        }

        public static float AngleDeg(Vector2 v1, Vector2 v2)
        {
            return AngleRad(v1, v2) * Maths.Rad2Deg;
        }

        public static float AngleRad(Vector2 v1, Vector2 v2)
        {
            float denom = v1.Length() * v2.Length();
            if (denom > Epsilon)
            {
                float cosAngle = Vector2.Dot(v1, v2) / denom;
                cosAngle = Math.Clamp(cosAngle, -1f, 1f);
                return MathF.Acos(cosAngle);
            }
            else
            {
                throw new Exception("AngleRad: " + v1 + " " + v2);
            }
        }

        public static float Cross(Vector2 v1, Vector2 v2)
        {
            return v1.X * v2.Y - v1.Y * v2.X;
        }

        public static SerializedVector2 ToSerializedVector2(this Vector2 v) => new SerializedVector2(v);

        public static Vector2Int ToVector2Int(this Vector2 v) => new Vector2Int(v);

        public static Vector2 Normalized(this Vector2 v, out float length)
        {
            length = v.Length();
            return length > Epsilon ? v / length : v;
        }

        public static Vector2 Normalized(this Vector2 v) => v.Normalized(out _);

        public static bool Equals(Vector2 v1, Vector2 v2, float tolerance)
        {
            var absDr = Vector2.Abs(v2 - v1);
            return absDr.X < tolerance && absDr.Y < tolerance;
        }

        public static string ToString(this Vector2 v, string format)
        {
            return string.Format("({0}, {1})", v.X.ToString(format), v.Y.ToString(format));
        }

        public static string ToStringF2(this Vector2 v)
        {
            return string.Format("({0}, {1})", v.X.ToString("f2"), v.Y.ToString("f2"));
        }
    }
}
