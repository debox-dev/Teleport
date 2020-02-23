using System.Collections;
using UnityEngine;
using DeBox.Teleport;
using DeBox.Teleport.Core;


namespace DeBox.Teleport.Tests
{

    public class TestProcessor : MonoBehaviour
    {
        public class TestMessage : BaseTeleportMessage
        {
            public override byte MsgTypeId => TeleportMsgTypeIds.Highest + 10;

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
        }

        public class TimedMessageTest : TimedTeleportMessage
        {
            public override byte MsgTypeId => TeleportMsgTypeIds.Highest + 11;

            public override void OnTimedPlayback()
            {
                base.OnTimedPlayback();
                Debug.Log("Timed playback! my stamp: " + Timestamp + " local time: " + Time.fixedTime);
            }
        }

        [SerializeField]
        private bool _start = false;
        private bool _isOn;
        private float _nextSendTime;

        bool didClientSend = false;

        private TeleportServerProcessor _server;
        private TeleportClientProcessor _client;

        // Update is called once per frame
        void FixedUpdate()
        {
            if (_start)
            {
                _start = false;

                StartCoroutine(TestCoro());
            }
            if (!_isOn)
            {
                return;
            }


            if (Time.fixedTime > _nextSendTime)
            {
              //  _server.HandleIncoming();
               // _client.HandleIncoming();

                _nextSendTime = Time.time + 0.001f;                
                for (int i = 0; i < 20; i++)
                {
                    _server.SendToAll(new TimedMessageTest());

                }

                for (int i = 0; i < 20; i++)
                {
                    _client.SendToServer(new TestMessage());

                }
            }

        }
        

        private void OnDestroy()
        {
            _client.Disconnect();
            _server.StopListening();
        }

        private IEnumerator TestCoro()
        {
            didClientSend = false;
            var port = 6000;
            var serverTransport = new TeleportUdpTransport(() => new SequencedTeleportChannel(new SimpleTeleportChannel()));
            var clientTransport = new TeleportUdpTransport(() => new SequencedTeleportChannel(new SimpleTeleportChannel()));          
            _server = new TeleportServerProcessor(serverTransport);
            _client = new TeleportClientProcessor(clientTransport);
            _server.RegisterMessage<TestMessage>();
            _client.RegisterMessage<TestMessage>();
            _server.RegisterMessage<TimedMessageTest>();
            _client.RegisterMessage<TimedMessageTest>();
            _server.Listen(port);
            _client.Connect("127.0.0.1", port);
            _nextSendTime = Time.time + 1;
            _isOn = true;
            yield return new WaitForSeconds(50);
            _isOn = false;
            _server.StopListening();
            _client.Disconnect();

        }
    }

}
