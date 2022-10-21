//#define DEBUG_DEGENERATE_TRIANGLES

using System;
using System.Collections.Generic;

namespace Triangulation
{
    public class IncrementalTriangulator : Triangulator
    {
        public IExceptionThrower ExceptionThrower { get; set; }

        public TriangleGrid TriangleGrid => triangleGrid;
        public PointGrid PointGrid => pointGrid;
        public EdgeInfo EdgeInfo => baseEdgeInfo;
        public List<int> CellPointsIndices => cellPointsIndices;
        public Polygon CellPolygon => cellPolygon;

        public bool PointsValidation = false;
        public bool EdgesValidation = false;

        private readonly List<int> cellPointsIndices = new List<int>();
        private readonly List<int> cellTrianglesIndices = new List<int>();
        private readonly Polygon cellPolygon = new Polygon(true);

        private readonly List<int> pointsToClear = new List<int>();

        private readonly TriangleSet baseTriangleSet = null;
        private readonly EdgeInfo baseEdgeInfo = null;
        private readonly Triangle[] addedTriangles = null;
        private readonly EdgeInfo addedEdgeInfo = null;

        private readonly int[] indexBuffer = new int[3];
        private readonly bool internalOnly = false;

        private int addedTrianglesCount = 0;

        private TriangleGrid triangleGrid = null;
        private PointGrid pointGrid = null;

        public IncrementalTriangulator(int pointsCapacity, float tolerance, bool internalOnly) : base(pointsCapacity, tolerance)
        {
            baseTriangleSet = new TriangleSet(triangles, points);
            baseEdgeInfo = baseTriangleSet.EdgeInfo;
            addedTriangles = new Triangle[triangles.Length];
            addedEdgeInfo = new EdgeInfo(triangles.Length << 1, null, points);
            this.internalOnly = internalOnly;
        }

        public override bool Triangulate()
        {
            if (base.Triangulate())
            {
                AddTrianglesToBaseTriangles(triangles, trianglesCount, Color.FloralWhite, false);

                baseEdgeInfo.FindExternalEdges(pointsCount, ExceptionThrower);

                //baseEdgeInfo.PrintPointsExternal(pointsCount);

                ValidateTriangulation(false, PointsValidation);

                return true;
            }
            return false;
        }

        public void Initialize(Vector2 gridSize, bool triangulate, int triangleGridDivs, int pointGridDivsMlp)
        {
            if (baseTriangleSet == null)
            {
                throw new Exception("baseTriangleSet == null");
            }
            triangleGrid = new TriangleGrid(baseTriangleSet, gridSize, triangleGridDivs);

            cellPolygon.Tolerance = triangleGrid.CellTolerance;

            pointGrid = new PointGrid(gridSize, triangleGrid.XYCount * pointGridDivsMlp);

#if DEBUG_DEGENERATE_TRIANGLES
            Triangle.SetMinAngle(15f);
#endif
            if (triangulate)
            {
                Triangulate();
            }
        }

        public bool TryAddPoint(Vector2 point, out int pointIndex)
        {
            if (pointGrid.CanAddPoint(point, out var cellXYI, out int savedIndex))
            {
                base.AddPoint(point, out pointIndex);
                pointGrid.AddPoint(pointIndex, points, cellXYI);
                return true;
            }
            else
            {
                pointIndex = savedIndex;
                return false;
            }
        }

        public bool TryRemovePointFromTriangulation(Vector2 point, out int pointIndex)
        {
            if (pointGrid.GetPointIndex(point, out pointIndex) && pointIndex >= 0)
            {
                RemovePointFromTriangulation(pointIndex);
                return true;
            }
            return false;
        }

        public void RemovePointFromTriangulation(int pointIndex)
        {
            if (pointIndex < 0 || pointIndex >= pointsCount)
            {
                throw new Exception("RemovePoint: " + pointIndex);
            }
            var point = points[pointIndex];

            if (trianglesCount <= 0 || pointsCount < 3)
            {
                ClearPoint(pointIndex);
            }
            else
            {
                if (triangleGrid.GetCell(point, out var cell, out _))
                {
                    ClearLastPointData();
                    ProcessClearPoint(pointIndex, cell);
                    ValidateTriangulation(EdgesValidation, PointsValidation);
                }
            }
        }

