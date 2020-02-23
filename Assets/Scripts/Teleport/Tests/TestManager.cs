using System.Collections;
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
            float duration = 20;
            _manager.StartServer();
            _manager.ConnectClient();
            _manager.RegisterClientMessage<TestMessage>();
            _manager.RegisterServerMessage<TestMessage>();

            while (duration > 0)
            {
                _manager.SendToAllClients(new TestMessage());
                yield return new WaitForSeconds(0.01f);
                
                if (_manager.ClientState == TeleportClientProcessor.StateType.Connected)
                {
                    _manager.SendToServer(new TestMessage());
                }                
                duration -= Time.deltaTime;
            }
            
            _manager.DisconnectClient();
            _manager.StopServer();
        }

    }

    

}
