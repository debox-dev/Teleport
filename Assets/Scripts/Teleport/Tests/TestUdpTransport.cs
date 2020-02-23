﻿using System.Collections;
using UnityEngine;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Tests
{

    public class TestUdpTransport : MonoBehaviour
    {
        [SerializeField]
        private bool _start = false;
        private bool _isOn;
        private float _nextSendTime;

        bool didClientSend = false;

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
            if (!_isOn)
            {
                return;
            }

   
            if (Time.time > _nextSendTime)
            {
                _serverTransport.ProcessIncoming((e, r) => Debug.Log("server got data from " + e));
                _clientTransport.ProcessIncoming((e, r) => Debug.Log("client got data from " + e));

                _nextSendTime = Time.time + 0.1f;
                if (!didClientSend)
                {
                    _clientTransport.Send(Serialize);
                    didClientSend = true;
                }
                for (int i = 0; i < 20; i ++)
                {
                    _serverTransport.Send(Serialize);

                }

                for (int i = 0; i < 20; i++)
                {
                    _clientTransport.Send(Serialize);

                }
                //_clientTransport.Send(Serialize);
                //_clientTransport.Send(Serialize);
                //_clientTransport.Send(Serialize);
            }
            
        }

        private void Serialize(TeleportWriter w)
        {
            w.Write("LALALAL");
        }

        private void OnDestroy()
        {
            //_clientTransport.StopClient();
            //_serverTransport.StopListener();
        }

        private IEnumerator TestCoro()
        {
            didClientSend = false;
            var port = 5000;
            //_serverTransport = new TeleportUdpTransport(() => new SimpleTeleportChannel());
            //_clientTransport = new TeleportUdpTransport(() => new SimpleTeleportChannel());
            _serverTransport = new TeleportUdpTransport(() => new SequencedTeleportChannel(new SimpleTeleportChannel()));
            _clientTransport = new TeleportUdpTransport(() => new SequencedTeleportChannel(new SimpleTeleportChannel()));
            //_serverTransport = new TeleportUdpTransport(() => new AggregatingTeleportChannel(new SimpleTeleportChannel()));
            //_clientTransport = new TeleportUdpTransport(() =>new AggregatingTeleportChannel(new SimpleTeleportChannel()));
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