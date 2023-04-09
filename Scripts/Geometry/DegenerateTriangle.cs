namespace Triangulation
{
    public struct DegenerateTriangle
    {
        public static readonly DegenerateTriangle None = new DegenerateTriangle(EdgeEntry.None, -1);

        public bool IsValid => PointIndex >= 0 && Edge.IsValid;
        public EdgeEntry Edge { get; private set; }
        public int PointIndex { get; private set; }

        public DegenerateTriangle(EdgeEntry edge, int pointIndex)
        {
            Edge = edge;
            PointIndex = pointIndex;
        }
    }
}
