using DeBox.Teleport.Core;

namespace DeBox.Teleport
{ 
    public class TimedMessageQueue
    {
        private ArrayQueue<ITeleportTimedMessage> _messageQueue;

        public TimedMessageQueue()
        {
            _messageQueue = new ArrayQueue<ITeleportTimedMessage>(4096);
        }

        public void AcceptMessage(ITeleportTimedMessage message)
        {
            _messageQueue.Enqueue(message);
        }

        public void ProcessUntil(float timestamp)
        {
            ITeleportTimedMessage message;
            while (_messageQueue.Count > 0 && _messageQueue.Peek().Timestamp < timestamp)
            {
                message = _messageQueue.Dequeue();
                message.OnTimedPlayback();
            }
        }
    }
}
