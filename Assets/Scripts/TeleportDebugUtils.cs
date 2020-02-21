using System.IO;
using DeBox.Teleport.Transport;

namespace DeBox.Teleport.Debugging
{
    public static class TeleportDebugUtils
    {
        public static string DebugString(TeleportWriter writer)
        {
            return DebugString((MemoryStream)writer.BaseStream);
        }

        public static string DebugString(TeleportReader reader)
        {
            return DebugString((MemoryStream)reader.BaseStream);
        }

        public static string DebugString(MemoryStream stream)
        {
            return DebugString(stream.ToArray());
        }

        public static string DebugString(byte[] data)
        {
            return DebugString(data, 0, data.Length);
        }

        public static string DebugString(byte[] data, int startIndex, int length)
        {
            string result = string.Empty;
            for (int i = startIndex; i < startIndex + length; i++)
            {
                if (i > startIndex)
                {
                    result += ", ";
                }
                result += data[i].ToString(); 
            }
            return result;
        }
    }
}
