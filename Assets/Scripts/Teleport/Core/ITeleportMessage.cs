namespace DeBox.Teleport.Core
{
    public interface ITeleportMessage
    {
        void Serialize(TeleportWriter writer);
        void Deserialize(TeleportReader reader);
    }
}
