using System;
using System.Drawing;

namespace Triangulation
{
    public class DeprecatedIncrementalTriangulator : IncrementalTriangulator
    {
        public DeprecatedIncrementalTriangulator(int pointsCapacity, float tolerance, IExceptionThrower exceptionThrower) : base(pointsCapacity, tolerance, exceptionThrower)
        {
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
