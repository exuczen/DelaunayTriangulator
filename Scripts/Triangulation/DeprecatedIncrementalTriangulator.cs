using System;
using System.Drawing;

namespace Triangulation
{
    public class DeprecatedIncrementalTriangulator : IncrementalTriangulator
    {
        public DeprecatedIncrementalTriangulator(int pointsCapacity, float tolerance, IExceptionThrower exceptionThrower) : base(pointsCapacity, tolerance, exceptionThrower)
        {
        }

        private Triangle GetFirstTriangleWithVertex(int pointIndex)
        {
            for (int i = 0; i < trianglesCount; i++)
            {
                if (triangles[i].HasVertex(pointIndex))
                {
                    return triangles[i];
                }
            }
            return Triangle.None;
        }

        private void SetTrianglesColor(Color color)
        {
            for (int i = 0; i < trianglesCount; i++)
            {
                triangles[i].FillColor = color;
            }
        }

        private void ForEachTriangle(Action<int, Triangle> action)
        {
            for (int i = 0; i < trianglesCount; i++)
            {
                action(i, triangles[i]);
            }
        }
    }
}
