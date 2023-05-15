using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

namespace Triangulation
{
    public class Triangulator
    {
        private const int SUPERTRIANGLES_MAX = 16;

        public PointGrid PointGrid => pointGrid;
        public EdgeInfo EdgeInfo => edgeInfo;
        public int TrianglesCount => trianglesCount;
        public int PointsOffset => pointsOffset;
        public int PointsCount { get => pointsCount; set => pointsCount = Math.Max(pointsOffset, value); }
        public int UsedPointsCount => PointsCount - UnusedPointsCount;
        public int UnusedPointsCount => unusedPointIndices.Count;
        public Vector2[] Points => points;
        public Triangle[] Triangles => triangles;
        public List<Triangle> CCTriangles => ccTriangles;
        public Triangle[] DebugSuperTriangles => debugSuperTriangles;
        public bool SuperCircumference { get; set; } = false;
        public bool Supermanent { get; set; } = false;
        public bool SuperPointsMismatch
        {
            get => !SuperCircumference && superTrianglesCount > 1 || SuperCircumference && superTrianglesCount <= 1 || pointsOffset != GetSuperPointsCount(superTrianglesCount);
        }
        public int SuperTrianglesCount => superTrianglesCount;
        public PointsSortingOrder PointsSortingOrder { get; set; } = default;

        protected readonly IExceptionThrower exceptionThrower = null;

        protected EdgeInfo edgeInfo = null;
        protected PointGrid pointGrid = null;

        protected readonly Triangle[] triangles = null;
        protected readonly Vector2[] points = null;
        protected readonly Triangle[] completedTriangles = null;
        protected readonly Dictionary<int, EdgeEntry> edgeDict = new();
        protected readonly List<int> unusedPointIndices = new();
        protected readonly List<int> initialPointIndices = new();
        protected readonly List<Triangle> ccTriangles = new();

        protected readonly EdgeEntry[] edgeBuffer = new EdgeEntry[3];
        protected readonly int[] edgeKeyBuffer = new int[3];
        protected readonly int[] indexBuffer = new int[3];

        protected readonly float pointTolerance = 0f;
        protected float circleTolerance = 0f;

        protected Bounds2 bounds = default;
        protected Triangle[] debugSuperTriangles = new Triangle[SUPERTRIANGLES_MAX];

        protected int pointsCount = 3;
        protected int pointsOffset = 3;
        protected int trianglesCount = 0;
        protected int completedTrianglesCount = 0;

        private int centerPointIndex = -1;
        private int superTrianglesCount = 1;
        private Circle superCircumCircle = default;

        public Triangulator(int pointsCapacity, IExceptionThrower exceptionThrower)
        {
            pointTolerance = Mathv.Epsilon;
            circleTolerance = pointTolerance;

            points = new Vector2[pointsCapacity];

            // Create triangle array
            int trianglesCapacity = pointsCapacity << 1;
            triangles = new Triangle[trianglesCapacity];
            completedTriangles = new Triangle[trianglesCapacity];

            this.exceptionThrower = exceptionThrower;

            SetupEdgeInfo();
        }

        protected virtual void SetupEdgeInfo()
        {
            edgeInfo = new EdgeInfo(points, exceptionThrower);
        }

        public void Initialize(Vector2 gridSize, Vector2Int xyCount)
        {
            pointGrid = new PointGrid(gridSize, xyCount);

            Circle.MinRadiusForSqrt = 250f * pointGrid.CellSizeMin;

            EdgeEntry.DegenerateDistance = 0.12f * pointGrid.CellSizeMin;

            circleTolerance = 0.01f * pointGrid.CellSizeMin;

            //Log.WriteLine(GetType() + ".Initialize: circleTolerance: " + circleTolerance);

            SetSuperCircumCirclePoints(Bounds2.MinMax(Vector2.Zero, gridSize), false);
        }

