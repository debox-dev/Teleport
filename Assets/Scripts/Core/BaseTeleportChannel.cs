namespace DeBox.Teleport.Core
{
    public abstract class BaseTeleportChannel : ITeleportChannel
    { 
        public abstract int IncomingMessageCount { get; }

        public abstract int OutgoingMessageCount { get; }

        public abstract void Receive(byte[] data, int startIndex, int length);

        public abstract byte[] GetNextIncomingData();

        public abstract byte[] GetNextOutgoingData();

        public abstract void Send(byte[] data);

        public abstract byte[] PrepareToSend(byte[] data);

        public virtual void Upkeep()
        {

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

        public override byte[] GetNextIncomingData()
        {
            return InternalChannel.GetNextIncomingData();
        }

        public override byte[] GetNextOutgoingData()
        {
            return InternalChannel.GetNextOutgoingData();
        }

        public override void Send(byte[] data)
        {
            InternalChannel.Send(data);
        }
    }
}
