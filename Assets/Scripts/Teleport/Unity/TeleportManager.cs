using System.Net;
using System.Collections.Generic;
using UnityEngine;

namespace DeBox.Teleport.Unity
{
    public class TeleportManager : BaseTeleportManager
    {
        [SerializeField] private string _clientHostname = "localhost";
        [SerializeField] private int _port = 5000;
        [SerializeField] private TeleportChannelType[] _channelTypes = { TeleportChannelType.SequencedReliable };
        [SerializeField] private GameObject[] _prefabSpawners = new GameObject[0];
        [SerializeField] private float _playbackDelay = 0.3f;

        public float PlaybackDelay => _playbackDelay;

        private List<ITeleportObjectSpawner> _clientSpawners;
        private List<ITeleportObjectSpawner> _ServerSpawners;

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
            if (Main == this)
            {
                Main = null;
            }
        }

        private void InitSpawners()
        {
            _clientSpawners = new List<ITeleportObjectSpawner>();
            _ServerSpawners = new List<ITeleportObjectSpawner>();
            for (int i = 0; i < _prefabSpawners.Length; i++)
            {
                var prefab = _prefabSpawners[i];
                var spawner = prefab.GetComponent<ITeleportObjectSpawner>();
                if (spawner == null)
                {
                    var go = new GameObject("TeleportSpawner_" + prefab.name);
                    var basicSpawner = go.AddComponent<BasicTeleportObjectSpawner>();
                    basicSpawner.AssignPrefab(prefab);
                    spawner = basicSpawner;
                }
                var clientSpawner = spawner.Duplicate(TeleportObjectSpawnerType.ClientSide);
                var serverSpawner = spawner.Duplicate(TeleportObjectSpawnerType.ServerSide);
                clientSpawner.AssignSpawnId((ushort)i);
                serverSpawner.AssignSpawnId((ushort)i);
                _clientSpawners.Add(clientSpawner);
                _ServerSpawners.Add(serverSpawner);
            }
        }

        public void RegisterSpawner(ITeleportObjectSpawner spawner)
        {
            var spawnerId = (ushort)_clientSpawners.Count;
            spawner.AssignSpawnId(spawnerId);
            _clientSpawners.Add(spawner.Duplicate(TeleportObjectSpawnerType.ClientSide));
            _ServerSpawners.Add(spawner.Duplicate(TeleportObjectSpawnerType.ServerSide));
        }

        public ITeleportObjectSpawner GetClientSpawner(ushort spawnId)
        {
            return _clientSpawners[spawnId];
        }

		public ITeleportObjectSpawner GetServerSpawnerForPrefab(GameObject prefab)
		{
			for (int i = 0; i < _ServerSpawners.Count; i++)
			{
				if (_ServerSpawners[i].IsManagedPrefab(prefab))
				{
					return _ServerSpawners[i];
				}
			}
			throw new System.Exception("No TeleportObjectSpawner for prefab " + prefab.name);
		}

        public ITeleportObjectSpawner GetServerSpawnerForInstance(GameObject instance)
        {
            return GetSpawnerForInstance(instance, _ServerSpawners);
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

		protected override TeleportChannelType[] GetChannelTypes() { return _channelTypes; }

        public override void ClientSideOnConnected(uint clientId) {}

        public override void ClientSideOnDisconnected(uint clientId, TeleportClientProcessor.DisconnectReasonType reason) {}

        public override void ClientSideOnMessageArrived(ITeleportMessage message) {}

        public override void ServerSideOnClientConnected(uint clientId, EndPoint endpoint) {}

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
