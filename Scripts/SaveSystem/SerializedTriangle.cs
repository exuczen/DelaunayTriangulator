using System;


namespace Triangulation
{
    [Serializable]
    public struct SerializedTriangle
    {
#if UNITY
        public int[] Indices;
#else
        public int[] Indices { get; set; }
#endif

        public SerializedTriangle(Triangle triangle)
        {
            triangle.GetIndices(Indices = new int[3]);
        }
    }
}
