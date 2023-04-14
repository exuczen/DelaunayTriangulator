using System;
using System.Collections.Generic;
using System.Drawing;

namespace Triangulation
{
    public class IncrementalTriangulator : Triangulator
    {
        public TriangleGrid TriangleGrid => triangleGrid;
        public PointGrid PointGrid => pointGrid;
        public EdgeInfo EdgeInfo => edgeInfo;
        public List<int> CellPointsIndices => cellPointsIndices;
        public Polygon CellPolygon => cellPolygon;
        public bool InternalOnly => Supermanent;

        public bool BaseOnly = false;
        public bool PointsValidation = false;
        public bool EdgesValidation = false;

        private readonly List<int> cellPointsIndices = new List<int>();
        private readonly List<int> cellTrianglesIndices = new List<int>();
        private readonly Polygon cellPolygon = new Polygon(true);

        private readonly List<int> pointsToClear = new List<int>();

        private readonly EdgeFlipper baseEdgeFlipper = null;
        private readonly Triangle[] addedTriangles = null;
        private readonly EdgeInfo addedEdgeInfo = null;

        private int addedTrianglesCount = 0;

        private TriangleGrid triangleGrid = null;
        private PointGrid pointGrid = null;

        public IncrementalTriangulator(int pointsCapacity, float tolerance, IExceptionThrower exceptionThrower) : base(pointsCapacity, tolerance, exceptionThrower)
        {
            baseEdgeFlipper = new EdgeFlipper(triangleSet, edgeInfo, points);
            addedTriangles = new Triangle[triangles.Length];
            addedEdgeInfo = new EdgeInfo(points, exceptionThrower);
        }

        public override bool Triangulate()
        {
            if (base.Triangulate())
            {
                if (!BaseOnly)
                {
                    AddBaseTrianglesToTriangleSet();
                }
                ValidateTriangulation(false, PointsValidation);

                return true;
            }
            return false;
        }

        public override void Load(SerializedTriangulator data)
        {
            base.Load(data);

            AddBaseTrianglesToTriangleSet();
        }

        public void Initialize(Vector2 gridSize, int triangleGridDivs, int pointGridDivsMlp, bool triangulate)
        {
            if (triangleSet == null)
            {
                throw new Exception("baseTriangleSet == null");
            }
            triangleGrid = new TriangleGrid(triangleSet, gridSize, triangleGridDivs);

            pointGrid = new PointGrid(gridSize, triangleGrid.XYCount * pointGridDivsMlp);

            Circle.MinRadiusForSqrt = 250f * pointGrid.CellSizeMin;

            circleTolerance = 0.01f * pointGrid.CellSizeMin;

            cellPolygon.Tolerance = 0.1f * circleTolerance;

            //Log.WriteLine(GetType() + ".Initialize: circleTolerance: " + circleTolerance);

            SetSuperCircumCirclePoints(Bounds2.MinMax(Vector2.Zero, gridSize), false);

            if (triangulate)
            {
                Triangulate();
            }
        }

        public bool TryAddPoint(Vector2 point, out int pointIndex, bool findClosestCell)
        {
            pointIndex = -1;
            return TryAddPointRefIndex(point, ref pointIndex, findClosestCell);
        }

        private bool TryAddPointRefIndex(Vector2 point, ref int pointIndex, bool findClosestCell)
        {
            int savedIndex = -1;
            if (findClosestCell && pointGrid.GetClosestClearCell(point, out var cellXYI) ||
                !findClosestCell && pointGrid.CanAddPoint(point, out cellXYI, out savedIndex))
            {
                base.AddPointRefIndex(point, ref pointIndex);
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
                RemovePointFromTriangulation(pointIndex, out _);
                return true;
            }
            return false;
        }

