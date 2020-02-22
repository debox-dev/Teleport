using System;
using UnityEngine;

using DeBox.Teleport.Utils;


namespace DeBox.Teleport.Transport
{
    public class TeleportPacketBuffer
    {
        private const byte HEADER_LENGTH = 2;
        private const byte FIXED_PACKET_PREFIX = 3;
        private byte[] _buffer = new byte[2048];
        private int _bufferLength = 0;


        public void ReceiveRawData(byte[] data, int dataLength)
        {            
            Array.Copy(data, 0, _buffer, _bufferLength, dataLength);
            _bufferLength += dataLength;
        }

        public byte[] CreatePacket(byte channelId, byte[] data, int offset = 0, byte length = 0)
        {
            if (length == 0)
            {
                length = (byte)(data.Length - offset);
            }
            byte[] packetData = new byte[length + HEADER_LENGTH];
            byte headerLength = CreateHeader(packetData, channelId, data, offset, length);
            Array.Copy(data, offset, packetData, HEADER_LENGTH, length);
            return packetData;
        }

        public int TryParseNextIncomingPacket(byte[] outBuffer, out byte channelId)
        {
            byte crc, dataLength, headerLength;
            if (!ParseHeader(_buffer, 0, out crc, out channelId, out dataLength, out headerLength))
            {
                throw new Exception("invalid message header!!!");
            }
            if (_bufferLength < (headerLength + dataLength))
            {
                Debug.LogError("Not enough data");
                return 0; //not enough data
            }
            // copy the data to outBuffer
            Array.Copy(_buffer, headerLength, outBuffer, 0, dataLength);
            // Trim the packet from the buffer
            _bufferLength -= (dataLength + headerLength);
            Array.Copy(_buffer, headerLength + dataLength, _buffer, 0, _bufferLength);

            if (!ValidateCrc(crc, outBuffer, 0, dataLength))
            {
                // throw new Exception("crc check failed");
                Debug.LogError("crc check failed");
                return 0;
            }
            return dataLength;
        }


        private byte ReadBits(byte fullData, byte offset, byte mask)
        {
            return (byte)((fullData >> offset) & mask);
        }

        private bool ParseHeader(byte[] data, int position, out byte crc, out byte channelId, out byte length, out byte headerLength)
        {
            crc = ReadBits(data[position], 0, 0b11);
            channelId = ReadBits(data[position], 2, 0b11);
            var prefix = ReadBits(data[position], 6, 0b11);
            length = data[position + 1];
            headerLength = HEADER_LENGTH;
            if (prefix != FIXED_PACKET_PREFIX)
            {
                return false;
            }
            return true;
        }

        private bool ValidateCrc(byte crc, byte[] data, byte position, byte length)
        {
            var dataCrc = Crc.Checksum(data, position, length);
            return dataCrc == crc;
        }

        private byte CreateHeader(byte[] outHeader, byte channelId, byte[] data, int startPosition, byte length)
        {
            var crc = Crc.Checksum(data, startPosition, length);
            outHeader[0] = crc;
            outHeader[0] |= (byte)(channelId << 2);
            outHeader[0] |= (byte)(FIXED_PACKET_PREFIX << 6);
            outHeader[1] = length;
            return HEADER_LENGTH;
        }
    
    }

}
