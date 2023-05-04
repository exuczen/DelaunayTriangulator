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
            SnapPointsToGrid(pointsXY, points, offset, pointsCount);

            // Sort grid points
            {
                var xyBounds = Bounds2Int.GetBounds(pointsXY, offset, pointsCount - 1);
                var pointsComparer = GetPointsComparer(xyBounds, sortingOrder, out xySort);
                Array.Sort(pointsXY, points, offset, pointsCount - offset, pointsComparer);
            }
            return SetSortedPoints(pointsXY, points, offset, pointsCount, out bounds);
        }

        public void SetPoints(Vector2[] points, int offset, int pointsCount)
        {
            if (pointsCount <= 0)
            {
                return;
            }
            ClearIndices();

            for (int i = offset; i < pointsCount; i++)
            {
                TryAddPoint(i, points, out _, out _);
            }
        }

        public void ClearPoint(int pointIndex, Vector2[] points)
        {
            if (GetCellXYIndex(points[pointIndex], out var cellXYI, false))
            {
                indices[cellXYI.z] = -1;
                points[pointIndex] = default;
            }
        }

        public bool CanAddPoint(Vector2 point, out Vector3Int cellXYI, out int savedIndex, bool throwOffGridException = true)
        {
            bool onGrid = GetCellXYIndex(point, out cellXYI, throwOffGridException);
            savedIndex = onGrid ? indices[cellXYI.z] : -1;
            return onGrid && savedIndex < 0;
        }

        public void AddPoint(int pointIndex, Vector2[] points, Vector3Int cellXYI)
        {
            indices[cellXYI.z] = pointIndex;
            points[pointIndex] = new Vector2(cellXYI.x * cellSize.X, cellXYI.y * cellSize.Y);
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

        public Vector2 SnapToGrid(Vector2 point, out Vector2Int xy)
        {
            int x = (int)(point.X / cellSize.X + 0.5f);
            int y = (int)(point.Y / cellSize.Y + 0.5f);
            xy = new Vector2Int(x, y);
            return new Vector2(x * cellSize.X, y * cellSize.Y);
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

        private IComparer<Vector2Int> GetPointsComparer(Bounds2Int bounds, PointsSortingOrder sortingOrder, out bool xySort)
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

        private void SnapPointsToGrid(Vector2Int[] pointsXY, Vector2[] points, int offset, int pointsCount)
        {
            for (int i = offset; i < pointsCount; i++)
            {
                points[i] = SnapToGrid(points[i], out pointsXY[i]);
            }
        }

        private int SetSortedPoints(Vector2Int[] pointsXY, Vector2[] points, int offset, int pointsCount, out Bounds2 bounds)
        {
            if (pointsCount <= 0)
            {
                bounds = default;
                return 0;
            }
            ClearIndices();

            int count = 0;
            var minXY = pointsXY[offset];
            var maxXY = minXY;
            var prevXY = -1 * Vector2Int.One;

            for (int i = offset; i < pointsCount; i++)
            {
                var xy = pointsXY[i];
                if (xy != prevXY && GetCellIndex(xy, out int index))
                {
                    var point = points[i];
                    indices[index] = offset + count;
                    points[pointsCount + count++] = point;
                    minXY = Vector2Int.Min(minXY, xy);
                    maxXY = Vector2Int.Max(maxXY, xy);
                }
                prevXY = xy;
            }
            bounds = Bounds2.MinMax(minXY * cellSize, maxXY * cellSize);

            Array.Copy(points, pointsCount, points, offset, count);

            //Log.PrintArray(pointsXY, pointsCount, "SetGridPoints: pointsXY");
            //Log.PrintArray(points, pointsCount, "SetGridPoints: points");

            return offset + count;
        }

        private bool TryAddPoint(int pointIndex, Vector2[] points, out Vector3Int cellXYI, out int savedIndex)
        {
            return TryAddPoint(pointIndex, points[pointIndex], point => points[pointIndex] = point, out cellXYI, out savedIndex);
        }

        private bool TryAddPoint(int pointIndex, List<Vector2> points, out Vector3Int cellXYI, out int savedIndex)
        {
            return TryAddPoint(pointIndex, points[pointIndex], point => points[pointIndex] = point, out cellXYI, out savedIndex);
        }

        private bool TryAddPoint(int pointIndex, Vector2 point, Action<Vector2> setPoint, out Vector3Int cellXYI, out int savedIndex)
        {
            if (Mathv.IsNaN(point))
            {
                cellXYI = -1 * Vector3Int.One;
                savedIndex = -1;
                return false;
            }
            bool result = CanAddPoint(point, out cellXYI, out savedIndex);
            if (result)
            {
                int cellIndex = cellXYI.z;
                indices[cellIndex] = pointIndex;
                setPoint(new Vector2(cellXYI.x * cellSize.X, cellXYI.y * cellSize.Y));
                //Log.WriteLine(GetType() + ".TryAddPoint: " + cellXYI + ", " + savedIndex + " " + result + " " + point);
            }
            //else
            //{
            //    //Log.WriteWarning(GetType() + ".TryAddPoint: " + cellXYI + ", " + savedIndex + " " + result + " " + point);
            //}
            return result;
        }

        private int GetPointIndex(Vector2Int cellXY)
        {
            return GetPointIndex(cellXY.x, cellXY.y);
        }

        private int GetPointIndex(int x, int y)
        {
            if (GetCellIndex(x, y, out int cellIndex))
            {
                return indices[cellIndex];
            }
            return -1;
        }

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
