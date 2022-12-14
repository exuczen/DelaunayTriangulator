using System;
using System.Collections.Generic;

namespace Triangulation
{
    public class Triangulator
    {
        public int TrianglesCount => trianglesCount;
        public int PointsCount { get => pointsCount; set => pointsCount = value; }
        public Vector2[] Points => points;
        public Triangle[] Triangles => triangles;

        protected Triangle[] triangles = null;
        protected readonly Vector2[] points = null;
        protected readonly Triangle[] completedTriangles = null;
        protected readonly Dictionary<int, EdgeEntry> edgeDict = new Dictionary<int, EdgeEntry>();
        protected readonly Dictionary<int, int> edgeCounterDict = new Dictionary<int, int>();
        protected readonly List<int> unusedPointIndices = new List<int>();
        protected readonly List<Triangle> ccTriangles = new List<Triangle>();

        protected readonly float tolerance = 0f;
        protected float circleTolerance = 0f;

        protected Bounds2 bounds = default;
        protected Triangle supertriangle = Triangle.None;
        protected Vector2[] supertriangleVerts = new Vector2[3];

        protected int pointsCount = 0;
        protected int trianglesCount = 0;
        protected int completedTrianglesCount = 0;

        private readonly List<int> usedPointIndices = new List<int>();

        protected readonly EdgeEntry[] edgeBuffer = new EdgeEntry[3];
        protected readonly int[] edgeKeyBuffer = new int[3];
        protected readonly int[] indexBuffer = new int[3];

        public Triangulator(int pointsCapacity, float tolerance)
        {
            this.tolerance = tolerance > 0f ? tolerance : Vector2.Epsilon;
            circleTolerance = this.tolerance;

            points = new Vector2[pointsCapacity];

            // Create triangle array
            int trianglesCapacity = pointsCapacity << 1;
            triangles = new Triangle[trianglesCapacity];
            completedTriangles = new Triangle[trianglesCapacity];
        }

        public Vector2 GetPoint(int i)
        {
            return points[i];
        }

        public void AddPoint(Vector2 point, out int pointIndex)
        {
            int unusedIndicesCount = unusedPointIndices.Count;
            if (unusedIndicesCount > 0)
            {
                int unusedLast = unusedIndicesCount - 1;
                pointIndex = unusedPointIndices[unusedLast];
                unusedPointIndices.RemoveAt(unusedLast);
                pointsCount = Math.Max(pointIndex + 1, pointsCount);
            }
            else
            {
                pointIndex = pointsCount++;
            }
            points[pointIndex] = point;
        }

        public void Clear()
        {
            ClearTriangles();
            ClearPoints();
        }

        public bool Triangulate(List<Vector2> pointsList)
        {
            if (pointsList.Count < 3)
            {
                return false;
            }
            Clear();

            bounds = Bounds2.GetBounds(pointsList);
            pointsList.Sort(GetPointsComparer(bounds, out bool xySorted));

            SetSortedPoints(pointsList);

            return Triangulate(bounds, xySorted);
        }

        public virtual bool Triangulate()
        {
            if (pointsCount < 3)
            {
                return false;
            }
            RemoveUnusedPoints();
            ClearTriangles();

            bounds = Bounds2.GetBounds(points, pointsCount);
            Array.Sort(points, 0, pointsCount, GetPointsComparer(bounds, out bool xySorted));

            SetSortedPoints(out bounds);

            return Triangulate(bounds, xySorted);
        }

        protected virtual void ClearTriangles()
        {
            completedTrianglesCount = 0;
            trianglesCount = 0;
            supertriangle = Triangle.None;

            edgeCounterDict.Clear();
        }

        protected virtual void ClearPoints()
        {
            unusedPointIndices.Clear();
            pointsCount = 0;
        }

        protected virtual void ClearPoint(int pointIndex, bool addToUnused = true)
        {
            if (pointIndex < 0)
            {
                throw new Exception("ClearPoint: " + pointIndex);
            }
            points[pointIndex] = default;
            int lastIndex = pointsCount - 1;
            if (addToUnused && pointIndex < lastIndex)
            {
                //if (unusedPointIndices.Contains(pointIndex))
                //{
                //    throw new Exception("ClearPoint: unusedPointIndices.Contains(pointIndex): " + pointIndex);
                //}
                unusedPointIndices.Add(pointIndex);
            }
            else if (pointIndex == lastIndex)
            {
                pointsCount--;
            }
            else if (addToUnused)
            {
                throw new Exception("ClearPoint: " + pointIndex + " / " + pointsCount);
            }
        }