        public bool AddPointToTriangulation(Vector2 point, out int pointIndex)
        {
            if (trianglesCount <= 0 || pointsCount < 3)
            {
                if (!TryAddPoint(point, out pointIndex))
                {
                    if (pointIndex >= 0)
                    {
                        ClearPoint(pointIndex);
                    }
                    return false;
                }
                else
                {
                    Triangulate();
                    return true;
                }
            }
            Log.WriteLine(GetType() + ".AddPointToTriangulation: *************");
            bool result;

            if (triangleGrid.GetCell(point, out var cell, out var cellXY))
            {
                ClearLastPointData();
                bool isInTriangle = false;

                if (!TryAddPoint(point, out pointIndex))
                {
                    if (pointIndex >= 0)
                    {
                        ProcessClearPoint(pointIndex, cell);
                    }
                    result = false;
                }
                else
                {
                    point = points[pointIndex];
                    cellPointsIndices.Add(pointIndex);

                    ForEachTriangleInCell(cell, (triangle, triangleIndex) => {
                        if (triangle.CircumCircle.ContainsPoint(point))
                        {
                            AddCellTriangleToProcess(triangleIndex);

                            if (!isInTriangle && triangle.ContainsPoint(point, points, false))
                            {
                                isInTriangle = true;
                                Log.WriteLine(GetType() + ".AddPoints: isInTriangle: " + triangle);
                            }
                        }
                    });
                    Log.WriteLine(GetType() + ".AddPointToTriangulation: " + pointIndex + " isInTriangle: " + isInTriangle + " cellTrianglesIndices.Count: " + cellTrianglesIndices.Count + " cellPointsIndices.Count: " + cellPointsIndices.Count);
                    if (!isInTriangle)
                    {
                        if (internalOnly)
                        {
                            throw new Exception(GetType() + ".AddPoints: internalOnly && !isInTriangle");
                        }
                        result = ProcessExternalPoint(cellXY, pointIndex);
                    }
                    else
                    {
                        result = ProcessInternalPoint(pointIndex);
                    }
                    if (!result)
                    {
                        ClearPoint(pointIndex);
                    }
                }
                //PrintTriangles();
                //baseEdgeInfo.PrintPointsExternal(pointsCount);

                ValidateTriangulation(EdgesValidation, PointsValidation);
            }
            else
            {
                pointIndex = -1;
                result = false;
            }
            return result;
        }

        protected override void ClearTriangles()
        {
            baseTriangleSet.Clear();
            triangleGrid.Clear();
            ClearLastPointData();
            base.ClearTriangles();
        }

        protected override void ClearPoints()
        {
            pointGrid.Clear();
            base.ClearPoints();
        }

        protected override void SetSortedPoints()
        {
            pointGrid.SetPoints(points, pointsCount);
        }

        protected override void SetSortedPoints(List<Vector2> pointsList)
        {
            pointGrid.SetPoints(pointsList);
            pointsCount = pointsList.Count;
            pointsList.CopyTo(points, 0);
        }

        protected override void ClearPoint(int pointIndex, bool addToUnused = true)
        {
            pointGrid.ClearPoint(pointIndex, points);
            base.ClearPoint(pointIndex, addToUnused);
        }

        private void ClearPoints(List<int> pointsIndices)
        {
            pointsIndices.Sort();
            for (int i = pointsIndices.Count - 1; i >= 0; i--)
            {
                ClearPoint(pointsIndices[i]);
            }
            pointsIndices.Clear();
        }

        private void ForEachTriangleInCell(Vector2 point, Action<Triangle, int> action)
        {
            if (triangleGrid.GetCell(point, out var cell))
            {
                ForEachTriangleInCell(cell, action);
            }
        }

