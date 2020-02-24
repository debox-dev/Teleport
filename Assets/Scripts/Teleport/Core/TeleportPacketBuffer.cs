using System;
using UnityEngine;

using DeBox.Teleport.Utils;


namespace DeBox.Teleport.Core
{
    public class TeleportPacketBuffer
    {
        private const byte HEADER_LENGTH = 3;
        private const byte FIXED_PACKET_PREFIX = 3;
        private byte[] _buffer = new byte[8096];
        private int _bufferLength = 0;


        public void ReceiveRawData(byte[] data, int dataLength)
        {
            
            Array.Copy(data, 0, _buffer, _bufferLength, dataLength);
            _bufferLength += dataLength;
            
        }

        public byte[] CreatePacket(byte channelId, byte[] data, int offset = 0, ushort length = 0)
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

        private void AdvanceToNextViablePacket()
        {
            for (int i = 1; i < _bufferLength; i++)
            {
                if (DoesLookLikeStartOfPacket(_buffer, i))
                {
                    Debug.LogError("Advanced to next packet!");
                    _bufferLength -= i;
                    Array.Copy(_buffer, i, _buffer, 0, _bufferLength);
                    return;
                }
            }            
            _bufferLength = 0;
        }

        public int TryParseNextIncomingPacket(byte[] outBuffer, out byte channelId)
        {
            
            ushort dataLength;
            byte crc, headerLength;
            if (!ParseHeader(_buffer, 0, out crc, out channelId, out dataLength, out headerLength))
            {
                Debug.LogError("invalid message header!!!");
                AdvanceToNextViablePacket();
                return 0;
            }
            if (_bufferLength == 0)
            {                
                return 0;
            }
            if (_bufferLength < (headerLength + dataLength))
            {
                Debug.Log("Buffer too small! " + _bufferLength + " < " + (headerLength + dataLength));
                return 0; //not enough data
            }

            
            // copy the data to outBuffer
            Array.Copy(_buffer, headerLength, outBuffer, 0, dataLength);
            // Trim the packet from the buffer
            _bufferLength -= (dataLength + headerLength);
            Array.Copy(_buffer, headerLength + dataLength, _buffer, 0, _bufferLength);

            if (!ValidateCrc(crc, outBuffer, 0, dataLength))
            {
                Debug.LogError("crc check failed");
                AdvanceToNextViablePacket();
                return 0;
            }
            return dataLength;
        }


        private byte ReadBits(byte fullData, byte offset, byte mask)
        {
            return (byte)((fullData >> offset) & mask);
        }

        private bool DoesLookLikeStartOfPacket(byte[] data, int position)
        {
            var prefix = ReadBits(data[position], 6, 0b11);
            return prefix == FIXED_PACKET_PREFIX;
        }

        private bool ParseHeader(byte[] data, int position, out byte crc, out byte channelId, out ushort length, out byte headerLength)
        {
            crc = ReadBits(data[position], 0, 0b11);
            channelId = ReadBits(data[position], 2, 0b11);
            var prefix = ReadBits(data[position], 6, 0b11);
            length = BitConverter.ToUInt16(data, position + 1);
            headerLength = HEADER_LENGTH;
            if (prefix != FIXED_PACKET_PREFIX)
            {
                return false;
            }
            return true;
        }

        private bool ValidateCrc(byte crc, byte[] data, byte position, ushort length)
        {
            var dataCrc = CrcUtils.Checksum(data, position, length);
            return dataCrc == crc;
        }

        private bool oneCrc = false;

        private byte CreateHeader(byte[] outHeader, byte channelId, byte[] data, int startPosition, ushort length)
        {
            var random = new System.Random();
            var crc = CrcUtils.Checksum(data, startPosition, length);
            outHeader[0] = crc;
            outHeader[0] |= (byte)(channelId << 2);
            outHeader[0] |= (byte)(FIXED_PACKET_PREFIX << 6);
            var lengthBytes = BitConverter.GetBytes(length);
            outHeader[1] = lengthBytes[0];
            outHeader[2] = lengthBytes[1];
            return HEADER_LENGTH;
        }
    
    }

}
