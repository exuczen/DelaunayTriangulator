using System;
using System.Diagnostics;

namespace Triangulation
{
    public class TriangulatorController
    {
        public Triangulator Triangulator => triangulator;

        public Action<Stopwatch, Triangulator> Triangulated = null;

        protected Triangulator triangulator = null;

        protected readonly IParticles particles = null;

        protected readonly Stopwatch stopwatch = new Stopwatch();

        public TriangulatorController(IParticles particles, bool createTriangulator)
        {
            this.particles = particles;
            if (createTriangulator)
            {
                triangulator = new Triangulator(particles.Capacity, Vector2.Epsilon);
            }
        }

        public virtual void UpdateTriangulation(TriangulationType type, Vector2 point)
        {
            switch (type)
            {
                case TriangulationType.None:
                    AddParticle(point);
                    break;
                case TriangulationType.Increment:
                case TriangulationType.Decrement:
                    throw new ArgumentException("UpdateTriangulation: " + type);
                case TriangulationType.Entire:
                    AddParticle(point);
                    UpdateTriangulation();
                    break;
                default:
                    break;
            }
        }

        public void AddGrid(int width, int height, float scale, bool triangulate)
        {
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    AddParticle(new Vector2(x * scale, y * scale));
                }
            }
            if (triangulate)
            {
                UpdateTriangulation();
            }
        }

        public void Clear(Vector2 viewSize)
        {
            triangulator.Clear();
            OnClear(viewSize);
            UpdateTriangulation();
        }

        public int UpdateTriangulation(int pointsCount)
        {
            triangulator.PointsCount = pointsCount;
            UpdateTriangulation();
            return triangulator.PointsCount;
        }

        public void UpdateTriangulation()
        {
            InvokeTriangulateAction(() => triangulator.Triangulate());
        }

        protected virtual void OnClear(Vector2 viewSize)
        {
        }

        protected virtual void AddParticle(Vector2 point)
        {
            triangulator.AddPoint(point, out int i);
            SetParticle(true, i);
        }

        protected void SetParticle(bool active, int i)
        {
            if (active)
            {
                particles.SetParticleActive(i, true);
                particles.SetParticlePosition(i, triangulator.GetPoint(i));
            }
            else if (i >= 0)
            {
                particles.ClearParticle(i);
            }
        }

        protected void InvokeTriangulateAction(Action triangulate)
        {
            //Debug.WriteLine("UpdateTriangulation " + triangulate);
            //Log.WriteLine("UpdateTriangulation " + triangulate + " " + addedOnly);

            var stopwatch = InvokeActionWithStopwatch(triangulate);

            Triangulated?.Invoke(stopwatch, triangulator);
        }

        protected Stopwatch InvokeActionWithStopwatch(Action action)
        {
            stopwatch.Start();
            action();
            stopwatch.Stop();
            return stopwatch;
        }
    }
}
