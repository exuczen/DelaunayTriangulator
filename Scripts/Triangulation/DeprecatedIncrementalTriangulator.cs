﻿using System;

namespace Triangulation
{
    public class DeprecatedIncrementalTriangulator : IncrementalTriangulator
    {
        public DeprecatedIncrementalTriangulator(int pointsCapacity, float tolerance, bool internalOnly) : base(pointsCapacity, tolerance, internalOnly)
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

        //private bool AddExternalTriangle(EdgeEntry edge, int pointIndex, out bool pointOnEdge, Predicate<EdgeEntry> predicate)
        //{
        //    pointOnEdge = edge.LastPointOnEdge;
        //    if (predicate(edge))
        //    {
        //        addedTriangles[addedTrianglesCount++] = new Triangle(edge.A, edge.B, pointIndex, points);
        //        return true;
        //    }
        //    return false;
        //}

        //private bool AddInternalTriangle(EdgeEntry edge, int pointIndex, out bool pointOnEdge)
        //{
        //    pointOnEdge = edge.IsPointOnEdge(points[pointIndex], points, out _, true);
        //    if (!edge.LastPointDegenerateTriangle)
        //    {
        //        addedTriangles[addedTrianglesCount++] = new Triangle(edge.A, edge.B, pointIndex, points);
        //        return true;
        //    }
        //    return false;
        //}

        //private void AddCellTrianglesToProcess(int pointIndex)
        //{
        //    var point = points[pointIndex];
        //
        //    ForEachTriangleInCell(point, (triangle, triangleIndex) => {
        //        if (triangle.HasVertex(pointIndex))
        //        {
        //            //Console.WriteLine(GetType() + ".AddCellTrianglesToProcess: " + triangle + " pointIndex: " + pointIndex);
        //            AddCellTriangleToProcess(triangle, triangleIndex);
        //        }
        //    });
        //}

        //private void RefreshCellPoints()
        //{
        //    cellPointsIndices.Clear();
        //    for (int i = 0; i < addedTriangleSet.Count; i++)
        //    {
        //        AddTriangleVertsToCellPoints(addedTriangleSet.Triangles[i]);
        //    }
        //}

        //private Bounds2 GetCellPointsBounds()
        //{
        //    if (cellPointsIndices.Count < 3)
        //    {
        //        return default;
        //    }
        //    var min = points[cellPointsIndices[0]];
        //    var max = points[cellPointsIndices[0]];
        //    for (int i = 1; i < cellPointsIndices.Count; i++)
        //    {
        //        var point = points[cellPointsIndices[i]];
        //        min.x = Math.Min(min.x, point.x);
        //        min.y = Math.Min(min.y, point.y);
        //        max.x = Math.Max(max.x, point.x);
        //        max.y = Math.Max(max.y, point.y);
        //    }
        //    return new Bounds2(min, max);
        //}

        //private void SortCellPoints(Bounds2 bounds, out bool xySorted)
        //{
        //    Vector2 boundsSize = bounds.Size;
        //    xySorted = boundsSize.x > boundsSize.y;
        //    if (xySorted)
        //    {
        //        int compareXY(int a, int b)
        //        {
        //            float f = points[a].x - points[b].x;
        //
        //            if (Math.Abs(f) > tolerance)
        //            {
        //                return Math.Sign(f);
        //            }
        //
        //            f = points[a].y - points[b].y;
        //
        //            if (Math.Abs(f) > tolerance)
        //            {
        //                return Math.Sign(f);
        //            }
        //
        //            return 0;
        //        }
        //        cellPointsIndices.Sort(compareXY);
        //    }
        //    else
        //    {
        //        int compareYX(int a, int b)
        //        {
        //            float f = points[a].y - points[b].y;
        //
        //            if (Math.Abs(f) > tolerance)
        //            {
        //                return Math.Sign(f);
        //            }
        //
        //            f = points[a].x - points[b].x;
        //
        //            if (Math.Abs(f) > tolerance)
        //            {
        //                return Math.Sign(f);
        //            }
        //
        //            return 0;
        //        }
        //        cellPointsIndices.Sort(compareYX);
        //    }
        //    //Console.WriteLine(GetType() + ".SortCellPoints: ");
        //    //foreach (var index in cellPointsIndices)
        //    //{
        //    //    Console.WriteLine(GetType() + "." + index + " " + points[index]);
        //    //}
        //}
    }
}