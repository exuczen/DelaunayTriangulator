using System;

namespace Triangulation
{
    [Serializable]
    public class SerializedTriangulator
    {
#if UNITY
        public SerializedPoint2[] SuperPoints;
        public SerializedGridPoint2[] GridPoints;
        public SerializedTriangle[] Triangles;
#else
        public SerializedPoint2[] SuperPoints { get; set; }
        public SerializedGridPoint2[] GridPoints { get; set; }
        public SerializedTriangle[] Triangles { get; set; }
#endif
        public int PointsLength => SuperPoints.Length + GridPoints.Length;

        public SerializedTriangulator() { }

        public SerializedTriangulator(Triangulator triangulator)
        {
            var pointsXY = triangulator.PointGrid.PointsXY;
            var points = triangulator.Points;
            var triangles = triangulator.Triangles;

            SuperPoints = new SerializedPoint2[triangulator.PointsOffset];
            GridPoints = new SerializedGridPoint2[triangulator.UsedPointsCount - SuperPoints.Length];
            Triangles = new SerializedTriangle[triangulator.TrianglesCount];

            int pointsCount = triangulator.PointsCount;
            int gridCount = 0;

            for (int i = 0; i < SuperPoints.Length; i++)
            {
                SuperPoints[i] = new SerializedPoint2(points[i], i);
            }
            for (int i = SuperPoints.Length; i < pointsCount; i++)
            {
                if (!Mathv.IsNaN(points[i]))
                {
                    GridPoints[gridCount++] = new SerializedGridPoint2(pointsXY[i], i);
                }
            }
            for (int i = 0; i < Triangles.Length; i++)
            {
                Triangles[i] = new SerializedTriangle(triangles[i]);
            }
        }
    }
}
