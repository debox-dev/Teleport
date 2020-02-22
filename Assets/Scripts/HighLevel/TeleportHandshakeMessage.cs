using DeBox.Teleport.Transport;

namespace DeBox.Teleport.HighLevel
{
    class TeleportHandshakeMessage : BaseTeleportMessage
    {
        public override byte MsgTypeId => TeleportMsgTypeIds.Handshake;

        public override void Deserialize(TeleportReader reader)
        {
            base.Deserialize(reader);
        }

        public override void Serialize(TeleportWriter writer)
        {
            base.Serialize(writer);
        }
    }
}
