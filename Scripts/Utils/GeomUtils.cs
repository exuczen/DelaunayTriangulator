using System;
using System.Numerics;

namespace Triangulation
{
    public struct GeomUtils
    {
        public static Vector2 GetPositionOnCircle(float angle, Vector2 center, float r)
        {
            float x = center.X + r * MathF.Sin(angle);
            float y = center.Y + r * MathF.Cos(angle);
            return new Vector2(x, y);
        }

        public static void AddCirclePoints(Vector2[] points, int offset, Vector2 center, float r, int count, float begAngle = 0f)
        {
            float dalfa = 2f * MathF.PI / count;
            int index = offset;
            for (int i = 0; i < count; i++)
            {
                points[index++] = GetPositionOnCircle(begAngle + i * dalfa, center, r);
            }
        }
    }
}
