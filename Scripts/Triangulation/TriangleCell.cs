using System.Collections.Generic;

namespace Triangulation
{
    public class TriangleCell
    {
        public bool HasTriangles => TrianglesCount > 0;
        public HashSet<long> TriangleKeys => triangleKeys;
        public int TrianglesCount => triangleKeys.Count;
        public bool Filled { get; set; }
        public Vector2 DebugPoint { get; set; }
        public string DebugText { get; set; }

        private readonly HashSet<long> triangleKeys = new HashSet<long>();

        public TriangleCell()
        {
        }

        public void AddTriangle(long key)
        {
            triangleKeys.Add(key);
        }

        public void RemoveTriangle(long key)
        {
            triangleKeys.Remove(key);
        }

        public void Clear()
        {
            Filled = false;
            DebugText = null;
            triangleKeys.Clear();
        }

        public override string ToString()
        {
            return "TriangleCell: TrianglesCount: " + TrianglesCount;
        }
    }
}
