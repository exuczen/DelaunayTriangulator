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
        //private readonly Vector2 cellHalfSize = default;
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
            //cellHalfSize = cellSize * 0.5f;

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

        public int SetGridPoints(Vector2[] points, int offset, int pointsCount, PointsSortingOrder sortingOrder, out Bounds2 bounds, out bool xySort)
        {
            if (pointsCount <= 0)
            {
                bounds = default;
                xySort = true;
                return 0;
            }
            SetGridPointsXY(points, offset, pointsCount);

            // Sort grid points
            {
                var xyBounds = Bounds2Int.GetBounds(pointsXY, offset, pointsCount - 1);
                var pointsComparer = GetPointsComparer(xyBounds, sortingOrder, out xySort);
                Array.Sort(pointsXY, points, offset, pointsCount - offset, pointsComparer);
            }
            return SetSortedPoints(points, offset, pointsCount, out bounds);
        }

        public void SetGridPoints(SerializedGridPoint2[] gridPoints, Vector2[] points)
        {
            ClearIndices();

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
        }

        public void ClearPoint(int pointIndex, Vector2[] points)
        {
            if (GetCellXYIndex(points[pointIndex], out var cellXYI, false))
            {
                indices[cellXYI.z] = -1;
                points[pointIndex] = Veconst2.NaN;
                pointsXY[pointIndex] = Vector2Int.NegativeOne;
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

        private void AddPoint(int pointIndex, Vector2[] points, Vector2Int cellXY, int cellIndex)
        {
            indices[cellIndex] = pointIndex;
            points[pointIndex] = cellXY * cellSize;
            pointsXY[pointIndex] = cellXY;
        }

        public bool GetPointIndex(Vector2 point, out int pointIndex)
        {
            bool result = GetCellXYIndex(point, out var cellXYI);
            pointIndex = result ? indices[cellXYI.z] : -1;
            return result;
        }

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
            ClearIndices();

            int count = offset;
            var minXY = pointsXY[offset];
            var maxXY = minXY;

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

        private bool GetCellIndex(Vector2Int cellXY, out int index)
        {
            return GetCellIndex(cellXY.x, cellXY.y, out index);
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

        private bool GetCellIndex(int x, int y, out int cellIndex)
        {
            cellIndex = GetCellIndex(x, y);
            return cellIndex >= 0;
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
