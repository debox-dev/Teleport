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
            string result = string.Empty;
            for (int i = 0; i < data.Length; i++)
            {
                if (i > 0)
                {
                    result += ", ";
                }
                result += data[i].ToString(); 
            }
            return result;
        }
    }
}
