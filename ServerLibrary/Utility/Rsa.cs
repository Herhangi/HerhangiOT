using System;
using System.Numerics;

namespace HerhangiOT.ServerLibrary.Utility
{
    public class Rsa
    {
        protected static BigInteger N; 
        protected static BigInteger D; 
        
        public static bool SetKey(string p, string q)
        {
            BigInteger mP, mQ;

            if (!BigInteger.TryParse(p, out mP))
                return false;
            
            if (!BigInteger.TryParse(q, out mQ))
                return false;
            
            N = mP * mQ;

            BigInteger mE = BigInteger.Parse("65537");
            BigInteger mod = (mP - 1)*(mQ - 1);

            D = mE.ModInverse(mod);
            byte[] a = D.ToByteArray();
            return true;
        }

        public static void Encrypt(ref byte[] buffer, int index)
        {
            byte[] temp = new byte[128];
            Array.Copy(buffer, index, temp, 0, 128);

            BigInteger c = new BigInteger(temp);
            BigInteger mE = BigInteger.Parse("65537");
            BigInteger m = BigInteger.ModPow(c, mE, N);
            byte[] a = m.ToByteArray();

            int length = (1024 >> 3);
            if (a.Length >= length)
                Array.Copy(a, 0, buffer, index, 128);
            else
            {
                temp.MemSet(index, (byte)0, length - a.Length);
                Array.Copy(a, 0, buffer, index + (length - a.Length), a.Length);
            }
        }

        public static void Decrypt(ref byte[] buffer, int index)
        {
            byte[] temp = new byte[128];
            Array.Copy(buffer, index, temp, 0, 128);
            Array.Reverse(temp);
            
            BigInteger c = new BigInteger(temp);
            BigInteger m = BigInteger.ModPow(c, D, N);
            byte[] a = m.ToByteArray();
            Array.Reverse(a);

            int length = (1024 >> 3);
            if (a.Length >= length)
                Array.Copy(a, 0, buffer, index, 128);
            else
            {
                buffer.MemSet(index, (byte)0, length - a.Length);
                Array.Copy(a, 0, buffer, index + (length - a.Length), a.Length);
            }
        }
    }
}