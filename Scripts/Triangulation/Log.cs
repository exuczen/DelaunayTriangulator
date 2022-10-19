#if UNITY_EDITOR || UNITY_STANDALONE
#define UNITY
#endif

using System;
using System.Collections.Generic;

namespace Triangulation
{
    public static class Log
    {
        public const string KIND_OF_FAKAP = " !!! KIND OF FAKAP !!!";

        public static void WriteLine(string value)
        {
#if UNITY
            UnityEngine.Debug.Log(value);
#else
            Console.WriteLine(value);
#endif
        }

        public static void WriteLine(string format, params object[] args)
        {
#if UNITY
            UnityEngine.Debug.LogFormat(format, args);
#else
            Console.WriteLine(format, args);
#endif
        }

        public static void PrintEdgePeaks(List<EdgePeak> peaks, string prefix = null)
        {
            Log.WriteLine("PrintEdgePeaks: " + prefix);
            for (int i = 0; i < peaks.Count; i++)
            {
                Log.WriteLine("PrintEdgePeaks: " + i + " " + peaks[i]);
            }
        }

        public static void PrintPoints(Vector2[] points, int pointsCount)
        {
            Log.WriteLine("PrintPoints: ");
            for (int i = 0; i < pointsCount; i++)
            {
                Log.WriteLine("PrintPoints: " + i + " " + points[i]);
            }
        }

        public static void PrintPoints(List<Vector2> points)
        {
            Log.WriteLine("PrintPoints: ");
            for (int i = 0; i < points.Count; i++)
            {
                Log.WriteLine("PrintPoints: " + i + " " + points[i]);
            }
        }

        public static void PrintTriangles(Triangle[] triangles, int triangleCount, string prefix = null)
        {
            Log.WriteLine("PrintTriangles: " + triangleCount + " " + prefix);
            for (int i = 0; i < triangleCount; i++)
            {
                Log.WriteLine(i + " " + triangles[i]);
            }
        }

        public static void PrintEdges(EdgeEntry[] edges, int edgeCount, string prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                Log.WriteLine("PrintEdges: " + prefix);
            }
            for (int i = 0; i < edgeCount; i++)
            {
                Log.WriteLine("PrintEdges: " + i + " of " + edgeCount + " : " + edges[i]);
            }
        }

        public static void PrintIndexRanges(List<IndexRange> ranges, string prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                Log.WriteLine("PrintIndexRanges: " + prefix);
            }
            for (int i = 0; i < ranges.Count; i++)
            {
                Log.WriteLine("PrintIndexRanges: " + ranges[i]);
            }
        }

        public static void PrintList<T>(List<T> list, string prefix)
        {
            Log.WriteLine(prefix);
            for (int i = 0; i < list.Count; i++)
            {
                Log.WriteLine(prefix + " " + i + " " + list[i]);
            }
        }

        public static void PrintArray<T>(T[] array, int count, string prefix)
        {
            Log.WriteLine(prefix);
            for (int i = 0; i < count; i++)
            {
                Log.WriteLine(prefix + " " + i + " " + array[i]);
            }
        }
    }
}