        public virtual void Load(SerializedTriangulator data)
        {
            var dataGridPoints = data.GridPoints;
            var dataSuperPoints = data.SuperPoints;
            var dataTriangles = data.Triangles;
            if (dataGridPoints.Length == 0 || dataSuperPoints.Length == 0)
            {
                return;
            }
            Clear();

            pointsOffset = dataSuperPoints.Length;
            pointsCount = dataGridPoints[^1].Index + 1;
            trianglesCount = dataTriangles.Length;

            var pointsXY = pointGrid.PointsXY;

            for (int i = 0; i < pointsOffset; i++)
            {
                edgeInfo.SetPointExternal(i, false);
                points[i] = dataSuperPoints[i].GetXY();
                pointsXY[i] = Vector2Int.NegativeOne;
            }
            for (int i = pointsOffset; i < pointsCount; i++)
            {
                edgeInfo.SetPointExternal(i, false);
                points[i] = Veconst2.NaN;
                pointsXY[i] = Vector2Int.NegativeOne;
            }
            pointGrid.SetGridPoints(dataGridPoints, points);

            for (int i = 0; i < pointsCount; i++)
            {
                if (Mathv.IsNaN(points[i]))
                {
                    unusedPointIndices.Add(i);
                }
                else
                {
                    initialPointIndices.Add(i);
                }
            }
            for (int i = 0; i < trianglesCount; i++)
            {
                triangles[i] = new Triangle(dataTriangles[i], points);
            }
            edgeInfo.AddEdgesToCounterDict(triangles, trianglesCount);
        }

        public void SetSuperCircumCirclePoints(Bounds2 bounds, bool usePointsOffset)
        {
            if (usePointsOffset)
            {
                superTrianglesCount = GetSuperTrianglesCount(pointsOffset);
                superCircumCircle = SetSuperCircumCircle(bounds, pointsOffset);

                GeomUtils.AddCirclePoints(points, 0, superCircumCircle.Center, superCircumCircle.Radius, pointsOffset);

                superCircumCircle = default;
            }
            else
            {
                int prevOffset = pointsOffset;

                superTrianglesCount = SuperCircumference ? SUPERTRIANGLES_MAX : 1;

                pointsOffset = GetSuperPointsCount(superTrianglesCount);
                superCircumCircle = SetSuperCircumCircle(bounds, pointsOffset);

                GeomUtils.AddCirclePoints(points, 0, superCircumCircle.Center, superCircumCircle.Radius, pointsOffset);

                if (prevOffset != pointsOffset)
                {
                    pointsCount = pointsOffset;
                }
                else
                {
                    pointsCount = Math.Max(pointsCount, pointsOffset);
                }
                //Log.WriteLine("SetSuperCircumCirclePoints: SuperCircumference: " + SuperCircumference + " pointsOffset: " + pointsOffset + " pointsCount: " + pointsCount);
            }
        }

        public bool GetPoint(int i, out Vector2 point)
        {
            point = points[i];
            return !Mathv.IsNaN(point);
        }

        public Vector2 GetPoint(int i)
        {
            return points[i];
        }

        public void GetInitialPointIndices(List<int> indices)
        {
            indices.Clear();
            indices.AddRange(initialPointIndices);
        }

        public bool TryAddPoint(Vector2 point, out int pointIndex, bool findClosestCell)
        {
            pointIndex = -1;
            return TryAddPointRefIndex(point, ref pointIndex, findClosestCell);
        }

        protected bool TryAddPointRefIndex(Vector2 point, ref int pointIndex, bool findClosestCell)
        {
            int savedIndex = -1;
            if (findClosestCell && pointGrid.GetClosestClearCell(point, out var cellXYI) ||
                !findClosestCell && pointGrid.CanAddPoint(point, out cellXYI, out savedIndex))
            {
                AddPointRefIndex(cellXYI, ref pointIndex);
                return true;
            }
            else
            {
                if (findClosestCell)
                {
                    exceptionThrower.ThrowException($"TryAddPointRefIndex: findClosestCell - NOT FOUND | pointIndex: {pointIndex}", ErrorCode.Undefined, pointIndex);
                }
                pointIndex = savedIndex;
                return false;
            }
        }