        public void RemovePointFromTriangulation(int pointIndex, out bool reset)
        {
            reset = false;
            if (pointIndex < 0 || pointIndex >= pointsCount)
            {
                throw new ArgumentOutOfRangeException("RemovePoint: " + pointIndex + " pointsCount: " + pointsCount);
            }
            else if (Vector2.IsNaN(points[pointIndex]))
            {
                //Log.WriteLine(GetType() + ".RemovePointFromTriangulation: " + pointIndex + " isNaN, lastIndex: " + (pointsCount - 1) + " | unusedPointIndices: " + unusedPointIndices.Contains(pointIndex));
                if (pointIndex == pointsCount - 1)
                {
                    unusedPointIndices.Remove(pointIndex);
                    pointsCount--;
                }
                return;
            }
            else if (trianglesCount <= 0 || NotEnoughPoints(pointsCount))
            {
                ClearPoint(pointIndex);
            }
            else
            {
                var point = points[pointIndex];

                if (TryForceClearPoint(pointIndex))
                {
                    reset = true;
                    return;
                }
                else if (triangleGrid.GetCell(point, out var cell, out var cellXY))
                {
                    //Log.WriteLine(GetType() + ".RemovePointFromTriangulation: cellXY: " + cellXY + " point: " + point + " pointIndex: " + pointIndex);
                    ClearLastPointData();
                    ProcessClearPoint(pointIndex, cell);
                    ValidateTriangulation(EdgesValidation, PointsValidation);
                }
                else
                {
                    Log.WriteWarning("RemovePointFromTriangulation: triangle cell not found for pointIndex: " + pointIndex);
                }
            }
        }

        public bool AddPointToTriangulation(Vector2 point, int pointIndex, bool findClosestCell)
        {
            return AddPointToTriangulationRefIndex(point, ref pointIndex, findClosestCell);
        }

        public bool AddPointToTriangulation(Vector2 point, out int pointIndex, bool findClosestCell)
        {
            pointIndex = -1;
            return AddPointToTriangulationRefIndex(point, ref pointIndex, findClosestCell);
        }

