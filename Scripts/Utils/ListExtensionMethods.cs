using System;
using System.Collections.Generic;

namespace Triangulation
{
    public static class ListExtensionMethods
    {
        public static T GetRandomElementInRange<T>(this List<T> list, int min, int max)
        {
            int randIndex = Maths.Random.Next(Math.Clamp(min, 0, list.Count), Math.Clamp(max, 0, list.Count));
            return list[randIndex];
        }

        public static T GetRandomElement<T>(this List<T> list)
        {
            int randIndex = Maths.Random.Next(0, list.Count);
            return list[randIndex];
        }

        public static T PickRandomElement<T>(this List<T> list)
        {
            int randIndex = Maths.Random.Next(0, list.Count);
            T element = list[randIndex];
            list.RemoveAt(randIndex);
            return element;
        }

        public static T PickFirstElement<T>(this List<T> list)
        {
            if (list.Count > 0)
            {
                T element = list[0];
                list.RemoveAt(0);
                return element;
            }
            return default;
        }

        public static T PickLastElement<T>(this List<T> list)
        {
            if (list.Count > 0)
            {
                int lastIndex = list.Count - 1;
                T element = list[lastIndex];
                list.RemoveAt(lastIndex);
                return element;
            }
            return default;
        }

        public static void RemoveNullElements<T>(this List<T> list)
        {
            list.RemoveAll(item => item == null);
        }

        public static void AddIntRange(this List<int> list, int beg, int count)
        {
            int end = beg + count;
            for (int i = beg; i < end; i++)
            {
                list.Add(i);
            }
        }

        public static void AddIntRangeBegEnd(this List<int> list, int beg, int end)
        {
            if (end >= beg)
            {
                for (int i = beg; i <= end; i++)
                {
                    list.Add(i);
                }
            }
            else
            {
                for (int i = beg; i >= end; i--)
                {
                    list.Add(i);
                }
            }
        }
    }
}
