#define THROW_POINT_OUT_OF_BOUNDS_EXCEPTION

using System;
using System.Collections.Generic;
using System.Numerics;
#if DEBUG_CLOSEST_CELLS
using System.Drawing;
#endif

namespace Triangulation
{
    public class PointGrid
    {
        public event Action<int, Vector2> PointAdded = delegate { };
        public event Action<int> PointCleared = delegate { };

        public int YCount => yCount;
        public int XCount => xCount;
        public Vector2Int XYCount => new Vector2Int(xCount, yCount);
        public float CellSizeMin => MathF.Min(cellSize.X, cellSize.Y);
        public Vector2 CellSize => cellSize;
        public Vector2 Size => size;
        public int[] Indices => indices;
        public Vector2Int[] PointsXY => pointsXY;

        private readonly int xCount, yCount = 0;
        private readonly Vector2 cellSize = default;
        private readonly Vector2 size = default;

        private readonly int[] clearIndices = null;
        private readonly int[] indices = null;

        private readonly Vector2Int[] pointsXY = null;

        public PointGrid(Vector2 size, Vector2Int xyCount)
        {
            this.size = size;

            xCount = xyCount.x;
            yCount = xyCount.y;

            cellSize = new Vector2(size.X / xCount, size.Y / yCount);

            xCount++;
            yCount++;

            indices = new int[xCount * yCount];
            clearIndices = new int[indices.Length];

            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = clearIndices[i] = -1;
            }
            pointsXY = new Vector2Int[indices.Length << 2];
        }

        public void Clear()
        {
            ClearIndices();
        }

        public int SetGridPoints(Triangulator triangulator, out Bounds2 bounds, out bool xySort, bool reset)
        {
            var points = triangulator.Points;
            int offset = triangulator.PointsOffset;
            int pointsCount = triangulator.PointsCount;

            if (pointsCount <= 0)
            {
                bounds = default;
                xySort = true;
                return 0;
            }
            Clear();

            if (reset)
            {
                SetGridPointsXY(points, offset, pointsCount);

                SortGridPoints(triangulator, out _, out xySort);

                return SetSortedPoints(points, offset, pointsCount, out bounds);
            }
            else
            {
                SortGridPoints(triangulator, out var xyBounds, out xySort);

                // Refresh indices
                {
                    for (int i = 0; i < offset; i++)
                    {
                        PointAdded(i, points[i]);
                    }
                    for (int i = offset; i < pointsCount; i++)
                    {
                        int cellIndex = GetCellIndex(pointsXY[i]);
                        indices[cellIndex] = i;
                        PointAdded(i, points[i]);
                    }
                }
                bounds = Bounds2.MinMax(xyBounds.min * cellSize, xyBounds.max * cellSize);

                return pointsCount;
            }
        }

        public int SetGridPoints(SerializedTriangulator data, Vector2[] points)
        {
            Clear();

            var gridPoints = data.GridPoints;
            var superPoints = data.SuperPoints;
            int pointsCount = gridPoints[^1].Index + 1;
            int offset = superPoints.Length;

            for (int i = 0; i < offset; i++)
            {
                points[i] = superPoints[i].GetXY();
                pointsXY[i] = Vector2Int.NegativeOne;
                PointAdded(i, points[i]);
            }
            for (int i = offset; i < pointsCount; i++)
            {
                points[i] = Veconst2.NaN;
                pointsXY[i] = Vector2Int.NegativeOne;
                PointCleared(i);
            }
            int gridPointsCount = gridPoints.Length;
            for (int i = 0; i < gridPointsCount; i++)
            {
                var gridPoint = gridPoints[i];
                if (GetCellIndex(gridPoint.X, gridPoint.Y, out int cellIndex))
                {
                    int pointIndex = gridPoint.Index;
                    var cellXY = gridPoint.GetXY();
                    AddPoint(pointIndex, points, cellXY, cellIndex);
                }
            }
            return pointsCount;
        }

        public void ClearPoint(int pointIndex, Vector2[] points)
        {
            if (GetCellIndex(pointsXY[pointIndex], out int cellIndex))
            {
                ClearPoint(pointIndex, cellIndex, points);
            }
        }

        public bool CanAddPoint(Vector2Int xy, out int index)
        {
            bool onGrid = GetCellIndex(xy, out index);
            int savedIndex = onGrid ? indices[index] : -1;
            return onGrid && savedIndex < 0;
        }

        public bool CanAddPoint(Vector2 point, out Vector3Int cellXYI, out int savedIndex, bool throwOffGridException = true)
        {
            bool onGrid = GetCellXYIndex(point, out cellXYI, throwOffGridException);
            savedIndex = onGrid ? indices[cellXYI.z] : -1;
            return onGrid && savedIndex < 0;
        }

