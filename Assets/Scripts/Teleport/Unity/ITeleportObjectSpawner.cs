using UnityEngine;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Unity
{
    public interface ITeleportObjectSpawner
    {
        ushort SpawnId { get;  }
        bool IsManagedPrefab(GameObject prefab);
        void AssignSpawnId(ushort spawnId);
        GameObject CreateInstance();
        void DestroyInstance(GameObject instance);
        void OnClientSpawn(TeleportReader reader, GameObject spawned);
        void OnClientDespawn(TeleportReader reader, GameObject despawned);
        void OnServerSpawn(TeleportWriter writer, GameObject spawned, object objectConfig);
        void OnServerDespawn(TeleportWriter reader, GameObject despawned);
    }
}