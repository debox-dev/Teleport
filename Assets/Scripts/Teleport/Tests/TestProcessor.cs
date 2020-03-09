using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Tests
{

    public class TestProcessor : MonoBehaviour
    {
        public static class TestStats
        {
            public static int ClientSeq = 0;
            public static int ClientTimedSeq = 0;
            public static int ServerSeq = 0;

            public static int GetNextClientSeq() { return ClientSeq++; }
            public static int GetNextClientTimedSeq() { return ClientTimedSeq++; }
            public static int GetNextServerSeq() { return ServerSeq++; }

            public static List<int> ClientMissingSeqs = new List<int>();
            public static List<int> ServerMissingSeqs = new List<int>();
            public static List<int> ClientMissingTimedSeqs = new List<int>();
        }

        public class TestMessage : BaseTeleportMessage
        {
            private int _msgSeq;

            public override byte MsgTypeId => TeleportMsgTypeIds.Highest + 10;

            public override void PreSendClient()
            {
                base.PreSendClient();
                _msgSeq = TestStats.GetNextClientSeq();
                TestStats.ServerMissingSeqs.Add(_msgSeq);
            }

            public override void PreSendServer()
            {
                base.PreSendServer();
                _msgSeq = TestStats.GetNextServerSeq();
                TestStats.ClientMissingSeqs.Add(_msgSeq);
            }

            public override void OnArrivalToClient()
            {
                base.OnArrivalToClient();
                TestStats.ClientMissingSeqs.Remove(_msgSeq);
            }

            public override void OnArrivalToServer(uint clientId)
            {
                base.OnArrivalToServer(clientId);
                TestStats.ServerMissingSeqs.Remove(_msgSeq);
            }

            public override void Deserialize(TeleportReader reader)
            {
                base.Deserialize(reader);
                _msgSeq = reader.ReadInt32();
            }

            public override void Serialize(TeleportWriter writer)
            {
                base.Serialize(writer);
                writer.Write(_msgSeq);
            }
        }

        public class TimedMessageTest : TimedTeleportMessage
        {
            public override byte MsgTypeId => TeleportMsgTypeIds.Highest + 11;

            private int _msgSeq;

            public override void PreSendServer()
            {
                base.PreSendServer();
                _msgSeq = TestStats.GetNextClientTimedSeq();
                TestStats.ClientMissingTimedSeqs.Add(_msgSeq);
            }

            public override void OnTimedPlayback()
            {
                base.OnTimedPlayback();
                TestStats.ClientMissingTimedSeqs.Remove(_msgSeq);
            }

            public override void Deserialize(TeleportReader reader)
            {
                base.Deserialize(reader);
                _msgSeq = reader.ReadInt32();
            }

            public override void Serialize(TeleportWriter writer)
            {
                base.Serialize(writer);
                writer.Write(_msgSeq);
            }
        }


        private IEnumerator Start()
        {
            float testDuration = 30;
            var port = 6000;
            var serverTransport = new TeleportUdpTransport(() => new TeleportPacketBuffer(), () => new SequencedTeleportChannel(new SimpleTeleportChannel()));
            var clientTransport = new TeleportUdpTransport(() => new TeleportPacketBuffer(), () => new SequencedTeleportChannel(new SimpleTeleportChannel()));          
            var server = new TeleportServerProcessor(serverTransport);
            var client = new TeleportClientProcessor(clientTransport);
            server.RegisterMessage<TestMessage>();
            client.RegisterMessage<TestMessage>();
            server.RegisterMessage<TimedMessageTest>();
            client.RegisterMessage<TimedMessageTest>();
            Debug.Log("Test will start in 3 seconds");
            yield return new WaitForSeconds(3);
            server.Listen(port);
            client.Connect("localhost", port);
            Debug.Log("Waiting for client to authenticate...");
            while (client.State != TeleportClientProcessor.StateType.Connected)
            {
                yield return null;
            }
            Debug.Log("Client authenticated with server!");
            Debug.Log("Starting transfer, please wait for results (" + testDuration + " seconds)");
            while (testDuration > 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    server.SendMessage(new TimedMessageTest());
                    server.SendMessage(new TestMessage());

                }

                for (int i = 0; i < 5; i++)
                {
                    client.SendToServer(new TestMessage());

                }
                testDuration -= Time.deltaTime;
                yield return null;
            }
            Debug.Log("Stopping client and server...");
            yield return new WaitForSeconds(5);
        
            server.StopListening();
            client.Disconnect();
            Debug.Log("Test complete! Checking results..");

            if (TestStats.ClientMissingSeqs.Count > 0)
            {
                Debug.LogError("Client missing " + TestStats.ClientMissingSeqs.Count + " messages");
            }
            else if (TestStats.ClientMissingTimedSeqs.Count > 0)
            {
                Debug.LogError("Client missing " + TestStats.ClientMissingTimedSeqs.Count + " timed messages");
            }
            else if (TestStats.ServerMissingSeqs.Count > 0)
            {
                Debug.LogError("Client missing " + TestStats.ServerMissingSeqs.Count + " timed messages");
            }
            else
            {
                Debug.Log("All passed! Client msgs: " + TestStats.ClientSeq + " Client timed msgs: " + TestStats.ClientTimedSeq + " Server msgs: " + TestStats.ServerSeq);
            }
        }
    }

}
