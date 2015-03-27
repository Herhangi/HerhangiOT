using System;
using HerhangiOT.ServerLibrary.Networking;

namespace HerhangiOT.ServerLibrary.Utility
{
    public class Tools
    {
        private static DateTime _startTime;
        public static uint Uptime { get { return (uint)((DateTime.Now - _startTime).Ticks/10000); } }

        public static void Initialize()
        {
            _startTime = DateTime.Now;
        }

        public static bool ConvertLuaBoolean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            char ch = value.ToLowerInvariant()[0];
            return ch != 'f' && ch != 'n' && ch != '0';
        }

        public static int FlagEnumToArrayPoint(uint value)
        {
            if (value == 0) return -1;

            for (int i = 0; i < 32; i++)
            {
                value >>= 1;
                if (value == 0) return i;
            }
            return -1;
        }

        public static uint AdlerChecksum(byte[] data, int index, int length)
        {
            const ushort adler = 65521;

            uint a = 1, b = 0;

            while (length > 0)
            {
                int tmp = (length > 5552) ? 5552 : length;
                length -= tmp;

                do
                {
                    a += data[index++];
                    b += a;
                } while (--tmp > 0);

                a %= adler;
                b %= adler;
            }

            return (b << 16) | a;
        }

        public unsafe static bool EncryptXtea(OutputMessage msg, uint[] key)
        {
            if (key == null)
                return false;

            int pad = msg.Length % 8;
            if (pad > 0)
                msg.AddPaddingBytes(8-pad);

            fixed (byte* bufferPtr = msg.Buffer)
            {
                uint* words = (uint*)(bufferPtr + msg.HeaderPosition);

                for (int pos = 0; pos < msg.Length / 4; pos += 2)
                {
                    uint x_sum = 0, x_delta = 0x9e3779b9, x_count = 32;

                    while (x_count-- > 0)
                    {
                        words[pos] += (words[pos + 1] << 4 ^ words[pos + 1] >> 5) + words[pos + 1] ^ x_sum
                            + key[x_sum & 3];
                        x_sum += x_delta;
                        words[pos + 1] += (words[pos] << 4 ^ words[pos] >> 5) + words[pos] ^ x_sum
                            + key[x_sum >> 11 & 3];
                    }
                }
            }

            return true;
        }

        public unsafe static bool Decrypt(ref byte[] buffer, ref int length, int index, uint[] key)
        {
            if (length <= index || (length - index) % 8 > 0 || key == null)
                return false;

            fixed (byte* bufferPtr = buffer)
            {
                uint* words = (uint*)(bufferPtr + index);
                int msgSize = length - index;

                for (int pos = 0; pos < msgSize / 4; pos += 2)
                {
                    uint x_count = 32, x_sum = 0xC6EF3720, x_delta = 0x9E3779B9;

                    while (x_count-- > 0)
                    {
                        words[pos + 1] -= (words[pos] << 4 ^ words[pos] >> 5) + words[pos] ^ x_sum
                            + key[x_sum >> 11 & 3];
                        x_sum -= x_delta;
                        words[pos] -= (words[pos + 1] << 4 ^ words[pos + 1] >> 5) + words[pos + 1] ^ x_sum
                            + key[x_sum & 3];
                    }
                }
            }

            length = (BitConverter.ToUInt16(buffer, index) + 2 + index);
            return true;
        }
    }
}
