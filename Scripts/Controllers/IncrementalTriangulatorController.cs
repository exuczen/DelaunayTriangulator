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

        public void Initialize(Vector2 gridSize, int triangleGridDivs = TriangleGrid.MinDivsCount, int pointGridDivsMlp = 5)
        {
            triangulator.Initialize(gridSize, triangleGridDivs, pointGridDivsMlp);
            AddCallbacks();
        }

        public override void UpdateTriangulation(TriangulationType type, Vector2 point)
        {
            switch (type)
            {
                case TriangulationType.None:
                    break;
                case TriangulationType.Increment:
                    InvokeTriangulateAction(() => triangulator.TryAddPointToTriangulation(point, false, true));
                    break;
                case TriangulationType.Decrement:
                    InvokeTriangulateAction(() => triangulator.TryRemovePointFromTriangulation(point, true));
                    break;
                case TriangulationType.Entire:
                    if (TryAddPoint(point))
                    {
                        UpdateTriangulation();
                    }
                    break;
                default:
                    break;
            }
            triangulator.TriangleGrid.SetSelectedCell(point);
        }
    }
}
