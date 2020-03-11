using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using UnityEngine;
using DeBox.Teleport.Utils;

namespace DeBox.Teleport.Core
{
    public class TeleportUdpTransport
    {
        public enum TransportType
        {
            None,
            Client,
            Server,
        }

        private struct ClientParams
        {
            public IPAddress address;
            public int port;
        }

        private const int THREAD_SLEEP_DURATION_IN_MS = 10;

        private Thread _thread;
        private bool _stopRequested;
        private Func<BaseTeleportChannel>[] _channelCreators;
        private Func<ITeleportPacketBuffer> _bufferCreator;
        private readonly double _endpointTimeout = 30;
        private EndpointCollection _endpointCollection = null;

        public bool ThreadStarted { get; private set; } = false;
        public TransportType Type { get; private set; } = TransportType.None;

        public TeleportUdpTransport(Func<ITeleportPacketBuffer> bufferCreator, params Func<BaseTeleportChannel>[] channelCreators)
        {
            _channelCreators = channelCreators;
            _bufferCreator = bufferCreator;
        }

        public void Send(Action<TeleportWriter> serializer, byte channelId = 0)
        {
            byte[] data;
            using (var stream = new MemoryStream())
            {
                using (var writer = new TeleportWriter(stream))
                {
                    serializer(writer);
                    data = stream.ToArray();
                }
            }
            foreach (var ep in _endpointCollection.GetEndpoints())
            {
                var channel = _endpointCollection.GetChannelOfEndpoint(ep, channelId);
                channel.Send(channel.PrepareToSend(data));
            }
        }

        public void ProcessIncoming(Action<EndPoint, TeleportReader> deserializer)
        {
            
            foreach (var endpoint in _endpointCollection.GetEndpoints())
            {                
                var endpointChannels = _endpointCollection.GetChannelsOfEndpoint(endpoint);
                
                for (byte channelId = 0; channelId < endpointChannels.Length; channelId++)
                {                    
                    var channel = _endpointCollection.GetChannelOfEndpoint(endpoint, channelId);
                    
                    while (channel.IncomingMessageCount > 0)
                    {                        
                        var next = channel.GetNextIncomingData();
                        using (var stream = new MemoryStream(next))
                        {
                            using (var reader = new TeleportReader(stream))
                            {

                                deserializer(endpoint, reader);
                            }                                
                        }                            
                    }
                }
            }
        }

        public void Send(byte[] data, byte channelId = 0)
        {            
            foreach (var ep in _endpointCollection.GetEndpoints())
            {
                var channel = _endpointCollection.GetChannelOfEndpoint(ep, channelId);
                channel.Send(channel.PrepareToSend(data));
            }
        }

        public void Send(byte[] data, byte channelId = 0, params EndPoint[] endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                var channel = _endpointCollection.GetChannelOfEndpoint(endpoint, channelId);
                channel.Send(channel.PrepareToSend(data));
            }
        }

        public void InternalListen(object portObj)
        {
            _endpointCollection = new EndpointCollection(_endpointTimeout, _channelCreators, _bufferCreator);
            Type = TransportType.Server;
            var port = (int)portObj;

            byte[] data = new byte[8096];
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            socket.Bind(ip);

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint endpoint = (EndPoint)(sender);
            ITeleportPacketBuffer packetBuffer;
            byte[] packetData = new byte[8096];
            int packetLength;
            byte channelId;
            int receivedDataLength;
            _stopRequested = false;
            ThreadStarted = true;
            while (!_stopRequested)
            {
                SendOutgoingDataAllChannelsOfAllEndpoints(socket, _endpointCollection);

                Thread.Sleep(THREAD_SLEEP_DURATION_IN_MS);
                while (socket.Available > 0)
                {
                    receivedDataLength = socket.ReceiveFrom(data, ref endpoint);
                    _endpointCollection.Ping(endpoint);
                    packetBuffer = _endpointCollection.GetBufferOfEndpoint(endpoint);
                    packetBuffer.ReceiveRawData(data, receivedDataLength);
                    do
                    {
                        packetLength = packetBuffer.TryParseNextIncomingPacket(packetData, out channelId);
                        if (packetLength > 0)
                        {
                            ReceiveIncomingData(channelId, packetData, packetLength, endpoint, _endpointCollection);
                        }
                    }
                    while (packetLength > 0);
                }
                

                Upkeep();
            }
        }

        private void Upkeep()
        {
            foreach (var ep in _endpointCollection.GetEndpoints())
            {
                foreach (var channel in _endpointCollection.GetChannelsOfEndpoint(ep))
                {
                    channel.Upkeep();
                }
            }
        }
        