        protected virtual void SetSortedPoints(out Bounds2 bounds) { bounds = this.bounds; }

        protected virtual void SetSortedPoints(List<Vector2> pointsList)
        {
            for (int i = pointsList.Count - 1; i > 0; i--)
            {
                if (pointsList[i].Equals(pointsList[i - 1], tolerance))
                {
                    pointsList.RemoveAt(i);
                }
            }
            pointsCount = pointsList.Count;
            pointsList.CopyTo(points, 0);
        }

        private bool Triangulate(Bounds2 bounds, bool xySorted)
        {
            if (pointsCount < 3)
            {
                return false;
            }
            AddSuperTriangle(bounds);

            var prevPoint = points[pointsCount - 1];

            for (int i = 0; i < pointsCount; i++)
            {
                var point = points[i];
                if (point.Equals(prevPoint, tolerance))
                {
                    Log.WriteError(GetType() + ".Triangulate: point.Equals(prevPoint, tolerance): " + i);
                    continue;
                }
                if (!ProcessPoint(i, xySorted))
                {
                    return false;
                }
                prevPoint = point;
            }

            FindValidTriangles(IsTriangleValid);

            FindUnusedPoints();

            ClearUnusedPoints();

            return true;
        }

        private IComparer<Vector2> GetPointsComparer(Bounds2 bounds, out bool xySort)
        {
            IComparer<Vector2> pointsComparer;
            Vector2 boundsSize = bounds.Size;
            xySort = boundsSize.x > boundsSize.y;
            if (xySort)
            {
                // Sort points by X (firstly), Y (secondly)
                pointsComparer = new PointsXYComparer(tolerance);
            }
            else // yxSorted
            {
                // Sort points by Y (firstly), X (secondly)
                pointsComparer = new PointsYXComparer(tolerance);
            }
            return pointsComparer;
        }

        private bool ProcessPoint(int pointIndex, bool xySorted)
        {
            ccTriangles.Clear();
            var point = points[pointIndex];
            int triangleEnd = trianglesCount - 1;

            for (int j = triangleEnd; j >= 0; j--)
            {
                var triangle = triangles[j];
                var cc = triangle.CircumCircle;

                if (cc.ContainsPoint(point, out var dr, out var sqrDr, circleTolerance))
                {
                    ccTriangles.Add(triangle);
                    ReplaceTriangleWithEdges(j);
                }
                else
                {
                    bool completed;
                    if (xySorted)
                    {
                        completed = dr.x > 0f && sqrDr.x > cc.SqrRadius + circleTolerance;
                    }
                    else
                    {
                        completed = dr.y > 0f && sqrDr.y > cc.SqrRadius + circleTolerance;
                    }
                    if (completed)
                    {
                        completedTriangles[completedTrianglesCount++] = triangle;
                        RemoveTriangleAt(j);
                    }
                }
            }
            return ReplaceEdgesWithTriangles(pointIndex);
        }

        protected void ForEachEdge(Triangle triangle, Action<int, EdgeEntry> action)
        {
            triangle.GetEdges(edgeBuffer);
            for (int i = 0; i < 3; i++)
            {
                action(GetEdgeKey(edgeBuffer[i]), edgeBuffer[i]);
            }
        }

        private void RemoveEdgesFromCounterDict(Triangle triangle)
        {
            triangle.GetIndices(indexBuffer);
            ForEachEdge(triangle, (edgeKey, edge) => {
                if (edgeCounterDict.ContainsKey(edgeKey))
                {
                    if (--edgeCounterDict[edgeKey] <= 0)
                    {
                        edgeCounterDict.Remove(edgeKey);
                    }
                }
            });
        }

        private bool AddEdgesToCounterDict(Triangle triangle)
        {
            triangle.GetEdges(edgeBuffer);
            for (int i = 0; i < 3; i++)
            {
                if (IsEdgeInternal(edgeBuffer[i], out int edgeKey))
                {
                    string message = GetType() + ".AddEdgesToCounterDict: IsEdgeInternal: " + edgeBuffer[i];
                    Log.WriteError(message);
                    return false;
                }
                edgeKeyBuffer[i] = edgeKey;
            }
            for (int i = 0; i < 3; i++)
            {
                int edgeKey = edgeKeyBuffer[i];
                if (edgeCounterDict.ContainsKey(edgeKey))
                {
                    edgeCounterDict[edgeKey]++;
                }
                else
                {
                    edgeCounterDict.Add(edgeKey, 1);
                }
            }
            return true;
        }

