//#define LOGS_ENABLED
#if UNITY
using Debug = UnityEngine.Debug;
#else
using Debug = System.Diagnostics.Debug;
#endif
using System;
using System.Collections.Generic;
using System.Text;

namespace Triangulation
{
#if !LOGS_ENABLED
    public static class Log
    {
        public const string KIND_OF_FAKAP = " !!! KIND OF FAKAP !!!";

        public static readonly StringBuilder StringBuilder = new StringBuilder();

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

        public static void WriteLine(string value) { }

        public static void WriteLine(string format, params object[] args) { }

        public static void PrintList<T>(List<T> list, string prefix = null) { }

        public static void PrintArrayToString<T>(T[] array, int count, Func<T, string> toString, string prefix) { }

        public static void PrintArray<T>(T[] array, int count, string prefix = null) { }

        public static void PrintArray<T>(T[] array, int count, Func<T, string> toString, string prefix = null) { }
    }
#else
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
            Debug.WriteLine(string.Format("WARNING: {0}", value));
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

        public static string ToString<T>(this T[] array, int count, Func<T, string> toString, string prefix)
        {
            StringBuilder.Append(string.Format("{0} count: {1}\n", prefix, count));
            for (int i = 0; i < count; i++)
            {
                StringBuilder.Append(string.Format("[{0}] {1}\n", i, toString(array[i])));
            }
            var text = StringBuilder.ToString();
            StringBuilder.Clear();
            return text;
        }

        public static void PrintArrayToString<T>(T[] array, int count, Func<T, string> toString, string prefix)
        {
            WriteLine(array.ToString(count, toString, prefix));
        }

        public static void PrintList<T>(List<T> list, string prefix)
        {
            PrintList(list, element => element.ToString(), prefix);
        }

        public static void PrintList<T>(List<T> list, Func<T, string> toString, string prefix)
        {
            WriteLine(string.Format("{0} count: {1}", prefix, list.Count));
            for (int i = 0; i < list.Count; i++)
            {
                WriteLine(string.Format("{0} [{1}] {2}", prefix, i, toString(list[i])));
            }
        }

        public static void PrintArray<T>(T[] array, int count, string prefix)
        {
            WriteLine(string.Format("{0} count: {1}", prefix, count));
            for (int i = 0; i < count; i++)
            {
                WriteLine(string.Format("{0} [{1}] {2}", prefix, i, array[i]));
            }
        }
    }
#endif
}
