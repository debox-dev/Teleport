using UnityEngine;
using System.Collections.Generic;
using DeBox.Teleport.Core;
using DeBox.Teleport.Utils;

namespace DeBox.Teleport.Unity
{
    public interface ITeleportState
    {
        ushort InstanceId { get; }
        void ApplyImmediate(GameObject instance);
        void FromInstance(GameObject instance);
        DeliveryTargetType GetDeliveryTarget();
        ITeleportState Interpolate(ITeleportState other, float progress01);
        bool ShouldSpawnForConnection(uint connectionId);
        void Serialize(TeleportWriter writer);
        bool SerializeForConnection(TeleportWriter writer, uint connectionId);
        void Deserialize(TeleportReader reader);
    }

    public class TeleportTransformState : ITeleportState
    {
        public ushort InstanceId { get; private set; }
        public Vector3 Position { get; private set; }
        public Quaternion Rotation { get; private set; }

        public TeleportTransformState() { }
        public TeleportTransformState(ushort instanceId) { InstanceId = instanceId; }

        public virtual DeliveryTargetType GetDeliveryTarget()
        {
            return DeliveryTargetType.Everyone;
        }

        public void FromInstance(GameObject instance)
        {
            var transform = instance.transform;
            Position = transform.position;
            Rotation = transform.rotation;
        }

        public void ApplyImmediate(GameObject instance)
        {
            var transform = instance.transform;
            transform.position = Position;
            transform.rotation = Rotation;
        }

        public void Deserialize(TeleportReader reader)
        {
            InstanceId = reader.ReadUInt16();
            Position = reader.ReadVector3(FloatCompressionTypeShort.Short_Two_Decimals);
            Rotation = reader.ReadQuaternion(FloatCompressionTypeShort.Short_Two_Decimals);
        }

        public void Serialize(TeleportWriter writer)
        {
            writer.Write(InstanceId);
            writer.Write(Position, FloatCompressionTypeShort.Short_Two_Decimals);
            writer.Write(Rotation, FloatCompressionTypeShort.Short_Two_Decimals);
        }

        public ITeleportState Interpolate(ITeleportState other, float progress01)
        {
            var otherState = (TeleportTransformState)other;
            var newState = new TeleportTransformState(InstanceId);
            newState.Position = Vector3.Lerp(Position, otherState.Position, progress01);
            newState.Rotation = Quaternion.Lerp(Rotation, otherState.Rotation, progress01);
            return newState;
        }

        public virtual bool SerializeForConnection(TeleportWriter writer, uint connectionId)
        {
            Serialize(writer);
            return true;
        }

        public virtual bool ShouldSpawnForConnection(uint connectionId)
        {
            return true;
        }
    }

    public enum TeleportObjectSpawnerType
    {
        None,
        ServerSide,
        ClientSide,
    }

    public interface ITeleportObjectSpawner
    {
        ushort SpawnId { get;  }
        bool ShouldSyncState { get; }
        ushort GetNextInstanceId();
        bool IsManagedPrefab(GameObject prefab);
        bool IsManagedInstance(GameObject instance);
        void AssignSpawnId(ushort spawnId);
        GameObject CreateInstance();
        void DestroyInstance(GameObject instance);
        void DestroySelf();
        void OnClientSpawn(ushort instanceId, TeleportReader reader, GameObject spawned);
        void OnClientDespawn(TeleportReader reader, GameObject despawned);
        void ServerSidePreSpawnToClient(TeleportWriter writer, GameObject spawned, object instanceConfig);
        GameObject SpawnOnServer(Vector3 position);
        void OnServerDespawn(TeleportWriter writer, GameObject despawned);
        GameObject GetInstanceById(ushort instanceId);
        ushort GetInstanceId(GameObject instance);
        ITeleportObjectSpawner Duplicate(TeleportObjectSpawnerType spawnerType);
        void ReceiveStates(float timestamp, ITeleportState[] instanceStates);
        ITeleportState[] GetCurrentStates();
        ITeleportState GenerateEmptyState();
        object GetConfigForLiveInstance(GameObject instance);
        ICollection<GameObject> GetInstances();
        bool IsSpawnedForClient(ushort instanceId, uint clientId);
        void SpawnForClient(ushort instanceId, uint clientId);
        void SendStatesToClient(uint clientId);
    }
}