        private bool IsEdgeInternal(int edgeKey)
        {
            return edgeCounterDict.TryGetValue(edgeKey, out int edgeCount) && edgeCount == 2;
        }

        private bool IsEdgeInternal(EdgeEntry edge, out int edgeKey)
        {
            return IsEdgeInternal(edgeKey = GetEdgeKey(edge));
        }

        private void RemoveUnusedPoints()
        {
            //Log.WriteLine(GetType() + ".RemoveUnusedPoints: unusedPointIndices.Count: " + unusedPointIndices.Count + " pointsCount: " + pointsCount);
            if (unusedPointIndices.Count > 0 && pointsCount > 0)
            {
                unusedPointIndices.Sort();
                for (int i = unusedPointIndices.Count - 1; i >= 0; i--)
                {
                    int pointIndex = unusedPointIndices[i];
                    points[pointIndex] = points[--pointsCount];
                }
            }
            unusedPointIndices.Clear();
        }

        private void FindUnusedPoints()
        {
            usedPointIndices.Clear();
            unusedPointIndices.Clear();

            int[] indexBuffer = new int[3];

            for (int i = 0; i < trianglesCount; i++)
            {
                triangles[i].GetIndices(indexBuffer);
                for (int j = 0; j < 3; j++)
                {
                    usedPointIndices.Add(indexBuffer[j]);
                }
            }
            usedPointIndices.Sort();

            if (usedPointIndices.Count > 0)
            {
                for (int l = 0; l < usedPointIndices[0]; l++)
                {
                    unusedPointIndices.Add(l);
                    Log.WriteLine(GetType() + ".FindUnusedPoints: " + l);
                }
                int k = 0;
                while (k < usedPointIndices.Count - 1)
                {
                    while (k < usedPointIndices.Count - 1 && usedPointIndices[k + 1] - usedPointIndices[k] <= 1)
                    {
                        k++;
                    }
                    if (k < usedPointIndices.Count - 1)
                    {
                        for (int l = usedPointIndices[k] + 1; l < usedPointIndices[k + 1]; l++)
                        {
                            unusedPointIndices.Add(l);
                            Log.WriteLine(GetType() + ".FindUnusedPoints: " + l);
                        }
                    }
                    k++;
                }
                int lastUsedIndex = usedPointIndices.Count - 1;
                for (int l = usedPointIndices[lastUsedIndex] + 1; l < pointsCount; l++)
                {
                    unusedPointIndices.Add(l);
                    //Log.WriteLine(GetType() + ".FindUnusedPoints: " + l);
                }
                usedPointIndices.Clear();
            }
            else
            {
                for (int i = 0; i < pointsCount; i++)
                {
                    unusedPointIndices.Add(i);
                }
            }
        }

        private void ClearUnusedPoints()
        {
            unusedPointIndices.Sort();
            for (int i = unusedPointIndices.Count - 1; i >= 0; i--)
            {
                ClearPoint(unusedPointIndices[i], false);
            }
            if (pointsCount == 0)
            {
                unusedPointIndices.Clear();
            }
        }

        private void FindValidTriangles(Predicate<Triangle> predicate)
        {
            //Log.WriteLine(GetType() + ".FindValidTriangles: " + completedTrianglesCount + " " + trianglesCount);

            Array.Copy(triangles, 0, completedTriangles, completedTrianglesCount, trianglesCount);
            completedTrianglesCount += trianglesCount;
            trianglesCount = 0;

            for (int i = 0; i < completedTrianglesCount; i++)
            {
                var triangle = completedTriangles[i];
                if (predicate(triangle))
                {
                    triangles[trianglesCount++] = triangle;
                }
            }
            completedTrianglesCount = 0;
        }

        private bool IsTriangleValid(Triangle triangle)
        {
            return triangle.A < pointsCount && triangle.B < pointsCount && triangle.C < pointsCount;
        }