        protected bool AddPointToTriangulationRefIndex(Vector2 point, ref int pointIndex, bool findClosestCell)
        {
            if (trianglesCount <= 0 || NotEnoughPoints(pointsCount))
            {
                if (!TryAddPointRefIndex(point, ref pointIndex, findClosestCell))
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

                if (!TryAddPointRefIndex(point, ref pointIndex, findClosestCell))
                {
                    if (pointIndex >= 0 && !TryForceClearPoint(pointIndex))
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
                        if (triangle.CircumCircle.ContainsPoint(point, circleTolerance))
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
                        if (InternalOnly)
                        {
                            throw new Exception(GetType() + ".AddPoints: InternalOnly && !isInTriangle");
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
                //Log.PrintArray(triangles, trianglesCount, "AddPointToTriangulation: ");
                //edgeInfo.PrintPointsExternal(pointsCount);

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
            triangleSet.Clear();
            edgeInfo.Clear();
            triangleGrid.Clear();
            ClearLastPointData();
            base.ClearTriangles();
        }

        protected override void ClearPoints()
        {
            pointGrid.Clear();
            base.ClearPoints();
        }

        protected override void SetSortedPoints(out Bounds2 bounds)
        {
            pointsCount = pointGrid.SetPoints(points, pointsOffset, pointsCount, out bounds);
        }

        protected override void SetSortedPoints(List<Vector2> pointsList)
        {
            pointGrid.SetPoints(pointsList, pointsOffset);
            pointsCount = pointsList.Count;
            pointsList.CopyTo(points, 0);
        }

        protected override int GetClosestPointIndex(Vector2 center)
        {
            return pointGrid.GetClosestPointIndex(center);
        }

        protected override void ClearPoint(int pointIndex, bool addToUnused = true)
        {
            pointGrid.ClearPoint(pointIndex, points);
            base.ClearPoint(pointIndex, addToUnused);
        }

        private bool TryForceClearPoint(int pointIndex)
        {
            if (edgeInfo.GetExternalTrianglesCount() == trianglesCount)
            {
                //Log.WriteLine(GetType() + ".TryForceClearPoint: " + pointIndex);
                ClearPoint(pointIndex);
                Triangulate();
                if (trianglesCount <= 0)
                {
                    ClearPoints();
                }
                return true;
            }
            return false;
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

        private bool ValidateTriangulation(bool validateEdges, bool validatePoints)
        {
            if (!validateEdges && !validatePoints)
            {
                return true;
            }
            bool edgesValid = !validateEdges || baseEdgeFlipper.Validate();
            bool circleOverlapsPoint = false;
            if (validatePoints)
            {
                //float circleSqrOffset = 0f;
                //float circleSqrOffset = -2f * circleTolerance;
                float circleSqrOffset = -0.1f * pointGrid.CellSizeMin;

                for (int i = pointsOffset; i < pointsCount; i++)
                {
                    var point = points[i];
                    if (Vector2.IsNaN(point))
                    {
                        continue;
                    }
                    ForEachTriangleInCell(point, (triangle, triangleIndex) => {
                        bool isPointExternal = edgeInfo.IsPointExternal(i);
                        if (!isPointExternal && !triangle.HasVertex(i) && triangle.CircumCircle.ContainsPointWithSqrt(point, circleSqrOffset, true) && !unusedPointIndices.Contains(i))
                        {
                            Log.WriteError(GetType() + ".ValidateTriangulation: point " + i + " inside triangle: " + triangle + " | isPointExternal: " + isPointExternal + " | " + point);
                            //edgeInfo.PrintExternalEdges("ValidateTriangulation: ");
                            triangles[triangleIndex].CircumCircle.Filled = true;
                            circleOverlapsPoint = true;
                            exceptionThrower.ThrowException("!ValidateTriangulation", ErrorCode.InvalidTriangulation, i);
                        }
                    });
                    if (circleOverlapsPoint)
                    {
                        break;
                    }
                }
            }
            return edgesValid && !circleOverlapsPoint;
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
                Log.WriteLine(GetType() + ".ProcessClearPoint: cellTrianglesIndices.Count == 0 for pointIndex: " + pointIndex + " unusedPointIndices: " + unusedPointIndices.Contains(pointIndex) + " pointsCount: " + pointsCount);
                return;
            }
            bool isPointExternal = edgeInfo.IsPointExternal(pointIndex);

            Log.WriteLine(GetType() + ".ProcessClearPoint: ***** " + pointIndex + " ***** isPointExternal: " + isPointExternal + " pointsCount: " + pointsCount);

            AddCellTrianglesEdges();

            AddTrianglesOnClearPoint(pointIndex, out bool debugFindExtEdges);

            ReplaceCellTriangles(addedTriangles, addedTrianglesCount);

            if (isPointExternal)
            {
                edgeInfo.UpdateTriangleDicts(addedEdgeInfo);
            }
            ClearPoints(pointsToClear);

            if (trianglesCount == 0)
            {
                ClearPoints();
            }
            addedEdgeInfo.Clear();

            if (debugFindExtEdges && !edgeInfo.FindExternalEdges(pointsCount, out _))
            {
                exceptionThrower.ThrowException("ProcessClearPoint: ", ErrorCode.ExternalEdgesBrokenLoop, pointIndex);
            }
            if (trianglesCount > 0 && !edgeInfo.ValidateExternalEdges(out var error))
            {
                Log.WriteWarning(GetType() + ".ProcessClearPoint: VALIDATION ERROR: " + error + " | pointIndex: " + pointIndex);
                exceptionThrower.ThrowException("ProcessClearPoint: VALIDATION ERROR: " + error, ErrorCode.ExternalEdgesBrokenLoop, pointIndex);
            }
        }

        private void AddTrianglesOnClearPoint(int pointIndex, out bool debugFindExtEdges)
        {
            debugFindExtEdges = false;

            if (!addedEdgeInfo.JoinSortExternalEdges(out _))
            {
                exceptionThrower.ThrowException("AddTrianglesOnClearPoint: ", ErrorCode.ExternalEdgesBrokenLoop, pointIndex);
                throw new Exception("AddTrianglesOnClearPoint: " + ErrorCode.ExternalEdgesBrokenLoop + " | pointIndex: " + pointIndex);
            }
            //addedEdgeInfo.PrintExternalEdges("AddTrianglesOnClearPoint");

            cellPolygon.SetFromExternalEdges(addedEdgeInfo, points);

            if (edgeInfo.IsPointExternal(pointIndex))
            {
                //addedEdgeInfo.Clear();
                //return;

                var peak = cellPolygon.GetPeak(pointIndex, out int peakIndex);

                bool canClipPeak = cellPolygon.CanClipPeak(peakIndex, points, pointGrid.CellSizeMin);

                if (!canClipPeak && cellPolygon.PeakCount > 3)
                {
                    var peakExtEdgesRange = addedEdgeInfo.GetPeakExternalEdgesRange(peak, edgeInfo, pointsToClear);

                    addedTrianglesCount = cellPolygon.TriangulateFromConcavePeaks(peakExtEdgesRange, edgeInfo, addedTriangles, addedEdgeInfo);

                    edgeInfo.ReplacePeakExternalEdges(peakExtEdgesRange, addedEdgeInfo);

                    //debugFindExtEdges = false;
                }
                else
                {
                    if (canClipPeak)
                    {
                        cellPolygon.ClipPeak(peakIndex, -1, points);

                        cellPolygon.Triangulate(addedTriangles, ref addedTrianglesCount, points);
                    }
                    edgeInfo.ClipPeakExternalEdges(peak, pointsToClear);

                    //pointsToClear.Add(peak.PeakVertex);
                    //debugFindExtEdges = false;
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
                return triangleGrid.GetFirstExternalEdgeInCell(c, edgeInfo, out firstExtEdge, out _);
            }
            if (triangleGrid.FindClosestCellWithPredicate(cellXY.x, cellXY.y, out var cell, cellPredicate))
            {
                if (edgeInfo.GetOppositeExternalEdgesRange(addedPointIndex, firstExtEdge, out IndexRange extEdgesRange, out bool pointOnExtEdge, out bool innerDegenerate))
                {
                    if (AddExternalPointTriangles(addedPointIndex, extEdgesRange, innerDegenerate, out EdgePeak loopPeak, out bool processInternal))
                    {
                        AddTrianglesToTriangleSet(addedTriangles, addedTrianglesCount, Color.FloralWhite, true);

                        edgeInfo.ClearTrianglesPointsExternal(addedTriangles, addedTrianglesCount);
                        edgeInfo.InsertExternalEdges(loopPeak, extEdgesRange);

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
            baseEdgeFlipper.FlipEdgesFrom(edgeDict, triangleGrid);
            edgeDict.Clear();
        }

        private void ReplaceCellTriangles(Triangle[] addedTriangles, int addedTrianglesCount)
        {
            RemoveCellTriangles();

            if (addedTrianglesCount > 0)
            {
                AddTrianglesToTriangleSet(addedTriangles, addedTrianglesCount, Color.Azure, true);
            }
        }

        private void AddBaseTrianglesToTriangleSet()
        {
            if (trianglesCount <= 0)
            {
                return;
            }
            trianglesCount = triangleSet.AddTriangles(triangles, trianglesCount);

            //edgeInfo.EdgeCounterDict.Clear();
            //edgeInfo.AddEdgesToCounterDict(triangles, trianglesCount);

            edgeInfo.AddEdgesToTriangleDicts(triangles, trianglesCount);
            edgeInfo.SetTrianglesColors(Color.FloralWhite);

            //edgeInfo.PrintEdgeCounterDict();
            //edgeInfo.PrintExtEdgeTriangleDict();
            //edgeInfo.PrintInnerEdgeTriangleDict();

            triangleGrid.AddTriangles(triangles, trianglesCount);

            if (!InternalOnly)
            {
                if (!edgeInfo.FindExternalEdges(pointsCount, out _))
                {
                    Log.WriteWarning(GetType() + ".base.Triangulate(): Clear on fail of FindExternalEdges");
                    Clear();
                }
                //edgeInfo.PrintPointsExternal(pointsCount);
            }
        }

        private void AddTrianglesToTriangleSet(Triangle[] addedTriangles, int addedTrianglesCount, Color innerColor, bool flipEdges)
        {
            if (addedTrianglesCount <= 0)
            {
                return;
            }
            trianglesCount = triangleSet.AddTriangles(addedTriangles, addedTrianglesCount);

            edgeInfo.AddEdgesToCounterDict(addedTriangles, addedTrianglesCount);
            edgeInfo.AddEdgesToTriangleDicts(addedTriangles, addedTrianglesCount, innerColor);

            //edgeInfo.PrintEdgeCounterDict();
            //edgeInfo.PrintExtEdgeTriangleDict();
            //edgeInfo.PrintInnerEdgeTriangleDict();

            triangleGrid.AddTriangles(addedTriangles, addedTrianglesCount);

            if (flipEdges)
            {
                FlipEdges(addedTriangles, addedTrianglesCount);
            }
        }

        private bool AddExternalPointTriangles(int addedPointIndex, IndexRange extEdgesRange, bool innerDegenerate, out EdgePeak loopPeak, out bool processInternal)
        {
            Log.WriteLine(GetType() + ".AddExternalPointTriangles: extEdgesRange: " + extEdgesRange + " innerDegenerate: " + innerDegenerate);
            processInternal = false;

            if (extEdgesRange.FullLength <= 0)
            {
                loopPeak = default;
                return false;
            }
            // The following call adds duplicates to cellPointsIndices.
            edgeInfo.ForEachPointInExtEdgesRange(extEdgesRange, pointIndex => cellPointsIndices.Add(pointIndex));

            if (innerDegenerate)
            {
                Log.WriteLine(GetType() + ".AddExternalPointTriangles: Inner Degenerate Triangle with addedPointIndex: " + addedPointIndex);
                edgeInfo.LoopCopyExternalEdgesRange(extEdgesRange, addedPointIndex, addedEdgeInfo, out loopPeak);
                cellPolygon.SetFromExternalEdges(addedEdgeInfo, points);
                cellPolygon.Triangulate(addedTriangles, ref addedTrianglesCount, points);
                addedEdgeInfo.Clear();
                return true;
            }
            else
            {
                loopPeak = edgeInfo.GetExternalEdgesRangeLoopPeak(addedPointIndex, extEdgesRange);
                bool addExternalTriangle(EdgeEntry edge) => AddExternalTriangle(edge, addedPointIndex);
                bool result = edgeInfo.InvokeForExternalEdgesRange(extEdgesRange, addExternalTriangle, true, out _);
                if (!result)
                {
                    if (extEdgesRange.GetIndexCount() == 1 && cellTrianglesIndices.Count > 0)
                    {
                        var extEdge = edgeInfo.GetExternalEdge(extEdgesRange.Beg);
                        processInternal = extEdge.LastPointInRange;
                    }
                    ClearAddedTriangles();
                }
                return result;
            }
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

            ClearDebugSuperTriangles();

            edgeInfo.ClearLastPointData();

            addedEdgeInfo.Clear();
            addedEdgeInfo.ClearLastPointData();

            cellPointsIndices.Clear();
            cellTrianglesIndices.Clear();
            cellPolygon.Clear();

            triangleGrid.ClearFilledCellsColorAndText();
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
                    AddTriangleVertsToCellPoints(triangleSet.Triangles[triangleIndex]);
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
                AddTriangleEdges(triangleSet.Triangles[triangleIndex]);
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
            triangleGrid.RemoveTriangle(triangleSet.Triangles[triangleIndex]);
            triangleSet.RemoveTriangle(triangleIndex, ref trianglesCount, edgeInfo);
        }

        private ref Triangle GetBaseTriangleRef(long triangleKey, out int triangleIndex)
        {
            return ref triangleSet.GetTriangleRef(triangleKey, out triangleIndex);
        }

        private bool ReplaceEdgesWithTriangles(int pointIndex, int extEdgeIndex = -1)
        {
            var degenerateTriangle = DegenerateTriangle.None;

            bool result = addedEdgeInfo.ForEachExternalEdge(edge => AddInternalTriangle(edge, pointIndex, ref degenerateTriangle));
            if (result)
            {
                if (degenerateTriangle.IsValid)
                {
                    result = edgeInfo.InsertExternalEdgesWithPointOnEdge(degenerateTriangle, extEdgeIndex);
                }
                else
                {
                    edgeInfo.SetPointExternal(pointIndex, false);
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
            else if (edge.LastPointInRange && edgeInfo.IsEdgeExternal(edge))
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
    }
}
