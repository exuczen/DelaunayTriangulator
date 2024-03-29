﻿using System;
using System.Collections.Generic;
using System.Numerics;
#if DEBUG_CLOSEST_CELLS
using System.Drawing;
#endif

namespace Triangulation
{
    public class TriangleGrid
    {
        public const int MinDivsCount = 16;

#if DEBUG_CLOSEST_CELLS
        public bool ClosestCellDebugging { get; } = true;
#else
        public bool ClosestCellDebugging { get; } = false;
#endif
        public float SizeMin => MathF.Min(size.X, size.Y);
        public float CellSizeMin => MathF.Min(cellSize.X, cellSize.Y);
        public float CellTolerance => cellTolerance;
        public Vector2 CellSize => cellSize;
        public Vector2 Size => size;
        public Vector2Int XYCount => new Vector2Int(xCount, yCount);
        public Vector2Int SelectedCellXY => selectedCellXY;
        public TriangleCell SelectedCell => selectedCell;

        private readonly Vector2 size = default;
        private readonly Vector2 cellSize = default;
        private readonly Vector2 cellHalfSize = default;
        private readonly float cellTolerance = 0f;
        private readonly float circleTolerance = 0f;
        private readonly int xCount, yCount = 0;

        private readonly TriangleCell[] cells = null;

        private readonly TriangleSet triangleSet = null;
        private readonly Dictionary<long, HashSet<int>> triangleCellsDict = new Dictionary<long, HashSet<int>>();

        private readonly Stack<HashSet<int>> cellIndicesPool = new Stack<HashSet<int>>();

        private TriangleCell selectedCell = null;
        private Vector2Int selectedCellXY = default;

        public TriangleGrid(TriangleSet triangleSet, Vector2 size, int minDivsCount = MinDivsCount)
        {
            this.triangleSet = triangleSet;
            this.size = size;

            var xyCount = GridUtils.GetXYCount(size, out cellSize, minDivsCount);
            xCount = xyCount.x;
            yCount = xyCount.y;
            cellHalfSize = 0.5f * cellSize;

            float minCellSize = CellSizeMin;
            cellTolerance = minCellSize * 0.01f;
            circleTolerance = minCellSize * 0.1f;

            int cellsCount = xCount * yCount;
            cells = new TriangleCell[cellsCount];

            Log.WriteLine(GetType() + ": " + xCount + "x" + yCount + " " + cellSize + " " + (MathF.Min(cellSize.X, cellSize.Y) / MathF.Max(cellSize.X, cellSize.Y)).ToString("f4"));

            for (int i = 0; i < cellsCount; i++)
            {
                cells[i] = new TriangleCell();
            }
            for (int i = 0; i < triangleSet.Capacity; i++)
            {
                cellIndicesPool.Push(new HashSet<int>());
            }
        }

        public void Clear()
        {
            foreach (var kvp in triangleCellsDict)
            {
                kvp.Value.Clear();
                cellIndicesPool.Push(kvp.Value);
            }
            triangleCellsDict.Clear();

            ForEachCell((i, cell) => cell.Clear());
        }

        public void ClearFilledCellCircles()
        {
            ForEachCell((i, cell) => SetCellCirclesFilled(cell, false));
        }

        public void ClearFilledCellsColorAndText()
        {
            ForEachCell((i, cell) => {
                cell.ClearFillColor();
                cell.DebugText = null;
            });
        }

        //public bool IsPointInsideTriangle(Vector2 point, Vector2[] points, float circleTolerance)
        //{
        //    if (GetCell(point, out var cell))
        //    {
        //        foreach (long triangleKey in cell.TriangleKeys)
        //        {
        //            ref var triangle = ref GetTriangleRef(triangleKey, out _);
        //            if (triangle.CircumCircle.ContainsPoint(point, circleTolerance))
        //            {
        //                if (triangle.ContainsPoint(point, points))
        //                {
        //                    Log.WriteLine(GetType() + ".IsPointInsideTriangle: " + triangle);
        //                    return true;
        //                }
        //            }
        //        }
        //    }
        //    return false;
        //}

        public bool FindClosestCellWithPredicate(int centerX, int centerY, out TriangleCell cell, Predicate<TriangleCell> predicate)
        {
#if DEBUG_CLOSEST_CELLS
            bool cellPredicate(Vector3Int xyi, Color color, string s)
            {
                var cell = cells[xyi.z];
                cell.DebugText = s;
                cell.SetFillColor(color);
                return predicate(cell);
            }
#else
            bool cellPredicate(Vector3Int xyi) => predicate(cells[xyi.z]);
#endif
            if (GridUtils.FindClosestCellWithPredicate(centerX, centerY, xCount, yCount, out var cellXYI, cellPredicate))
            {
                cell = cells[cellXYI.z];
                return true;
            }
            else
            {
                cell = null;
                return false;
            }
        }