        protected void AddTriangleEdges(Triangle triangle)
        {
            AddEdge(triangle.A, triangle.B);
            AddEdge(triangle.B, triangle.C);
            AddEdge(triangle.C, triangle.A);
        }

        private void ReplaceTriangleWithEdges(int triangleIndex)
        {
            var triangle = triangles[triangleIndex];

            //Log.WriteLine(GetType() + ".ReplaceTriangleWithEdges: " + triangleIndex + " " + triangle);

            AddTriangleEdges(triangle);

            RemoveTriangleAt(triangleIndex);
        }

        private bool ReplaceEdgesWithTriangles(int pointIndex)
        {
            int trianglesCountPrev = trianglesCount;

            string triangleToString(Triangle t)
            {
                return t + " " + t.CircumCircle.ContainsPoint(points[pointIndex], circleTolerance, out float sqrDelta).ToString() + " sqrDelta: " + sqrDelta;
            }
            foreach (var kvp in edgeDict)
            {
                var edge = kvp.Value;
                if (edge.Count == 1)
                {
                    if (!AddTriangle(edge.A, edge.B, pointIndex))
                    {
                        Log.WriteError("ReplaceEdgesWithTriangles: pointIndex: " + pointIndex + " pointsCount: " + pointsCount);
                        Log.PrintList(ccTriangles, "ReplaceEdgesWithTriangles: ccTriangles:");
                        edgeDict.Clear();
                        trianglesCount = trianglesCountPrev;
                        for (int i = 0; i < ccTriangles.Count; i++)
                        {
                            var ccTriangle = ccTriangles[i];
                            ccTriangle.CircumCircle.Filled = true;
                            ccTriangle.FillColor = Color.Red;
                            triangles[trianglesCount++] = ccTriangle;
                        }
                        ccTriangles.Clear();
                        Log.PrintTriangles(triangles, trianglesCount, triangleToString, "ReplaceEdgesWithTriangles: ");
                        return false;
                    }
                }
            }
            edgeDict.Clear();
            return true;
        }

        protected void ForEachEdgeInDict(Action<int, EdgeEntry> action)
        {
            foreach (var kvp in edgeDict)
            {
                action(kvp.Key, kvp.Value);
            }
        }

        private void AddEdge(int edgeA, int edgeB)
        {
            int edgeKey = GetEdgeKey(edgeA, edgeB);
            if (edgeDict.ContainsKey(edgeKey))
            {
                var edge = edgeDict[edgeKey];
                edge.Count++;
                edgeDict[edgeKey] = edge;
            }
            else
            {
                edgeDict.Add(edgeKey, new EdgeEntry(edgeA, edgeB));
            }
        }

        private int GetEdgeKey(int edgeA, int edgeB)
        {
            if (edgeB > edgeA)
            {
                (edgeB, edgeA) = (edgeA, edgeB);
            }
            return edgeA * points.Length + edgeB;
        }

        private int GetEdgeKey(EdgeEntry edge)
        {
            return edge.A * points.Length + edge.B;
        }

        private void AddSuperTriangle(Bounds2 bounds)
        {
            var center = bounds.Center;
            var halfSize = 0.5f * bounds.Size;

            //Log.WriteLine(GetType() + ".AddSuperTriangle: center: " + center + " halfSize: " + halfSize + " bounds: " + bounds.ToString("f4"));

            float r = halfSize.Length * 1.5f;
            float a = MathF.Sqrt(3f) * r;

            supertriangleVerts[0] = center + new Vector2(-a, -r);
            supertriangleVerts[1] = center + new Vector2(0f, 2f * r);
            supertriangleVerts[2] = center + new Vector2(+a, -r);

            for (int i = 0; i < 3; i++)
            {
                points[pointsCount + i] = supertriangleVerts[i];
            }

            AddTriangle(pointsCount + 0, pointsCount + 1, pointsCount + 2);

            supertriangle = new Triangle(0, 1, 2);
        }

        private bool AddTriangle(int a, int b, int c)
        {
            var triangle = new Triangle(a, b, c, points);
            if (AddEdgesToCounterDict(triangle))
            {
                triangles[trianglesCount++] = triangle;
                return true;
            }
            return false;
        }

        private void RemoveTriangleAt(int index)
        {
            if (index >= 0 && index < trianglesCount)
            {
                RemoveEdgesFromCounterDict(triangles[index]);
                triangles[index] = triangles[--trianglesCount];
            }
        }
    }
}
