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

        private bool RemoveUnusedPoint(int pointIndex)
        {
            int index = unusedPointIndices.IndexOf(pointIndex);
            if (index >= 0)
            {
                if (pointIndex == pointsCount - 1)
                {
                    pointsCount--;
                }
                unusedPointIndices.RemoveAt(index);
                return true;
            }
            return false;
        }

        //private void DeprecatedFindUnusedPoints()
        //{
        //    var usedPointIndices = new List<int>();
        //
        //    usedPointIndices.Clear();
        //    unusedPointIndices.Clear();
        //
        //    for (int i = 0; i < pointsOffset; i++)
        //    {
        //        usedPointIndices.Add(i);
        //    }
        //    int[] indexBuffer = new int[3];
        //
        //    for (int i = 0; i < trianglesCount; i++)
        //    {
        //        triangles[i].GetIndices(indexBuffer);
        //        for (int j = 0; j < 3; j++)
        //        {
        //            usedPointIndices.Add(indexBuffer[j]);
        //        }
        //    }
        //    usedPointIndices.Sort();
        //
        //    if (usedPointIndices.Count > 0)
        //    {
        //        for (int j = 0; j < usedPointIndices[0]; j++)
        //        {
        //            unusedPointIndices.Add(j);
        //            Log.WriteLine(GetType() + ".FindUnusedPoints: " + j);
        //        }
        //        int i = 0;
        //        while (i < usedPointIndices.Count - 1)
        //        {
        //            while (i < usedPointIndices.Count - 1 && usedPointIndices[i + 1] - usedPointIndices[i] <= 1)
        //            {
        //                i++;
        //            }
        //            if (i < usedPointIndices.Count - 1)
        //            {
        //                for (int j = usedPointIndices[i] + 1; j < usedPointIndices[i + 1]; j++)
        //                {
        //                    unusedPointIndices.Add(j);
        //                    Log.WriteLine(GetType() + ".FindUnusedPoints: " + j);
        //                }
        //            }
        //            i++;
        //        }
        //        int lastUsedIndex = usedPointIndices.Count - 1;
        //        for (int j = usedPointIndices[lastUsedIndex] + 1; j < pointsCount; j++)
        //        {
        //            unusedPointIndices.Add(j);
        //            Log.WriteLine(GetType() + ".FindUnusedPoints: " + j);
        //        }
        //        usedPointIndices.Clear();
        //    }
        //    else
        //    {
        //        for (int i = 0; i < pointsCount; i++)
        //        {
        //            unusedPointIndices.Add(i);
        //        }
        //    }
        //}

        //private void DeprecatedClearUnusedPoints()
        //{
        //    unusedPointIndices.Sort();
        //    for (int i = unusedPointIndices.Count - 1; i >= 0; i--)
        //    {
        //        base.ClearPoint(unusedPointIndices[i], false);
        //    }
        //    if (pointsCount == 0)
        //    {
        //        unusedPointIndices.Clear();
        //    }
        //}
    }
}
