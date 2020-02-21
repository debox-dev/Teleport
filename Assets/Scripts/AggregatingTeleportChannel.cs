using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeBox.Teleport.Transport
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
            var expectedDataSize = BitConverter.ToUInt16(data, startIndex);
            startIndex += sizeof(ushort);
            var receivedDataSize = length - startIndex;
            if (_receiveLeftoversLength > 0)
            {
                Array.Copy(data, startIndex, _receiveLeftovers, _receiveLeftoversLength, receivedDataSize);
                data = _receiveLeftovers;                
                _receiveLeftoversLength = _receiveLeftoversLength + receivedDataSize;
                receivedDataSize = _receiveLeftoversLength;
                startIndex = 0;
                length = _receiveLeftoversLength;
            }
            if (expectedDataSize > receivedDataSize)
            {
                Array.Copy(data, startIndex, _receiveLeftovers, 0, receivedDataSize);
                _receiveLeftoversLength = receivedDataSize;
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

        public override void Send(byte[] data)
        {
            InternalChannel.Send(data);
        }
    }
}
