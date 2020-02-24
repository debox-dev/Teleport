using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeBox.Teleport.Unity;

namespace DeBox.Teleport.Tests
{
    public class TestMessage : TimedTeleportMessage
    {
        public override byte MsgTypeId => TeleportMsgTypeIds.Highest + 1;

        public override void OnArrivalToClient()
        {
            base.OnArrivalToClient();
            Debug.Log("Arrived to client");
        }

        public override void OnArrivalToServer(uint clientId)
        {
            base.OnArrivalToServer(clientId);
            Debug.Log("Arrived to server");
        }
        
        public override void PostSendClient()
        {
            base.PostSendClient();
            Debug.Log("Post send client");
        }

        public override void PostSendServer()
        {
            base.PostSendServer();
            Debug.Log("Post send server");
        }

        public override void PreSendClient()
        {
            base.PreSendClient();
            Debug.Log("Pre send client");
        }

        public override void PreSendServer()
        {
            base.PreSendServer();
            Debug.Log("Pre send server");
        }

        public override void OnTimedPlayback()
        {
            base.OnTimedPlayback();
            Debug.Log("Timed playback");
        }
    }


    public class TestManager : MonoBehaviour
    {
        [SerializeField]
        private TeleportManager _manager = null;

        [SerializeField]
        private bool _start;

        [SerializeField]
        private GameObject _spawnedPrefab = null;

        private List<GameObject> _spawnedServerInstances = new List<GameObject>();

        private void Update()
        {
            if (_start)
            {
                _start = false;
                StartCoroutine(TestCoroutine());
            }
        }

        private IEnumerator TestCoroutine()
        {
            int spawnCount = 3;
            float duration = 10;
            _manager.StartServer();
            _manager.ConnectClient();
            _manager.RegisterClientMessage<TestMessage>();
            _manager.RegisterServerMessage<TestMessage>();
            var spawner = new GameObject("Spanwer").AddComponent<TestSpawner>();
            spawner.AssignPrefab(_spawnedPrefab);
            _manager.RegisterSpawner(spawner);

            while (duration > 0)
            {
                _manager.SendToAllClients(new TestMessage());
                yield return new WaitForSeconds(0.01f);
                
                if (_manager.ClientState == TeleportClientProcessor.StateType.Connected)
                {
                    _manager.SendToServer(new TestMessage());
                    while (spawnCount-- > 0)
                    {
                        var config = new TestSpawner.TestSpawnConfig() { Color = Color.red };
                        _spawnedServerInstances.Add(_manager.ServerSideSpawn(_spawnedPrefab, Vector3.zero, config));
                    }
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
