using System.IO;
using System.Text;
using UnityEngine;

namespace DeBox.Teleport.Transport
{
    public class TeleportReader : BinaryReader
    {
        public TeleportReader(Stream input) : base(input)
        {
        }

        public TeleportReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public TeleportReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        public Vector4 ReadQuaternion()
        {
            var result = new Vector4();
            result.x = ReadSingle();
            result.y = ReadSingle();
            result.z = ReadSingle();
            result.w = ReadSingle();
            return result;
        }

        public Vector2 ReadVector2()
        {
            var result = new Vector2();
            result.x = ReadSingle();
            result.y = ReadSingle();
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
    }


}