        private void AddPointRefIndex(Vector3Int cellXYI, ref int pointIndex)
        {
            int unusedIndicesCount = unusedPointIndices.Count;

            if (pointIndex >= 0)
            {
                if (unusedIndicesCount > 0)
                {
                    unusedPointIndices.Remove(pointIndex);
                }
                pointsCount = Math.Max(pointIndex + 1, pointsCount);
            }
            else if (unusedIndicesCount > 0)
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
            pointGrid.AddPoint(pointIndex, points, cellXYI);
        }

        public void Clear()
        {
            ClearTriangles();
            ClearPoints();
        }

        public bool Triangulate(List<Vector2> pointsList)
        {
            if (NotEnoughPoints(pointsList.Count))
            {
                return false;
            }
            Clear();

            pointsList.CopyTo(points);

            pointsCount = pointGrid.SetGridPoints(points, pointsOffset, pointsCount, PointsSortingOrder, out bounds, out bool xySorted);

            pointsList.Clear();
            for (int i = 0; i < pointsCount; i++)
            {
                pointsList.Add(points[i]);
            }
            return Triangulate(bounds, xySorted);
        }

        public virtual bool Triangulate()
        {
            RemoveUnusedPoints();
            ClearTriangles();

            if (NotEnoughPoints(pointsCount))
            {
                return false;
            }
            pointsCount = pointGrid.SetGridPoints(points, pointsOffset, pointsCount, PointsSortingOrder, out bounds, out bool xySorted);

            return Triangulate(bounds, xySorted);
        }

        protected virtual void ClearTriangles()
        {
            ClearDebugSuperTriangles();

            completedTrianglesCount = 0;
            trianglesCount = 0;

            edgeInfo.EdgeCounterDict.Clear();
        }

        protected void ClearPoints()
        {
            pointGrid.Clear();
            initialPointIndices.Clear();
            unusedPointIndices.Clear();
            pointsCount = pointsOffset;
            centerPointIndex = -1;
        }

        protected void ClearPoint(int pointIndex, bool addToUnused = true)
        {
            if (pointIndex < 0)
            {
                throw new Exception("ClearPoint: " + pointIndex);
            }
            pointGrid.ClearPoint(pointIndex, points);
            edgeInfo.SetPointExternal(pointIndex, false);
            points[pointIndex] = Veconst2.NaN;

            int lastIndex = pointsCount - 1;

            //Log.WriteLine(".ClearPoint:" + pointIndex + " pointsCount: " + pointsCount);

            if (addToUnused && pointIndex < lastIndex)
            {
                //if (unusedPointIndices.Contains(pointIndex))
                //{
                //    throw new Exception("ClearPoint: unusedPointIndices.Contains(pointIndex): " + pointIndex);
                //}
                //Log.WriteLine(".ClearPoint: pointsCount: " + pointsCount + " pointIndex: " + pointIndex + " added to unusedPointIndices");

                unusedPointIndices.Add(pointIndex);
            }
            else if (pointIndex == lastIndex)
            {
                pointsCount = lastIndex;
            }
            else if (addToUnused)
            {
                throw new Exception("ClearPoint: " + pointIndex + " / " + pointsCount);
            }
        }

        private int GetClosestPointIndex(Vector2 center)
        {
            return pointGrid.GetClosestPointIndex(center);
        }

        protected void ClearDebugSuperTriangles()
        {
            if (debugSuperTriangles[0].A < 0)
            {
                return;
            }
            for (int i = 0; i < superTrianglesCount; i++)
            {
                debugSuperTriangles[i] = Triangle.None;
            }
        }

        protected bool NotEnoughPoints(int pointsCount)
        {
            return !Supermanent && pointsCount < pointsOffset + 3 || pointsCount <= pointsOffset;
        }

        private Circle SetSuperCircumCircle(Bounds2 bounds, int circlePointsCount)
        {
            var center = bounds.Center;
            float r = (0.5f * bounds.Size).Length();
            int n = circlePointsCount;
            float alfa = MathF.PI / n;
            float R = 1.2f * r / MathF.Cos(alfa);
            superCircumCircle = new Circle(center, R * R);

            return superCircumCircle;
        }

