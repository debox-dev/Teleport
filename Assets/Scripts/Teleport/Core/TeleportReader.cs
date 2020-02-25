using System.IO;
using System.Text;
using UnityEngine;
using DeBox.Teleport.Utils;

namespace DeBox.Teleport.Core
{
    public class TeleportReader : BinaryReader
    {
        public TeleportReader(byte[] input) : base(new MemoryStream(input))
        {
        }

        public TeleportReader(Stream input) : base(input)
        {
        }

        public TeleportReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public TeleportReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        public byte[] ReadBytesAndSize()
        {
            byte length = base.ReadByte();
            return base.ReadBytes(length);
        }

        public float ReadSingle(FloatCompressionTypeShort compressionType)
        {
            return CompressionUtils.DecompressFromShort(ReadInt16(), compressionType);
        }

        public float ReadSingle(FloatCompressionTypeChar compressionType)
        {
            return CompressionUtils.DecompressFromChar(ReadChar(), compressionType);
        }

        public Color ReadColor()
        {
            var result = new Color();
            result.r = ReadSingle();
            result.g = ReadSingle();
            result.b = ReadSingle();
            return result;
        }

        public Quaternion ReadQuaternion()
        {
            var result = new Quaternion();
            result.x = ReadSingle();
            result.y = ReadSingle();
            result.z = ReadSingle();
            result.w = ReadSingle();
            return result;
        }

        public Quaternion ReadQuaternion(FloatCompressionTypeShort compressionType)
        {
            var result = new Quaternion();
            result.x = ReadSingle(compressionType);
            result.y = ReadSingle(compressionType);
            result.z = ReadSingle(compressionType);
            result.w = ReadSingle(compressionType);
            return result;
        }

        public Vector2 ReadVector2()
        {
            var result = new Vector2();
            result.x = ReadSingle();
            result.y = ReadSingle();
            return result;
        }

        public Vector2 ReadVector2(FloatCompressionTypeShort compressionType)
        {
            var result = new Vector2();
            result.x = ReadSingle(compressionType);
            result.y = ReadSingle(compressionType);
            return result;
        }

        public Vector3 ReadVector3()
        {
            var result = new Vector3();
            result.x = ReadSingle();
            result.y = ReadSingle();
            result.z = ReadSingle();
            return result;
        }

        public Vector3 ReadVector3(FloatCompressionTypeShort compressionType)
        {
            var result = new Vector3();
            result.x = ReadSingle(compressionType);
            result.y = ReadSingle(compressionType);
            result.z = ReadSingle(compressionType);
            return result;
        }
    }


}
