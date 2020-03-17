using System;
using DeBox.Teleport.Logging;
using DeBox.Teleport.Utils;

namespace DeBox.Teleport.Core
{
    public class SimpleTeleportChannel : BaseTeleportChannel
    {
        private ArrayQueue<byte[]> _receiveQueue;

        private ArrayQueue<byte[]> _sendQueue;

        private BaseTeleportLogger _logger;



        public SimpleTeleportChannel(int maxReceiveBuffer = 80960, int maxSendBuffer = 80960, BaseTeleportLogger logger = null)
        {
            _receiveQueue = new ArrayQueue<byte[]>(maxReceiveBuffer);
            _sendQueue = new ArrayQueue<byte[]>(maxSendBuffer);
            _logger = logger;
        }

        public override int IncomingMessageCount => _receiveQueue.Count;

        public override int OutgoingMessageCount => _sendQueue.Count;

        public override void Receive(byte[] data, int startIndex, int length)
        {
            var strippedData = new byte[length];
            Array.Copy(data, startIndex, strippedData, 0, length);
            _logger.Debug("SimpleTeleportChannel: got data: " + TeleportDebugUtils.DebugString(strippedData) + "\n" + TeleportDebugUtils.DebugString(data));
            _receiveQueue.Enqueue(strippedData);
        }

        public override byte[] GetNextIncomingData()
        {
            var deq =  _receiveQueue.Dequeue();
            _logger.Debug("SimpleTeleportChannel: dispatching incoming data: " + TeleportDebugUtils.DebugString(deq));
            return deq
        }

        public override byte[] GetNextOutgoingData()
        {
            return _sendQueue.Dequeue();            
        }

        public override void Send(byte[] data)
        {            
            _sendQueue.Enqueue(data);
        }

        public override byte[] PrepareToSend(byte[] data)
        {
            return data;
        }
    }
}
