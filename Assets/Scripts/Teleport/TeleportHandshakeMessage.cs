using DeBox.Teleport.Core;

namespace DeBox.Teleport
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
