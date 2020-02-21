using System;
using System.IO;
using UnityEngine;

namespace DeBox.Teleport.Transport
{

    public class ChecksumTeleportChannel : SimpleTeleportChannel
    {
        protected override void PreReceive(TeleportReader reader)
        {
            var checksumFromMessage = reader.ReadByte();
            var data = ((MemoryStream)reader.BaseStream).ToArray();
            var checksumOfData = Checksum(data, 1);
            if (checksumOfData != checksumFromMessage)
            {
                Debug.LogError("Checksum mismatch: received: " + checksumFromMessage + " calculated" + checksumOfData);
                return;
            }
            reader.ReadByte();
            base.PreReceive(reader);
        }

        protected override byte[] PreSend(byte[] data)
        {
            var checksum = Checksum(data, 0);
            var newData = new byte[data.Length + 1];
            newData[0] = checksum;
            Array.Copy(data, 0, newData, 1, data.Length);
            return newData;
        }

        private byte Checksum(byte[] data, long startOffset)
        {
            byte checksumCalculated = 0;
            unchecked
            {
                for (long i = startOffset; i < data.Length; i++)
                {
                    checksumCalculated += data[i];
                }
            }
            return checksumCalculated;
        }
    }

}
