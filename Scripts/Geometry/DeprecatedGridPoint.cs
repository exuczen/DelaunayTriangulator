using System.Numerics;

namespace Triangulation
{
    public struct DeprecatedGridPoint
    {
        public static readonly DeprecatedGridPoint None = new DeprecatedGridPoint(Veconst2.NaN);

        public Vector2 p;
        public Vector2Int xy;

        public static Vector2 SnapToGrid(Vector2 point, Vector2 cellSize, out Vector2Int xy)
        {
            int x = (int)(point.X / cellSize.X + 0.5f);
            int y = (int)(point.Y / cellSize.Y + 0.5f);
            xy = new Vector2Int(x, y);
            return xy * cellSize;
        }

        public DeprecatedGridPoint(Vector2 point, Vector2 cellSize) : this()
        {
            p = SnapToGrid(point, cellSize, out xy);
        }

        public DeprecatedGridPoint(Vector2 point)
        {
            p = point;
            xy = -1 * Vector2Int.One;
        }
    }
}
