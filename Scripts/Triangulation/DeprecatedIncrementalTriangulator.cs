using System;

namespace Triangulation
{
    public class DeprecatedIncrementalTriangulator : IncrementalTriangulator
    {
        public DeprecatedIncrementalTriangulator(int pointsCapacity, float tolerance, bool internalOnly, IExceptionThrower exceptionThrower) : base(pointsCapacity, tolerance, internalOnly, exceptionThrower)
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
