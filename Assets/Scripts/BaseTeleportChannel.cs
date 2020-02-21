using System;
using System.IO;

namespace DeBox.Teleport.Transport
{
    public abstract class BaseTeleportChannel : ITeleportChannel
    { 
        public abstract int IncomingMessageCount { get; }

        public abstract int OutgoingMessageCount { get; }

        public abstract void Receive(TeleportReader reader);

        public abstract TeleportReader GetNextIncomingData();

        public abstract byte[] GetNextOutgoingData();

        public abstract void Send(TeleportWriter writer, MemoryStream stream, Action<TeleportWriter> serializerFunc);

        public void Send(Action<TeleportWriter> serializerFunc)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new TeleportWriter(stream))
                {
                    Send(writer, stream, serializerFunc);                    
                }
            }
        }
    }

    public abstract class BaseTeleportProxyChannel : BaseTeleportChannel
    {
        public override int IncomingMessageCount => InternalChannel.IncomingMessageCount;

        public override int OutgoingMessageCount => InternalChannel.OutgoingMessageCount;

        protected BaseTeleportChannel InternalChannel { get; private set; }

        public BaseTeleportProxyChannel(BaseTeleportChannel internalChannel)
        {
            InternalChannel = internalChannel;
        }

        public override TeleportReader GetNextIncomingData()
        {
            return InternalChannel.GetNextIncomingData();
        }

        public override byte[] GetNextOutgoingData()
        {
            return InternalChannel.GetNextOutgoingData();
        }
    }
}
