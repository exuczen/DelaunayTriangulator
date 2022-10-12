//#define TRIANGULATION_INTERNAL

using System;

namespace Triangulation
{
    public class IncrementalTriangulatorController : TriangulatorController
    {
        public new IncrementalTriangulator Triangulator => triangulator;

        protected new IncrementalTriangulator triangulator = null;

        public IncrementalTriangulatorController(IParticles particles, IExceptionThrower exceptionThrower) : base(particles)
        {
#if TRIANGULATION_INTERNAL
            base.triangulator = triangulator = new IncrementalTriangulator(particles.Capacity, Vector2.Epsilon, true);
#else
            base.triangulator = triangulator = new IncrementalTriangulator(particles.Capacity, Vector2.Epsilon, false);
#endif
            triangulator.ExceptionThrower = exceptionThrower;
        }

        public override void Initialize(Vector2Int viewSize)
        {
            triangulator.Initialize(new Vector2(viewSize.x, viewSize.y), true);
#if TRIANGULATION_INTERNAL
            if (triangulator.PointsCount == 0)
            {
                AddCornerVertices(viewSize, true);
            }
#endif
        }

        public override void UpdateTriangulation(TriangulationType type, Vector2 point)
        {
            triangulator.TriangleGrid.SetSelectedCell(point);
            switch (type)
            {
                case TriangulationType.None:
                    break;
                case TriangulationType.Increment:
                    InvokeTriangulateAction(() => AddParticleToTriangulation(point));
                    break;
                case TriangulationType.Decrement:
                    InvokeTriangulateAction(() => TryRemoveParticleFromTriangulation(point));
                    break;
                case TriangulationType.Entire:
                    AddParticle(point);
                    UpdateTriangulation();
                    break;
                default:
                    break;
            }
        }

        protected override void OnClear(Vector2Int viewSize)
        {
#if TRIANGULATION_INTERNAL
            AddCornerVertices(viewSize, true);
#endif
        }

        protected override void AddParticle(Vector2 point)
        {
            bool active = triangulator.TryAddPoint(point, out int i);
            SetParticle(active, i);
        }

        private void AddParticleToTriangulation(Vector2 point)
        {
            bool active = triangulator.AddPointToTriangulation(point, out int i);
            SetParticle(active, i);
        }

        private void TryRemoveParticleFromTriangulation(Vector2 point)
        {
            if (triangulator.TryRemovePointFromTriangulation(point, out int i))
            {
                SetParticle(false, i);
            }
        }

        private void AddCornerVertices(Vector2Int viewSize, bool triangulate)
        {
            int halfSize = Math.Max(viewSize.x, viewSize.y) * 3 >> 2;
            int centerX = viewSize.x >> 1;
            int centerY = viewSize.y >> 1;

            for (int i = -1; i <= 1; i += 2)
            {
                int y = centerY + i * halfSize;
                for (int j = -1; j <= 1; j++)
                {
                    int x = centerX + j * halfSize;
                    base.AddParticle(new Vector2(x, y));
                }
            }
            base.AddParticle(new Vector2(centerX - halfSize, centerY));
            base.AddParticle(new Vector2(centerX + halfSize, centerY));

            if (triangulate)
            {
                UpdateTriangulation();
            }
        }
    }
}
