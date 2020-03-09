using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using DeBox.Teleport.Core;

namespace DeBox.Teleport
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

        public event Action<uint, EndPoint> ClientConnected;
        public event Action<uint, DisconnectReasonType> ClientDisconnected;
        public event Action<uint, EndPoint, ITeleportMessage> MessageArrived;
        public event Action ServerBecameOnline;

        public bool IsListening => _transport.ThreadStarted;

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
            StartUnityHelper("Server");
            _transport.StartListener(port);
        }

        public void StopListening()
        {
            StopUnityHelper();
            _transport.StopListener();
        }

        public void SendMessage<T>(T message) where T : ITeleportMessage
        {
            StampMessageIfTimed(message);
            message.PreSendServer();
            switch (message.GetSerializationType())
            {
                case SerializationTargetType.Everyone:
                    Send(message, message.GetChannelId());
                    break;
                case SerializationTargetType.NoOne:
                    break;
                case SerializationTargetType.PerConnection:
                    EndPoint endpoint;
                    MemoryStream stream;
                    bool shouldSend;
                    foreach (var pair in _clientDataById)
                    {
                        endpoint = pair.Value.endpoint;
                        using (stream = new MemoryStream())
                        {
                            using (var writer = new TeleportWriter(stream))
                            {
                                shouldSend = message.SerializeForClient(writer, pair.Key);
                            }
                            if (shouldSend)
                            {
                                _transport.Send(stream.ToArray(), message.GetChannelId(), endpoint);
                            }
                        }
                    }
                    break;
                default:
                    throw new Exception("Unknown SerializationTargetType: " + message.GetSerializationType());
            } 
            message.PostSendServer();
        }

        public void SendToAll<T>(T message, byte channelId = 0) where T : ITeleportMessage
        {
            message.PreSendServer();
            StampMessageIfTimed(message);
            Send(message, message.GetChannelId());
            message.PostSendServer();
        }

        public void SendToClient<T>(uint clientId, T message, byte channelId = 0) where T : ITeleportMessage
        {
            SendToClients(message, channelId, clientId);
        }

        public void SendToClients<T>(T message, params uint[] clientIds) where T : ITeleportMessage
        {
            SendToClients(message, 0, clientIds);
        }

        public void SendToClients<T>(T message, byte channelId = 0, params uint[] clientIds) where T : ITeleportMessage
        {
            StampMessageIfTimed(message);
            message.PreSendServer();
            SendToEndpoints(message.SerializeWithId, channelId, GetEndpointsOfClients(clientIds));
            message.PostSendServer();
        }

        public void SendToAllExcept<T>(T message, byte channelId = 0, params uint[] excludedClientIds) where T : ITeleportMessage
        {
            StampMessageIfTimed(message);
            message.PreSendServer();
            var endpointsToSend = GetAllEndpointsExceptExcluded(excludedClientIds);
            SendToEndpoints(message.SerializeWithId, channelId, endpointsToSend.ToArray());
            message.PostSendServer();
        }

        private EndPoint[] GetEndpointsOfClients(params uint[] clientIds)
        {
            EndPoint[] endpoints = new EndPoint[clientIds.Length];
            for (int i = 0; i < clientIds.Length; i++)
            {
                endpoints[i] = GetClientData(clientIds[i]).endpoint;
            }
            return endpoints;
        }

        private void StampMessageIfTimed<T>(T message) where T : ITeleportMessage
        {
            var timedMessage = message as ITeleportTimedMessage;
            if (timedMessage != null)
            {
                timedMessage.SetTimestamp(LocalTime);
            }
        }

        private List<EndPoint> GetAllEndpointsExceptExcluded(params uint[] excludedClientIds)
        {
            List<EndPoint> endpointsToSend = new List<EndPoint>();
            foreach (var pair in _clientDataById)
            {
                bool exclude = false;
                for (int i = 0; i < excludedClientIds.Length; i++)
                {
                    if (pair.Key == excludedClientIds[i])
                    {
                        exclude = true;
                        break;
                    }
                }
                if (!exclude)
                {
                    endpointsToSend.Add(pair.Value.endpoint);
                }
            }
            return endpointsToSend;
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
            OnClientDisconnect(clientData.clientId, reason);
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
            SendToEndpoints((w) => { w.Write(TeleportMsgTypeIds.Disconnect); w.Write((byte)reason); }, channelId: 0, endpoint);
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
            message.OnArrivalToServer(clientData.clientId);
        }

        protected override void OnSocketThreadStarted()
        {
            base.OnSocketThreadStarted();
            ServerBecameOnline?.Invoke();
        }

        protected virtual void OnClientDisconnect(uint clientId, DisconnectReasonType reason)
        {
            ClientDisconnected?.Invoke(clientId, reason);
        }

        protected virtual void OnMessageArrival(uint clientId, EndPoint endpoint, ITeleportMessage message)
        {
            MessageArrived?.Invoke(clientId, endpoint, message);
        }
        protected virtual void OnClientConnected(uint clientId, EndPoint endpoint)
        {
            ClientConnected?.Invoke(clientId, endpoint);
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
                    OnClientDisconnect(clientData.clientId, DisconnectReasonType.ClientInitiatedDisconnect);
                    break;
                case TeleportMsgTypeIds.TimeSync:
                    ReplyToTimeSyncRequest(sender, reader);
                    break;
                default:
                    ProcessMessage(sender, msgTypeId, reader);
                    break;
            }
        }

        private void ReplyToTimeSyncRequest(EndPoint sender, TeleportReader reader)
        {
            var clientTime = reader.ReadSingle();
            SendToEndpoints((w) => SerializeTimeSyncResponse(w, clientTime), 0, sender);
        }

        private void SerializeTimeSyncResponse(TeleportWriter writer, float clientTime)
        {
            writer.Write(TeleportMsgTypeIds.TimeSync);
            writer.Write(clientTime);
            writer.Write(LocalTime);
        }


    }
}
