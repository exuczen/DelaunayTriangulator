#define THROW_POINT_OUT_OF_BOUNDS_EXCEPTION

using System;
using System.Collections.Generic;

namespace Triangulation
{
    public class PointGrid
    {
        public int YCount => yCount;
        public int XCount => xCount;
        public float CellSizeMin => MathF.Min(cellSize.x, cellSize.y);
        public Vector2 CellSize => cellSize;
        public Vector2 Size => size;
        public int[] Indices => indices;

        private readonly HashSet<Vector3Int> offGridXYI = new();
        private readonly List<Vector3Int> offGridXYIPool = new();

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
            offGridXYI.Clear();
        }

        public bool AddOffGridPointXYI(Vector2 point)
        {
            if (GetCellXYIndex(point, out var xyi, false))
            {
                throw new ArgumentException("AddOffGridPointXYI: Point is on grid: " + xyi);
            }
            else if (offGridXYI.Contains(xyi))
            {
                throw new ArgumentException("AddOffGridPointXYI: offGridXYI.Contains(xyi): " + xyi);
            }
            else
            {
                offGridXYI.Add(xyi);
                return true;
            }
        }

        public int SetPoints(Vector2[] points, int pointsCount, out Bounds2 bounds)
        {
            if (pointsCount <= 0)
            {
                bounds = default;
                return 0;
            }
            ClearIndices();

            int count = 0;
            int offset = pointsCount;
            int offGridIndex = -1;

            var min = points[0];
            var max = min;

            offGridXYIPool.Clear();
            offGridXYIPool.AddRange(offGridXYI);

            void addPoint(int i)
            {
                var point = points[i];
                points[offset + count++] = point;
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            for (int i = 0; i < pointsCount; i++)
            {
                if (TryAddPoint(i, points, out var xyi))
                {
                    indices[xyi.z] = count;
                    addPoint(i);
                }
                else if (xyi.z < 0 && (offGridIndex = offGridXYIPool.IndexOf(xyi)) >= 0)
                {
                    offGridXYIPool.RemoveAt(offGridIndex);
                    addPoint(i);
                }
#if THROW_POINT_OUT_OF_BOUNDS_EXCEPTION
                else
                {
                    throw new ArgumentOutOfRangeException("SetPoints: " + xyi);
                }
#endif
            }
            bounds = new Bounds2(min, max);
            Array.Copy(points, offset, points, 0, count);
            return count;
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
            if (GetCellXYIndex(points[pointIndex], out var cellXYI, false))
            {
                indices[cellXYI.z] = -1;
                points[pointIndex] = default;
            }
        }

        public bool CanAddPoint(Vector2 point, out Vector3Int cellXYI, out int savedIndex, bool throwOffGridException)
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
            bool result = GetCellXYIndex(point, out var cellXYI, true);
            pointIndex = result ? indices[cellXYI.z] : -1;
            return result;
        }

        private bool TryAddPoint(int pointIndex, Vector2[] points, out Vector3Int cellXYI)
        {
            return TryAddPoint(pointIndex, points[pointIndex], point => points[pointIndex] = point, out cellXYI);
        }

        private bool TryAddPoint(int pointIndex, List<Vector2> points, out Vector3Int cellXYI)
        {
            return TryAddPoint(pointIndex, points[pointIndex], point => points[pointIndex] = point, out cellXYI);
        }

        private bool TryAddPoint(int pointIndex, Vector2 point, Action<Vector2> setPoint, out Vector3Int cellXYI)
        {
            bool result = CanAddPoint(point, out cellXYI, out _, false);
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

        private bool GetCellXYIndex(Vector2 point, out Vector3Int cellXYI, bool throwException)
        {
            int x = (int)((point.x + cellHalfSize.x) / cellSize.x);
            int y = (int)((point.y + cellHalfSize.y) / cellSize.y);
            bool inBounds = x >= 0 && x < xCount && y >= 0 && y < yCount;
            cellXYI = new Vector3Int(x, y, inBounds ? x + y * xCount : -1);
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
