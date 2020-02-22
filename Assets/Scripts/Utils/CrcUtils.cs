namespace DeBox.Teleport.Utils
{
    public static class CrcUtils
    {
        public static byte Checksum(byte[] data, long startOffset, long amount, params byte[] additional)
        {
            byte checksumCalculated = 0;
            unchecked
            {
                for (long i = startOffset; i < amount; i++)
                {
                    checksumCalculated += data[i];
                }
                for (long i = 0; i < additional.Length; i++)
                {
                    checksumCalculated += additional[i];
                }
            }
            return (byte)(checksumCalculated % 4);
        }
    }
}
