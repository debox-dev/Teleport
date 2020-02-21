using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using UnityEngine;
using DeBox.Teleport.Transport;
using DeBox.Teleport.Debugging;


namespace DeBox.Teleport
{


    public class TeleportUdpTransport
    {
        private enum TransportType
        {
            None,
            Client,
            Server,
        }

        private struct ClientParams
        {
            public string host;
            public int port;
        }

        private Thread _thread;
        private bool _stopRequested;
        private TransportType _transportType = TransportType.None;
        private Func<BaseTeleportChannel>[] _channelCreators;
        private readonly double _endpointTimeout = 30;
        private EndpointCollection _endpointCollection = null;

        public TeleportUdpTransport(params Func<BaseTeleportChannel>[] channelCreators)
        {
            _channelCreators = channelCreators;
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

        public void ProcessIncoming(Action<TeleportReader> deserializer)
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
                                deserializer(reader);
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
                channel.Send(data);
            }
        }

        public void InternalListen(object portObj)
        {
            _endpointCollection = new EndpointCollection(_endpointTimeout, _channelCreators);
            _transportType = TransportType.Server;
            var port = (int)portObj;

            byte[] data = new byte[1024];
            IPEndPoint ip = new IPEndPoint(IPAddress.Any, port);

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Bind(ip);

            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint endpoint = (EndPoint)(sender);
            int receivedDataLength;
            _stopRequested = false;
            while (!_stopRequested)
            {
                SendOutgoingDataAllChannelsOfAllEndpoints(socket, _endpointCollection);
                data = new byte[1024];

                while (socket.Available > 0)
                {
                    receivedDataLength = socket.ReceiveFrom(data, ref endpoint);
                    ReceiveIncomingData(data, receivedDataLength, endpoint, _endpointCollection);
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


        private void ReceiveIncomingData(byte[] data, int receivedDataLength, EndPoint endpoint, EndpointCollection endpointCollection)
        {
            byte[] header = new byte[1];
            endpointCollection.Ping(endpoint);

      

            header[0] = data[0];

            var strippedData = new byte[receivedDataLength - header.Length];
            Array.Copy(data, header.Length, strippedData, 0, strippedData.Length);
            byte channelId = (byte)(header[0] & 0b11);
            var checksumFromData = (header[0] >> 2) & 0b11;
            var calculatedChecksum = Checksum(data, header.Length, data.Length - header.Length);
            if (checksumFromData != calculatedChecksum)
            {
                Debug.LogError(_transportType + " Checksum mismatch, got: " + checksumFromData + " expected: " + calculatedChecksum + " " + TeleportDebugUtils.DebugString(data));
                return;
            }
            var endpointChannel = endpointCollection.GetChannelOfEndpoint(endpoint, channelId);
            ReceiveIncomingChannelData(endpointChannel, strippedData, 0, strippedData.Length, endpoint, endpointCollection);            
        }

        private void SendOutgoingDataAllChannelsOfAllEndpoints(Socket socket, EndpointCollection endpointCollection)
        {
            byte[] header = new byte[1];
            byte[] data;
            byte[] internalData;
            BaseTeleportChannel channel;
            foreach (var endpoint in endpointCollection.GetEndpoints())
            {
                var endpointChannels = endpointCollection.GetChannelsOfEndpoint(endpoint);
                for (byte channelId = 0; channelId < endpointChannels.Length; channelId++)
                {
                    
                    channel = endpointChannels[channelId];
                    while (channel.OutgoingMessageCount > 0)
                    {
                        internalData = channel.GetNextOutgoingData();
                        header[0] = channelId;
                        header[0] += (byte)(Checksum(internalData, 0, internalData.Length, channelId) << 2);
                        data = new byte[internalData.Length + header.Length];
                        data[0] = header[0];
                        Array.Copy(internalData, 0, data, header.Length, internalData.Length);
                        socket.SendTo(data, data.Length, SocketFlags.None, endpoint);
                    }
                }
            }
            
        }

        private byte Checksum(byte[] data, long startOffset, long amount, params byte[] additional)
        {
            byte checksumCalculated = 0;
            unchecked
            {
                for (long i = startOffset; i < amount; i++)
                {
                    checksumCalculated += data[i];
                }
                for (long i = 0; i < additional.Length; i++)
                {
                    checksumCalculated += additional[i];
                }
            }
            return (byte)(checksumCalculated % 4);
        }

        private void InternalClient(object clientParamsObj)
        {
            _endpointCollection = new EndpointCollection(_endpointTimeout, _channelCreators);
            
            var clientParams = (ClientParams)clientParamsObj;
            var udpClient = new UdpClient();
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var endpoint = new IPEndPoint(IPAddress.Parse(clientParams.host), clientParams.port);
            byte[] data;
            _transportType = TransportType.Client;
            socket.Connect(endpoint);
            _endpointCollection.Ping(endpoint);

            int receivedDataLength;
            _stopRequested = false;
            while (!_stopRequested)
            {
                SendOutgoingDataAllChannelsOfAllEndpoints(socket, _endpointCollection);
                data = new byte[1024];

                while (socket.Available > 0)
                {
                    receivedDataLength = socket.Receive(data);
                    ReceiveIncomingData(data, receivedDataLength, endpoint, _endpointCollection);
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
            Debug.Log("Starting client");
            _stopRequested = false;
            _thread = new Thread(InternalClient);
            _thread.Start(new ClientParams() { host = host, port = port });
        }

        public void StopClient()
        {
            Debug.Log("Stopping client");
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
            _stopRequested = false;
            _thread = new Thread(new ParameterizedThreadStart(InternalListen));
            _thread.Start(port);
        }

        public void StopListener()
        {
            Debug.Log("Stopping");
            _stopRequested = true;
            _thread.Join();
            _thread = null;
            Debug.Log("Stopped server");
        }

    }

}
