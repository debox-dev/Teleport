using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using UnityEngine;
using DeBox.Teleport.Transport;


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

        public TeleportUdpTransport(params Func<BaseTeleportChannel>[] channelCreators)
        {
            _channelCreators = channelCreators;
        }

        public void InternalListen(object portObj)
        {
            EndpointCollection endpointCollection = new EndpointCollection(_endpointTimeout, _channelCreators);
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
                SendOutgoingDataAllChannelsOfAllEndpoints(socket, endpointCollection);
                data = new byte[1024];

                while (socket.Available > 0)
                {
                    receivedDataLength = socket.ReceiveFrom(data, ref endpoint);
                    ReceiveIncomingData(data, endpoint, endpointCollection);
                }
            }
        }

        private void ReceiveIncomingChannelData(BaseTeleportChannel channel, TeleportReader reader, EndPoint endpoint, EndpointCollection endpointCollection)
        {
            endpointCollection.Ping(endpoint);
            channel.Receive(reader);
        }

        private void ReceiveIncomingData(Byte[] data, EndPoint endpoint, EndpointCollection endpointCollection)
        {
            endpointCollection.Ping(endpoint);
            var stream = new MemoryStream(data);
            var reader = new TeleportReader(stream);
            var header = reader.ReadByte();
            byte channelId = (byte)(header & 0b11);
            var endpointChannel = endpointCollection.GetChannelOfEndpoint(endpoint, channelId);
            ReceiveIncomingChannelData(endpointChannel, reader, endpoint, endpointCollection);            
        }

        private void SendOutgoingDataAllChannelsOfAllEndpoints(Socket socket, EndpointCollection endpointCollection)
        {
            byte header;
            byte[] data;
            byte[] internalData;
            BaseTeleportChannel channel;
            foreach (var endpoint in endpointCollection.GetEndpoints())
            {
                var endpointChannels = endpointCollection.GetChannelsOfEndpoint(endpoint);
                for (byte channelId = 0; channelId < endpointChannels.Length; channelId++)
                {
                    header = channelId;
                    channel = endpointChannels[channelId];
                    while (channel.OutgoingMessageCount > 0)
                    {
                        internalData = channel.GetNextOutgoingData();
                        data = new byte[internalData.Length + 1];
                        data[0] = header;
                        Array.Copy(internalData, 0, data, 1, internalData.Length);
                        socket.SendTo(data, data.Length, SocketFlags.None, endpoint);
                    }
                }
            }
            
        }
       
        private void InternalClient(object clientParamsObj)
        {
            var endpointCollection = new EndpointCollection(_endpointTimeout, _channelCreators);
            
            var clientParams = (ClientParams)clientParamsObj;
            var udpClient = new UdpClient();
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            var endpoint = new IPEndPoint(IPAddress.Parse(clientParams.host), clientParams.port);
            byte[] data;
            _transportType = TransportType.Client;
            socket.Connect(endpoint);
            endpointCollection.Ping(endpoint);

            int receivedDataLength;
            _stopRequested = false;
            while (!_stopRequested)
            {
                SendOutgoingDataAllChannelsOfAllEndpoints(socket, endpointCollection);
                data = new byte[1024];

                while (socket.Available > 0)
                {
                    receivedDataLength = socket.Receive(data);
                    ReceiveIncomingData(data, endpoint, endpointCollection);
                }
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
