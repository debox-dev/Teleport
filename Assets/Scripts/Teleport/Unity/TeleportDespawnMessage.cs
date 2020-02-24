using System;
using System.IO;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Unity
{
    public class TeleportDespawnMessage : BaseTeleportMessage, ITeleportTimedMessage
    {
        public float Timestamp { get; private set; }
        public ushort SpawnId { get; private set; }

        public override byte MsgTypeId => TeleportMsgTypeIds.Despawn;

        private ITeleportObjectSpawner _spawner;
        private TeleportReader _reader;
        private ushort _instanceId;

        public TeleportDespawnMessage() { }

        public TeleportDespawnMessage(ITeleportObjectSpawner spawner, ushort instanceId)
        {
            _spawner = spawner;
            _instanceId = instanceId;
        }

        public void OnTimedPlayback()
        {
            var instance = _spawner.GetInstanceById(_instanceId);
            _spawner.OnClientDespawn(_reader, instance);
            _reader.Close();
            _spawner.DestroyInstance(instance);
        }

        public override void Deserialize(TeleportReader reader)
        {
            base.Deserialize(reader);
            SpawnId = reader.ReadUInt16();
            _instanceId = reader.ReadUInt16();
            _spawner = TeleportManager.Main.GetClientSpawner(SpawnId);
            // The reader will be closed by the time we use it, so we create a new reader
            var rawData = ((MemoryStream)reader.BaseStream).ToArray();
            var data = new byte[rawData.Length - reader.BaseStream.Position];
            Array.Copy(rawData, reader.BaseStream.Position, data, 0, data.Length);
            _reader = new TeleportReader(data);
        }

        public override void Serialize(TeleportWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_spawner.SpawnId);
            writer.Write(_instanceId);
            var despawned = _spawner.GetInstanceById(_instanceId);
            _spawner.OnServerDespawn(writer, despawned);
            _spawner.DestroyInstance(despawned);
        }

        public void SetTimestamp(float timestamp)
        {
            Timestamp = timestamp;
        }
    }
}

