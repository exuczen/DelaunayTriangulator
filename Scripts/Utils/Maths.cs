using System;
using System.Numerics;

namespace Triangulation
{
    public static class Maths
    {
        public static readonly float Tau = MathF.PI * 2f;

        public static readonly float Sin1Deg = MathF.Sin(Deg2Rad);
        public static readonly float Cos1Deg = MathF.Cos(Deg2Rad);
        public static readonly float Cos2Deg = MathF.Cos(2f * Deg2Rad);

        public const float Rad2Deg = 180f / MathF.PI;
        public const float Deg2Rad = MathF.PI / 180f;

        public static readonly Random Random = new Random();

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
                    int limit = (int)MathF.Sqrt(i);
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

        public static float ACos2Deg(float cosAngle) => MathF.Acos(cosAngle) * Rad2Deg;

        public static float Round(float x, float gradation)
        {
            return ((int)(x / gradation + 0.5f)) * gradation;
        }

        public static float GetMaxSqrtOrSqr(float x)
        {
            float absX = MathF.Abs(x);
            return MathF.Sign(x) * (absX > 1f ? MathF.Sqrt(absX) : x * x);
        }

        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Clamp(t, 0f, 1f);
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

        public static int GetClosestPointIndex(Vector2 center, Vector2[] points, int beg, int end)
        {
            if (beg < 0 || end < 0)
            {
                return -1;
            }
            int pointIndex = beg;
            float sqrDistMin = (points[beg] - center).LengthSquared();
            for (int i = beg + 1; i <= end; i++)
            {
                float sqrDist = (points[i] - center).LengthSquared();
                if (sqrDist < sqrDistMin)
                {
                    sqrDistMin = sqrDist;
                    pointIndex = i;
                }
            }
            return pointIndex;
        }

        public static string ToStringF2(this float x)
        {
            return x.ToString("f2");
        }
    }
}
