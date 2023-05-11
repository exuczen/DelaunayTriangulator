using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace Triangulation
{
    public class TriangulatorController
    {
        public Triangulator Triangulator => triangulator;

        public Action<Stopwatch, Triangulator> Triangulated = null;

        protected Triangulator triangulator = null;

        protected IParticles particles = null;

        protected readonly Stopwatch stopwatch = new Stopwatch();

        public TriangulatorController() { }

        public TriangulatorController(IParticles particles, IExceptionThrower exceptionThrower)
        {
            this.particles = particles;
            triangulator = new Triangulator(particles.Capacity, exceptionThrower);
        }

        public SerializedTriangulator CreateSerializedTriangulator()
        {
            return new SerializedTriangulator(triangulator);
        }

        public static void SaveTriangulator(SerializedTriangulator save, string filepath)
        {
            if (string.IsNullOrEmpty(filepath))
            {
                return;
            }
            string folderPath = Path.GetDirectoryName(filepath);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            JsonUtils.SaveToJson(save, filepath);
        }

        public void SaveTriangulator(string filepath)
        {
            SaveTriangulator(CreateSerializedTriangulator(), filepath);
        }

        public SerializedTriangulator LoadTriangulator(string filepath)
        {
            var save = JsonUtils.LoadFromJson<SerializedTriangulator>(filepath);
            if (save != null)
            {
                triangulator.Load(save);
            }
            return save;
        }

        public virtual void UpdateTriangulation(TriangulationType type, Vector2 point)
        {
            switch (type)
            {
                case TriangulationType.None:
                    TryAddParticle(point);
                    break;
                case TriangulationType.Increment:
                case TriangulationType.Decrement:
                    throw new ArgumentException("UpdateTriangulation: " + type);
                case TriangulationType.Entire:
                    if (TryAddParticle(point))
                    {
                        UpdateTriangulation();
                    }
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
                    TryAddParticle(new Vector2(x * scale, y * scale));
                }
            }
            if (triangulate)
            {
                UpdateTriangulation();
            }
        }

        public void Clear()
        {
            triangulator.Clear();
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

        protected bool TryAddParticle(Vector2 point)
        {
            if (triangulator.TryAddPoint(point, out int i, false))
            {
                SetParticle(true, i);
                return true;
            }
            return false;
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
            stopwatch.Restart();
            action();
            stopwatch.Stop();
            return stopwatch;
        }
    }
}
