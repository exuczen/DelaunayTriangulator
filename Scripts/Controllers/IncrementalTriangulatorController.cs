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
                    InvokeTriangulateAction(() => AddParticleToTriangulation(point, false, true));
                    break;
                case TriangulationType.Decrement:
                    InvokeTriangulateAction(() => TryRemoveParticleFromTriangulation(point, true));
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

        public void AddParticleToTriangulation(Vector2 point, int i, bool findClosestCell, bool validate)
        {
            bool active = triangulator.AddPointToTriangulation(point, i, findClosestCell, validate);
            SetParticle(active, i);
        }

        public void AddParticleToTriangulation(Vector2 point, bool findClosestCell, bool validate)
        {
            bool active = triangulator.AddPointToTriangulation(point, out int i, findClosestCell, validate);
            SetParticle(active, i);
        }

        public void RemoveParticleFromTriangulation(int i, bool validate)
        {
            triangulator.RemovePointFromTriangulation(i, validate);
            SetParticle(false, i);
        }

        private void TryRemoveParticleFromTriangulation(Vector2 point, bool validate)
        {
            if (triangulator.TryRemovePointFromTriangulation(point, validate, out int i))
            {
                SetParticle(false, i);
            }
        }
    }
}
