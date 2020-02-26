using System;
using UnityEngine;

using DeBox.Teleport.Utils;


namespace DeBox.Teleport.Core
{
    public class TeleportPacketBuffer : ITeleportPacketBuffer
    {
        private const byte HEADER_LENGTH = 3;
        private const byte FIXED_PACKET_PREFIX = 3;
        private const byte HEADER_CRC_SIZE = 0b1111;
        private const byte DATA_CRC_SIZE = 0b1111;
        private const byte TWO_BITS_MASK = 0b11;
        private const byte FOUR_BITS_MASK = 0b1111;
        private const byte FULL_BYTE_MASK = 0xFF;
        private byte[] _buffer = new byte[8096];
        private int _bufferLength = 0;


        public void ReceiveRawData(byte[] data, int dataLength)
        {
            if (_bufferLength + dataLength > _buffer.Length)
            {
                Debug.LogError("Buffer overflow! Dropping entire buffer");
                _bufferLength = 0;
            }
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
            byte crc, headerLength, headerCrc;
            if (_bufferLength == 0)
            {
                channelId = 0;
                return 0;
            }
            if (!ParseHeader(_buffer, 0, out crc, out headerCrc, out channelId, out dataLength, out headerLength))
            {
                Debug.LogError("invalid message header!!!");
                AdvanceToNextViablePacket();
                return 0;
            }
            var dataLengthBytes = BitConverter.GetBytes(dataLength);
            var actualHeaderCrc = CrcUtils.Checksum(new byte[] { channelId, dataLengthBytes[0], dataLengthBytes[1] }, 0, 3, HEADER_CRC_SIZE);
            if (actualHeaderCrc != headerCrc)
            {
                Debug.LogError("header crc check failed");
                AdvanceToNextViablePacket();
                return 0;
            }
            if (_bufferLength == 0)
            {
                return 0;
            }
            if (_bufferLength < (headerLength + dataLength))
            {
                return 0; //not enough data
            }

            
            // copy the data to outBuffer
            Array.Copy(_buffer, headerLength, outBuffer, 0, dataLength);
            // Trim the packet from the buffer
            _bufferLength -= (dataLength + headerLength);
            Array.Copy(_buffer, headerLength + dataLength, _buffer, 0, _bufferLength);

            if (!ValidateCrc(crc, outBuffer, 0, dataLength))
            {
                Debug.LogError("data crc check failed");
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
            var prefix = ReadBits(data[position], 6, TWO_BITS_MASK);
            return prefix == FIXED_PACKET_PREFIX;
        }

        private bool ParseHeader(byte[] data, int position, out byte crc, out byte headerCrc, out byte channelId, out ushort length, out byte headerLength)
        {
            crc = ReadBits(data[position], 0, DATA_CRC_SIZE);
            channelId = ReadBits(data[position], 4, TWO_BITS_MASK);
            var prefix = ReadBits(data[position], 6, TWO_BITS_MASK);
            length = (ushort)(((data[position + 1] & FOUR_BITS_MASK) << 8) + data[position + 2]);
            headerCrc = ReadBits(data[position + 1], 4, HEADER_CRC_SIZE);
            headerLength = HEADER_LENGTH;
            if (prefix != FIXED_PACKET_PREFIX)
            {
                return false;
            }
            return true;
        }

        private bool ValidateCrc(byte crc, byte[] data, byte position, ushort length)
        {
            var dataCrc = CrcUtils.Checksum(data, position, length, DATA_CRC_SIZE);
            return dataCrc == crc;
        }

        /// <summary>
        /// Header structure:
        /// ========================================
        /// 1. Fixed packet prefix (2 bits)
        /// 2. Channel Id (2 bits)
        /// 3. Data CRC (4 bits)
        /// 4. Header CRC (4 bits) 
        /// 5. Data Length (12 bits, trimmed ushort, max=4096)
        /// 
        /// Illustrated header structure
        /// ========================================
        /// BYTE 1  +-+[] FIXED PACKET PREFIX (2 bits)
        ///         |  []
        ///         |
        ///         |  [] CHANNEL ID (2 bits)
        ///         |  []
        ///         |
        ///         |  [] DATA CRC (4 bits)
        ///         |  []
        ///         |  []
        ///         +-+[]
        ///
        /// BYTE 2  +-+[] HEADER CRC (4 bits)
        ///         |  []
        ///         |  []
        ///         |  []
        ///         |
        ///         |  [] DATA LENGTH (12 bits)
        ///         |  []
        ///         |  []
        ///         +-+[]
        /// BYTE 3  +-+[]
        ///         |  []
        ///         |  []
        ///         |  []
        ///         |  []
        ///         |  []
        ///         |  []
        ///         +-+[]
        /// 
        /// </summary>
        /// <param name="outHeader">byte[] buffer to write the header into</param>
        /// <param name="channelId">Channel ID to embed into the header</param>
        /// <param name="data">Data of the packet, given for calculating the data CRC</param>
        /// <param name="startPosition">Start position of the data in the given Data buffer</param>
        /// <param name="length">Length of the data in the given data buffer</param>
        /// <returns>The length of the header in bytes</returns>
        private byte CreateHeader(byte[] outHeader, byte channelId, byte[] data, int startPosition, ushort length)
        {
            if (length > 4096)
            {
                throw new Exception("Maximum data length is 4096 bytes! Got " + length);
            }
            var random = new System.Random();
            var lengthBytes = BitConverter.GetBytes(length);
            var crc = CrcUtils.Checksum(data, startPosition, length, DATA_CRC_SIZE);
            var headerCrc = CrcUtils.Checksum(new byte[] { channelId, lengthBytes[0], lengthBytes[1] }, 0, 3, HEADER_CRC_SIZE);
            outHeader[0] = crc;
            outHeader[0] |= (byte)(channelId << 4);
            outHeader[0] |= (byte)(FIXED_PACKET_PREFIX << 6);            
            outHeader[1] = (byte)(length >> 8 & FOUR_BITS_MASK);
            outHeader[2] = (byte)(length & FULL_BYTE_MASK);
            outHeader[1] |= (byte)(headerCrc << 4);
            return HEADER_LENGTH;
        }
    
    }

}
