using System;
using System.Collections.Generic;
using System.Numerics;

namespace Triangulation
{
    public readonly struct PointsXYComparer : IComparer<Vector2>
    {
        private readonly float tolerance;

        public PointsXYComparer(float tolerance)
        {
            this.tolerance = tolerance;
        }

        /// <summary>
        /// Sort points by X (firstly), Y (secondly)
        /// </summary>
        public int Compare(Vector2 p1, Vector2 p2)
        {
            float delta = p1.X - p2.X;

            if (MathF.Abs(delta) > tolerance)
            {
                return MathF.Sign(delta);
            }

            delta = p1.Y - p2.Y;

            if (MathF.Abs(delta) > tolerance)
            {
                return MathF.Sign(delta);
            }

            return 0;
        }
    }
}
