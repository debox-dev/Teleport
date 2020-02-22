using System;
using System.Net;
using System.Collections.Generic;
using DeBox.Teleport.Transport;

namespace DeBox.Teleport.HighLevel
{
    public class TeleportServerProcessor : BaseTeleportProcessor
    {
        public enum DisconnectReasonType
        {
            ServerWantsToDisconnectClient,
            ClientInitiatedDisconnect,
            ServerShutdown,
            ClientTimeout,
        }

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

        public void DisconnectClient(uint clientId)
        {
            TeleportClientData clientData;
            if (!_clientDataById.TryGetValue(clientId, out clientData))
            {
                throw new Exception("Client is not connected: " + clientId);
            }
            var reason = DisconnectReasonType.ServerWantsToDisconnectClient;
            SendDisconnectToClient(0, clientData.endpoint, reason);
            CleanupClientData(clientData);
            OnClientDisconnect(reason);
        }

        private TeleportClientData GetClientData(uint clientId)
        {
            return _clientDataById[clientId];
        }

        private TeleportClientData GetClientData(EndPoint endpoint)
        {
            return _clientDataByEndpoint[endpoint];
        }

        private void CleanupClientData(TeleportClientData clientData)
        {
            _clientDataById.Remove(clientData.clientId);
            _clientDataByEndpoint.Remove(clientData.endpoint);
        }

        private void SendDisconnectToClient(byte channelId, EndPoint endpoint, DisconnectReasonType reason)
        {
            Send((w) => { w.Write(TeleportMsgTypeIds.Disconnect); w.Write((byte)reason); }, channelId: 0, endpoint);
        }

        protected sealed override void OnMessageArrival(EndPoint endpoint, ITeleportMessage message)
        {
            TeleportClientData clientData;
            if (!_clientDataByEndpoint.TryGetValue(endpoint, out clientData))
            {
                UnityEngine.Debug.LogWarning("Server got unauthorized message, client needs to handshake first!");
                return;
            }
            OnMessageArrival(clientData.clientId, endpoint, message);
        }

        protected virtual void OnClientDisconnect(DisconnectReasonType reason) { }
        protected virtual void OnMessageArrival(uint clientId, EndPoint endpoint, ITeleportMessage message) { }
        protected virtual void OnClientConnected(uint clientId, EndPoint endpoint) { }

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
                OnClientConnected(clientId, sender);
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
                    break; // Already handshaked, do nothing
                case TeleportMsgTypeIds.Disconnect:
                    clientData = GetClientData(sender);
                    CleanupClientData(clientData);
                    OnClientDisconnect(DisconnectReasonType.ClientInitiatedDisconnect);
                    break;
                default:
                    ProcessMessage(sender, msgTypeId, reader);
                    break;
            }
        }


    }
}
