using System.Numerics;

namespace Triangulation
{
    public struct DeprecatedGridPoint
    {
        public static readonly DeprecatedGridPoint None = new DeprecatedGridPoint(Veconst2.NaN);

        public Vector2 p;
        public Vector2Int xy;

        public DeprecatedGridPoint(Vector2 point, Vector2 cellSize) : this()
        {
            p = GridUtils.SnapToGrid(point, cellSize, out xy);
        }

        public DeprecatedGridPoint(Vector2 point)
        {
            p = point;
            xy = -1 * Vector2Int.One;
        }
    }
}
