using System;

using System.IO;
using DeBox.Collections;

namespace DeBox.Teleport.Transport
{
    public class SimpleTeleportChannel : BaseTeleportChannel
    {
        private ArrayQueue<TeleportReader> _receiveQueue;

        private ArrayQueue<byte[]> _sendQueue;

        public SimpleTeleportChannel(int maxReceiveBuffer = 1000, int maxSendBuffer = 1000)
        {
            _receiveQueue = new ArrayQueue<TeleportReader>(maxReceiveBuffer);
            _sendQueue = new ArrayQueue<byte[]>(maxSendBuffer);
        }

        public override int IncomingMessageCount => _receiveQueue.Count;

        public override int OutgoingMessageCount => _sendQueue.Count;

        public override void Receive(TeleportReader reader)
        {
            
            PreReceive(reader);
            _receiveQueue.Enqueue(reader);
        }

        public override TeleportReader GetNextIncomingData()
        {
            return _receiveQueue.Dequeue();
        }

        public override byte[] GetNextOutgoingData()
        {
            return _sendQueue.Dequeue();
        }

        public override void Send(TeleportWriter writer, MemoryStream stream, Action<TeleportWriter> serializerFunc)
        {
            serializerFunc?.Invoke(writer);
            _sendQueue.Enqueue(PreSend(stream.ToArray()));
        }

        protected virtual byte[] PreSend(byte[] data)
        {
            return data;
        }

        protected virtual void PreReceive(TeleportReader reader)
        {

        }
    }
}
