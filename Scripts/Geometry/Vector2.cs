using System;

namespace Triangulation
{
    public struct Vector2
    {
        public const float Epsilon = 1E-05F;

        public static readonly Vector2 Zero = new Vector2(0f, 0f);
        public static readonly Vector2 One = new Vector2(1f, 1f);
        public static readonly Vector2 Up = new Vector2(0f, 1f);
        public static readonly Vector2 Right = new Vector2(1f, 0f);

        public float x;
        public float y;

        public float SqrLength => x * x + y * y;
        public float Length => MathF.Sqrt(SqrLength);

        public float this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    default:
                        throw new IndexOutOfRangeException("Vector2.this[" + index + "]");
                }
            }
            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    default:
                        throw new IndexOutOfRangeException("Vector2.this[" + index + "]");
                }
            }
        }

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public static float CosAngle(Vector2 v1, Vector2 v2)
        {
            return Dot(v1.Normalized(), v2.Normalized());
        }

        public static float AngleDeg(Vector2 v1, Vector2 v2)
        {
            return AngleRad(v1, v2) * Maths.Rad2Deg;
        }

        public static float AngleRad(Vector2 v1, Vector2 v2)
        {
            float denom = v1.Length * v2.Length;
            if (denom > Epsilon)
            {
                float cosAngle = Dot(v1, v2) / denom;
                cosAngle = Math.Clamp(cosAngle, -1f, 1f);
                return MathF.Acos(cosAngle);
            }
            else
            {
                throw new Exception("AngleRad: " + v1 + " " + v2);
            }
        }

        public static Vector2 Min(Vector2 v1, Vector2 v2)
        {
            return new Vector2(MathF.Min(v1.x, v2.x), MathF.Min(v1.y, v2.y));
        }

        public static Vector2 Max(Vector2 v1, Vector2 v2)
        {
            return new Vector2(MathF.Max(v1.x, v2.x), MathF.Max(v1.y, v2.y));
        }

        public static float SqrDistance(Vector2 v1, Vector2 v2)
        {
            return (v2 - v1).SqrLength;
        }

        public static float Cross(Vector2 v1, Vector2 v2)
        {
            return v1.x * v2.y - v1.y * v2.x;
        }

        public static float Dot(Vector2 v1, Vector2 v2)
        {
            return v1.x * v2.x + v1.y * v2.y;
        }

        public static implicit operator Vector2(Vector2Int v)
        {
            return new Vector2(v.x, v.y);
        }

        public static Vector2 operator /(Vector2 v1, float div)
        {
            return new Vector2(v1.x / div, v1.y / div);
        }

        public static Vector2 operator *(float mlp, Vector2 v)
        {
            return new Vector2(v.x * mlp, v.y * mlp);
        }

        public static Vector2 operator *(Vector2 v, float mlp)
        {
            return mlp * v;
        }

        public static Vector2 operator *(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x * v2.x, v1.y * v2.y);
        }

        public static Vector2 operator +(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x + v2.x, v1.y + v2.y);
        }

        public static Vector2 operator -(Vector2 v1, Vector2 v2)
        {
            return new Vector2(v1.x - v2.x, v1.y - v2.y);
        }

        public static Vector2 operator -(Vector2 v1)
        {
            return new Vector2(-v1.x, -v1.y);
        }

        public Vector2 Normalized(out float length)
        {
            length = Length;
            return length > Epsilon ? this / length : this;
        }

        public Vector2 Normalized()
        {
            return Normalized(out _);
        }

        public bool Equals(Vector2 v, float tolerance)
        {
            return MathF.Abs(v.x - x) < tolerance && MathF.Abs(v.y - y) < tolerance;
        }

        public string ToString(string format)
        {
            return string.Format("({0}, {1})", x.ToString(format), y.ToString(format));
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", x.ToString("f2"), y.ToString("f2"));
        }
    }
}
