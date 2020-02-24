using UnityEngine;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Unity
{
    public class BasicTeleportObjectSpawner : MonoBehaviour, ITeleportObjectSpawner
    {
        [SerializeField]
        private GameObject _prefab = null;

        public ushort SpawnId { get; private set; }

        public bool IsManagedPrefab(GameObject prefab)
        {
            return prefab == _prefab;
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

        public virtual void OnClientDespawn(TeleportReader reader, GameObject despawned) { }
        public virtual void OnClientSpawn(TeleportReader reader, GameObject spawned)
        {
            spawned.name = "ClientSpawn_" + spawned.name;
        }
        public virtual void OnServerDespawn(TeleportWriter reader, GameObject despawned) { }
        public virtual void OnServerSpawn(TeleportWriter writer, GameObject spawned, object instanceConfig) { }
    }

}

