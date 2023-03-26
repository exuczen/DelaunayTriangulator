using System;

namespace Triangulation
{
    public class IncrementalTriangulatorController : TriangulatorController
    {
        public new IncrementalTriangulator Triangulator => triangulator;

        protected new IncrementalTriangulator triangulator = null;

        public IncrementalTriangulatorController(IParticles particles, IExceptionThrower exceptionThrower) : base(particles, false, exceptionThrower)
        {
            base.triangulator = triangulator = new IncrementalTriangulator(particles.Capacity, Vector2.Epsilon, exceptionThrower);
        }

        public void Initialize(Vector2 viewSize, int triangleGridDivs = TriangleGrid.MinDivsCount, int pointGridDivsMlp = 5)
        {
            triangulator.Initialize(viewSize, triangleGridDivs, pointGridDivsMlp, true);

            if (triangulator.InternalOnly && triangulator.PointsCount == 0)
            {
                AddCornerVertices(viewSize, true);
            }
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

        protected override void OnClear(Vector2 viewSize)
        {
            if (triangulator.InternalOnly && triangulator.PointsCount == 0)
            {
                AddCornerVertices(viewSize, false);
            }
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

        private void AddCornerVertices(Vector2 viewSize, bool triangulate)
        {
            float halfSize = MathF.Max(viewSize.x, viewSize.y) * 0.75f;
            float centerX = viewSize.x * 0.5f;
            float centerY = viewSize.y * 0.5f;

            for (int i = -1; i <= 1; i += 2)
            {
                float y = centerY + i * halfSize;
                for (int j = -1; j <= 1; j++)
                {
                    float x = centerX + j * halfSize;
                    AddOffGridParticle(new Vector2(x, y));
                }
            }
            AddOffGridParticle(new Vector2(centerX - halfSize, centerY));
            AddOffGridParticle(new Vector2(centerX + halfSize, centerY));

            if (triangulate)
            {
                UpdateTriangulation();
            }
        }

        private void AddOffGridParticle(Vector2 point)
        {
            if (triangulator.TryAddOffGridPoint(point, out int i))
            {
                SetParticle(true, i);
            }
        }
    }
}
