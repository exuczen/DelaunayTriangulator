using System;
using System.Numerics;

namespace Triangulation
{
    public static class Mathv
    {
        public const float Epsilon = 1E-05F;

        //public float X;
        //public float Y;

        //public float SqrLength => X * X + Y * Y;
        //public float Length => MathF.Sqrt(SqrLength);

        //public float this[int index]
        //{
        //    get
        //    {
        //        switch (index)
        //        {
        //            case 0: return X;
        //            case 1: return Y;
        //            default:
        //                throw new IndexOutOfRangeException("Vector2.this[" + index + "]");
        //        }
        //    }
        //    set
        //    {
        //        switch (index)
        //        {
        //            case 0: X = value; break;
        //            case 1: Y = value; break;
        //            default:
        //                throw new IndexOutOfRangeException("Vector2.this[" + index + "]");
        //        }
        //    }
        //}

        //public Vector2(float x, float y)
        //{
        //    this.X = x;
        //    this.Y = y;
        //}

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

        //public static Vector2 Min(Vector2 v1, Vector2 v2)
        //{
        //    return new Vector2(MathF.Min(v1.X, v2.X), MathF.Min(v1.Y, v2.Y));
        //}

        //public static Vector2 Max(Vector2 v1, Vector2 v2)
        //{
        //    return new Vector2(MathF.Max(v1.X, v2.X), MathF.Max(v1.Y, v2.Y));
        //}

        //public static float SqrDistance(Vector2 v1, Vector2 v2)
        //{
        //    //return (v2 - v1).LengthSquared();
        //    return Vector2.DistanceSquared(v1, v2);
        //}

        public static float Cross(Vector2 v1, Vector2 v2)
        {
            return v1.X * v2.Y - v1.Y * v2.X;
        }

        //public static float Dot(Vector2 v1, Vector2 v2)
        //{
        //    //return v1.X * v2.X + v1.Y * v2.Y;
        //    return Vector2.Dot(v1, v2);
        //}

        public static SerializedVector2 ToSerializedVector2(this Vector2 v) => new SerializedVector2(v);

        public static Vector2Int ToVector2Int(this Vector2 v) => new Vector2Int(v);

        //public static implicit operator Vector2(SerializedVector2 v)
        //{
        //    return new Vector2(v.X, v.Y);
        //}

        //public static implicit operator Vector2(Vector2Int v)
        //{
        //    return new Vector2(v.x, v.y);
        //}

        //public static Vector2 operator /(Vector2 v1, float div)
        //{
        //    return new Vector2(v1.X / div, v1.Y / div);
        //}

        //public static Vector2 operator *(float mlp, Vector2 v)
        //{
        //    return new Vector2(v.X * mlp, v.Y * mlp);
        //}

        //public static Vector2 operator *(Vector2 v, float mlp)
        //{
        //    return mlp * v;
        //}

        //public static Vector2 operator *(Vector2 v1, Vector2 v2)
        //{
        //    return new Vector2(v1.X * v2.X, v1.Y * v2.Y);
        //}

        //public static Vector2 operator +(Vector2 v1, Vector2 v2)
        //{
        //    return new Vector2(v1.X + v2.X, v1.Y + v2.Y);
        //}

        //public static Vector2 operator -(Vector2 v1, Vector2 v2)
        //{
        //    return new Vector2(v1.X - v2.X, v1.Y - v2.Y);
        //}

        //public static Vector2 operator -(Vector2 v1)
        //{
        //    return new Vector2(-v1.X, -v1.Y);
        //}

        //public Vector2 Normalized(out float length)
        //{
        //    length = Length;
        //    return length > Epsilon ? this / length : this;
        //}

        //public Vector2 Normalized()
        //{
        //    return Normalized(out _);
        //}

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
