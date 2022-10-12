using System;

namespace Triangulation
{
    public static class Maths
    {
        public static readonly float Sin1Deg = (float)Math.Sin(Deg2Rad);
        public static readonly float Cos1Deg = (float)Math.Cos(Deg2Rad);
        public static readonly float Cos2Deg = (float)Math.Cos(2f * Deg2Rad);

        public const float Rad2Deg = 180f / (float)Math.PI;
        public const float Deg2Rad = (float)Math.PI / 180f;

        #region Prime numbers

        internal static readonly int[] primes = {
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
        };

        public static int GetPrime(int min)
        {
            if (min < 0)
            {
                return min;
            }
            for (int i = 0; i < primes.Length; i++)
            {
                int prime = primes[i];
                if (prime >= min)
                {
                    return prime;
                }
            }
            //outside of our predefined table. 
            //compute the hard way.
            for (int i = min | 1; i < int.MaxValue; i += 2)
            {
                if ((i & 1) != 0)
                {
                    int limit = (int)Math.Sqrt(i);
                    for (int divisor = 3; divisor <= limit; divisor += 2)
                    {
                        if ((i % divisor) == 0)
                        {
                            continue;
                        }
                    }
                    return i;
                }
                if (i == 2)
                {
                    return i;
                }
            }
            return min;
        }

        #endregion

        public static float Clamp(float x, float min, float max)
        {
            return Math.Min(max, Math.Max(x, min));
        }

        public static int Clamp(int x, int min, int max)
        {
            return Math.Min(max, Math.Max(x, min));
        }

        public static int GetClosestPowerOf2(int i, bool upper)
        {
            int x = i;
            if (x < 0)
                return 0;
            x--;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x++;
            if (upper && x < i)
            {
                return x << 1;
            }
            else if (!upper && x > i)
            {
                return x >> 1;
            }
            else
            {
                return x;
            }
        }
    }
}
