using DeBox.Teleport.Core;

namespace DeBox.Teleport.Unity
{

    public class TeleportStateSyncMessage : TimedTeleportMessage
    {
        public override byte MsgTypeId => TeleportMsgTypeIds.StateSync;

        protected ushort _spawnId;
        protected ITeleportState[] _states;

        public TeleportStateSyncMessage() { }

        public TeleportStateSyncMessage(ushort spawnId, ITeleportState[] states)
        {
            _spawnId = spawnId;
            _states = states;
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

        public override void Deserialize(TeleportReader reader)
        {            
            base.Deserialize(reader);
            _spawnId = reader.ReadUInt16();
            var stateAmount = reader.ReadUInt16();
            //UnityEngine.Debug.Log("Got " + stateAmount + " states!");
            
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

