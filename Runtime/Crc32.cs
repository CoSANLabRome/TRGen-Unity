using System;

namespace Trgen
{
    public static class Crc32
    {
        private static readonly uint[] Table = new uint[256];

        static Crc32()
        {
            const uint poly = 0xEDB88320;
            for (uint i = 0; i < Table.Length; ++i)
            {
                uint crc = i;
                for (int j = 0; j < 8; ++j)
                    crc = (crc >> 1) ^ ((crc & 1) == 1 ? poly : 0);
                Table[i] = crc;
            }
        }

        public static uint Compute(byte[] bytes)
        {
            uint crc = 0xFFFFFFFF;
            foreach (byte b in bytes)
                crc = (crc >> 8) ^ Table[(crc ^ b) & 0xFF];
            return ~crc;
        }
    }
}