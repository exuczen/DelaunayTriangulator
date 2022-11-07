using System;
using System.Collections.Generic;

namespace Triangulation
{
    public struct PointsYXComparer : IComparer<Vector2>
    {
        private readonly float tolerance;

        public PointsYXComparer(float tolerance)
        {
            this.tolerance = tolerance;
        }

        /// <summary>
        /// Sort points by Y (firstly), X (secondly)
        /// </summary>
        public int Compare(Vector2 va, Vector2 vb)
        {
            float f = va.y - vb.y;

            if (MathF.Abs(f) > tolerance)
            {
                return MathF.Sign(f);
            }

            f = va.x - vb.x;

            if (MathF.Abs(f) > tolerance)
            {
                return MathF.Sign(f);
            }

            return 0;
        }
    }
}
