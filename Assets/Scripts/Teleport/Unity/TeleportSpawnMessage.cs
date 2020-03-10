using System;
using System.IO;
using DeBox.Teleport.Core;
using UnityEngine;

namespace DeBox.Teleport.Unity
{
    public class TeleportSpawnMessage : BaseTeleportMessage, ITeleportTimedMessage
    {
        public float Timestamp { get; private set; }
        public ushort SpawnId { get; private set; }

        public override byte MsgTypeId => TeleportMsgTypeIds.Spawn;

        public GameObject SpawnedObject { get; private set; }

        private ITeleportObjectSpawner _spawner;
        private TeleportReader _reader;
        private object _objectConfig;
        private ushort _instanceId;
    

        public TeleportSpawnMessage() {}

        public TeleportSpawnMessage(ITeleportObjectSpawner spawner, GameObject existing, object objectConfig)
        {
            _objectConfig = objectConfig;
            _spawner = spawner;
            _instanceId = _spawner.GetInstanceId(existing);
            SpawnedObject = existing;
        }

        public void OnTimedPlayback()
        {
            _spawner = TeleportManager.Main.GetClientSpawner(SpawnId);
            SpawnedObject = _spawner.ClientSideSpawn(_instanceId, _reader);
            _reader.Close();
        }

        public override void Deserialize(TeleportReader reader)
        {
            base.Deserialize(reader);
            SpawnId = reader.ReadUInt16();
            _instanceId = reader.ReadUInt16();
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
            _spawner.ServerSidePreSpawnToClient(writer, SpawnedObject, _objectConfig);
        }

        public void SetTimestamp(float timestamp)
        {
            Timestamp = timestamp;
        }
    }
}

