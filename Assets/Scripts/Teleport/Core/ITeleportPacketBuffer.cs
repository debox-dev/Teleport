namespace DeBox.Teleport.Core
{
    public interface ITeleportPacketBuffer
    {
        void ReceiveRawData(byte[] data, int dataLength);
        byte[] CreatePacket(byte channelId, byte[] data, int offset = 0, ushort length = 0);
        int TryParseNextIncomingPacket(byte[] outBuffer, out byte channelId);
    }
}