        private static int GetSuperPointsCount(int supertrianglesCount)
        {
            if (supertrianglesCount <= 0)
            {
                return 0;
            }
            else if (supertrianglesCount == 1)
            {
                return 3;
            }
            else if (supertrianglesCount == 2)
            {
                throw new ArgumentOutOfRangeException("GetSuperPointsCount: supertrianglesCount : " + supertrianglesCount);
            }
            else
            {
                return supertrianglesCount;
            }
        }

        private static int GetSuperTrianglesCount(int superpointsCount)
        {
            if (superpointsCount < 3)
            {
                throw new ArgumentOutOfRangeException("GetSuperTrianglesCount: superpointsCount : " + superpointsCount);
            }
            else if (superpointsCount == 3)
            {
                return 1;
            }
            else
            {
                return superpointsCount;
            }
        }

        private bool Triangulate(Bounds2 bounds, bool xySorted)
        {
            if (NotEnoughPoints(pointsCount))
            {
                return false;
            }
            AddSuperTriangles(bounds);

            bool result;

            if (centerPointIndex >= pointsOffset)
            {
                edgeInfo.SetPointExternal(centerPointIndex, false);
                result = ProcessPoints(pointsOffset, centerPointIndex - 1, xySorted);
                result = result && ProcessPoints(centerPointIndex + 1, pointsCount - 1, xySorted);
            }
            else
            {
                result = ProcessPoints(pointsOffset, pointsCount - 1, xySorted);
            }
            if (result)
            {
                FindValidTriangles(IsTriangleValid);

                FindPointsIndices();
            }
            return result;
        }

        private bool ProcessPoints(int beg, int end, bool xySorted)
        {
            Func<Vector2, Circle, bool> canCompleteTriangle = xySorted ? CanCompleteTriangleForXYSorted : CanCompleteTriangleForYXSorted;
            Vector2 prevPoint;
            if (pointsOffset == pointsCount - 1)
            {
                if (beg != pointsOffset && end != pointsOffset)
                {
                    throw new ArgumentOutOfRangeException("ProcessPoints: beg: " + beg + " end: " + end + " pointsOffset: " + pointsOffset + " pointsCount: " + pointsCount);
                }
                prevPoint = new Vector2(float.MaxValue, float.MaxValue);
            }
            else
            {
                prevPoint = points[beg > pointsOffset ? beg - 1 : pointsCount - 1];
            }

            for (int i = beg; i <= end; i++)
            {
                var point = points[i];
                if (Mathv.Equals(point, prevPoint, pointTolerance))
                {
                    Log.WriteError(GetType() + ".Triangulate: point.Equals(prevPoint, tolerance): " + i);
                }
                else if (!ProcessPoint(i, canCompleteTriangle))
                {
                    ThrowCCTrianglesException(i);
                    return false;
                }
                prevPoint = point;
            }

            return true;
        }

        private bool ProcessPoint(int pointIndex, Func<Vector2, Circle, bool> canCompleteTriangle)
        {
            ccTriangles.Clear();
            edgeInfo.SetPointExternal(pointIndex, false);

            var point = points[pointIndex];
            int triangleEnd = trianglesCount - 1;

            for (int j = triangleEnd; j >= 0; j--)
            {
                var triangle = triangles[j];
                var cc = triangle.CircumCircle;

                if (cc.ContainsPoint(point, out var dr, out _, circleTolerance))
                {
                    ccTriangles.Add(triangle);
                    ReplaceTriangleWithEdges(j);
                }
                else if (canCompleteTriangle(dr, cc))
                {
                    completedTriangles[completedTrianglesCount++] = triangle;
                    triangles[j] = triangles[--trianglesCount];
                }
            }
            DiscardSeparatedCCTriangles();

            return ReplaceEdgesWithTriangles(pointIndex);
        }

        private bool CanCompleteTriangleForXYSorted(Vector2 dr, Circle cc)
        {
            return CanCompleteTriangle(dr.X, cc);
        }

        private bool CanCompleteTriangleForYXSorted(Vector2 dr, Circle cc)
        {
            return CanCompleteTriangle(dr.Y, cc);
        }

        private bool CanCompleteTriangle(float dl, Circle cc)
        {
            return dl > 0f && dl > cc.Radius + circleTolerance;
        }

