namespace Triangulation
{
    public class SerializedTriangulator
    {
        public SerializedVector2[] Points { get; set; }
        public SerializedTriangle[] Triangles { get; set; }
        public int PointsOffset { get; set; }

        public SerializedTriangulator() { }

        public SerializedTriangulator(Triangulator triangulator)
        {
            var points = triangulator.Points;
            var triangles = triangulator.Triangles;

            Points = new SerializedVector2[triangulator.PointsCount];
            for (int i = 0; i < Points.Length; i++)
            {
                Points[i] = new SerializedVector2(points[i]);
            }
            Triangles = new SerializedTriangle[triangulator.TrianglesCount];
            for (int i = 0; i < Triangles.Length; i++)
            {
                Triangles[i] = new SerializedTriangle(triangles[i]);
            }
            PointsOffset = triangulator.PointsOffset;
        }
    }
}
