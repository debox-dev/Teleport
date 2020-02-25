namespace DeBox.Teleport.Utils
{
    public enum FloatCompressionTypeShort
    {
        None,
        Short_One_Decimal,
        Short_Two_Decimals,
        Short_Three_Decimals,

    }

    public enum FloatCompressionTypeChar
    {
        None,
        Char_Two_Decimals,
        Char_One_Decimal,
    }

    public static class CompressionUtils
    {
        public static short CompressToShort(float value, FloatCompressionTypeShort compressionType)
        {
            var multi = GetMultiplayer(compressionType);
            return (short)(value * multi);
        }

        public static float DecompressFromShort(short value, FloatCompressionTypeShort compressionType)
        {
            var multi = GetMultiplayer(compressionType);
            return (float)value / multi;
        }

        public static char CompressToChar(float value, FloatCompressionTypeChar compressionType)
        {
            var multi = GetMultiplayer(compressionType);
            return (char)(value * multi);
        }

        public static float DecompressFromChar(char value, FloatCompressionTypeChar compressionType)
        {
            var multi = GetMultiplayer(compressionType);
            return (float)value / multi;
        }

        private static short GetMultiplayer(FloatCompressionTypeShort compressionType)
        {
            switch (compressionType)
            {
                case FloatCompressionTypeShort.Short_One_Decimal:
                    return 10;
                case FloatCompressionTypeShort.Short_Two_Decimals:
                    return 100;
                case FloatCompressionTypeShort.Short_Three_Decimals:
                    return 1000;
                case FloatCompressionTypeShort.None:
                default:
                    return 1;
            }
        }

        private static byte GetMultiplayer(FloatCompressionTypeChar compressionType)
        {
            switch (compressionType)
            {
                case FloatCompressionTypeChar.Char_Two_Decimals:
                    return 100;
                case FloatCompressionTypeChar.Char_One_Decimal:
                    return 10;
                case FloatCompressionTypeChar.None:
                default:
                    return 1;
            }
        }
    }
}
