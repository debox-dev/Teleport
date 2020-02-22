using DeBox.Teleport.Transport;

namespace DeBox.Teleport.HighLevel
{
    public abstract class BaseTeleportMessage : ITeleportMessage
    {
        public BaseTeleportMessage()
        {
        }

        public abstract byte MsgTypeId { get; }

        public virtual void SerializeWithId(TeleportWriter writer)
        {
            writer.Write(MsgTypeId);
            Serialize(writer);
        }

        public virtual void Deserialize(TeleportReader reader)
        {

        }

        public virtual void Serialize(TeleportWriter writer)
        {

        }

        public virtual void PreSendClient() { }
        public virtual void PreSendServer() { }
        public virtual void OnArrivalToServer(uint clientId) { }
        public virtual void OnArrivalToClient() { }
        public virtual void PostSendClient() { }
        public virtual void PostSendServer() { }
    }
}
