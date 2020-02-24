using System.Net;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Tests
{
    public class TestUdpTransport : MonoBehaviour
    {
        private int _clientDataSeq;
        private int _serverDataSeq;
        private List<int> _clientMissingSeqs;
        private List<int> _serverMissingSeqs;
 

        private IEnumerator Start()
        {
            var port = 5006;
            float testDuration = 30;
            _clientDataSeq = 0;
            _serverDataSeq = 0;
            _clientMissingSeqs = new List<int>();
            _serverMissingSeqs = new List<int>();
            Debug.Log("Starting test in 3 seconds...");
            yield return new WaitForSeconds(3);
            var server = new TeleportUdpTransport(() => new SequencedTeleportChannel(new SimpleTeleportChannel()));
            var client = new TeleportUdpTransport(() => new SequencedTeleportChannel(new SimpleTeleportChannel()));
            server.StartListener(port);
            client.StartClient("127.0.0.1", port);
            Debug.Log("Waiting for client and server to start...");
            while (!client.ThreadStarted || !server.ThreadStarted)
            {
                yield return null;
            }
            yield return new WaitForSeconds(3);
            client.Send(new byte[] { 0, 0, 0, 0 }); // Say hello so server will recognize us
            bool didServerGetHello = false;
            while (!didServerGetHello)
            {
                server.ProcessIncoming((e, r) => { didServerGetHello = true; });
                yield return null;
            }
            Debug.Log("Server got hello from client");
            yield return new WaitForSeconds(1);
            while (testDuration > 0)
            {

                for (int i = 0; i < 20; i++)
                {
                    client.Send(SerializeClient);
                }
                for (int i = 0; i < 20; i++)
                {
                    server.Send(SerializeServer);
                }
                server.ProcessIncoming(DeserializeServer);
                client.ProcessIncoming(DeserializeClient);
                testDuration -= Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(10);

            server.StopListener();
            client.StopClient();

            for (int i = 0; i < _serverMissingSeqs.Count; i++)
            {
                Debug.LogError("Missing sequence never got to server: " + i); 
            }

            for (int i = 0; i < _clientMissingSeqs.Count; i++)
            { 
                Debug.LogError("Missing sequence never got to client: " + i);
            }

            if (_clientMissingSeqs.Count == 0 && _serverMissingSeqs.Count == 0)
            {
                Debug.Log("All passed!");
            }
        }

        private void SerializeClient(TeleportWriter w)
        {
            var seq = _clientDataSeq++;
            w.Write(seq);
            _serverMissingSeqs.Add(seq);
        }

        private void DeserializeClient(EndPoint e, TeleportReader r)
        {
            var seq = r.ReadInt32();
            Debug.Log("Client got data");
            _clientMissingSeqs.Remove(seq);
        }

        private void SerializeServer(TeleportWriter w)
        {
            var seq = _serverDataSeq++;
            w.Write(seq);
            _clientMissingSeqs.Add(seq);
        }

        private void DeserializeServer(EndPoint e, TeleportReader r)
        {
            Debug.Log("Server got data");
            var seq = r.ReadInt32();
            _serverMissingSeqs.Remove(seq);
        }
    }

}
