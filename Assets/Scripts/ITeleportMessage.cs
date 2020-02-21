namespace DeBox.Teleport.Transport
{
    public interface ITeleportMessage
    {
        void Serialize(TeleportWriter writer);
        void Deserialize(TeleportReader reader);
    }
}
