using System;

namespace Triangulation
{
    public struct GeomUtils
    {
        public static Vector2 GetPositionOnCircle(float angle, Vector2 center, float r)
        {
            float x = center.x + r * MathF.Cos(angle);
            float y = center.y + r * MathF.Sin(angle);
            return new Vector2(x, y);
        }
    }
}