        public void AddTriangles(Triangle[] triangles, int trianglesCount)
        {
            for (int i = 0; i < trianglesCount; i++)
            {
                AddTriangle(triangles[i]);
            }
        }

        public void AddTriangle(Triangle triangle)
        {
            if (triangleCellsDict.ContainsKey(triangle.Key))
            {
                return;
            }
            var cc = triangle.CircumCircle;
            var ccBounds = cc.Bounds;
            ccBounds.min -= Vector2.One * cellTolerance;
            ccBounds.max += Vector2.One * cellTolerance;
            int begX = Math.Clamp((int)(ccBounds.min.X / cellSize.X), 0, xCount - 1);
            int endX = Math.Clamp((int)(ccBounds.max.X / cellSize.X), 0, xCount - 1);
            int begY = Math.Clamp((int)(ccBounds.min.Y / cellSize.Y), 0, yCount - 1);
            int endY = Math.Clamp((int)(ccBounds.max.Y / cellSize.Y), 0, yCount - 1);

            //Log.WriteLine(GetType() + ".AddTriangle: " + triangle + " to cells: x: " + begX + "-" + endX + " y: " + begY + "-" + endY);

            var cellIndices = cellIndicesPool.Pop();
            triangleCellsDict.Add(triangle.Key, cellIndices);

            for (int y = begY; y <= endY; y++)
            {
                for (int x = begX; x <= endX; x++)
                {
                    if (GetCellOverlap(x, y, cc, ccBounds, out int cellIndex))
                    {
                        cells[cellIndex].AddTriangle(triangle.Key);
                        //Log.WriteLine(GetType() + ".AddTriangle: " + cells[cellIndex]);
                        cellIndices.Add(cellIndex);
                    }
                }
            }
        }

        public void RemoveTriangle(long triangleKey)
        {
            if (!triangleCellsDict.ContainsKey(triangleKey))
            {
                return;
            }
            var cellIndices = triangleCellsDict[triangleKey];

            foreach (int cellIndex in cellIndices)
            {
                cells[cellIndex].RemoveTriangle(triangleKey);
            }
            cellIndices.Clear();
            cellIndicesPool.Push(cellIndices);

            triangleCellsDict.Remove(triangleKey);
        }

        public void RemoveTriangle(Triangle triangle)
        {
            RemoveTriangle(triangle.Key);
        }

