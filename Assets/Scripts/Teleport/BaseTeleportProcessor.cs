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
        private const float RECEIVE_INCOMING_MESSAGES_RATE_IN_SECONDS = 0.1f;

        public float LocalTime => Time.fixedTime;

        protected readonly TeleportUdpTransport _transport;
        private Dictionary<byte, Action<EndPoint, TeleportReader>> _incomingMessageProcessors;
        private TeleportUnityHelper _unityHelper;
        private float _nextHandleIncomingTime;

        public BaseTeleportProcessor(TeleportUdpTransport transport)
        {
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
            if (LocalTime > _nextHandleIncomingTime)
            {
                _nextHandleIncomingTime = LocalTime + RECEIVE_INCOMING_MESSAGES_RATE_IN_SECONDS;
                HandleIncoming();
            }            
        }

        protected void Send<T>(T message, byte channelId = 0) where T : ITeleportMessage
        {
            Send(message.SerializeWithId);
        }

        public void HandleIncoming()
        {
            _transport.ProcessIncoming(InternalHandleIncomingMessage);
        }

        public void RegisterMessage<T>(Action<EndPoint, TeleportReader> processor = null) where T : ITeleportMessage, new()
        {
            var dummyMsg = new T();
            var msgTypeId = dummyMsg.MsgTypeId;
            if (processor == null)
            {
                processor = ProcessIncomingMessage<T>;
            }
            _incomingMessageProcessors[msgTypeId] = processor;
        }

        private void ProcessIncomingMessage<T>(EndPoint endpoint, TeleportReader reader) where T : ITeleportMessage, new()
        {
            var message = new T();
            message.Deserialize(reader);
            OnMessageArrival(endpoint, message);
        }

        protected virtual void OnMessageArrival(EndPoint endpoint, ITeleportMessage message) {}

        private void InternalHandleIncomingMessage(EndPoint sender, TeleportReader reader)
        {
            HandleIncomingMessage(sender, reader);
        }

        protected abstract void HandleIncomingMessage(EndPoint sender, TeleportReader reader);

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
