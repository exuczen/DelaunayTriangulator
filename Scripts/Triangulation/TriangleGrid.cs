using System;
using System.Collections.Generic;

namespace Triangulation
{
    public class TriangleGrid
    {
        public const int MinDivsCount = 16;

        public float CellTolerance => cellTolerance;
        public Vector2 CellSize => cellSize;
        public Vector2 Size => size;
        public Vector2Int XYCount => new Vector2Int(xCount, yCount);
        public Vector2Int SelectedCellXY => selectedCellXY;

        private readonly Vector2 size = default;
        private readonly Vector2 cellSize = default;
        private readonly Vector2 cellHalfSize = default;
        private readonly float cellTolerance = 0f;
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

            if (size.x == size.y)
            {
                xCount = yCount = minDivsCount;
                cellSize = size / xCount;
                cellHalfSize = 0.5f * cellSize;
            }
            else if (size.y < size.x)
            {
                yCount = minDivsCount;
                cellSize.y = size.y / yCount;
                cellHalfSize.y = 0.5f * cellSize.y;
                xCount = (int)((size.x + cellHalfSize.y) / cellSize.y);
                cellSize.x = size.x / xCount;
                cellHalfSize.x = 0.5f * cellSize.x;
            }
            else
            {
                xCount = minDivsCount;
                cellSize.x = size.x / xCount;
                cellHalfSize.x = 0.5f * cellSize.x;
                yCount = (int)((size.y + cellHalfSize.x) / cellSize.x);
                cellSize.y = size.y / yCount;
                cellHalfSize.y = 0.5f * cellSize.y;
            }
            cellTolerance = Math.Min(cellSize.x, cellSize.y) * 0.01f;

            int cellsCount = xCount * yCount;
            cells = new TriangleCell[cellsCount];

            Log.WriteLine(GetType() + ": " + xCount + "x" + yCount + " " + cellSize + " " + (Math.Min(cellSize.x, cellSize.y) / Math.Max(cellSize.x, cellSize.y)).ToString("f4"));

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

        public void ClearFilledCells()
        {
            ForEachCell((i, cell) => SetCellFilled(cell, false));
        }

