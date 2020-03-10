using System.IO;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Unity
{

    public class TeleportStateSyncMessage : TimedTeleportMessage
    {
        public override byte MsgTypeId => TeleportMsgTypeIds.StateSync;

        protected ushort _spawnId;
        protected ITeleportState[] _states;
        private ITeleportObjectSpawner _spawner;

        public TeleportStateSyncMessage() { }

        public TeleportStateSyncMessage(ITeleportObjectSpawner spawner, ITeleportState[] states)
        {
            _spawner = spawner;
            _spawnId = spawner.SpawnId;
            _states = states;
        }

        public override DeliveryTargetType GetDeliveryTarget()
        {
            return DeliveryTargetType.PerConnection;
        }

        public override void OnArrivalToClient()
        {
            base.OnArrivalToClient();
            var spawner = TeleportManager.Main.GetClientSpawner(_spawnId);
            spawner.ReceiveStates(Timestamp, _states);
        }

        public override void Serialize(TeleportWriter writer)
        {
            base.Serialize(writer);
            writer.Write(_spawnId);
            writer.Write((ushort)_states.Length);
            for (int i = 0; i < _states.Length; i++)
            {
                _states[i].Serialize(writer);
            }            
        }

        public override void PreSendServer()
        {
            base.PreSendServer();
        }

        public override bool PreSendServerForClient(uint clientId)
        {
            base.PreSendServer();
            ITeleportState currentState;
            bool shouldSpawn;
            bool shouldSyncToClient = false;
            for (int i = 0; i < _states.Length; i++)
            {
                shouldSpawn = false;
                currentState = _states[i];
                switch (currentState.GetDeliveryTarget())
                {
                    case DeliveryTargetType.NoOne:
                        continue;
                    case DeliveryTargetType.Everyone:
                        shouldSpawn = !_spawner.IsSpawnedForClient(currentState.InstanceId, clientId);
                        shouldSyncToClient = true;
                        break;
                    case DeliveryTargetType.PerConnection:
                        shouldSpawn = currentState.ShouldSpawnForConnection(clientId) && !_spawner.IsSpawnedForClient(currentState.InstanceId, clientId);
                        shouldSyncToClient = true;
                        break;
                }
                if (shouldSpawn)
                {
                    _spawner.SpawnForClient(currentState.InstanceId, clientId);
                }
            }
            return shouldSyncToClient;
        }

        public override bool SerializeForClient(TeleportWriter writer, uint clientId)
        {
            base.Serialize(writer);
            int stateCount = 0;
            ITeleportState currentState;
            using (var subStream = new MemoryStream())
            {
                using (var subWriter = new TeleportWriter(subStream))
                {
                    for (int i = 0; i < _states.Length; i++)
                    {
                        currentState = _states[i];
                        switch (currentState.GetDeliveryTarget())
                        {
                            case DeliveryTargetType.NoOne:
                                continue;
                            case DeliveryTargetType.Everyone:
                                stateCount++;
                                _states[i].Serialize(subWriter);
                                break;
                            case DeliveryTargetType.PerConnection:
                                if (_states[i].SerializeForConnection(subWriter, clientId))
                                {
                                    stateCount++;
                                }
                                break;
                        }
                    }
                    writer.Write(_spawnId);
                    writer.Write((ushort)stateCount);
                    writer.Write(((MemoryStream)subWriter.BaseStream).ToArray());
                }
 
            }
            return stateCount > 0;
        }

        public override void Deserialize(TeleportReader reader)
        {
            base.Deserialize(reader);
            _spawnId = reader.ReadUInt16();
            var stateAmount = reader.ReadUInt16();
            var spawner = TeleportManager.Main.GetClientSpawner(_spawnId);
            _states = new ITeleportState[stateAmount];
            ITeleportState currentState;
            for (int i = 0; i < stateAmount; i++)
            {
                currentState = spawner.GenerateEmptyState();
                currentState.Deserialize(reader);
                _states[i] = currentState;
            }
        }
    }
}

