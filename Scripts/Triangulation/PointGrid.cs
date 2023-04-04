#define THROW_POINT_OUT_OF_BOUNDS_EXCEPTION

using System;
using System.Collections.Generic;
using System.Drawing;

namespace Triangulation
{
    public class PointGrid
    {
        public int YCount => yCount;
        public int XCount => xCount;
        public Vector2Int XYCount => new Vector2Int(xCount, yCount);
        public float CellSizeMin => MathF.Min(cellSize.x, cellSize.y);
        public Vector2 CellSize => cellSize;
        public Vector2 Size => size;
        public int[] Indices => indices;

        private readonly int xCount, yCount = 0;
        private readonly Vector2 cellSize = default;
        private readonly Vector2 cellHalfSize = default;
        private readonly Vector2 size = default;

        private readonly int[] indices = null;

        public PointGrid(Vector2 size, Vector2Int xyCount)
        {
            this.size = size;

            xCount = xyCount.x;
            yCount = xyCount.y;

            cellSize = new Vector2(size.x / xCount, size.y / yCount);
            cellHalfSize = cellSize * 0.5f;

            xCount++;
            yCount++;

            indices = new int[xCount * yCount];

            Clear();
        }

        public void Clear()
        {
            ClearIndices();
        }

        public int SetPoints(Vector2[] points, int offset, int pointsCount, out Bounds2 bounds)
        {
            if (pointsCount <= 0)
            {
                bounds = default;
                return 0;
            }
            ClearIndices();

            int count = 0;
            var min = points[offset];
            var max = min;

            for (int i = offset; i < pointsCount; i++)
            {
                if (TryAddPoint(i, points, out var xyi, out _))
                {
                    var point = points[i];
                    indices[xyi.z] = offset + count;
                    points[pointsCount + count++] = point;
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            }
            bounds = Bounds2.MinMax(min, max);
            Array.Copy(points, pointsCount, points, offset, count);
            return offset + count;
        }

        public void SetPoints(List<Vector2> points, int offset)
        {
            Clear();
            for (int i = offset; i < points.Count; i++)
            {
                if (!TryAddPoint(i, points, out _, out _))
                {
                    points.RemoveAt(i--);
                }
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
            points[pointIndex] = new Vector2(cellXYI.x * cellSize.x, cellXYI.y * cellSize.y);
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
            bool result = CanAddPoint(point, out cellXYI, out savedIndex);
            if (result)
            {
                int cellIndex = cellXYI.z;
                indices[cellIndex] = pointIndex;
                setPoint(new Vector2(cellXYI.x * cellSize.x, cellXYI.y * cellSize.y));
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

        private bool GetCellIndex(int x, int y, out int cellIndex)
        {
            cellIndex = GetCellIndex(x, y);
            return cellIndex >= 0;
        }

        private bool GetCellXYIndex(Vector2 point, out Vector3Int cellXYI, bool throwException = true)
        {
            int x = (int)((point.x + cellHalfSize.x) / cellSize.x);
            int y = (int)((point.y + cellHalfSize.y) / cellSize.y);

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
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = -1;
            }
        }
    }
}