        public void AddPoint(int pointIndex, Vector2[] points, Vector3Int cellXYI)
        {
            AddPoint(pointIndex, points, cellXYI, cellXYI.z);
        }

        public void AddPoint(int pointIndex, Vector2[] points, Vector2Int cellXY)
        {
            AddPoint(pointIndex, points, cellXY, GetCellIndex(cellXY.x, cellXY.y));
        }

        private void AddPoint(int pointIndex, Vector2[] points, Vector2Int cellXY, int cellIndex)
        {
            indices[cellIndex] = pointIndex;
            points[pointIndex] = cellXY * cellSize;
            pointsXY[pointIndex] = cellXY;
            PointAdded(pointIndex, points[pointIndex]);
        }

        private void ClearPoint(int pointIndex, int cellIndex, Vector2[] points)
        {
            indices[cellIndex] = -1;
            points[pointIndex] = Veconst2.NaN;
            pointsXY[pointIndex] = Vector2Int.NegativeOne;
            PointCleared(pointIndex);
        }

        public bool GetPointIndex(Vector2 point, out int pointIndex)
        {
            bool result = GetCellXYIndex(point, out var cellXYI);
            pointIndex = result ? indices[cellXYI.z] : -1;
            return result;
        }

        public Vector2 GetPosition(Vector2Int xy) => xy * cellSize;

        public Vector2 SnapToGrid(Vector2 point) => GridUtils.SnapToGrid(point, cellSize, out _);

        public bool IsPointBoundary(int pointIndex)
        {
            int x = pointIndex % yCount;
            int y = pointIndex / yCount;
            return x == 0 || x == xCount - 1 || y == 0 || y == yCount - 1;
        }

        public int GetClosestPointIndex(Vector2 center)
        {
#if DEBUG_CLOSEST_CELLS
            bool cellPredicate(Vector3Int xyi, Color color, string s) => indices[xyi.z] >= 0;
#else
            bool cellPredicate(Vector3Int xyi) => indices[xyi.z] >= 0;
#endif
            if (GetClosestCellWithPredicate(center, out var cellXYI, cellPredicate))
            {
                //Log.WriteLine("PointGrid.GetClosestPointIndex: cellXYI: " + cellXYI + " " + indices[cellXYI.z]);
                return indices[cellXYI.z];
            }
            return -1;
        }

        public bool GetClosestClearCell(Vector2 point, out Vector3Int cellXYI)
        {
#if DEBUG_CLOSEST_CELLS
            bool cellPredicate(Vector3Int xyi, Color color, string s) => indices[xyi.z] < 0;
#else
            bool cellPredicate(Vector3Int xyi) => indices[xyi.z] < 0;
#endif
            return GetClosestCellWithPredicate(point, out cellXYI, cellPredicate);
        }

        private bool GetClosestCellWithPredicate(Vector2 point, out Vector3Int cellXYI,
#if DEBUG_CLOSEST_CELLS
            Func<Vector3Int, Color, string, bool> predicate)
#else
            Predicate<Vector3Int> predicate)
