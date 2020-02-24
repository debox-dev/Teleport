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

        private List<ITeleportObjectSpawner> _spawners;

        public static TeleportManager Main { get; private set; }

        public void StartServer() { StartServer(_port); }

        public void ConnectClient() { ConnectClient(_clientHostname, _port); }

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
            _spawners = new List<ITeleportObjectSpawner>();
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
                spawner.AssignSpawnId((ushort)i);
                _spawners.Add(spawner);
            }
        }

        public void RegisterSpawner(ITeleportObjectSpawner spawner)
        {
            var spawnerId = (ushort)_spawners.Count;
            spawner.AssignSpawnId(spawnerId);
            _spawners.Add(spawner);
        }

        public ITeleportObjectSpawner GetSpawner(ushort spawnId)
        {
            return _spawners[spawnId];
        }

		public ITeleportObjectSpawner GetSpawnerForGameObject(GameObject prefab)
		{
			for (int i = 0; i < _spawners.Count; i++)
			{
				if (_spawners[i].IsManagedPrefab(prefab))
				{
					return _spawners[i];
				}
			}
			throw new System.Exception("No TeleportObjectSpawner for prefab " + prefab.name);
		}

        public void ServerSideSpawn(GameObject prefab, object instanceConfig, byte channelId = 0)
        {
            var spawner = GetSpawnerForGameObject(prefab);
            var message = new TeleportSpawnMessage(spawner, instanceConfig);
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
        }
    }
}
