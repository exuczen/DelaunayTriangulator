using System;
using System.Numerics;

namespace Triangulation
{
    public class IncrementalTriangulatorController : TriangulatorController
    {
        public new IncrementalTriangulator Triangulator => triangulator;

        protected new IncrementalTriangulator triangulator = null;

        public IncrementalTriangulatorController(IParticles particles, IExceptionThrower exceptionThrower)
        {
            this.particles = particles;
            base.triangulator = triangulator = new IncrementalTriangulator(particles.Capacity, exceptionThrower);
        }

        public override void UpdateTriangulation(TriangulationType type, Vector2 point)
        {
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
                    if (TryAddParticle(point))
                    {
                        UpdateTriangulation();
                    }
                    break;
                default:
                    break;
            }
            triangulator.TriangleGrid.SetSelectedCell(point);
        }

        private void AddParticleToTriangulation(Vector2 point)
        {
            bool active = triangulator.AddPointToTriangulation(point, out int i, false);
            SetParticle(active, i);
        }

        private void TryRemoveParticleFromTriangulation(Vector2 point)
        {
            if (triangulator.TryRemovePointFromTriangulation(point, true, out int i))
            {
                SetParticle(false, i);
            }
        }
    }
}
