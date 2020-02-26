namespace DeBox.Teleport.Utils
{
    public static class CrcUtils
    {
        public static byte Checksum(byte[] data, long startOffset, long amount, int maxChecksum)
        {
            byte checksumCalculated = 0;
            unchecked
            {
                for (long i = startOffset; i < amount; i++)
                {
                    checksumCalculated += data[i];
                }
            }
            return (byte)(checksumCalculated % maxChecksum);
        }
    }
}
