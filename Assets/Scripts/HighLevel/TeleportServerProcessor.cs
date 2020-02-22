using System;
using System.Net;
using System.Collections.Generic;
using DeBox.Teleport.Transport;

namespace DeBox.Teleport.HighLevel
{
    public class TeleportServerProcessor : BaseTeleportProcessor
    {
        private class TeleportClientData
        {
            public EndPoint endpoint;
            public bool isAuthenticated;
            public uint clientId;
            public byte authKey;
        }


        private uint _nextClientId;
        private Dictionary<EndPoint, TeleportClientData> _clientDataByEndpoint;
        private Dictionary<uint, TeleportClientData> _clientDataById;


        public TeleportServerProcessor(TeleportUdpTransport transport) : base(transport)
        {
            _nextClientId = 0;
            _clientDataByEndpoint = new Dictionary<EndPoint, TeleportClientData>();
            _clientDataById = new Dictionary<uint, TeleportClientData>();
        }

        public void Listen(int port)
        {
            _transport.StartListener(port);
        }

        public void StopListening()
        {
            _transport.StopListener();
        }

        private TeleportClientData PerformFirstMessageAuthentication(EndPoint sender, TeleportReader reader)
        {
            TeleportClientData clientData;
            var header = reader.ReadByte();
            var isFirstAuth = (header & 1) == 0;
            byte authKey;
            uint clientId;
            if (isFirstAuth)
            {
                UnityEngine.Debug.Log("Server got handshake from " + sender);
                authKey = 13; // TODO: Randomize
                clientId = _nextClientId;
                _nextClientId++;
                clientData = new TeleportClientData()
                {
                    endpoint = sender,
                    isAuthenticated = true,
                    authKey = authKey,
                    clientId = clientId,
                };
                _clientDataByEndpoint[sender] = clientData;
                _clientDataById[clientId] = clientData;
                UnityEngine.Debug.Log("Server sends handshake ack to client " + sender);
                Send((w) =>
                {
                    w.Write(TeleportMsgTypeIds.Handshake);
                    w.Write(authKey);
                    w.Write(clientId);
                });
            }
            else
            {
                authKey = (byte)(header >> 1);
                clientId = reader.ReadUInt32();
                clientData = _clientDataById[clientId];
                if (authKey != clientData.authKey)
                {
                    throw new Exception("Auth key mismatches");
                }
            }
            return clientData;
        }

        protected override void HandleIncomingMessage(EndPoint sender, TeleportReader reader)
        {
            TeleportClientData clientData;

            var msgTypeId = reader.ReadByte();
            if (!_clientDataByEndpoint.TryGetValue(sender, out clientData))
            {
                if (msgTypeId != TeleportMsgTypeIds.Handshake)
                {
                    throw new Exception("First message must be handshake!");
                }
                clientData = PerformFirstMessageAuthentication(sender, reader);
                return;
            }
            
            switch (msgTypeId)
            {
                case TeleportMsgTypeIds.Handshake:
                    break; // Already handshaked
                default:
                    ProcessMessage(sender, msgTypeId, reader);
                    break;
            }
        }


    }
}
