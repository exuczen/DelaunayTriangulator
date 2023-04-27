using System;

namespace Triangulation
{
    [Serializable]
    public class SerializedTriangulator
    {
#if UNITY
        public SerializedPoint2[] Points;
        public SerializedTriangle[] Triangles;
        public int PointsOffset;
#else
        public SerializedPoint2[] Points { get; set; }
        public SerializedTriangle[] Triangles { get; set; }
        public int PointsOffset { get; set; }
#endif

        public SerializedTriangulator() { }

        public SerializedTriangulator(Triangulator triangulator)
        {
            var points = triangulator.Points;
            var triangles = triangulator.Triangles;

            Points = new SerializedPoint2[triangulator.UsedPointsCount];
            Triangles = new SerializedTriangle[triangulator.TrianglesCount];

            int pointsCount = triangulator.PointsCount;
            int count = 0;
            for (int i = 0; i < pointsCount; i++)
            {
                if (!Mathv.IsNaN(points[i]))
                {
                    Points[count++] = new SerializedPoint2(points[i], i);
                }
            }
            for (int i = 0; i < Triangles.Length; i++)
            {
                Triangles[i] = new SerializedTriangle(triangles[i]);
            }
            PointsOffset = triangulator.PointsOffset;
        }
    }
}
