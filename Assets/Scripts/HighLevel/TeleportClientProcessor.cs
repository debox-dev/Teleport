using System.Net;
using DeBox.Teleport.Transport;

namespace DeBox.Teleport.HighLevel
{
    public class TeleportClientProcessor : BaseTeleportProcessor
    {
        public enum DisconnectReasonType
        {
            ClientSideDisconnectRequested,
            ServerTimeout,
            ServerRequestedDisconnect,
        }

        private byte _authKey;
        private uint _clientId;
        private bool _isAuthenticated;
        private TimedMessageQueue _timedMessageQueue;

        public TeleportClientProcessor(TeleportUdpTransport transport) : base(transport)
        {
            _timedMessageQueue = new TimedMessageQueue();
        }

        public void Connect(string host, int port)
        {
            StartUnityHelper("Client");
            //_transport.StartClient(host, port);
        }

        protected override void UnityUpdate()
        {
            base.UnityUpdate();
            PlayTimedMessages(0);
        }

        public void Disconnect()
        {
            StopUnityHelper();
            Send((w) => { w.Write(TeleportMsgTypeIds.Disconnect); });
            Disconnect(DisconnectReasonType.ClientSideDisconnectRequested);
        }

        public void PlayTimedMessages(float untilTimestamp)
        {
            _timedMessageQueue.ProcessUntil(untilTimestamp);
        }

        protected void Disconnect(DisconnectReasonType reason)
        {
            _transport.StopClient();
            OnDisconnect(reason);
        }

        public void SendToServer<T>(T message, byte channelId = 0) where T : ITeleportMessage
        {            
            if (!_isAuthenticated)
            {
                SendHandshake();
            }
            message.PreSendClient();
            Send(message, channelId);
            message.PostSendClient();
        }

        protected virtual void OnConnectionEstablisehd(uint clientId) { }
        protected virtual void OnDisconnect(DisconnectReasonType reason) {}

        protected sealed override void OnMessageArrival(EndPoint endpoint, ITeleportMessage message)
        {
            if (!_isAuthenticated)
            {
                UnityEngine.Debug.LogWarning("Client got a message while not yet authenticated!");
                return;
            }
            OnMessageArrival(message);
            message.OnArrivalToClient();
            var timedMessage = message as TimedTeleportMessage;
            if (timedMessage != null)
            {
                _timedMessageQueue.AcceptMessage(timedMessage);
            }
        }

        protected virtual void OnMessageArrival(ITeleportMessage message)
        {

        }

        private void ProcessHandshake(TeleportReader reader)
        {
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
                    OnConnectionEstablisehd(_clientId);
                    break;
                case TeleportMsgTypeIds.Disconnect:
                    Disconnect(DisconnectReasonType.ServerRequestedDisconnect);
                    break;
                default:
                    ProcessMessage(sender, msgTypeId, reader);
                    break;
            }
        }
    }
}
