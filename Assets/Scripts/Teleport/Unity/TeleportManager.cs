using System;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Unity
{
    public class TeleportManager : BaseTeleportManager
    {
        [Header("Connection")]
        [SerializeField] private string _clientHostname = "localhost";
        [SerializeField] private int _port = 5000;
        [SerializeField] private TeleportChannelType[] _channelTypes = { TeleportChannelType.SequencedReliable };
        [Header("Prefabs")]
        [SerializeField] private GameObject[] _prefabSpawners = new GameObject[0];
        [Header("Client Playback")]
        [SerializeField] private float _playbackDelay = 0.3f;
        [Header("Chaos Generator")]
        [SerializeField] private TeleportChaoticPacketBuffer.ChaosSettings _chaosSettings = null;

        public float PlaybackDelay => _playbackDelay;

        private List<ITeleportObjectSpawner> _clientSpawners;
        private List<ITeleportObjectSpawner> _serverSpawners;

        public static TeleportManager Main { get; private set; }

        public void StartServer() { StartServer(_port); }

        public void ConnectClient() { ConnectClient(_clientHostname, _port); }

        public void ConnectClient(string hostname, int port) { ConnectClient(hostname, port, _playbackDelay); }

        protected virtual void Start()
        {
            InitSpawners();
            if (Main != null)
            {
                Debug.LogWarning("More than one TeleportManager exists");
                return;
            }
            Main = this;
        }

        protected virtual void OnDestroy()
        {
            foreach (var spawner in _clientSpawners)
            {
                spawner.DestroySelf();
            }
            foreach (var spawner in _serverSpawners)
            {
                spawner.DestroySelf();
            }
            _clientSpawners.Clear();
            _serverSpawners.Clear();
            if (Main == this)
            {
                Main = null;
            }
        }

        protected override Func<ITeleportPacketBuffer> GetBufferCreator()
        {
            if (_chaosSettings == null || !_chaosSettings.EnableChaos)
            {
                return base.GetBufferCreator();
            }
 	        return () => new TeleportChaoticPacketBuffer(_chaosSettings);
        }

        private void InitSpawners()
        {
            _clientSpawners = new List<ITeleportObjectSpawner>();
            _serverSpawners = new List<ITeleportObjectSpawner>();
            for (int i = 0; i < _prefabSpawners.Length; i++)
            {
                var prefab = _prefabSpawners[i];
                var spawner = prefab.GetComponent<ITeleportObjectSpawner>();
                if (spawner == null)
                {
                    var go = new GameObject("TeleportSpawner_" + prefab.name);
                    go.hideFlags = HideFlags.HideAndDontSave;
                    var basicSpawner = go.AddComponent<BasicTeleportObjectSpawner>();
                    basicSpawner.AssignPrefab(prefab, prefab);
                    spawner = basicSpawner;
                }
                var clientSpawner = spawner.Duplicate(TeleportObjectSpawnerType.ClientSide);
                var serverSpawner = spawner.Duplicate(TeleportObjectSpawnerType.ServerSide);
                clientSpawner.AssignSpawnId((ushort)i);
                serverSpawner.AssignSpawnId((ushort)i);
                _clientSpawners.Add(clientSpawner);
                _serverSpawners.Add(serverSpawner);
            }
        }

        public void RegisterSpawner(ITeleportObjectSpawner spawner)
        {
            var spawnerId = (ushort)_clientSpawners.Count;
            spawner.AssignSpawnId(spawnerId);
            _clientSpawners.Add(spawner.Duplicate(TeleportObjectSpawnerType.ClientSide));
            _serverSpawners.Add(spawner.Duplicate(TeleportObjectSpawnerType.ServerSide));
        }

        public ITeleportObjectSpawner GetClientSpawner(ushort spawnId)
        {
            return _clientSpawners[spawnId];
        }

		public ITeleportObjectSpawner GetServerSpawnerForPrefab(GameObject prefab)
		{
			for (int i = 0; i < _serverSpawners.Count; i++)
			{
				if (_serverSpawners[i].IsManagedPrefab(prefab))
				{
					return _serverSpawners[i];
				}
			}
			throw new System.Exception("No TeleportObjectSpawner for prefab " + prefab.name);
		}

        public ITeleportObjectSpawner GetServerSpawnerForInstance(GameObject instance)
        {
            return GetSpawnerForInstance(instance, _serverSpawners);
        }

        public ITeleportObjectSpawner GetServerClientForInstance(GameObject instance)
        {
            return GetSpawnerForInstance(instance, _clientSpawners);
        }

        public ITeleportObjectSpawner GetSpawnerForInstance(GameObject instance, List<ITeleportObjectSpawner> spawners)
        {
            for (int i = 0; i < spawners.Count; i++)
            {
                if (spawners[i].IsManagedInstance(instance))
                {
                    return spawners[i];
                }
            }
            throw new System.Exception("No TeleportObjectSpawner for prefab " + instance.name);
        }

        public GameObject ServerSideSpawn(GameObject prefab, Vector3 position, object instanceConfig, byte channelId = 0)
        {
            var spawner = GetServerSpawnerForPrefab(prefab);
            var message = new TeleportSpawnMessage(spawner, position, instanceConfig);
            SendToAllClients(message, channelId);
            return message.SpawnedObject;
        }

        public void ServerSideDespawn(GameObject instance, byte channelId = 0)
        {
            var spawner = GetServerSpawnerForInstance(instance);
            var instanceId = spawner.GetInstanceId(instance);
            var message = new TeleportDespawnMessage(spawner, instanceId);
            SendToAllClients(message, channelId);
        }

        private GameObject ServerSideSpawnRetroactiveForClient(uint clientId, GameObject instance, byte channelId = 0)
        {
            var spawner = GetServerSpawnerForInstance(instance);
            var instanceConfig = spawner.GetConfigForLiveInstance(instance);
            var message = new TeleportSpawnMessage(spawner, instance, instanceConfig);
            SendToClient(clientId, message, channelId);
            return message.SpawnedObject;
        }

        private void SpawnAllInstancesRetroactivelyForClient(uint clientId)
        {
            foreach (var serverSpawner in _serverSpawners)
            {
                foreach (var instance in serverSpawner.GetInstances())
                {
                    ServerSideSpawnRetroactiveForClient(clientId, instance);
                }
            }
        }

        protected override TeleportChannelType[] GetChannelTypes() { return _channelTypes; }

        public override void ClientSideOnConnected(uint clientId) {}

        public override void ClientSideOnDisconnected(uint clientId, TeleportClientProcessor.DisconnectReasonType reason) {}

        public override void ClientSideOnMessageArrived(ITeleportMessage message) {}

        public override void ServerSideOnClientConnected(uint clientId, EndPoint endpoint)
        {
            SpawnAllInstancesRetroactivelyForClient(clientId);
        }

        public override void ServerSideOnClientDisconnected(uint clientId, TeleportServerProcessor.DisconnectReasonType reason) {}

        public override void ServerSideOnMessageArrived(uint clientId, EndPoint endpoint, ITeleportMessage message) {}

        public override void ServerStarted() {}

        public override void ServerSidePrestart()
        {
        }

        public override void ClientSidePreconnect()
        {
            RegisterClientMessage<TeleportSpawnMessage>();
            RegisterClientMessage<TeleportDespawnMessage>();
            RegisterClientMessage<TeleportStateSyncMessage>();
        }
    }
}
