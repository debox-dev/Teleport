using System;
using DeBox.Collections;

namespace DeBox.Teleport.Transport
{
    public class SimpleTeleportChannel : BaseTeleportChannel
    {
        private ArrayQueue<byte[]> _receiveQueue;

        private ArrayQueue<byte[]> _sendQueue;

        public SimpleTeleportChannel(int maxReceiveBuffer = 1000, int maxSendBuffer = 1000)
        {
            _receiveQueue = new ArrayQueue<byte[]>(maxReceiveBuffer);
            _sendQueue = new ArrayQueue<byte[]>(maxSendBuffer);
        }

        public override int IncomingMessageCount => _receiveQueue.Count;

        public override int OutgoingMessageCount => _sendQueue.Count;

        public override void Receive(byte[] data, int startIndex, int length)
        {
            var strippedData = new byte[length];
            Array.Copy(data, startIndex, strippedData, 0, length);
            _receiveQueue.Enqueue(strippedData);
        }

        public override byte[] GetNextIncomingData()
        {
            return _receiveQueue.Dequeue();
        }

        public override byte[] GetNextOutgoingData()
        {
            return _sendQueue.Dequeue();
        }

        public override void Send(byte[] data)
        {
            _sendQueue.Enqueue(data);
        }
    }
}
