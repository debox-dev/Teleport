using System;
using DeBox.Teleport.Utils;

namespace DeBox.Teleport.Core
{
    class AggregatingTeleportChannel : BaseTeleportProxyChannel
    {
        private byte[] _receiveLeftovers;
        private int _receiveLeftoversLength;

        public AggregatingTeleportChannel(BaseTeleportChannel internalChannel) : base(internalChannel)
        {
            _receiveLeftovers = new byte[1024];
            _receiveLeftoversLength = 0;
        }

        public override void Receive(byte[] data, int startIndex, int length)
        {
            //UnityEngine.Debug.LogError("Receive: " + GetType().ToString() + ": " + DeBox.Teleport.Debugging.TeleportDebugUtils.DebugString(data, startIndex, length));
            var expectedDataSize = BitConverter.ToUInt16(data, startIndex);
            startIndex += sizeof(ushort);
            length = length - sizeof(ushort);
            var receivedDataSize = length;

            if (_receiveLeftoversLength > 0)
            {
                Array.Copy(data, startIndex, _receiveLeftovers, _receiveLeftoversLength, receivedDataSize);
                data = _receiveLeftovers;                
                _receiveLeftoversLength = _receiveLeftoversLength + receivedDataSize;
                receivedDataSize = _receiveLeftoversLength;
                UnityEngine.Debug.LogError("Had leftovers");
                startIndex = 0;
                length = _receiveLeftoversLength;
            }
            if (expectedDataSize > receivedDataSize)
            {
                Array.Copy(data, startIndex, _receiveLeftovers, 0, receivedDataSize);
                _receiveLeftoversLength = receivedDataSize;
                UnityEngine.Debug.LogError("Not enough data! expected: "  + expectedDataSize + " got: " + receivedDataSize);
                return;
            }
            else if (expectedDataSize < receivedDataSize)
            {
                _receiveLeftoversLength = receivedDataSize - expectedDataSize;
                Array.Copy(data, startIndex + expectedDataSize, _receiveLeftovers, 0, _receiveLeftoversLength);
                length = expectedDataSize;
            }            
            InternalChannel.Receive(data, startIndex, length);
        }

        public override byte[] PrepareToSend(byte[] data)
        {
            data = InternalChannel.PrepareToSend(data);
            var header = BitConverter.GetBytes((ushort)data.Length);
            var fullData = new byte[header.Length + data.Length];
            Array.Copy(header, 0, fullData, 0, header.Length);
            Array.Copy(data, 0, fullData, header.Length, data.Length);
            //UnityEngine.Debug.LogError("Prepare: " + GetType().ToString() + ": " + DeBox.Teleport.Debugging.TeleportDebugUtils.DebugString(fullData));
            return fullData;
        }
    }
}
