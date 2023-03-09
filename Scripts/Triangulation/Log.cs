#if UNITY_EDITOR || UNITY_STANDALONE
#define UNITY
using Debug = UnityEngine.Debug;
#else
using Debug = System.Diagnostics.Debug;
#endif
using System;
using System.Collections.Generic;
using System.Text;

namespace Triangulation
{
    public enum ErrorCode
    {
        None,
        BaseTriangulationFailed,
        InvalidTriangulation,
        InternalEdgeExists,
    }

    public static class Log
    {
        public const string KIND_OF_FAKAP = " !!! KIND OF FAKAP !!!";

        public static readonly StringBuilder StringBuilder = new StringBuilder();

        public static void Write(string value)
        {
#if UNITY
            Debug.Log(value);
#else
            Debug.Write(value);
#endif
        }

        public static void WriteWarning(string value)
        {
#if UNITY
            Debug.LogWarning(value);
#else
            Debug.WriteLine("WARNING: {0}", value);
#endif
        }

        public static void WriteError(string value)
        {
#if UNITY
            Debug.LogError(value);
#else
            Debug.Fail(value);
#endif
        }

        public static void WriteLine(string value)
        {
#if UNITY
            Debug.Log(value);
#else
            Debug.WriteLine(value);
#endif
        }

        public static void WriteLine(string format, params object[] args)
        {
#if UNITY
            Debug.LogFormat(format, args);
#else
            Debug.WriteLine(format, args);
#endif
        }

        public static void PrintEdgePeaks(List<EdgePeak> peaks, string prefix = null)
        {
            WriteLine("PrintEdgePeaks: " + prefix);
            for (int i = 0; i < peaks.Count; i++)
            {
                WriteLine("PrintEdgePeaks: " + i + " " + peaks[i]);
            }
        }

        public static void PrintPoints(Vector2[] points, int pointsCount)
        {
            WriteLine("PrintPoints: ");
            for (int i = 0; i < pointsCount; i++)
            {
                WriteLine("PrintPoints: " + i + " " + points[i]);
            }
        }

        public static void PrintPoints(List<Vector2> points)
        {
            WriteLine("PrintPoints: ");
            for (int i = 0; i < points.Count; i++)
            {
                WriteLine("PrintPoints: " + i + " " + points[i]);
            }
        }

        public static string TrianglesToString(Triangle[] triangles, int triangleCount, Func<Triangle, string> toString, string prefix = null)
        {
            StringBuilder.Append("TrianglesToString: " + triangleCount + " : " + prefix + " \n");
            for (int i = 0; i < triangleCount; i++)
            {
                StringBuilder.Append(i + " " + toString(triangles[i]) + "\n");
            }
            var text = StringBuilder.ToString();
            StringBuilder.Clear();
            return text;
        }

        public static void PrintTriangles(Triangle[] triangles, int triangleCount, Func<Triangle, string> toString, string prefix = null)
        {
            WriteLine(TrianglesToString(triangles, triangleCount, toString, prefix));
        }

        public static void PrintTriangles(Triangle[] triangles, int triangleCount, string prefix = null)
        {
            WriteLine("PrintTriangles: " + triangleCount + " " + prefix);
            for (int i = 0; i < triangleCount; i++)
            {
                WriteLine(i + " " + triangles[i]);
            }
        }

        public static void PrintEdges(EdgeEntry[] edges, int edgeCount, string prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                WriteLine("PrintEdges: " + prefix);
            }
            for (int i = 0; i < edgeCount; i++)
            {
                WriteLine("PrintEdges: " + i + " of " + edgeCount + " : " + edges[i]);
            }
        }

        public static void PrintIndexRanges(List<IndexRange> ranges, string prefix = null)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                WriteLine("PrintIndexRanges: " + prefix);
            }
            for (int i = 0; i < ranges.Count; i++)
            {
                WriteLine("PrintIndexRanges: " + ranges[i]);
            }
        }

        public static void PrintList<T>(List<T> list, string prefix)
        {
            WriteLine(prefix);
            for (int i = 0; i < list.Count; i++)
            {
                WriteLine(prefix + " " + i + " " + list[i]);
            }
        }

        public static void PrintArray<T>(T[] array, int count, string prefix)
        {
            WriteLine(prefix);
            for (int i = 0; i < count; i++)
            {
                WriteLine(prefix + " " + i + " " + array[i]);
            }
        }
    }
}