#endif
        {
            if (GetCellXYIndex(point, out cellXYI))
            {
                return GridUtils.FindClosestCellWithPredicate(cellXYI.x, cellXYI.y, xCount, yCount, out cellXYI, predicate);
            }
            return false;
        }

        private void SortGridPoints(Triangulator triangulator, out Bounds2Int xyBounds, out bool xySort)
        {
            int offset = triangulator.PointsOffset;
            var points = triangulator.Points;
            int pointsCount = triangulator.PointsCount;
            var sortingOrder = triangulator.PointsSortingOrder;

            xyBounds = Bounds2Int.GetBounds(pointsXY, offset, pointsCount - 1);
            var pointsComparer = GetPointsComparer(xyBounds, sortingOrder, out xySort);
            Array.Sort(pointsXY, points, offset, pointsCount - offset, pointsComparer);
        }

        private static IComparer<Vector2Int> GetPointsComparer(Bounds2Int bounds, PointsSortingOrder sortingOrder, out bool xySort)
        {
            Vector2Int boundsSize = bounds.Size;
            switch (sortingOrder)
            {
                case PointsSortingOrder.Default:
                    xySort = boundsSize.x > boundsSize.y;
                    break;
                case PointsSortingOrder.XY:
                    xySort = true;
                    break;
                case PointsSortingOrder.YX:
                    xySort = false;
                    break;
                default:
                    xySort = true;
                    break;
            }
            return xySort ? new GridPointsXYComparer() : new GridPointsYXComparer();
        }

        private void SetGridPointsXY(Vector2[] points, int offset, int pointsCount)
        {
            for (int i = 0; i < offset; i++)
            {
                pointsXY[i] = Vector2Int.NegativeOne;
            }
            for (int i = offset; i < pointsCount; i++)
            {
                pointsXY[i] = GridUtils.SnapToGrid(points[i], cellSize);
            }
        }

        private int SetSortedPoints(Vector2[] points, int offset, int pointsCount, out Bounds2 bounds)
        {
            if (pointsCount <= 0)
            {
                bounds = default;
                return 0;
            }
            int count = offset;
            var minXY = pointsXY[offset];
            var maxXY = minXY;

            for (int i = 0; i < offset; i++)
            {
                pointsXY[i] = Vector2Int.NegativeOne;
                PointAdded(i, points[i]);
            }
            for (int i = offset; i < pointsCount; i++)
            {
                var xy = pointsXY[i];
                if (CanAddPoint(xy, out int index))
                {
                    AddPoint(count++, points, xy, index);
                    minXY = Vector2Int.Min(minXY, xy);
                    maxXY = Vector2Int.Max(maxXY, xy);
                }
            }
            for (int i = count; i < pointsCount; i++)
            {
                points[i] = Veconst2.NaN;
                pointsXY[i] = Vector2Int.NegativeOne;
                PointCleared(i);
            }
            bounds = Bounds2.MinMax(minXY * cellSize, maxXY * cellSize);

            //Log.PrintArray(pointsXY, pointsCount, "SetSortedPoints: pointsXY");
            //Log.PrintArray(points, pointsCount, "SetSortedPoints: points");

            return count;
        }

        //private bool TryAddPoint(int pointIndex, Vector2[] points, out Vector3Int cellXYI, out int savedIndex)
        //{
        //    return TryAddPoint(pointIndex, points[pointIndex], point => points[pointIndex] = point, out cellXYI, out savedIndex);
        //}

        //private bool TryAddPoint(int pointIndex, List<Vector2> points, out Vector3Int cellXYI, out int savedIndex)
        //{
        //    return TryAddPoint(pointIndex, points[pointIndex], point => points[pointIndex] = point, out cellXYI, out savedIndex);
        //}

        //private bool TryAddPoint(int pointIndex, Vector2[] points, out Vector3Int cellXYI, out int savedIndex)
        //{
        //    var point = points[pointIndex];
        //
        //    if (Mathv.IsNaN(point))
        //    {
        //        cellXYI = -1 * Vector3Int.One;
        //        savedIndex = -1;
        //        return false;
        //    }
        //    if (CanAddPoint(point, out cellXYI, out savedIndex))
        //    {
        //        AddPoint(pointIndex, points, cellXYI);
        //        //Log.WriteLine(GetType() + ".TryAddPoint: " + cellXYI + ", " + savedIndex + " " + result + " " + point);
        //        return true;
        //    }
        //    //else
        //    //{
        //    //    Log.WriteWarning(GetType() + ".TryAddPoint failed: " + cellXYI + ", " + savedIndex + " " + point);
        //    //}
        //    return false;
        //}

        //private int GetPointIndex(Vector2Int cellXY)
        //{
        //    return GetPointIndex(cellXY.x, cellXY.y);
        //}

        //private int GetPointIndex(int x, int y)
        //{
        //    if (GetCellIndex(x, y, out int cellIndex))
        //    {
        //        return indices[cellIndex];
        //    }
        //    return -1;
        //}

        private bool GetCellIndex(Vector2Int cellXY, out int cellIndex)
        {
            return GetCellIndex(cellXY.x, cellXY.y, out cellIndex);
        }

        private bool GetCellIndex(int x, int y, out int cellIndex)
        {
            cellIndex = GetCellIndex(x, y);
            return cellIndex >= 0;
        }

        private int GetCellIndex(Vector2Int cellXY)
        {
            return GetCellIndex(cellXY.x, cellXY.y);
        }

        private int GetCellIndex(int x, int y)
        {
            if (x >= 0 && x < xCount && y >= 0 && y < yCount)
            {
                return x + y * xCount;
            }
            else
            {
                return -1;
            }
        }

        private bool GetCellXYIndex(Vector2 point, out Vector3Int cellXYI, bool throwException = true)
        {
            int x = (int)(point.X / cellSize.X + 0.5f);
            int y = (int)(point.Y / cellSize.Y + 0.5f);

            bool inBounds = GetCellIndex(x, y, out int cellIndex);
            cellXYI = new Vector3Int(x, y, cellIndex);

#if THROW_POINT_OUT_OF_BOUNDS_EXCEPTION
            if (throwException && !inBounds)
            {
                throw new ArgumentOutOfRangeException("GetCellXYIndex: " + point + " for size: " + size + " cellXYI: " + cellXYI);
            }
#endif
            return inBounds;
        }

        private void ClearIndices()
        {
            Array.Copy(clearIndices, indices, indices.Length);
        }
    }
}
