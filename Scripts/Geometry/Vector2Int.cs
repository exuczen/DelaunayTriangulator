﻿using System;
using System.Numerics;

namespace Triangulation
{
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public static readonly Vector2Int Zero = new Vector2Int(0, 0);
        public static readonly Vector2Int One = new Vector2Int(1, 1);
        public static readonly Vector2Int Up = new Vector2Int(0, 1);
        public static readonly Vector2Int Right = new Vector2Int(1, 0);

        public static readonly Vector2Int NegativeOne = new Vector2Int(-1, -1);

        public int x;
        public int y;

        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    default:
                        throw new IndexOutOfRangeException($"Vector2Int.this[{index}]");
                }
            }
            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    default:
                        throw new IndexOutOfRangeException($"Vector2Int.this[{index}]");
                }
            }
        }

        public float SqrLength => x * x + y * y;

        public static implicit operator Vector2Int(Vector3Int v) => new Vector2Int(v.x, v.y);

        public static bool operator ==(Vector2Int v1, Vector2Int v2) => v1.Equals(v2);

        public static bool operator !=(Vector2Int v1, Vector2Int v2) => !v1.Equals(v2);

        public static Vector2Int operator -(Vector2Int v1, Vector2Int v2) => new Vector2Int(v1.x - v2.x, v1.y - v2.y);

        public static Vector2Int operator +(Vector2Int v1, Vector2Int v2) => new Vector2Int(v1.x + v2.x, v1.y + v2.y);

        public static Vector2 operator *(Vector2 v1, Vector2Int v2) => v2 * v1;

        public static Vector2 operator *(Vector2Int v1, Vector2 v2) => new Vector2(v1.x * v2.X, v1.y * v2.Y);

        public static Vector2Int operator *(Vector2Int v1, Vector2Int v2) => new Vector2Int(v1.x * v2.x, v1.y * v2.y);

        public static Vector2Int operator *(int mlp, Vector2Int v) => new Vector2Int(v.x * mlp, v.y * mlp);

        public static Vector2Int operator *(Vector2Int v, int mlp) => mlp * v;

        public static Vector2Int Max(Vector2Int v1, Vector2Int v2)
        {
            return new Vector2Int(Math.Max(v1.x, v2.x), Math.Max(v1.y, v2.y));
        }

        public static Vector2Int Min(Vector2Int v1, Vector2Int v2)
        {
            return new Vector2Int(Math.Min(v1.x, v2.x), Math.Min(v1.y, v2.y));
        }

        public Vector2Int(Vector2 v) : this() => Set((int)v.X, (int)v.Y);

        public Vector2Int(int x, int y) : this() => Set(x, y);

        public void Set(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public Vector2 ToVector2() => new Vector2(x, y);

        public bool Equals(Vector2Int other)
        {
            return x == other.x && y == other.y;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", x, y);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
