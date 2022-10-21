#define THROW_POINT_OUT_OF_BOUNDS_EXCEPTION

using System;
using System.Collections.Generic;

namespace Triangulation
{
    public class PointGrid
    {
        public int YCount => yCount;
        public int XCount => xCount;
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
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = -1;
            }
        }

        public void SetPoints(Vector2[] points, int pointsCount)
        {
            Clear();
            for (int i = 0; i < pointsCount; i++)
            {
                TryAddPoint(i, points, out _);
            }
        }

        public void SetPoints(List<Vector2> points)
        {
            Clear();
            for (int i = 0; i < points.Count; i++)
            {
                if (!TryAddPoint(i, points, out _))
                {
                    points.RemoveAt(i--);
                }
            }
        }

        public void ClearPoint(int pointIndex, Vector2[] points)
        {
            if (GetCellIndex(points[pointIndex], out int cellIndex, out _))
            {
                indices[cellIndex] = -1;
                points[pointIndex] = default;
            }
        }

        public bool TryAddPoint(int pointIndex, Vector2[] points, out int savedIndex)
        {
            return TryAddPoint(pointIndex, points[pointIndex], point => points[pointIndex] = point, out savedIndex);
        }

        public bool TryAddPoint(int pointIndex, List<Vector2> points, out int savedIndex)
        {
            return TryAddPoint(pointIndex, points[pointIndex], point => points[pointIndex] = point, out savedIndex);
        }

        public bool CanAddPoint(Vector2 point, out Vector3Int cellXYI, out int savedIndex)
        {
            bool onGrid = GetCellIndex(point, out int cellIndex, out cellXYI);
            savedIndex = onGrid ? indices[cellIndex] : -1;
            return onGrid && savedIndex < 0;
        }

        public void AddPoint(int pointIndex, Vector2[] points, Vector3Int cellXYI)
        {
            int cellIndex = cellXYI.z;
            indices[cellIndex] = pointIndex;
            points[pointIndex] = new Vector2(cellXYI.x * cellSize.x, cellXYI.y * cellSize.y);
        }

        public bool GetPointIndex(Vector2 point, out int pointIndex)
        {
            bool result = GetCellIndex(point, out int cellIndex, out _);
            pointIndex = result ? indices[cellIndex] : -1;
            return result;
        }

        private bool TryAddPoint(int pointIndex, Vector2 point, Action<Vector2> setPoint, out int savedIndex)
        {
            bool result = false;
            if (CanAddPoint(point, out var cellXYI, out savedIndex))
            {
                int cellIndex = cellXYI.z;
                indices[cellIndex] = pointIndex;
                setPoint(new Vector2(cellXYI.x * cellSize.x, cellXYI.y * cellSize.y));
                result = true;
            }
            //Log.WriteLine(GetType() + ".TryAddPoint: " + cellXYI + ", " + savedIndex + " " + result);
            return result;
        }

        private bool GetCellIndex(Vector2 point, out int cellIndex, out Vector3Int cellXYI)
        {
            int x = (int)((point.x + cellHalfSize.x) / cellSize.x);
            int y = (int)((point.y + cellHalfSize.y) / cellSize.y);
            bool inBounds = x >= 0 && x < xCount && y >= 0 && y < yCount;
#if THROW_POINT_OUT_OF_BOUNDS_EXCEPTION
            if (inBounds)
            {
                cellXYI = new Vector3Int(x, y, cellIndex = x + y * xCount);
                return true;
            }
            else
            {
                throw new ArgumentOutOfRangeException("GetCellIndex: " + point + " for size: " + size);
            }
#else
            cellIndex = inBounds ? x + y * xCount : -1;
            cellXYI = new Vector3Int(x, y, cellIndex);
            return inBounds;
#endif
        }
    }
}
