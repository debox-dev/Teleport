using DeBox.Teleport.Transport;
using System;

namespace DeBox.Teleport.HighLevel
{
    public abstract class TimedTeleportMessage : BaseTeleportMessage
    {
        public float Timestamp { get; private set; }

        public TimedTeleportMessage()
        {
            Timestamp = -1;
        }

        public TimedTeleportMessage(float timestamp)
        {
            Timestamp = timestamp;
        }

        public virtual void OnTimedPlayback() {}

        public override void Deserialize(TeleportReader reader)
        {
            Timestamp = reader.ReadSingle();
            base.Deserialize(reader);
        }

        public override void Serialize(TeleportWriter writer)
        {
            if (Timestamp < 0)
            {
                throw new Exception("Timed message must be serialized after it was called with the Timestamp constructor!");
            }
            writer.Write(Timestamp);
            base.Serialize(writer);
        }
    }
}
