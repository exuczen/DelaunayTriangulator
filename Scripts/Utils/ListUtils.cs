using System.Collections.Generic;

namespace Triangulation
{
    public struct ListUtils
    {
        public static void Swap<T>(List<T> list, int a, int b)
        {
            (list[b], list[a]) = (list[a], list[b]);
        }

        public static void InvertOrder<T>(List<T> list, int count)
        {
            int halfCount = count >> 1;
            for (int i = 0; i < halfCount; i++)
            {
                Swap(list, i, count - 1 - i);
            }
        }
    }
}
