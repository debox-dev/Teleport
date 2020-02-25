using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeBox.Teleport.Unity;

namespace DeBox.Teleport.Tests
{
    

    public class TestBoids : MonoBehaviour
    {
        [SerializeField]
        private TeleportManager _manager = null;

        [SerializeField]
        private GameObject _serverPrefab = null;

        [SerializeField]
        private GameObject _clientPrefab = null;

        [SerializeField]
        private int _spawnedObjectCount = 10;

        [SerializeField]
        private float _spawnDuration = 60;

        private List<GameObject> _spawnedServerInstances = new List<GameObject>();

        private IEnumerator Start()
        {
            Debug.Log("Test will start in 3 seconds...");
            yield return new WaitForSeconds(3);
            int spawnCount = _spawnedObjectCount;
            float duration = _spawnDuration;
            _manager.StartServer();

            _manager.RegisterServerMessage<TestMessage>();
            var spawner = new GameObject("Spanwer").AddComponent<TestSpawner>();
            spawner.AssignPrefab(_serverPrefab, _clientPrefab);
            _manager.RegisterSpawner(spawner);
            Debug.Log("Waiting for server to start...");
            while (!_manager.IsServerListening)
            {
                yield return null;
            }
            Debug.Log("Server started, spawning objects...");
            while (spawnCount-- > 0)
            {
                var config = new TestSpawner.TestSpawnConfig() { Color = Color.red };
                _spawnedServerInstances.Add(_manager.ServerSideSpawn(_serverPrefab, Vector3.one * Random.Range(-10, 10), config));
            }
            yield return new WaitForSeconds(2);
            Debug.Log("Waiting for client to connect..");
            _manager.ConnectClient();
            _manager.RegisterClientMessage<TestMessage>();
            while (!_manager.IsServerListening || _manager.ClientState != TeleportClientProcessor.StateType.Connected)
            {
                yield return null;
            }
            Debug.Log("Client connected!");


            while (duration > 0)
            {
                //_manager.SendToAllClients(new TestMessage());
                yield return new WaitForSeconds(0.01f);

                if (_manager.ClientState == TeleportClientProcessor.StateType.Connected)
                {
                    //_manager.SendToServer(new TestMessage());

                }
                duration -= Time.deltaTime;
            }
            foreach (var instance in _spawnedServerInstances)
            {
                _manager.ServerSideDespawn(instance);
            }
            yield return new WaitForSeconds(5);
            _manager.DisconnectClient();
            _manager.StopServer();
        }

    }



}
