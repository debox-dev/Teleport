﻿using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using DeBox.Teleport.Transport;

namespace DeBox.Teleport.HighLevel
{
    public abstract class BaseTeleportProcessor
    {
        protected readonly TeleportUdpTransport _transport;
        private Dictionary<byte, Action<EndPoint, TeleportReader>> _incomingMessageProcessors;

        public BaseTeleportProcessor(TeleportUdpTransport transport)
        {
            _transport = transport;
            _incomingMessageProcessors = new Dictionary<byte, Action<EndPoint, TeleportReader>>();
        }

        public virtual void Send<T>(T message, byte channelId = 0) where T : BaseTeleportMessage
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
            message.OnArrival();
        }

        private void InternalHandleIncomingMessage(EndPoint sender, TeleportReader reader)
        {
            HandleIncomingMessage(sender, reader);
        }

        protected abstract void HandleIncomingMessage(EndPoint sender, TeleportReader reader);

        protected void Send(Action<TeleportWriter> serializer, byte channelId = 0, params EndPoint[] endpoints)
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
                throw new Exception("Unknown msg type id: " + msgTypeId);
            }

            
            processor(endpoint, reader);
        }
    }
}
