using UnityEngine;
using System.Collections.Generic;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Unity
{
    public class BasicTeleportObjectSpawner : MonoBehaviour, ITeleportObjectSpawner
    {
        [SerializeField]
        private GameObject _serverPrefab = null;

        [SerializeField]
        private GameObject _clientPrefab = null;

        [SerializeField]
        private bool _syncState = true;

        [SerializeField]
        private float _stateUpdateRateInSeconds = 0.1f;

        private ushort _instanceIdSequence = 0;
        private Dictionary<ushort, GameObject> _instanceByInstanceId = new Dictionary<ushort, GameObject>();
        private Dictionary<GameObject, ushort> _instanceIdByInstance = new Dictionary<GameObject, ushort>();
        private Dictionary<ushort, HashSet<uint>> _instanceIdClientIdSpawnMap = new Dictionary<ushort, HashSet<uint>>();
        private float _nextSendStateTime = 0;
        private TeleportStateQueue _stateQueue = new TeleportStateQueue();
        private TeleportObjectSpawnerType _spawnerType;

        public ushort SpawnId { get; private set; }

        public bool ShouldSyncState => _syncState;

        public bool IsManagedPrefab(GameObject prefab)
        {
            return prefab == _serverPrefab;
        }

        public bool IsManagedInstance(GameObject instance)
        {
            return _instanceIdByInstance.ContainsKey(instance);
        }

        public ushort GetNextInstanceId() { return _instanceIdSequence++; }

        public virtual GameObject CreateInstance()
        {
            if (_serverPrefab == null)
            {
                throw new System.Exception("BasicTeleportObjectSpawner: Prefab cannot be null!");
            }
            return Instantiate(_spawnerType == TeleportObjectSpawnerType.ServerSide ? _serverPrefab : _clientPrefab);
        }

        public void DestroyInstance(GameObject instance)
        {
            Destroy(instance);
        }

        public void AssignSpawnId(ushort spawnId)
        {
            SpawnId = spawnId;
        }

        public void AssignPrefab(GameObject serverPrefab, GameObject clientPrefab)
        {
            _serverPrefab = serverPrefab;
            _clientPrefab = clientPrefab;
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

        public bool IsSpawnedForClient(ushort instanceId, uint clientId)
        {
            HashSet<uint> clientIdMap;
            if (_instanceIdClientIdSpawnMap.TryGetValue(instanceId, out clientIdMap))
            {
                return clientIdMap.Contains(clientId);
            }
            return false;
        }

        public void SpawnForClient(ushort instanceId, uint clientId)
        {
            var instance = _instanceByInstanceId[instanceId];
            var instanceConfig = GetConfigForLiveInstance(instance);
            var message = new TeleportSpawnMessage(this, instance, instanceConfig);
            MarkInstanceAsSpawnedForClient(instanceId, clientId);
            TeleportManager.Main.SendToClient(clientId, message);
        }

        private void MarkInstanceAsSpawnedForClient(ushort instanceId, uint clientId)
        {
            HashSet<uint> clientIdMap;
            if (!_instanceIdClientIdSpawnMap.TryGetValue(instanceId, out clientIdMap))
            {
                clientIdMap = new HashSet<uint>();
                _instanceIdClientIdSpawnMap[instanceId] = clientIdMap;
            }
            else
            {
                if (clientIdMap.Contains(clientId))
                {
                    throw new System.Exception("Already marked as spawned");
                }
            }
            clientIdMap.Add(clientId);
        }
        
        public void OnServerDespawn(GameObject despawned)
        {
            var instanceId = _instanceIdByInstance[despawned];
            _instanceIdByInstance.Remove(despawned);
            _instanceByInstanceId.Remove(instanceId);
            _instanceIdClientIdSpawnMap.Remove(instanceId);
        }

        public GameObject SpawnOnServer(Vector3 position)
        {
            var spawned = CreateInstance();
            var instanceId = GetNextInstanceId();
            spawned.transform.position = position;
            _instanceIdByInstance[spawned] = instanceId;
            _instanceByInstanceId[instanceId] = spawned;
            return spawned;
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
            if (ShouldSyncState)
            {
                if (TeleportManager.Main.IsServerListening && _spawnerType == TeleportObjectSpawnerType.ServerSide)
                {
                    if (_nextSendStateTime < TeleportManager.Main.ServerSideTime)
                    {
                        _nextSendStateTime = TeleportManager.Main.ServerSideTime + _stateUpdateRateInSeconds;
                        SendStatesToClients();
                    }
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

        public ITeleportState GenerateEmptyState()
        {
            return new TeleportTransformState(SpawnId);
        }

        public void SendStatesToClient(uint clientId)
        {
            var states = GetCurrentStates();
            var message = new TeleportStateSyncMessage(this, states);
            TeleportManager.Main.SendToClients(message);
        }

        protected void SendStatesToClients()
        {
            var states = GetCurrentStates();
            var message = new TeleportStateSyncMessage(this, states);
            TeleportManager.Main.SendToClients(message);
        }

        public ICollection<GameObject> GetInstances()
        {
            return _instanceByInstanceId.Values;
        }


    }

}

