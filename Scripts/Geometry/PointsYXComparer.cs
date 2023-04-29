//#define SKIP_X_COMPARE

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Triangulation
{
    public readonly struct PointsYXComparer : IComparer<Vector2>
    {
        private readonly float tolerance;

        public PointsYXComparer(float tolerance)
        {
            this.tolerance = tolerance;
        }

        /// <summary>
        /// Sort points by Y (firstly), X (secondly)
        /// </summary>
        public int Compare(Vector2 p1, Vector2 p2)
        {
            float delta = p1.Y - p2.Y;

            if (MathF.Abs(delta) > tolerance)
            {
                return MathF.Sign(delta);
            }
#if !SKIP_X_COMPARE
            delta = p1.X - p2.X;

            if (MathF.Abs(delta) > tolerance)
            {
                return MathF.Sign(delta);
            }
#endif
            return 0;
        }
    }
}
