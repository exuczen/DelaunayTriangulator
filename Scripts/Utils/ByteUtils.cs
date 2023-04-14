namespace Triangulation
{
    public struct ByteUtils
    {
        public static byte[] ConvertIntToBitsByteArray(int value)
        {
            byte[] bits = new byte[32];
            int bitIndex = 0;
            while (value != 0)
            {
                bits[bitIndex++] = (byte)(value & 1);
                value >>= 1;
            }
            return bits;
        }

        public static int ConvertBitsByteArrayToInt(byte[] bits)
        {
            int value = 0;
            if (bits != null && bits.Length == 32)
            {
                for (int i = 0; i < 32; i++)
                {
                    value |= (bits[i] & 1) << i;
                }
            }
            return value;
        }
    }
}
