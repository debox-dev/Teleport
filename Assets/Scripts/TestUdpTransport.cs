using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using DeBox.Teleport.Transport;

namespace DeBox.Teleport.Tests
{

    public class TestUdpTransport : MonoBehaviour
    {
        [SerializeField]
        private bool _start = false;
        private bool _isOn;
        private float _nextSendTime;

        //private BaseTeleportChannel _clientChannel = new SequencedTeleportChannel(new SimpleTeleportChannel());
        //private BaseTeleportChannel _serverChannel = new SequencedTeleportChannel(new SimpleTeleportChannel());
        private BaseTeleportChannel _clientChannel = new SimpleTeleportChannel();
        private BaseTeleportChannel _serverChannel = new SimpleTeleportChannel();

        private TeleportUdpTransport _serverTransport;
        private TeleportUdpTransport _clientTransport;

        // Update is called once per frame
        void Update()
        {
            if (_start)
            {
                _start = false;
                StartCoroutine(TestCoro());
            }
            while (_serverChannel.IncomingMessageCount > 0)
            {
                _serverChannel.GetNextIncomingData();
                Debug.Log("Server got data!");
            }
            while (_clientChannel.IncomingMessageCount > 0)
            {
                _clientChannel.GetNextIncomingData();
                Debug.Log("Client got data!");
            }
            if (!_isOn)
            {
                return;
            }
            if (Time.time > _nextSendTime)
            {
                _nextSendTime = Time.time + 1;
                _clientChannel.Send(Serialize);
                _serverChannel.Send(Serialize);
            }
            
        }

        private void Serialize(TeleportWriter w)
        {
            w.Write("LALALAL");
        }

        private void OnDestroy()
        {
            _clientTransport.StopClient();
            _serverTransport.StopListener();
        }

        private IEnumerator TestCoro()
        {
            var port = 5000;
            _serverTransport = new TeleportUdpTransport(() => new SimpleTeleportChannel());
            _clientTransport = new TeleportUdpTransport(() => new SimpleTeleportChannel());
            _serverTransport.StartListener(port);
            _clientTransport.StartClient("127.0.0.1", port);
            _nextSendTime = Time.time + 1;
            _isOn = true;
            yield return new WaitForSeconds(50);
            _isOn = false;
            _serverTransport.StopListener();
            _clientTransport.StopClient();
            
        }
    }

}
