using UnityEngine;
using System.Collections.Generic;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Unity
{
    public class BasicTeleportObjectSpawner : MonoBehaviour, ITeleportObjectSpawner
    {
        [SerializeField]
        private GameObject _prefab = null;

        private ushort _instanceIdSequence = 0;
        private Dictionary<ushort, GameObject> _instanceByInstanceId = new Dictionary<ushort, GameObject>();
        private Dictionary<GameObject, ushort> _instanceIdByInstance = new Dictionary<GameObject, ushort>();

        public ushort SpawnId { get; private set; }

        public bool IsManagedPrefab(GameObject prefab)
        {
            return prefab == _prefab;
        }

        public bool IsManagedInstance(GameObject instance)
        {
            return _instanceIdByInstance.ContainsKey(instance);
        }

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

        public virtual void OnClientDespawn(TeleportReader reader, GameObject despawned)
        {
            var instanceId = _instanceIdByInstance[despawned];
            _instanceIdByInstance.Remove(despawned);
            _instanceByInstanceId.Remove(instanceId);
        }

        public virtual void OnClientSpawn(TeleportReader reader, GameObject spawned)
        {
            var instanceId = reader.ReadUInt16();
            spawned.name = "ClientSpawn_" + spawned.name;
            _instanceIdByInstance[spawned] = instanceId;
            _instanceByInstanceId[instanceId] = spawned;
        }

        public virtual void OnServerDespawn(TeleportWriter reader, GameObject despawned)
        {
            var instanceId = _instanceIdByInstance[despawned];
            _instanceIdByInstance.Remove(despawned);
            _instanceByInstanceId.Remove(instanceId);
        }

        public virtual void OnServerSpawn(TeleportWriter writer, GameObject spawned, object instanceConfig)
        {
            var instanceId = _instanceIdSequence++;
            writer.Write(instanceId);
            _instanceIdByInstance[spawned] = instanceId;
            _instanceByInstanceId[instanceId] = spawned;
        }

        public GameObject GetInstanceById(ushort instanceId)
        {
            return _instanceByInstanceId[instanceId];
        }

        public ushort GetInstanceId(GameObject instance)
        {
            return _instanceIdByInstance[instance];
        }

        public ITeleportObjectSpawner Duplicate()
        {
            return Instantiate(this);
        }
    }

}

