using System.IO;
using System.Text;
using UnityEngine;

namespace DeBox.Teleport.Transport
{

    public class TeleportWriter : BinaryWriter
    {
        public TeleportWriter(Stream output) : base(output)
        {
        }

        public TeleportWriter(Stream output, Encoding encoding) : base(output, encoding)
        {
        }

        public TeleportWriter(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen)
        {
        }

        protected TeleportWriter()
        {
        }

        public void Write(Vector4 data)
        {
            base.Write(data.x);
            base.Write(data.y);
            base.Write(data.z);
            base.Write(data.w);
        }

        public void Write(Vector3 data)
        {
            base.Write(data.x);
            base.Write(data.y);
            base.Write(data.z);
        }

        public void Write(Vector2 data)
        {
            base.Write(data.x);
            base.Write(data.y);
        }
    }

}
