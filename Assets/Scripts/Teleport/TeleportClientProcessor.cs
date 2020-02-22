using System;
using System.Net;
using UnityEngine;
using DeBox.Teleport.Core;

namespace DeBox.Teleport
{
    public class TeleportClientProcessor : BaseTeleportProcessor
    {
        public enum DisconnectReasonType
        {
            ClientSideDisconnectRequested,
            ServerTimeout,
            ServerRequestedDisconnect,
        }

        private const float TIME_SYNC_MESSAGE_RATE_IN_SECONDS = 10;
        private const float TIME_SYNC_MAX_TIME_DRIFT_BEFORE_HARD_SET_IN_SECONDS = 1;
        private const float TIME_SYNC_MAX_TIME_DRIFT_MAGNITUDE = 0.1f;        
        private byte _authKey;
        private uint _clientId;
        private bool _isAuthenticated;
        private TimedMessageQueue _timedMessageQueue;
        private float _nextTimeSyncTime;
        private float _timedMessagePlaybackDelay;

        public float ServerTime { get; private set; }

        public TeleportClientProcessor(TeleportUdpTransport transport, float timedMessagePlaybackDelay = 0.08f) : base(transport)
        {
            _isAuthenticated = false;
            _timedMessageQueue = new TimedMessageQueue();
            ServerTime = -1;
            _nextTimeSyncTime = -1;
            _timedMessagePlaybackDelay = timedMessagePlaybackDelay;
        }

        public void Connect(string host, int port)
        {
            StartUnityHelper("Client");
            _transport.StartClient(host, port);
        }

        protected override void UnityUpdate()
        {
            base.UnityUpdate();
            if (_isAuthenticated && LocalTime > _nextTimeSyncTime)
            {
                _nextTimeSyncTime = LocalTime + TIME_SYNC_MESSAGE_RATE_IN_SECONDS;
                SendTimesyncRequest();
            }
            if (ServerTime > 0)
            {
                ServerTime += Time.fixedDeltaTime;
                PlayTimedMessages(ServerTime - _timedMessagePlaybackDelay);
            }            
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

        private void SendTimesyncRequest()
        {
            Send(SerializeTimeSyncRequest);
        }

        private void SerializeTimeSyncRequest(TeleportWriter writer)
        {
            writer.Write(TeleportMsgTypeIds.TimeSync);
            writer.Write(LocalTime);
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
                case TeleportMsgTypeIds.TimeSync:
                    HandleTimeSyncReponse(reader);
                    break;
                default:
                    ProcessMessage(sender, msgTypeId, reader);
                    break;
            }
        }

        private void HandleTimeSyncReponse(TeleportReader reader)
        {
            var clientTimeOnRequestSent = reader.ReadSingle();
            var serverTimeOnRequestArrival = reader.ReadSingle();
            var clientTimeOnResponseArrival = LocalTime;
            var totalRoundtripDuration = clientTimeOnResponseArrival - clientTimeOnRequestSent;
            var estimatedReturnTripDuration = totalRoundtripDuration * 0.5f;
            var estimatedServerTime = serverTimeOnRequestArrival + estimatedReturnTripDuration;
            var delta = estimatedServerTime - ServerTime;
            var absDelta = Math.Abs(delta);
            var deltaSign = Math.Sign(delta);
            if (absDelta > TIME_SYNC_MAX_TIME_DRIFT_BEFORE_HARD_SET_IN_SECONDS)
            {
                ServerTime = estimatedServerTime;
            }
            else
            {
                ServerTime += Math.Max(absDelta, TIME_SYNC_MAX_TIME_DRIFT_MAGNITUDE) * deltaSign;
            }
        }
    }
}
