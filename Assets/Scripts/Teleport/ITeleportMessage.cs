using DeBox.Teleport.Core;

namespace DeBox.Teleport
{
    public interface ITeleportMessage
    {
        byte MsgTypeId { get; }
        void SerializeWithId(TeleportWriter writer);
        void Serialize(TeleportWriter writer);
        void Deserialize(TeleportReader reader);
        void PreSendClient();
        void PreSendServer();
        void OnArrivalToServer(uint clientId);
        void OnArrivalToClient();
        void PostSendClient();
        void PostSendServer();
    }
}