        protected bool DiscardSeparatedTriangle(Triangle triangle)
        {
            bool separated = true;
            triangle.GetEdges(edgeBuffer);
            for (int i = 0; i < 3; i++)
            {
                int edgeKey = edgeKeyBuffer[i] = edgeInfo.GetEdgeKey(edgeBuffer[i]);
                if (edgeDict[edgeKey].Count != 1)
                {
                    separated = false;
                    break;
                }
            }
            if (separated)
            {
                Log.WriteWarning($"DiscardSeparatedTriangle: {triangle}");
                for (int i = 0; i < 3; i++)
                {
                    edgeDict.Remove(edgeKeyBuffer[i]);
                }
            }
            return separated;
        }

        private void DiscardSeparatedCCTriangles()
        {
            if (ccTriangles.Count > 1)
            {
                for (int i = ccTriangles.Count - 1; i >= 0; i--)
                {
                    var triangle = ccTriangles[i];
                    if (DiscardSeparatedTriangle(triangle))
                    {
                        ccTriangles.RemoveAt(i);
                        AddTriangle(triangle, out _);
                    }
                }
            }
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

        private void FindPointsIndices()
        {
            initialPointIndices.Clear();
            unusedPointIndices.Clear();

            if (trianglesCount <= 0)
            {
                return;
            }
            else if (Supermanent)
            {
                initialPointIndices.AddIntRange(pointsOffset, pointsCount - pointsOffset);
                return;
            }
            int beg = pointsCount;
            int end = beg + pointsCount - 1;

            for (int i = 0; i < pointsOffset; i++)
            {
                points[beg + i] = points[i];
            }
            for (int i = beg + pointsOffset; i <= end; i++)
            {
                points[i] = Veconst2.NaN;
            }
            for (int i = 0; i < trianglesCount; i++)
            {
                triangles[i].GetIndices(indexBuffer);
                for (int j = 0; j < 3; j++)
                {
                    int pointIndex = indexBuffer[j];
                    points[beg + pointIndex] = points[pointIndex];
                }
            }
            for (int i = beg + pointsOffset; i <= end; i++)
            {
                int pointIndex = i - beg;
                if (Mathv.IsNaN(points[i]))
                {
                    pointGrid.ClearPoint(pointIndex, points);
                    unusedPointIndices.Add(pointIndex);
                    points[pointIndex] = Veconst2.NaN;
                }
                else
                {
                    initialPointIndices.Add(pointIndex);
                }
            }
            int unusedLast = unusedPointIndices.Count - 1;
            int lastIndex = pointsCount - 1;
            while (unusedLast >= 0 && unusedPointIndices[unusedLast] >= lastIndex)
            {
                if (unusedPointIndices[unusedLast] == lastIndex)
                {
                    pointsCount--;
                    lastIndex = pointsCount - 1;
                }
                unusedPointIndices.RemoveAt(unusedLast);
                unusedLast = unusedPointIndices.Count - 1;
            }
            if (unusedPointIndices.Count > 0)
            {
                Log.WriteWarning($"{GetType().Name}.FindUnusedPoints: count: {unusedPointIndices.Count}");
                Log.PrintList(unusedPointIndices, GetType() + ".FindUnusedPoints: ");
            }
        }

        private void FindValidTriangles(Predicate<Triangle> predicate)
        {
            //Log.WriteLine(GetType() + ".FindValidTriangles: " + completedTrianglesCount + " " + trianglesCount);
            //edgeInfo.PrintEdgeCounterDict("FindValidTriangles");

            if (Supermanent)
            {
                Array.Copy(completedTriangles, 0, triangles, trianglesCount, completedTrianglesCount);
                trianglesCount += completedTrianglesCount;
                completedTrianglesCount = 0;
            }
            else
            {
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
                    else
                    {
                        edgeInfo.RemoveEdgesFromCounterDict(triangle);
                    }
                }
                completedTrianglesCount = 0;
            }
            //Log.WriteLine(GetType() + ".FindValidTriangles: " + completedTrianglesCount + " " + trianglesCount);
        }

