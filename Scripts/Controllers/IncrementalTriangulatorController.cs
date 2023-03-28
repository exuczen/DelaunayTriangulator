﻿using System;

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
    }
}
