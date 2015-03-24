using System;
using System.Numerics;

namespace HerhangiOT.ServerLibrary.Utility
{
    static class CsharpExtensions
    {
        public static BigInteger ModInverse(this BigInteger a, BigInteger n)
        {
            BigInteger i = n, v = 0, d = 1;
            while (a > 0)
            {
                BigInteger t = i / a, x = a;
                a = i % x;
                i = x;
                x = d;
                d = v - t * x;
                v = x;
            }
            v %= n;
            if (v < 0) v = (v + n) % n;
            return v;
        }

        public static void MemSet<T>(this T[] a, int index, T value, int num)
        {
            for (int i = 0; i < num; i++)
            {
                a[index + i] = value;
            }
        }

        public static int MemCmp<T>(this T[] source, T[] target, int comparisonSize) where T : IComparable<T>
        {
            if(source.Length < comparisonSize || target.Length < comparisonSize)
                throw new ArgumentOutOfRangeException();

            for (int i = 0; i < comparisonSize; i++)
            {
                int comparison = source[i].CompareTo(target[i]);
                if (comparison != 0)
                    return comparison;
            }
            return 0;
        }
    }
}
