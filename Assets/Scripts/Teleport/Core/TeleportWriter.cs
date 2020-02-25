using System.IO;
using System.Text;
using UnityEngine;
using DeBox.Teleport.Utils;

namespace DeBox.Teleport.Core
{

    public class TeleportWriter : BinaryWriter
    {
        public TeleportWriter() : base()
        {
        }

        public TeleportWriter(Stream output) : base(output)
        {
        }

        public TeleportWriter(Stream output, Encoding encoding) : base(output, encoding)
        {
        }

        public TeleportWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen)
        {
        }

        public void Write(float value, FloatCompressionTypeShort compressionType)
        {
            Write(CompressionUtils.CompressToShort(value, compressionType));
        }

        public void Write(float value, FloatCompressionTypeChar compressionType)
        {
            Write(CompressionUtils.CompressToChar(value, compressionType));
        }

        public void Write(Quaternion data)
        {
            base.Write(data.x);
            base.Write(data.y);
            base.Write(data.z);
            base.Write(data.w);
        }

        public void Write(Quaternion data, FloatCompressionTypeShort compressionType)
        {
            Write(data.x, compressionType);
            Write(data.y, compressionType);
            Write(data.z, compressionType);
            Write(data.w, compressionType);
        }

        public void WriteBytesAndSize(byte[] data, byte length)
        {
            base.Write(length);
            base.Write(data);
        }

        public void Write(Color data)
        {
            base.Write(data.r);
            base.Write(data.g);
            base.Write(data.b);
        }

        public void Write(Vector3 data)
        {
            base.Write(data.x);
            base.Write(data.y);
            base.Write(data.z);
        }

        public void Write(Vector3 data, FloatCompressionTypeShort compressionType)
        {
            Write(data.x, compressionType);
            Write(data.y, compressionType);
            Write(data.z, compressionType);
        }

        public void Write(Vector2 data)
        {
            base.Write(data.x);
            base.Write(data.y);
        }

        public void Write(Vector2 data, FloatCompressionTypeShort compressionType)
        {
            Write(data.x, compressionType);
            Write(data.y, compressionType);
        }
    }

}
