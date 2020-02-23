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

        [SerializeField]
        private TeleportChannelType[] _channelTypes = new[] { TeleportChannelType.SequencedReliable };

        protected TeleportClientProcessor _client;
        protected TeleportServerProcessor _server;

        private TeleportUdpTransport CreateTransport()
        {
            return new TeleportUdpTransport(GetChannelCreators());
        }
        private Func<BaseTeleportChannel>[] GetChannelCreators()
        {
            var channelFuncs = new Func<BaseTeleportChannel>[_channelTypes.Length];
            for (int i = 0; i < _channelTypes.Length; i++)
            {
                Func<BaseTeleportChannel> channelFunc;
                switch (_channelTypes[i])
                {
                    case TeleportChannelType.Unreliable:
                        channelFunc = () => new SimpleTeleportChannel();
                        break;
                    case TeleportChannelType.SequencedReliable:
                        channelFunc = () => new SequencedTeleportChannel(new SimpleTeleportChannel());
                        break;
                    default:
                        throw new Exception("Don't know how to create channel type: " + _channelTypes[i]);
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
            _server.Listen(port);
        }

        public void StopServer() {
            _server.ClientConnected -= ServerSideOnClientConnected;
            _server.ClientDisconnected -= ServerSideOnClientDisconnected;
            _server.MessageArrived -= ServerSideOnMessageArrived;
            if (_server.IsListening)
            {
                _server.StopListening();
            }
            _server = null;
        }

        public void ConnectClient(string hostname, int port)
        {
            _client = new TeleportClientProcessor(CreateTransport());
            _client.ConnectedToServer += ClientSideOnConnected;
            _client.DisconnectedFromServer += ClientSideOnDisconnected;
            _client.MessageArrived += ClientSideOnMessageArrived;
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

        public abstract void ServerSideOnClientConnected(uint clientId, EndPoint endpoint);
        public abstract void ServerSideOnClientDisconnected(uint clientId, TeleportServerProcessor.DisconnectReasonType reason);
        public abstract void ServerSideOnMessageArrived(uint clientId, EndPoint endpoint, ITeleportMessage message);

        public abstract void ClientSideOnConnected(uint clientId);
        public abstract void ClientSideOnDisconnected(uint clientId, TeleportClientProcessor.DisconnectReasonType reason);
        public abstract void ClientSideOnMessageArrived(ITeleportMessage message);
        

        public void RegisterServerMessage<T>() where T : ITeleportMessage, new()
        {
            RegisterMessage<T>(_server);
        }

        public void RegisterClientMessage<T>() where T : ITeleportMessage, new()
        {
            RegisterMessage<T>(_client);
        }

        private void RegisterMessage<T>(BaseTeleportProcessor processor) where T : ITeleportMessage, new()
        {
            processor.RegisterMessage<T>();
        }
    }
}