        private void ForEachTriangleInCell(TriangleCell cell, Action<Triangle, int> action)
        {
            foreach (long triangleKey in cell.TriangleKeys)
            {
                ref var triangle = ref GetBaseTriangleRef(triangleKey, out int triangleIndex);
                action(triangle, triangleIndex);
            }
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

        private bool ValidateTriangulation(bool validateEdges, bool validatePoints)
        {
            if (!validateEdges && !validatePoints)
            {
                return true;
            }
            bool edgesValid = !validateEdges || baseTriangleSet.EdgeFlipper.Validate();
            bool circleOverlapsPoint = false;
            if (validatePoints)
            {
                for (int i = 0; i < pointsCount; i++)
                {
                    var point = points[i];
                    ForEachTriangleInCell(point, (triangle, triangleIndex) => {
                        bool isPointExternal = baseEdgeInfo.IsPointExternal(i);
                        if (!isPointExternal && !triangle.HasVertex(i) && triangle.CircumCircle.ContainsPoint(point, 1f) && !unusedPointIndices.Contains(i))
                        {
                            Log.WriteError(GetType() + ".ValidateTriangulation: point " + i + " of triangle " + GetFirstTriangleWithVertex(i) + " inside triangle: " + triangle + " isPointExternal: " + isPointExternal);
                            //baseEdgeInfo.PrintExternalEdges("ValidateTriangulation: ");
                            circleOverlapsPoint = true;
                        }
                    });
                    if (circleOverlapsPoint)
                    {
                        break;
                    }
                }
            }
            bool result = edgesValid && !circleOverlapsPoint;
            if (!result)
            {
                ExceptionThrower.ThrowException("!ValidateTriangulation");
            }
            return result;
        }

        private void ProcessClearPoint(int pointIndex, TriangleCell cell)
        {
            ForEachTriangleInCell(cell, (triangle, triangleIndex) => {
                if (triangle.HasVertex(pointIndex))
                {
                    AddCellTriangleToProcess(triangleIndex);
                }
            });
            if (cellTrianglesIndices.Count == 0)
            {
                Log.WriteLine(GetType() + ".ProcessClearPoint: cellTrianglesIndices.Count == 0 for pointIndex: " + pointIndex);
                return;
            }
            bool isPointExternal = baseEdgeInfo.IsPointExternal(pointIndex);

            Log.WriteLine(GetType() + ".ProcessClearPoint: " + pointIndex + " isPointExternal: " + isPointExternal);

            AddCellTrianglesEdges();

            AddTrianglesOnClearPoint(pointIndex, out bool findExternalEdges, out bool updateTriangleDicts);

            ReplaceCellTriangles(addedTriangles, addedTrianglesCount);

            if (updateTriangleDicts)
            {
                baseEdgeInfo.UpdateTriangleDicts(addedEdgeInfo);
            }
            ClearPoints(pointsToClear);

            if (trianglesCount == 0)
            {
                ClearPoints();
            }
            addedEdgeInfo.Clear();

            if (findExternalEdges)
            {
                baseEdgeInfo.FindExternalEdges(pointsCount, ExceptionThrower);
            }
        }

        private void AddTrianglesOnClearPoint(int pointIndex, out bool findExternalEdges, out bool updateTriangleDicts)
        {
            findExternalEdges = false;
            updateTriangleDicts = false;

            addedEdgeInfo.JoinSortExternalEdges(ExceptionThrower);

            addedEdgeInfo.PrintExternalEdges("AddTrianglesOnClearPoint");

            cellPolygon.SetFromExternalEdges(addedEdgeInfo, points);

            if (baseEdgeInfo.IsPointExternal(pointIndex))
            {
                //addedEdgeInfo.Clear();
                //return;

                var peak = cellPolygon.GetPeak(pointIndex, out int peakIndex);

                bool canClipPeak = cellPolygon.CanClipPeak(peakIndex, points, out _);

                if (!canClipPeak && cellPolygon.PeakCount > 3)
                {
                    var peakExtEdgesRange = addedEdgeInfo.GetPeakExternalEdgesRange(peak, baseEdgeInfo, pointsToClear);

                    addedTrianglesCount = cellPolygon.TriangulateFromConcavePeaks(peakExtEdgesRange, addedTriangles, points, addedEdgeInfo);

                    baseEdgeInfo.ReplacePeakExternalEdges(peakExtEdgesRange, addedEdgeInfo);

                    updateTriangleDicts = true;
                }
                else
                {
                    if (canClipPeak)
                    {
                        cellPolygon.ClipPeak(peakIndex, -1, points);

                        cellPolygon.Triangulate(addedTriangles, ref addedTrianglesCount, points);
                    }
                    baseEdgeInfo.ClipPeakExternalEdges(peak, pointsToClear);

                    updateTriangleDicts = addedTrianglesCount == 0;
                }
            }
            else
            {
                cellPolygon.Triangulate(addedTriangles, ref addedTrianglesCount, points);

                pointsToClear.Add(pointIndex);
            }
        }

        private bool ProcessInternalPoint(int addedPointIndex, int extEdgeIndex = -1)
        {
            if (cellTrianglesIndices.Count == 0)
            {
                Log.WriteLine(GetType() + ".ProcessInternalPoint: cellTrianglesIndices.Count == 0");
                return false;
            }
            AddCellTrianglesEdges();

            if (ReplaceEdgesWithTriangles(addedPointIndex, extEdgeIndex))
            {
                ReplaceCellTriangles(addedTriangles, addedTrianglesCount);

                return addedTrianglesCount > 0;
            }
            return false;
        }

        private bool ProcessExternalPoint(Vector2Int cellXY, int addedPointIndex)
        {
            var firstExtEdge = EdgeEntry.None;

            bool cellPredicate(TriangleCell c)
            {
                return triangleGrid.GetFirstExternalEdgeInCell(c, baseEdgeInfo, out firstExtEdge, out _);
            }
            if (triangleGrid.FindClosestCellWithPredicate(cellXY.x, cellXY.y, out var cell, cellPredicate))
            {
                if (baseEdgeInfo.GetOppositeExternalEdgesRange(addedPointIndex, firstExtEdge, out IndexRange extEdgesRange, out bool pointOnExtEdge))
                {
                    if (AddExternalPointTriangles(addedPointIndex, ref extEdgesRange, out EdgePeak loopPeak, out bool processInternal))
                    {
                        AddTrianglesToBaseTriangles(addedTriangles, addedTrianglesCount, Color.FloralWhite, true);

                        baseEdgeInfo.ClearTrianglesPointsExternal(addedTriangles, addedTrianglesCount);
                        baseEdgeInfo.InsertExternalEdges(loopPeak, extEdgesRange);

                        return addedTrianglesCount > 0;
                    }
                    else if (processInternal)
                    {
                        return ProcessInternalPoint(addedPointIndex, extEdgesRange.Beg);
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (pointOnExtEdge)
                {
                    return ProcessInternalPoint(addedPointIndex, extEdgesRange.Beg);
                }
            }
            return false;
        }

        private void FlipEdges(Triangle[] addedTriangles, int addedTrianglesCount)
        {
            if (addedTrianglesCount <= 0)
            {
                return;
            }
            for (int i = 0; i < addedTrianglesCount; i++)
            {
                AddTriangleEdges(addedTriangles[i]);
            }
            baseTriangleSet.FlipEdgesFrom(edgeDict, triangleGrid);
            edgeDict.Clear();
        }

        private void ReplaceCellTriangles(Triangle[] addedTriangles, int addedTrianglesCount)
        {
            RemoveCellTriangles();

            if (addedTrianglesCount > 0)
            {
                AddTrianglesToBaseTriangles(addedTriangles, addedTrianglesCount, Color.Azure, true);
            }
        }

        private void AddTrianglesToBaseTriangles(Triangle[] addedTriangles, int addedTrianglesCount, Color innerColor, bool flipEdges)
        {
            if (addedTrianglesCount <= 0)
            {
                return;
            }
            trianglesCount = baseTriangleSet.AddTriangles(addedTriangles, addedTrianglesCount, innerColor);

            triangleGrid.AddTriangles(addedTriangles, addedTrianglesCount);

            if (flipEdges)
            {
                FlipEdges(addedTriangles, addedTrianglesCount);
            }
        }

        private bool AddExternalPointTriangles(int addedPointIndex, ref IndexRange extEdgesRange, out EdgePeak loopPeak, out bool processInternal)
        {
            processInternal = false;
            Log.WriteLine(GetType() + ".AddExternalPointTriangles: extEdgesRange: " + extEdgesRange);
            extEdgesRange = baseEdgeInfo.TrimExternalEdgesRange(extEdgesRange, out bool innerDegenerate);
            Log.WriteLine(GetType() + ".AddExternalPointTriangles: extEdgesRange: " + extEdgesRange + " innerDegenerate: " + innerDegenerate);

            if (extEdgesRange.FullLength <= 0)
            {
                loopPeak = default;
                return false;
            }
            // The following call adds duplicates to cellPointsIndices.
            baseEdgeInfo.ForEachPointInExtEdgesRange(extEdgesRange, pointIndex => cellPointsIndices.Add(pointIndex));

            bool result;
            if (innerDegenerate)
            {
                Log.WriteLine(GetType() + ".AddExternalPointTriangles: Inner Degenerate Triangle with addedPointIndex: " + addedPointIndex);
                baseEdgeInfo.LoopCopyExternalEdgesRange(extEdgesRange, addedPointIndex, addedEdgeInfo, out loopPeak);
                cellPolygon.SetFromExternalEdges(addedEdgeInfo, points);
                cellPolygon.Triangulate(addedTriangles, ref addedTrianglesCount, points);
                addedEdgeInfo.Clear();
                result = true;
            }
            else
            {
                loopPeak = baseEdgeInfo.GetExternalEdgesRangeLoopPeak(addedPointIndex, extEdgesRange);
                bool addExternalTriangle(EdgeEntry edge) => AddExternalTriangle(edge, addedPointIndex);
                result = baseEdgeInfo.InvokeForExternalEdgesRange(extEdgesRange, addExternalTriangle, true, out _);
            }
            if (!result)
            {
                if (extEdgesRange.GetIndexCount() == 1 && cellTrianglesIndices.Count > 0)
                {
                    var extEdge = baseEdgeInfo.GetExternalEdge(extEdgesRange.Beg);
                    processInternal = extEdge.LastPointInRange;
                }
                ClearAddedTriangles();
            }
            return result;
        }

        private void ClearAddedTriangles()
        {
            for (int i = 0; i < addedTrianglesCount; i++)
            {
                addedTriangles[i] = Triangle.None;
            }
            addedTrianglesCount = 0;
        }

        private void ClearLastPointData()
        {
            ClearAddedTriangles();

            baseEdgeInfo.ClearLastPointData();

            cellPointsIndices.Clear();
            cellTrianglesIndices.Clear();
            cellPolygon.Clear();
        }

        private void AddTriangleVertsToCellPoints(Triangle triangle)
        {
            triangle.GetIndices(indexBuffer);
            for (int i = 0; i < 3; i++)
            {
                int vertIndex = indexBuffer[i];
                if (!cellPointsIndices.Contains(vertIndex))
                {
                    cellPointsIndices.Add(vertIndex);
                }
            }
        }

        private bool AddCellTriangleToProcess(int triangleIndex, bool addToCellPoints = true)
        {
            if (!cellTrianglesIndices.Contains(triangleIndex))
            {
                //Log.WriteLine(GetType() + ".AddCellTriangleToProcess: " + triangles[triangleIndex]);
                cellTrianglesIndices.Add(triangleIndex);

                if (addToCellPoints)
                {
                    AddTriangleVertsToCellPoints(baseTriangleSet.Triangles[triangleIndex]);
                }
                return true;
            }
            return false;
        }

        private void AddCellTrianglesEdges()
        {
            for (int i = 0; i < cellTrianglesIndices.Count; i++)
            {
                int triangleIndex = cellTrianglesIndices[i];
                AddTriangleEdges(baseTriangleSet.Triangles[triangleIndex]);
            }
            ForEachEdgeInDict((edgeKey, edge) => {
                //Log.WriteLine(GetType() + ".AddCellTrianglesEdges: " + edge);
                if (edge.Count == 1)
                {
                    addedEdgeInfo.AddExternalEdge(edge);
                }
            });
            edgeDict.Clear();
        }

        private void RemoveCellTriangles()
        {
            if (cellTrianglesIndices.Count == 0)
            {
                return;
            }
            cellTrianglesIndices.Sort();

            for (int i = cellTrianglesIndices.Count - 1; i >= 0; i--)
            {
                int triangleIndex = cellTrianglesIndices[i];
                RemoveBaseTriangle(triangleIndex);
            }
            cellTrianglesIndices.Clear();
        }

        private void RemoveBaseTriangle(int triangleIndex)
        {
            triangleGrid.RemoveTriangle(baseTriangleSet.Triangles[triangleIndex]);
            baseTriangleSet.RemoveTriangle(triangleIndex, ref trianglesCount);
        }

        private ref Triangle GetBaseTriangleRef(long triangleKey, out int triangleIndex)
        {
            return ref baseTriangleSet.GetTriangleRef(triangleKey, out triangleIndex);
        }

        private bool ReplaceEdgesWithTriangles(int pointIndex, int extEdgeIndex = -1)
        {
            var degenerateTriangle = DegenerateTriangle.None;

            bool result = addedEdgeInfo.ForEachExternalEdge(edge => AddInternalTriangle(edge, pointIndex, ref degenerateTriangle));
            if (result)
            {
                if (degenerateTriangle.IsValid)
                {
                    result = baseEdgeInfo.InsertExternalEdgesWithPointOnEdge(degenerateTriangle, extEdgeIndex);
                }
                else
                {
                    baseEdgeInfo.SetPointExternal(pointIndex, false);
                }
            }
            if (!result)
            {
                ClearAddedTriangles();
            }
            addedEdgeInfo.Clear();
            return result;
        }

        private bool AddExternalTriangle(EdgeEntry edge, int pointIndex)
        {
            if (!edge.LastPointDegenerateTriangle)
            {
                AddTriangleToProcess(edge, pointIndex);
                //Log.WriteLine(GetType() + ".AddExternalTriangle: " + addedTriangles[addedTrianglesCount - 1]);
                return true;
            }
            else
            {
                Log.WriteLine(GetType() + ".AddExternalTriangle: SKIPPED EXTERNAL DEGENERATE TRIANGLE: " + edge + " pointIndex: " + pointIndex + Log.KIND_OF_FAKAP);
                return false;
            }
        }

        private bool AddInternalTriangle(EdgeEntry edge, int pointIndex, ref DegenerateTriangle degenerateTriangle)
        {
            edge.SetLastPointOnEgdeData(points[pointIndex], points, out _);

            if (!edge.LastPointDegenerateTriangle)
            {
                AddTriangleToProcess(edge, pointIndex);
                //Log.WriteLine(GetType() + ".AddInternalTriangle: " + addedTriangles[addedTrianglesCount - 1]);
                return true;
            }
            else if (edge.LastPointInRange && baseEdgeInfo.IsEdgeExternal(edge))
            {
                if (!degenerateTriangle.IsValid)
                {
                    Log.WriteLine(GetType() + ".AddInternalTriangle: ADDED DEGENERATE TRIANGLE: edge: " + edge + " pointIndex: " + pointIndex);
                    degenerateTriangle = new DegenerateTriangle(edge, pointIndex);
                    return true;
                }
                else
                {
                    Log.WriteLine(GetType() + ".AddInternalTriangle: SKIPPED INTERNAL DEGENERATE TRIANGLE > 1: edge: " + edge + " pointIndex: " + pointIndex);
                    return false;
                }
            }
            else
            {
                Log.WriteLine(GetType() + ".AddInternalTriangle: SKIPPED INTERNAL DEGENERATE TRIANGLE: " + edge + " pointIndex: " + pointIndex + Log.KIND_OF_FAKAP);
                return false;
            }
        }

        private void AddTriangleToProcess(EdgeEntry edge, int pointIndex)
        {
            addedTriangles[addedTrianglesCount++] = new Triangle(edge.A, edge.B, pointIndex, points);
        }

        private void PrintTriangles()
        {
            for (int i = 0; i < trianglesCount; i++)
            {
                Log.WriteLine(GetType() + ".PrintTriangles: " + i + " " + triangles[i]);
            }
        }
    }
}
