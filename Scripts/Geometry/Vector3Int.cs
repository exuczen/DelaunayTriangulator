using System;

namespace Triangulation
{
    public struct Vector3Int
    {
        public static readonly Vector3Int One = new Vector3Int(1, 1, 1);
        public static readonly Vector3Int Up = new Vector3Int(0, 1, 0);
        public static readonly Vector3Int Right = new Vector3Int(1, 0, 0);

        public int x;
        public int y;
        public int z;

        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return x;
                    case 1: return y;
                    case 2: return z;
                    default:
                        throw new IndexOutOfRangeException("Vector3Int.this[" + index + "]");
                }
            }
            set
            {
                switch (index)
                {
                    case 0: x = value; break;
                    case 1: y = value; break;
                    case 2: z = value; break;
                    default:
                        throw new IndexOutOfRangeException("Vector3Int.this[" + index + "]");
                }
            }
        }

        public float SqrLength => x * x + y * y + z * z;

        public static Vector3Int operator *(int mlp, Vector3Int v) => new Vector3Int(v.x * mlp, v.y * mlp, v.z * mlp);

        public static Vector3Int operator *(Vector3Int v, int mlp) => mlp * v;

        public Vector3Int(int x, int y, int z) : this() => Set(x, y, z);

        public void Set(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2})", x, y, z);
        }
    }
}
