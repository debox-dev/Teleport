using UnityEngine;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Unity
{
    public interface ITeleportObjectSpawner
    {
        ushort SpawnId { get;  }
        bool IsManagedPrefab(GameObject prefab);
        bool IsManagedInstance(GameObject instance);
        void AssignSpawnId(ushort spawnId);
        GameObject CreateInstance();
        void DestroyInstance(GameObject instance);
        void OnClientSpawn(TeleportReader reader, GameObject spawned);
        void OnClientDespawn(TeleportReader reader, GameObject despawned);
        void OnServerSpawn(TeleportWriter writer, GameObject spawned, object objectConfig);
        void OnServerDespawn(TeleportWriter writer, GameObject despawned);
        GameObject GetInstanceById(ushort instanceId);
        ushort GetInstanceId(GameObject instance);
        ITeleportObjectSpawner Duplicate();

    }
}