        private bool IsTriangleValid(Triangle triangle)
        {
            return triangle.A >= pointsOffset && triangle.B >= pointsOffset && triangle.C >= pointsOffset;
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

            foreach (var kvp in edgeDict)
            {
                var edge = kvp.Value;
                if (edge.Count == 1)
                {
                    if (!AddTriangle(edge.A, edge.B, pointIndex, out _))
                    {
                        Log.WriteError("ReplaceEdgesWithTriangles: pointIndex: " + pointIndex + " pointsCount: " + pointsCount);
                        edgeDict.Clear();
                        trianglesCount = trianglesCountPrev;
                        return false;
                    }
                }
            }
            edgeDict.Clear();
            return true;
        }

        private void ThrowCCTrianglesException(int pointIndex)
        {
            string triangleToString(Triangle t)
            {
                return t + " " + t.CircumCircle.ContainsPoint(points[pointIndex], circleTolerance).ToString();
            }
            for (int i = 0; i < ccTriangles.Count; i++)
            {
                var ccTriangle = ccTriangles[i];
                ccTriangle.CircumCircle.Filled = true;
                ccTriangle.FillColor = Color.Red;
                triangles[trianglesCount++] = ccTriangle;
            }
            Log.PrintList(ccTriangles, "ThrowCCTrianglesException: ccTriangles: ");
            Log.PrintArrayToString(triangles, trianglesCount, triangleToString, "ThrowCCTrianglesException: ");
            exceptionThrower.ThrowException("BASE TRIANGULATION FAILED", ErrorCode.BaseTriangulationFailed, pointIndex);
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
            int edgeKey = edgeInfo.GetEdgeKey(edgeA, edgeB);
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

        private void AddSuperTriangles(Bounds2 bounds)
        {
            if (pointsOffset < 3)
            {
                throw new NotImplementedException("AddSuperTriangles: pointsOffset: " + pointsOffset);
            }
            bool mismatch = SuperPointsMismatch || !superCircumCircle.SizeValid;
            if (mismatch)
            {
                SetSuperCircumCirclePoints(bounds, true);
                //Log.WriteLine("AddSuperTriangles: SuperPointsMismatch:" + SuperPointsMismatch + " pointsOffset: " + pointsOffset + " superTrianglesCount: " + superTrianglesCount + " pointsCount: " + pointsCount + " superCircumCircle: " + superCircumCircle);
            }
            if (superTrianglesCount > 1)
            {
                var center = mismatch ? bounds.Center : superCircumCircle.Center;
                centerPointIndex = GetClosestPointIndex(center);

                int triangleIndex;
                for (int i = 0; i < superTrianglesCount - 1; i++)
                {
                    AddTriangle(centerPointIndex, i, i + 1, out triangleIndex);
                    debugSuperTriangles[i] = triangles[triangleIndex];
                }
                AddTriangle(centerPointIndex, superTrianglesCount - 1, 0, out triangleIndex);
                debugSuperTriangles[superTrianglesCount - 1] = triangles[triangleIndex];

                //Log.PrintArray(debugSuperTriangles, superTrianglesCount, "AddSuperTriangles: triangles:");
                //Log.PrintArray(points, pointsOffset, "AddSuperTriangles: points:");
            }
            else
            {
                AddTriangle(0, 1, 2, out int triangleIndex);
                debugSuperTriangles[0] = triangles[triangleIndex];
            }
        }

        private bool AddTriangle(int a, int b, int c, out int triangleIndex)
        {
            return AddTriangle(new Triangle(a, b, c, points), out triangleIndex);
        }

        private bool AddTriangle(Triangle triangle, out int triangleIndex)
        {
            if (edgeInfo.AddEdgesToCounterDict(triangle))
            {
                triangles[triangleIndex = trianglesCount++] = triangle;
                return true;
            }
            else
            {
                triangleIndex = -1;
                return false;
            }
        }

        private void RemoveTriangleAt(int index)
        {
            if (index >= 0 && index < trianglesCount)
            {
                edgeInfo.RemoveEdgesFromCounterDict(triangles[index]);
                triangles[index] = triangles[--trianglesCount];
            }
        }
    }
}
