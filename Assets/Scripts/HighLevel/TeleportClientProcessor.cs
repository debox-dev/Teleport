using System.Net;
using DeBox.Teleport.Transport;

namespace DeBox.Teleport.HighLevel
{
    public class TeleportClientProcessor : BaseTeleportProcessor
    {
        private byte _authKey;
        private uint _clientId;
        private bool _isAuthenticated;

        public TeleportClientProcessor(TeleportUdpTransport transport) : base(transport)
        { }

        public void Connect(string host, int port)
        {
            _transport.StartClient(host, port);
        }

        public void Disconnect()
        {
            _transport.StopClient();
        }

        public override void Send<T>(T message, byte channelId = 0)
        {

            if (!_isAuthenticated)
            {
                SendHandshake();
            }
            base.Send(message, channelId);
        }

        private void ProcessHandshake(TeleportReader reader)
        {
            UnityEngine.Debug.Log("Client got handshake ack");
            _authKey = reader.ReadByte();
            _clientId = reader.ReadUInt32();
            _isAuthenticated = true;           
        }

        private void SendHandshake()
        {            
            if (!_isAuthenticated)
            {
                Send((w) =>
                {
                    w.Write(TeleportMsgTypeIds.Handshake);
                    w.Write(0);
                });
                return;
            }
            Send((w) =>
            {
                byte header = 1;
                header |= (byte)(_authKey << 1);
                w.Write(TeleportMsgTypeIds.Handshake);
                w.Write(header);
                w.Write(_clientId);
            });
        }

        protected override void HandleIncomingMessage(EndPoint sender, TeleportReader reader)
        {
            var msgTypeId = reader.ReadByte();
            switch (msgTypeId)
            {
                case TeleportMsgTypeIds.Handshake:
                    ProcessHandshake(reader);
                    break;
                default:
                    ProcessMessage(sender, msgTypeId, reader);
                    break;
            }
        }
    }
}