        public void ForEachCell(Action<int, TriangleCell> action)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                action(i, cells[i]);
            }
        }

        public void ForEachNgbrCell(int cellX, int cellY, Action<TriangleCell> action)
        {
            for (int y = -1; y < 2; y++)
            {
                for (int x = -1; x < 2; x++)
                {
                    if (GetCell(cellX + x, cellY + y, out var cell, out _))
                    {
                        action(cell);
                    }
                }
            }
        }

        public void ForEachCell(Action<int, int, TriangleCell> action)
        {
            for (int y = 0; y < yCount; y++)
            {
                int yOffset = y * xCount;
                for (int x = 0; x < xCount; x++)
                {
                    action(x, y, cells[yOffset + x]);
                }
            }
        }

        public void SetSelectedCell(Vector2 p)
        {
            if (GetCell(p, out var cell, out var cellXY))
            {
                if (selectedCell != null && selectedCell != cell)
                {
                    SetCellCirclesFilled(selectedCell, false);
                }
                selectedCell = cell;
                selectedCellXY = cellXY;
                SetCellCirclesFilled(cell, true);
            }
        }

        public bool GetCell(Vector2 p, out TriangleCell cell)
        {
            return GetCell(p, out cell, out _, out _);
        }

        public bool GetCell(Vector2 p, out TriangleCell cell, out int x, out int y)
        {
            x = (int)(p.X / cellSize.X);
            y = (int)(p.Y / cellSize.Y);

            if (x >= xCount)
            {
                if (p.X > size.X + cellTolerance)
                {
                    throw new ArgumentOutOfRangeException(GetType() + ".GetCell: " + p + " | " + new Vector2Int(x, y) + " for size: " + size + " | " + XYCount);
                }
                else if (x == xCount)
                {
                    x--;
                }
            }
            if (y >= yCount)
            {
                if (p.Y > size.Y + cellTolerance)
                {
                    throw new ArgumentOutOfRangeException(GetType() + ".GetCell: " + p + " | " + new Vector2Int(x, y) + " for size: " + size + " | " + XYCount);
                }
                else if (y == yCount)
                {
                    y--;
                }
            }
            return GetCell(x, y, out cell, out _);
        }

        public bool GetCell(Vector2 p, out TriangleCell cell, out Vector2Int cellXY)
        {
            bool result = GetCell(p, out cell, out int x, out int y);
            cellXY = new Vector2Int(x, y);
            return result;
        }

        public bool GetFirstExternalEdgeInCell(TriangleCell cell, IncrementalEdgeInfo edgeInfo, out int extEdgeIndex)
        {
            foreach (var key in cell.TriangleKeys)
            {
                ref var triangle = ref GetTriangleRef(key, out _);

                if (edgeInfo.GetFirstExternalEdgeIndex(triangle, out extEdgeIndex))
                {
                    return true;
                }
            }
            extEdgeIndex = -1;
            return false;
        }

        public bool GetFirstOppositeExternalEdgeInCell(Vector2 point, TriangleCell cell, IncrementalEdgeInfo edgeInfo, out int extEdgeIndex, out bool pointOnEdge)
        {
            foreach (var key in cell.TriangleKeys)
            {
                ref var triangle = ref GetTriangleRef(key, out _);

                if (edgeInfo.GetFirstExternalEdgeOppositeToExternalPoint(point, triangle, out extEdgeIndex, out pointOnEdge, GetType().Name))
                {
                    return true;
                }
            }
            extEdgeIndex = -1;
            pointOnEdge = false;
            return false;
        }

        //private void AddCellToTriangleCells(Triangle t, int cellIndex)
        //{
        //    triangleCellsDict[t.Key].Add(cellIndex);
        //}

        //private void ClearTriangleCells(Triangle t)
        //{
        //    triangleCellsDict[t.Key].Clear();
        //}

        private void SetCellCirclesFilled(TriangleCell cell, bool filled)
        {
            int pointsLength = triangleSet.PointsLength;
            foreach (long key in cell.TriangleKeys)
            {
                ref var triangle = ref GetTriangleRef(key, out int triangleIndex);
                if (triangleIndex < 0)
                {
                    Log.WriteLine(GetType() + ".SetCellFilled: " + Triangle.GetTriangleFromKey(key, pointsLength));
                }
                triangle.CircumCircle.Filled = filled;
            }
        }

        private ref Triangle GetTriangleRef(long triangleKey, out int triangleIndex)
        {
            return ref triangleSet.GetTriangleRef(triangleKey, out triangleIndex);
        }

        private bool GetCell(int x, int y, out TriangleCell cell, out int cellIndex)
        {
            if (x >= 0 && x < xCount && y >= 0 && y < yCount)
            {
                cell = cells[cellIndex = y * xCount + x];
                return true;
            }
            else
            {
                throw new ArgumentOutOfRangeException(GetType() + ".GetCell: (" + x + ", " + y + ") | " + XYCount);
            }
            //cellIndex = -1;
            //cell = null;
            //return false;
        }

        private bool GetCellOverlap(int x, int y, Circle cc, Bounds2 ccBounds, out int cellIndex)
        {
            cellIndex = y * xCount + x;
            var ccCenter = cc.Center;
            var cellMin = new Vector2(x * cellSize.X, y * cellSize.Y);
            var cellMax = cellMin + cellSize;
            cellMin -= Vector2.One * cellTolerance;
            cellMax += Vector2.One * cellTolerance;
            var cellBounds = Bounds2.MinMax(cellMin, cellMax);

            if (!cellBounds.Overlap(ccBounds))
            {
                cells[cellIndex].DebugPoint = default;
                return false;
            }

            bool overlap = (ccCenter.X >= cellMin.X && ccCenter.X <= cellMax.X) ||
                           (ccCenter.Y >= cellMin.Y && ccCenter.Y <= cellMax.Y);
            var cellCenter = cellBounds.Center;
            var debugPoint = cellCenter;

            if (!overlap)
            {
                var dr = ccCenter - cellCenter;
                var n = new Vector2(MathF.Sign(dr.X), MathF.Sign(dr.Y));
                var cellVert = cellCenter + n * cellHalfSize;
                overlap = cc.ContainsPointWithSqrt(cellVert, circleTolerance, false);
                debugPoint = cellVert;
            }
            cells[cellIndex].DebugPoint = debugPoint;
            return overlap;
        }

        private Bounds2 GetCellBounds(int x, int y)
        {
            var cellMin = new Vector2(x * cellSize.X, y * cellSize.Y);
            var cellMax = cellMin + cellSize;
            return Bounds2.MinMax(cellMin, cellMax);
        }

        private void PrintCells()
        {
            ForEachCell((x, y, cell) => {
                if (cell.TrianglesCount > 0)
                {
                    Log.WriteLine(string.Format("TriangleCell: {0}x{1}:{2}", x, y, cell.TrianglesCount));
                }
            });
        }
    }
}
