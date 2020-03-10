namespace DeBox.Teleport
{
    public static class TeleportMsgTypeIds
    {
        public const byte Handshake = 0;
        public const byte Disconnect = 1;
        public const byte TimeSync = 2;
        public const byte Spawn = 3;
        public const byte Despawn = 4;
        public const byte StateSync = 5;
        public const byte ClientReady = 6;
        public const byte Highest = 20;
    }
}
