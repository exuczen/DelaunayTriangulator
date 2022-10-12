using System;

namespace Triangulation
{
    public struct Vector4Int
    {
        public Vector3Int v;
        public int w;

        public Vector4Int(Vector3Int v, int w)
        {
            this.v = v;
            this.w = w;
        }

        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: case 1: case 2: return v[index];
                    case 3: return w;
                    default:
                        throw new IndexOutOfRangeException("Vector3Int.this[" + index + "]");
                }
            }
            set
            {
                switch (index)
                {
                    case 0: case 1: case 2: v[index] = value; break;
                    case 3: w = value; break;
                    default:
                        throw new IndexOutOfRangeException("Vector3Int.this[" + index + "]");
                }
            }
        }

        public override string ToString()
        {
            return string.Format("({0}, {1}, {2}, {3})", v.x, v.y, v.z, w);
        }
    }
}
