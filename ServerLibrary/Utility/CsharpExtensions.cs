﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace HerhangiOT.ServerLibrary.Utility
{
    public static class CsharpExtensions
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

        public static void MemCpy<T>(this T[] destination, int index, T[] source, int num)
        {
            for (int i = 0; i < num; i++)
            {
                destination[index + i] = source[i];
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

        public static string GetString(this BinaryReader reader)
        {
            ushort len = reader.ReadUInt16();
            return Encoding.Default.GetString(reader.ReadBytes(len));
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void Shuffle<T>(this IList<T> list, int index, int count)
        {
            Random rng = new Random();

            if (index + count > list.Count)
                count = list.Count - index;

            int n = index + count;
            while (n > index)
            {
                n--;
                int k = rng.Next(index, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
