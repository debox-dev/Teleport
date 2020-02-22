using DeBox.Teleport.Transport;

namespace DeBox.Teleport.HighLevel
{
    public interface ITeleportMessage
    {
        byte MsgTypeId { get; }
        void Serialize(TeleportWriter writer);
        void Deserialize(TeleportReader reader);
        void OnArrival();
    }
}
