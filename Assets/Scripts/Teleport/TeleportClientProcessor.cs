using System;
using System.Net;
using UnityEngine;
using DeBox.Teleport.Core;

namespace DeBox.Teleport
{
    public class TeleportClientProcessor : BaseTeleportProcessor
    {
        public enum StateType
        {
            Disconnected,
            Connecting,
            Connected,
        }

        public enum DisconnectReasonType
        {
            ClientSideDisconnectRequested,
            ServerTimeout,
            ServerRequestedDisconnect,
        }

        public event Action<uint> ConnectedToServer;
        public event Action<uint, DisconnectReasonType> DisconnectedFromServer;
        public event Action<ITeleportMessage> MessageArrived;

        private const float TIME_SYNC_MESSAGE_RATE_IN_SECONDS = 1;
        private const float TIME_SYNC_MAX_TIME_DRIFT_BEFORE_HARD_SET_IN_SECONDS = 1;
        private const float TIME_SYNC_MAX_TIME_DRIFT_MAGNITUDE = 0.1f;        
        private byte _authKey;
        private uint _clientId;
        private bool _isAuthenticated;
        private TimedMessageQueue _timedMessageQueue;
        private float _nextTimeSyncTime;
        private float _timedMessagePlaybackDelay;
        private float _handshakeTime;
        

        public StateType State { get; private set; }
        public float ServerTime { get; private set; }
        public bool IsTimeSynchronized { get; private set; }

        public TeleportClientProcessor(TeleportUdpTransport transport, float timedMessagePlaybackDelay = 0.08f) : base(transport)
        {
            State = StateType.Disconnected;
            _timedMessageQueue = new TimedMessageQueue();
            _timedMessagePlaybackDelay = timedMessagePlaybackDelay;            
        }

        public void Connect(string host, int port)
        {
            _isAuthenticated = false;
            IsTimeSynchronized = false;
            ServerTime = -1;
            _nextTimeSyncTime = -1;
            StartUnityHelper("Client");
            State = StateType.Connecting;
            _transport.StartClient(host, port);
            _handshakeTime = LocalTime + 0.5f;
        }

        protected override void UnityUpdate()
        {
            base.UnityUpdate();       
            if (!_isAuthenticated && _handshakeTime < LocalTime)
            {
                SendHandshake();
                _handshakeTime = LocalTime + 5;
            }
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
            _timedMessageQueue.Clear();
            _transport.StopClient();
            State = StateType.Disconnected;
            OnDisconnect(reason);
        }

        public void SendToServer<T>(T message, byte channelId = 0) where T : ITeleportMessage
        {            
            if (!_isAuthenticated)
            {
                SendHandshake();
            }
            StampMessageIfTimed(message);
            message.PreSendClient();
            Send(message, channelId);
            message.PostSendClient();
        }

        private void StampMessageIfTimed<T>(T message) where T : ITeleportMessage
        {
            var timedMessage = message as ITeleportTimedMessage;
            if (timedMessage != null)
            {
                timedMessage.SetTimestamp(ServerTime);
            }
        }

        protected virtual void OnConnectionEstablisehd(uint clientId)
        {
            ConnectedToServer?.Invoke(clientId);
        }

        protected virtual void OnDisconnect(DisconnectReasonType reason)
        {
            DisconnectedFromServer?.Invoke(_clientId, reason);
        }

        protected virtual void OnMessageArrival(ITeleportMessage message)
        {
            MessageArrived?.Invoke(message);
        }

        protected sealed override void OnMessageArrival(EndPoint endpoint, ITeleportMessage message)
        {
            if (!_isAuthenticated)
            {
                UnityEngine.Debug.LogWarning("Client got a message while not yet authenticated!");
                return;
            }
            OnMessageArrival(message);
            message.OnArrivalToClient();
            var timedMessage = message as ITeleportTimedMessage;
            if (timedMessage != null)
            {
                _timedMessageQueue.AcceptMessage(timedMessage);
            }
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

        private void SendClientReady()
        {
            Send(w =>
            {
                w.Write(TeleportMsgTypeIds.ClientReady);
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
            bool shouldNotifyReady = !IsTimeSynchronized;
            var clientTimeOnRequestSent = reader.ReadSingle();
            var serverTimeOnRequestArrival = reader.ReadSingle();
            var clientTimeOnResponseArrival = LocalTime;
            var totalRoundtripDuration = clientTimeOnResponseArrival - clientTimeOnRequestSent;
            var estimatedReturnTripDuration = totalRoundtripDuration * 0.5f;
            var estimatedServerTime = serverTimeOnRequestArrival + estimatedReturnTripDuration;
            var delta = estimatedServerTime - ServerTime;
            var absDelta = Math.Abs(delta);
            var deltaSign = Math.Sign(delta);
            IsTimeSynchronized = true;
            State = StateType.Connected;
            if (absDelta > TIME_SYNC_MAX_TIME_DRIFT_BEFORE_HARD_SET_IN_SECONDS)
            {
                ServerTime = estimatedServerTime;
            }
            else
            {
                ServerTime += Math.Max(absDelta, TIME_SYNC_MAX_TIME_DRIFT_MAGNITUDE) * deltaSign;
            }
            if (shouldNotifyReady)
            {
                SendClientReady();
            }
        }
    }
}
