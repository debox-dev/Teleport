using UnityEngine;
using System.Collections.Generic;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Unity
{
    public class BasicTeleportObjectSpawner : MonoBehaviour, ITeleportObjectSpawner
    {
        [SerializeField]
        private GameObject _prefab = null;

        [SerializeField]
        private bool _syncTransform = true;

        [SerializeField]
        private float _stateUpdateRateInSeconds = 0.1f;

        private ushort _instanceIdSequence = 0;
        private Dictionary<ushort, GameObject> _instanceByInstanceId = new Dictionary<ushort, GameObject>();
        private Dictionary<GameObject, ushort> _instanceIdByInstance = new Dictionary<GameObject, ushort>();
        private float _nextSendStateTime = 0;
        private TeleportStateQueue _stateQueue = new TeleportStateQueue();
        private TeleportObjectSpawnerType _spawnerType;

        public ushort SpawnId { get; private set; }

        public bool ShouldSyncState => _syncTransform;

        public bool IsManagedPrefab(GameObject prefab)
        {
            return prefab == _prefab;
        }

        public bool IsManagedInstance(GameObject instance)
        {
            return _instanceIdByInstance.ContainsKey(instance);
        }

        public ushort GetNextInstanceId() { return _instanceIdSequence++; }

        public GameObject CreateInstance()
        {
            if (_prefab == null)
            {
                throw new System.Exception("BasicTeleportObjectSpawner: Prefab cannot be null!");
            }
            return Instantiate(_prefab);
        }

        public void DestroyInstance(GameObject instance)
        {
            Destroy(instance);
        }

        public void SetPrefab(GameObject prefab)
        {
            _prefab = prefab;
        }

        public void AssignSpawnId(ushort spawnId)
        {
            SpawnId = spawnId;
        }

        public void AssignPrefab(GameObject prefab)
        {
            _prefab = prefab;
        }

        public void OnClientDespawn(TeleportReader reader, GameObject despawned)
        {
            var instanceId = _instanceIdByInstance[despawned];
            _instanceIdByInstance.Remove(despawned);
            _instanceByInstanceId.Remove(instanceId);
        }


        public void OnClientSpawn(ushort instanceId, TeleportReader reader, GameObject spawned)
        {
            spawned.name = "ClientSpawn_" + spawned.name;
            _instanceIdByInstance[spawned] = instanceId;
            _instanceByInstanceId[instanceId] = spawned;
            PostClientSpawn(reader, spawned);
        }

        protected virtual void PostClientSpawn(TeleportReader reader, GameObject spawned)
        {

        }


        public void OnServerDespawn(TeleportWriter reader, GameObject despawned)
        {
            var instanceId = _instanceIdByInstance[despawned];
            _instanceIdByInstance.Remove(despawned);
            _instanceByInstanceId.Remove(instanceId);
        }


        public void OnServerSpawn(ushort instanceId, TeleportWriter writer, GameObject spawned)
        {
            _instanceIdByInstance[spawned] = instanceId;
            _instanceByInstanceId[instanceId] = spawned;
        }

        public virtual void ServerSidePreSpawnToClient(TeleportWriter writer, GameObject spawned, object instanceConfig)
        {

        }

        public virtual object GetConfigForLiveInstance(GameObject instance)
        {
            return null;
        }

        public GameObject GetInstanceById(ushort instanceId)
        {
            return _instanceByInstanceId[instanceId];
        }

        public ushort GetInstanceId(GameObject instance)
        {
            return _instanceIdByInstance[instance];
        }

        public ITeleportObjectSpawner Duplicate(TeleportObjectSpawnerType spawnerType)
        {
            var instance = Instantiate(this);
            instance.hideFlags = HideFlags.HideAndDontSave;
            instance._spawnerType = spawnerType;
            return instance;
        }

        public void DestroySelf()
        {
            Destroy(gameObject);
        }

        public void ReceiveStates(float timestamp, ITeleportState[] instanceStates)
        {
            _stateQueue.EnqueueEntry(timestamp, instanceStates);
        }

        public ITeleportState[] GetCurrentStates()
        {
            ushort instanceId;
            GameObject instance;
            ITeleportState state;
            var states = new ITeleportState[_instanceByInstanceId.Count];
            int stateIndex = 0;
            foreach (var pair in _instanceByInstanceId)
            {
                instanceId = pair.Key;
                instance = pair.Value;
                state = new TeleportTransformState(instanceId);
                state.FromInstance(instance);
                states[stateIndex++] = state;
            }
            return states;
        }

        protected virtual void FixedUpdate()
        {
            if (_syncTransform)
            {
                if (TeleportManager.Main.IsServerListening && _spawnerType == TeleportObjectSpawnerType.ServerSide)
                {
                    if (_nextSendStateTime < TeleportManager.Main.ServerSideTime)
                    {
                        _nextSendStateTime = TeleportManager.Main.ServerSideTime + _stateUpdateRateInSeconds;
                        SendStatesToClients();
                    }
                }
                if (TeleportManager.Main.ClientState == TeleportClientProcessor.StateType.Connected && _spawnerType == TeleportObjectSpawnerType.ClientSide)
                {
                    var timestamp = TeleportManager.Main.ClientSideServerTime - TeleportManager.Main.PlaybackDelay;
                    var states = _stateQueue.GetInterpolatedStates(timestamp);
                    ITeleportState state;
                    for (int i = 0; i < states.Length; i++)
                    {
                        state = states[i];
                        if (_instanceByInstanceId.ContainsKey(state.InstanceId))
                        {
                            state.ApplyImmediate(_instanceByInstanceId[state.InstanceId]);
                        }
                    }
                }
            }
        }

        private void Update()
        {
            if (_syncTransform)
            {
                
            }

        }

        public ITeleportState GenerateEmptyState()
        {
            return new TeleportTransformState(SpawnId);
        }

        protected void SendStatesToClients()
        {
            var states = GetCurrentStates();
            var message = new TeleportStateSyncMessage(SpawnId, states);
            TeleportManager.Main.SendToAllClients(message);
        }

        public ICollection<GameObject> GetInstances()
        {
            return _instanceByInstanceId.Values;
        }
    }

}

