using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;
using DeBox.Teleport.Core;

namespace DeBox.Teleport
{
    public abstract class BaseTeleportProcessor
    {
        private const float RECEIVE_INCOMING_MESSAGES_RATE_IN_SECONDS = 0.0001f;

        public float LocalTime => Time.fixedTime;

        protected readonly TeleportUdpTransport _transport;
        private Dictionary<byte, Action<EndPoint, TeleportReader>> _incomingMessageProcessors;
        private TeleportUnityHelper _unityHelper;
        private float _nextHandleIncomingTime;
        private bool _socketStarted;

        public bool IsOnline => _transport.ThreadStarted;

        public BaseTeleportProcessor(TeleportUdpTransport transport)
        {
            _socketStarted = false;
            _transport = transport;
            _incomingMessageProcessors = new Dictionary<byte, Action<EndPoint, TeleportReader>>();
            _unityHelper = null;
        }

        protected void StartUnityHelper(string name)
        {
            var go = new UnityEngine.GameObject(name);
            go.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            _unityHelper = go.AddComponent<TeleportUnityHelper>();
            _unityHelper.Initialize(UnityUpdate);
            _nextHandleIncomingTime = 0;
        }

        protected void StopUnityHelper()
        {
            if (_unityHelper != null)
            {
                _unityHelper.Deinitialize();
            } 
        }

        protected virtual void UnityUpdate()
        {
            if (_socketStarted != _transport.ThreadStarted)
            {
                if (_transport.ThreadStarted)
                {
                    OnSocketThreadStarted();
                }
                _socketStarted = _transport.ThreadStarted;
            }
            if (LocalTime > _nextHandleIncomingTime)
            {
                _nextHandleIncomingTime = LocalTime + RECEIVE_INCOMING_MESSAGES_RATE_IN_SECONDS;
                HandleIncoming();
            }            
        }


        public void HandleIncoming()
        {
            _transport.ProcessIncoming(InternalHandleIncomingMessage);
        }

        public byte RegisterMessage<T>(Action<EndPoint, TeleportReader> processor = null) where T : ITeleportMessage, new()
        {
            var dummyMsg = new T();
            var msgTypeId = dummyMsg.MsgTypeId;
            if (processor == null)
            {
                processor = ProcessIncomingMessage<T>;
            }
            if (_incomingMessageProcessors.ContainsKey(msgTypeId))
            {
                throw new Exception("Msg type " + msgTypeId + " is already registered");
            }
            _incomingMessageProcessors[msgTypeId] = processor;
            return msgTypeId;
        }

        public void UnregisterMessage(byte msgTypeId)
        {
            _incomingMessageProcessors.Remove(msgTypeId);
        }

        public void UnregisterAllMessages()
        {
            _incomingMessageProcessors.Clear();
        }

        public void UnregisterMessage<T>() where T : ITeleportMessage, new()
        {
            var dummyMsg = new T();
            var msgTypeId = dummyMsg.MsgTypeId;
            UnregisterMessage(msgTypeId);
        }

        private void ProcessIncomingMessage<T>(EndPoint endpoint, TeleportReader reader) where T : ITeleportMessage, new()
        {
            var message = new T();
            message.Deserialize(reader);
            OnMessageArrival(endpoint, message);
        }

        protected virtual void OnSocketThreadStarted() { }

        protected virtual void OnMessageArrival(EndPoint endpoint, ITeleportMessage message) {}

        private void InternalHandleIncomingMessage(EndPoint sender, TeleportReader reader)
        {
            HandleIncomingMessage(sender, reader);
        }

        protected abstract void HandleIncomingMessage(EndPoint sender, TeleportReader reader);

        protected void Send<T>(T message, byte channelId = 0) where T : ITeleportMessage
        {
            Send(message.SerializeWithId);
        }

        protected void SendToEndpoints(Action<TeleportWriter> serializer, byte channelId = 0, params EndPoint[] endpoints)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new TeleportWriter(stream))
                {
                    serializer(writer);
                }
                _transport.Send(stream.ToArray(), channelId, endpoints);
            }
        }

        protected void Send(Action<TeleportWriter> serializer, byte channelId = 0)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new TeleportWriter(stream))
                {
                    serializer(writer);                   
                }
                _transport.Send(stream.ToArray(), channelId);
            }
        }

        protected void ProcessMessage(EndPoint endpoint, byte msgTypeId, TeleportReader reader)
        {
            Action<EndPoint, TeleportReader> processor;
            if (!_incomingMessageProcessors.TryGetValue(msgTypeId, out processor))
            {
                throw new Exception("Unknown msg type id, don't know how to process: " + msgTypeId);
            }
            processor(endpoint, reader);
        }
    }
}