        public bool IsPointInsideTriangle(Vector2 point, Vector2[] points)
        {
            if (GetCell(point, out var cell))
            {
                foreach (long triangleKey in cell.TriangleKeys)
                {
                    ref var triangle = ref GetTriangleRef(triangleKey, out _);
                    if (triangle.CircumCircle.ContainsPoint(point))
                    {
                        if (triangle.ContainsPoint(point, points))
                        {
                            Log.WriteLine(GetType() + ".IsPointInsideTriangle: " + triangle);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public bool FindClosestCellWithPredicate(int centerX, int centerY, out TriangleCell cell, Predicate<TriangleCell> predicate)
        {
            if (GetCell(centerX, centerY, out cell, out _) && predicate(cell))
            {
                return true;
            }
            int radius = 1;
            var xInBounds = new bool[2] {
                centerX - radius >= 0,
                centerX + radius < xCount
            };
            var yInBounds = new bool[2] {
                centerY - radius >= 0,
                centerY + radius < yCount
            };
            while (xInBounds[0] || xInBounds[1] || yInBounds[0] || yInBounds[1])
            {
                int dxMax = Math.Min(radius - 1, xCount - 1 - centerX);
                int dxMin = -Math.Min(radius - 1, centerX);
                int dyMax = Math.Min(radius, yCount - 1 - centerY);
                int dyMin = -Math.Min(radius, centerY);

                for (int absDr = 0; absDr <= radius; absDr++)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        int boundsSign = (i << 1) - 1;

                        for (int j = -1; j <= 1; j += 2)
                        {
                            int dr = j * absDr;
                            if (yInBounds[i] && dr >= dxMin && dr <= dxMax)
                            {
                                int y = centerY + radius * boundsSign;
                                int x = centerX + dr;
                                if (GetCell(x, y, out cell, out _) && predicate(cell))
                                {
                                    return true;
                                }
                            }
                            if (xInBounds[i] && dr >= dyMin && dr <= dyMax)
                            {
                                int x = centerX + radius * boundsSign;
                                int y = centerY + dr;
                                if (GetCell(x, y, out cell, out _) && predicate(cell))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
                radius++;
                xInBounds[0] = centerX - radius >= 0;
                xInBounds[1] = centerX + radius < xCount;
                yInBounds[0] = centerY - radius >= 0;
                yInBounds[1] = centerY + radius < yCount;
            }
            return false;
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
            var cc = triangle.CircumCircle;
            var ccBounds = cc.Bounds;
            ccBounds.min -= Vector2.One * cellTolerance;
            ccBounds.max += Vector2.One * cellTolerance;
            int begX = Maths.Clamp((int)(ccBounds.min.x / cellSize.x), 0, xCount - 1);
            int endX = Maths.Clamp((int)(ccBounds.max.x / cellSize.x), 0, xCount - 1);
            int begY = Maths.Clamp((int)(ccBounds.min.y / cellSize.y), 0, yCount - 1);
            int endY = Maths.Clamp((int)(ccBounds.max.y / cellSize.y), 0, yCount - 1);

            var cellIndices = cellIndicesPool.Pop();
            triangleCellsDict.Add(triangle.Key, cellIndices);

            for (int y = begY; y <= endY; y++)
            {
                for (int x = begX; x <= endX; x++)
                {
                    if (GetCellOverlap(x, y, cc, out int cellIndex))
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
                    SetCellFilled(selectedCell, false);
                }
                selectedCell = cell;
                selectedCellXY = cellXY;
                SetCellFilled(cell, true);

                //cell.DebugPoint = default;
                //foreach (int tIndex in cell.TriangleIndices)
                //{
                //    GetCellOverlap(x, y, triangles[tIndex].CircumCircle, out _);
                //}
            }
        }

        public bool GetCell(Vector2 p, out TriangleCell cell)
        {
            return GetCell(p, out cell, out _, out _);
        }

        public bool GetCell(Vector2 p, out TriangleCell cell, out int x, out int y)
        {
            x = (int)(p.x / cellSize.x);
            y = (int)(p.y / cellSize.y);
            return GetCell(x, y, out cell, out _);
        }

        public bool GetCell(Vector2 p, out TriangleCell cell, out Vector2Int cellXY)
        {
            bool result = GetCell(p, out cell, out int x, out int y);
            cellXY = new Vector2Int(x, y);
            return result;
        }

        public bool GetFirstExternalEdgeInCell(TriangleCell cell, EdgeInfo edgeInfo, out EdgeEntry extEdge, out Triangle extTriangle)
        {
            extEdge = EdgeEntry.None;
            extTriangle = Triangle.None;

            foreach (var key in cell.TriangleKeys)
            {
                ref var triangle = ref GetTriangleRef(key, out _);
                if (edgeInfo.GetFirstExternalEdge(triangle, out extEdge))
                {
                    extTriangle = triangle;
                    return true;
                }
            }
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

        private void SetCellFilled(TriangleCell cell, bool filled)
        {
            int pointsLength = triangleSet.PointsLength;
            cell.Filled = filled;
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
            cellIndex = -1;
            cell = null;
            return false;
        }

        private bool GetCellOverlap(int x, int y, Circle cc, out int cellIndex)
        {
            cellIndex = y * xCount + x;
            var ccCenter = cc.Center;
            var cellMin = new Vector2(x * cellSize.x, y * cellSize.y);
            var cellMax = cellMin + cellSize;
            cellMin -= Vector2.One * cellTolerance;
            cellMax += Vector2.One * cellTolerance;
            var cellBounds = new Bounds2(cellMin, cellMax);

            if (!cellBounds.Overlap(cc.Bounds))
            {
                cells[cellIndex].DebugPoint = default;
                return false;
            }

            bool overlap = (ccCenter.x >= cellMin.x && ccCenter.x <= cellMax.x) ||
                           (ccCenter.y >= cellMin.y && ccCenter.y <= cellMax.y);
            var cellCenter = cellBounds.Center;
            cells[cellIndex].DebugPoint = cellCenter;
            if (!overlap)
            {
                var dr = ccCenter - cellCenter;
                var n = new Vector2(Math.Sign(dr.x), Math.Sign(dr.y));
                var cellVert = cellCenter + n * cellHalfSize;
                float sqrL = (cellVert - ccCenter).SqrLength;
                overlap = sqrL <= cc.SqrRadius + cellTolerance;
                cells[cellIndex].DebugPoint = cellVert;
            }
            return overlap;
        }

        private Bounds2 GetCellBounds(int x, int y)
        {
            var cellMin = new Vector2(x * cellSize.x, y * cellSize.y);
            var cellMax = cellMin + cellSize;
            return new Bounds2(cellMin, cellMax);
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