        private void ReceiveIncomingChannelData(BaseTeleportChannel channel, byte[] data, int startIndex, int length, EndPoint endpoint, EndpointCollection endpointCollection)
        {
            endpointCollection.Ping(endpoint);
            channel.Receive(data, startIndex, length);
        }


        private void ReceiveIncomingData(byte channelId, byte[] data, int receivedDataLength, EndPoint endpoint, EndpointCollection endpointCollection)
        {
            var endpointChannel = endpointCollection.GetChannelOfEndpoint(endpoint, channelId);
            ReceiveIncomingChannelData(endpointChannel, data, 0, receivedDataLength, endpoint, endpointCollection);            
        }

        private void SendOutgoingDataAllChannelsOfAllEndpoints(Socket socket, EndpointCollection endpointCollection)
        {
            byte[] data;
            BaseTeleportChannel channel;
            ITeleportPacketBuffer packetBuffer;
            foreach (var endpoint in endpointCollection.GetEndpoints())
            {
                packetBuffer = _endpointCollection.GetBufferOfEndpoint(endpoint);
                var endpointChannels = endpointCollection.GetChannelsOfEndpoint(endpoint);
                for (byte channelId = 0; channelId < endpointChannels.Length; channelId++)
                {
                    channel = endpointChannels[channelId];
                    while (channel.OutgoingMessageCount > 0)
                    {
                        data = channel.GetNextOutgoingData();                        
                        data = packetBuffer.CreatePacket(channelId, data, 0, (ushort)data.Length);
                        ClientSocketSend(socket, data, data.Length, SocketFlags.None, endpoint);
                    }
                }
            }
        }

        private void ClientSocketSend(Socket socket, byte[] data, int dataLength, SocketFlags socketFlags, EndPoint endpoint)
        {
            if (Type == TransportType.Client)
            {
                socket.Send(data, data.Length, socketFlags);
            }
            else
            {
                socket.SendTo(data, data.Length, socketFlags, endpoint);
            }
        }

        private void InternalClient(object clientParamsObj)
        {
            _endpointCollection = new EndpointCollection(_endpointTimeout, _channelCreators, _bufferCreator);
            
            var clientParams = (ClientParams)clientParamsObj;
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            var endpoint = new IPEndPoint(clientParams.address, clientParams.port);
            byte[] data = new byte[8096];
            byte[] packetData = new byte[8096];
            int packetLength;
            ITeleportPacketBuffer packetBuffer;
            byte channelId;
            Type = TransportType.Client;
            socket.Connect(endpoint);
            _endpointCollection.Ping(endpoint);

            int receivedDataLength;
            _stopRequested = false;
            ThreadStarted = true;
            while (!_stopRequested)
            {
                SendOutgoingDataAllChannelsOfAllEndpoints(socket, _endpointCollection);
               
                Thread.Sleep(THREAD_SLEEP_DURATION_IN_MS);
                while (socket.Available > 0)
                {
                    receivedDataLength = socket.Receive(data);
                    _endpointCollection.Ping(endpoint);
                    packetBuffer = _endpointCollection.GetBufferOfEndpoint(endpoint);
                    packetBuffer.ReceiveRawData(data, receivedDataLength);
                    do
                    {
                        packetLength = packetBuffer.TryParseNextIncomingPacket(packetData, out channelId);
                        if (packetLength > 0)
                        {
                            ReceiveIncomingData(channelId, packetData, packetLength, endpoint, _endpointCollection);
                        }
                    }
                    while (packetLength > 0);
                }

                Upkeep();
            }
        }


        public void StartClient(string host, int port)
        {
            if (_thread != null)
            {
                throw new Exception("Thread already active");
            }
            ThreadStarted = false;
            IPAddress address;
            if (!DnsIpUtils.TryGetIp(host, out address))
            {
                return;
            }    
            Debug.Log("Starting client");
            _stopRequested = false;
            _thread = new Thread(InternalClient);
            
            _thread.Start(new ClientParams() { address = address, port = port });
        }

        public void StopClient()
        {
            Debug.Log("Stopping client");
            ThreadStarted = false;
            _stopRequested = true;            
            _thread.Join();            
            _thread = null;
            Debug.Log("Stopped client");
        }

        public void StartListener(int port)
        {
            if (_thread != null)
            {
                throw new Exception("Thread already active");
            }
            Debug.Log("Starting server");
            ThreadStarted = false;
            _stopRequested = false;
            _thread = new Thread(new ParameterizedThreadStart(InternalListen));
            _thread.Start(port);
        }

        public void StopListener()
        {
            Debug.Log("Stopping server");
            ThreadStarted = false;
            _stopRequested = true;
            _thread.Join();
            _thread = null;
            Debug.Log("Stopped server");
        }

    }

}
