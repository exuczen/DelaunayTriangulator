namespace Triangulation
{
    public struct SerializedTriangle
    {
        public int[] Indices { get; set; } = new int[3];

        public SerializedTriangle(Triangle triangle)
        {
            triangle.GetIndices(Indices);
        }
    }
}
