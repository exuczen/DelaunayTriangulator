﻿using System;
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

        public void Initialize(Vector2 gridSize, Vector2Int xyCount)
        {
            triangulator.Initialize(gridSize, xyCount);
            AddCallbacks();
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
                    TryAddPoint(point);
                    break;
                case TriangulationType.Increment:
                case TriangulationType.Decrement:
                    throw new ArgumentException("UpdateTriangulation: " + type);
                case TriangulationType.Entire:
                    if (TryAddPoint(point))
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
                    TryAddPoint(new Vector2(x * scale, y * scale));
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

        public bool TryAddPoint(Vector2 point, bool findClosestCell = false)
        {
            return triangulator.TryAddPoint(point, out _, findClosestCell);
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

        protected void AddCallbacks()
        {
            triangulator.AddPointAddedAction(OnPointAdded);
            triangulator.AddPointClearedAction(OnPointCleared);
        }

        private void OnPointAdded(int i, Vector2 point)
        {
            particles.SetParticleActive(i, true);
            particles.SetParticlePosition(i, point);
        }

        private void OnPointCleared(int i)
        {
            particles.SetParticleActive(i, false);
        }
    }
}
