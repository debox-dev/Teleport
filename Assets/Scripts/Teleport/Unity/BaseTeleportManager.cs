using System;
using System.Net;
using UnityEngine;
using DeBox.Teleport.Core;

namespace DeBox.Teleport.Unity
{
    public abstract class BaseTeleportManager : MonoBehaviour
    {
        public enum TeleportChannelType
        {
            SequencedReliable,
            Unreliable
        }

        protected TeleportClientProcessor _client;
        protected TeleportServerProcessor _server;

        public TeleportClientProcessor.StateType ClientState => _client == null ? TeleportClientProcessor.StateType.Disconnected : _client.State;
        public bool IsServerListening => _server != null && _server.IsListening;
        public float ServerSideTime => _server != null ? _server.LocalTime : 0;
        public float ClientSideServerTime => _client != null ? _client.ServerTime : 0;
        public float ClientSideLocalTime => _client != null ? _client.LocalTime : 0;

        private TeleportUdpTransport CreateTransport()
        {
            return new TeleportUdpTransport(GetBufferCreator(), GetChannelCreators());
        }

        protected virtual Func<ITeleportPacketBuffer> GetBufferCreator()
        {
            return () => new TeleportPacketBuffer();
        }

        private Func<BaseTeleportChannel>[] GetChannelCreators()
        {
            var channelTypes = GetChannelTypes();
            var channelFuncs = new Func<BaseTeleportChannel>[channelTypes.Length];
            for (int i = 0; i < channelTypes.Length; i++)
            {
                Func<BaseTeleportChannel> channelFunc;
                switch (channelTypes[i])
                {
                    case TeleportChannelType.Unreliable:
                        channelFunc = () => new SimpleTeleportChannel();
                        break;
                    case TeleportChannelType.SequencedReliable:
                        channelFunc = () => new SequencedTeleportChannel(new SimpleTeleportChannel());
                        break;
                    default:
                        throw new Exception("Don't know how to create channel type: " + channelTypes[i]);
                }
                channelFuncs[i] = channelFunc;
            }
            return channelFuncs;
        }

        public void StartServer(int port)
        {
            _server = new TeleportServerProcessor(CreateTransport());
            _server.ClientConnected += ServerSideOnClientConnected;
            _server.ClientDisconnected += ServerSideOnClientDisconnected;
            _server.MessageArrived += ServerSideOnMessageArrived;
            _server.ServerBecameOnline += ServerStarted;
            ServerSidePrestart();
            _server.Listen(port);
        }

        public void StopServer() {
            _server.ClientConnected -= ServerSideOnClientConnected;
            _server.ClientDisconnected -= ServerSideOnClientDisconnected;
            _server.MessageArrived -= ServerSideOnMessageArrived;
            _server.ServerBecameOnline -= ServerStarted;
            if (_server.IsListening)
            {
                _server.StopListening();
            }
            _server = null;
        }

        public void ConnectClient(string hostname, int port, float playbackDelay)
        {
            _client = new TeleportClientProcessor(CreateTransport(), playbackDelay);
            _client.ConnectedToServer += ClientSideOnConnected;
            _client.DisconnectedFromServer += ClientSideOnDisconnected;
            _client.MessageArrived += ClientSideOnMessageArrived;
            ClientSidePreconnect();
            _client.Connect(hostname, port);
        }

        public void DisconnectClient()
        {
            _client.ConnectedToServer -= ClientSideOnConnected;
            _client.DisconnectedFromServer -= ClientSideOnDisconnected;
            _client.MessageArrived -= ClientSideOnMessageArrived;
            if (_client.State != TeleportClientProcessor.StateType.Disconnected)
            {
                _client.Disconnect();
            }
            _client = null;    
        }

        public void SendToAllClients(ITeleportMessage message, byte channelId = 0)
        {
            _server.SendToAll(message, channelId);
        }

        public void SendToClient(uint clientId, ITeleportMessage message, byte channelId = 0)
        {
            _server.SendToClient(clientId, message, channelId);
        }

        public void SendToClients(ITeleportMessage message, byte channelId = 0, params uint[] clientIds)
        {
            _server.SendToClients(message, channelId, clientIds);
        }

        public void SendToAllExcept(ITeleportMessage message, byte channelId = 0, params uint[] excludedClientIds)
        {
            _server.SendToAllExcept(message, channelId, excludedClientIds);
        }

        public void SendToServer(ITeleportMessage message, byte channelId = 0)
        {
            _client.SendToServer(message, channelId);
        }

        protected abstract TeleportChannelType[] GetChannelTypes();

        public abstract void ServerStarted();
        public abstract void ServerSideOnClientConnected(uint clientId, EndPoint endpoint);
        public abstract void ServerSideOnClientDisconnected(uint clientId, TeleportServerProcessor.DisconnectReasonType reason);
        public abstract void ServerSideOnMessageArrived(uint clientId, EndPoint endpoint, ITeleportMessage message);
        public abstract void ServerSidePrestart();

        public abstract void ClientSideOnConnected(uint clientId);
        public abstract void ClientSideOnDisconnected(uint clientId, TeleportClientProcessor.DisconnectReasonType reason);
        public abstract void ClientSideOnMessageArrived(ITeleportMessage message);
        public abstract void ClientSidePreconnect();

        public void RegisterServerMessage<T>() where T : ITeleportMessage, new()
        {
            RegisterMessage<T>(_server);
        }

        public void RegisterClientMessage<T>() where T : ITeleportMessage, new()
        {
            RegisterMessage<T>(_client);
        }

        public void RegisterTwoWayMessage<T>() where T : ITeleportMessage, new()
        {
            RegisterClientMessage<T>();
            RegisterServerMessage<T>();
        }

        private void RegisterMessage<T>(BaseTeleportProcessor processor) where T : ITeleportMessage, new()
        {
            processor.RegisterMessage<T>();
        }
    }
}

