namespace Triangulation
{
    public struct SerializedTriangle
    {
        public int[] Indices { get; set; }

        public SerializedTriangle(Triangle triangle)
        {
            triangle.GetIndices(Indices = new int[3]);
        }
    }
}
