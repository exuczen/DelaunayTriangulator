using System;

namespace Triangulation
{
    [Serializable]
    public class SerializedTriangulator
    {
#if UNITY
        public SerializedVector2[] Points;
        public SerializedTriangle[] Triangles;
        public int PointsOffset;
        public bool[] PointsUsed;
#else
        public SerializedVector2[] Points { get; set; }
        public SerializedTriangle[] Triangles { get; set; }
        public int PointsOffset { get; set; }
        public bool[] PointsUsed { get; set; }
#endif

        public SerializedTriangulator() { }

        public SerializedTriangulator(Triangulator triangulator)
        {
            var points = triangulator.Points;
            var triangles = triangulator.Triangles;

            Points = new SerializedVector2[triangulator.PointsCount];
            PointsUsed = new bool[Points.Length];
            for (int i = 0; i < Points.Length; i++)
            {
                PointsUsed[i] = !Vector2.IsNaN(points[i]);
                Points[i] = PointsUsed[i] ? new SerializedVector2(points[i]) : default;
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
