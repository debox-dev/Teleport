using DeBox.Teleport.Core;

namespace DeBox.Teleport
{
    public enum DeliveryTargetType
    {
        NoOne,
        Everyone,
        PerConnection
    }

    public interface ITeleportMessage
    {
        byte MsgTypeId { get; }
        byte GetChannelId();
        DeliveryTargetType GetDeliveryTarget();
        void SerializeWithId(TeleportWriter writer);
        void Serialize(TeleportWriter writer);
        bool SerializeForClient(TeleportWriter writer, uint clientId);
        void Deserialize(TeleportReader reader);
        void PreSendClient();
        void PreSendServer();
        bool PreSendServerForClient(uint clientId);
        void OnArrivalToServer(uint clientId);
        void OnArrivalToClient();
        void PostSendClient();
        void PostSendServer();
    }
}
