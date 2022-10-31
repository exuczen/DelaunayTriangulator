using System;

namespace Triangulation
{
    public struct ArrayUtils
    {
        public static int CutRange<T>(T[] srcArray, IndexRange range, T[] destArray)
        {
            int length = range.FullLength;
            int count = length - range.GetIndexCount();
            int beg = range.Beg;
            int end = range.End;
            if (end < beg || beg == 0)
            {
                Array.Copy(srcArray, end + 1, destArray, 0, count);
            }
            else if (end < length - 1)
            {
                Array.Copy(srcArray, 0, destArray, length, beg);
                Array.Copy(srcArray, end + 1, destArray, 0, count);
            }
            return count;
        }

        public static void Swap<T>(T[] array, int a, int b)
        {
            (array[b], array[a]) = (array[a], array[b]);
        }

        public static void InvertOrder<T>(T[] array, int count)
        {
            int halfCount = count >> 1;
            for (int i = 0; i < halfCount; i++)
            {
                Swap(array, i, count - 1 - i);
            }
        }

        public static void InvertOrder<T>(T[] array, int beg, int end)
        {
            int count = end - beg + 1;
            int halfCount = count >> 1;
            for (int i = 0; i < halfCount; i++)
            {
                Swap(array, beg + i, end - i);
            }
        }
    }
}
