namespace DeBox.Teleport.Core
{
    public interface ITeleportChannel
    {
        int IncomingMessageCount { get; }
        byte[] GetNextIncomingData();        
    }

}
