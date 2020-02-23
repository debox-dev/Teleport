using System.Net;
using UnityEngine;

namespace DeBox.Teleport.Unity
{
    public class TeleportManager : BaseTeleportManager
    {
        [SerializeField] private string _clientHostname = "localhost";
        [SerializeField] private int _port = 5000;
        [SerializeField] private TeleportChannelType[] _channelTypes = new[] { TeleportChannelType.SequencedReliable };

        public void StartServer() { StartServer(_port); }

        public void ConnectClient() { ConnectClient(_clientHostname, _port); }

        protected override TeleportChannelType[] GetChannelTypes() { return _channelTypes; }

        public override void ClientSideOnConnected(uint clientId) {}

        public override void ClientSideOnDisconnected(uint clientId, TeleportClientProcessor.DisconnectReasonType reason) {}

        public override void ClientSideOnMessageArrived(ITeleportMessage message) {}

        public override void ServerSideOnClientConnected(uint clientId, EndPoint endpoint) {}

        public override void ServerSideOnClientDisconnected(uint clientId, TeleportServerProcessor.DisconnectReasonType reason) {}

        public override void ServerSideOnMessageArrived(uint clientId, EndPoint endpoint, ITeleportMessage message) {}

        public override void ServerStarted() {}
    }
}
