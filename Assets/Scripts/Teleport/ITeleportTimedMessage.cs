namespace DeBox.Teleport
{
    public interface ITeleportTimedMessage : ITeleportMessage
    {
        float Timestamp { get; }
        void SetTimestamp(float timestamp);
        void OnTimedPlayback();
    }
}
