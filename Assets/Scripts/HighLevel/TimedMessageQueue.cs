using DeBox.Teleport.Core;

namespace DeBox.Teleport
{ 
    public class TimedMessageQueue
    {
        private ArrayQueue<TimedTeleportMessage> _messageQueue;

        public TimedMessageQueue()
        {
            _messageQueue = new ArrayQueue<TimedTeleportMessage>(4096);
        }

        public void AcceptMessage(TimedTeleportMessage message)
        {
            _messageQueue.Enqueue(message);
        }

        public void ProcessUntil(float timestamp)
        {
            TimedTeleportMessage message;
            while (_messageQueue.Count > 0 && _messageQueue.Peek().Timestamp < timestamp)
            {
                message = _messageQueue.Dequeue();
                message.OnTimedPlayback();
            }
        }
    }
}
