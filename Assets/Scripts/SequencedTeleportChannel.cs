using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DeBox.Teleport.Debugging;

namespace DeBox.Teleport.Transport
{
    public class SequencedTeleportChannel : BaseTeleportProxyChannel
    {
        private class OutboxItem
        {
            public byte[] data;
            public long lastSendTime;
        }

        private ushort _outgoingSequence;
        private int _lastReceiveIndex;
        private int _lastProcessedReceiveIndex;
        private Dictionary<ushort, TeleportReader> _inbox;
        private Dictionary<ushort, OutboxItem> _outbox;
        private bool _sendAcks;

        public SequencedTeleportChannel() : this(new SimpleTeleportChannel()) { }

        public SequencedTeleportChannel(BaseTeleportChannel internalChannel) : this(internalChannel, true)
        {            
        }

        public SequencedTeleportChannel(BaseTeleportChannel internalChannel, bool sendAcks) : base(internalChannel)
        {
            _sendAcks = sendAcks;
            _outgoingSequence = 0;
            _lastReceiveIndex = -1;
            _lastProcessedReceiveIndex = -1;
            _inbox = new Dictionary<ushort, TeleportReader>();
            _outbox = new Dictionary<ushort, OutboxItem>();
        }

        public override void Receive(TeleportReader reader)
        {
            //Debug.LogError("RECV: " + TeleportDebugUtils.DebugString(reader));
            var sequenceNumber = reader.ReadUInt16();
            if (_sendAcks && sequenceNumber == ushort.MaxValue)
            {
                sequenceNumber = reader.ReadUInt16();
                _outbox.Remove(sequenceNumber);
                return;
            }
            if (_inbox.ContainsKey(sequenceNumber))
            {
                //Debug.LogWarning("Got same sequence twice: " + sequenceNumber);
            }
            _inbox[sequenceNumber] = reader;
            if (sequenceNumber > _lastReceiveIndex)
            {
                _lastReceiveIndex = sequenceNumber;
            }
            if (_sendAcks)
            {
                var ackData = new byte[4];
                Array.Copy(BitConverter.GetBytes(ushort.MaxValue), ackData, 2);
                Array.Copy(BitConverter.GetBytes(sequenceNumber), 0, ackData, 2, 2);
                InternalChannel.Send(ackData);
                ProcessOutbox();
            }            
            ProcessInbox();
        }

        public override void Send(byte[] data)
        {
            byte[] sequenceBytes = BitConverter.GetBytes(_outgoingSequence);
            var newData = new byte[data.Length + sequenceBytes.Length];
            Array.Copy(sequenceBytes, 0, newData, 0, sequenceBytes.Length);
            Array.Copy(data, 0, newData, sequenceBytes.Length, data.Length);
            if (_sendAcks)
            {
                Debug.LogError("SEND: " + TeleportDebugUtils.DebugString(newData));
                _outbox[_outgoingSequence] = new OutboxItem() { data = newData, lastSendTime = DateTime.UtcNow.Ticks };
                ProcessOutbox();
            }
            _outgoingSequence++;
            InternalChannel.Send(newData);

        }

        private void ProcessOutbox()
        {
            if (_outbox.Count == 0)
            {
                return;
            }
            ushort seqId;
            OutboxItem outboxItem;
            foreach (var p in _outbox)
            {
                seqId = p.Key;
                outboxItem = p.Value;
                if (outboxItem.lastSendTime < DateTime.UtcNow.Ticks - 10000)
                {
                    outboxItem.lastSendTime = DateTime.UtcNow.Ticks;
                    //InternalChannel.Send(outboxItem.data);
                }
            }
        }

        private void ProcessInbox()
        {
            if (_inbox.Count == 0)
            {
                return;
            }
            TeleportReader reader;
            ushort nextIndex;
            while (_lastReceiveIndex > _lastProcessedReceiveIndex)
            {
                nextIndex = (ushort)(_lastProcessedReceiveIndex + 1);
                if (!_inbox.TryGetValue(nextIndex, out reader))
                {
                    
                    break;
                }
                
                InternalChannel.Receive(reader);
                _lastProcessedReceiveIndex = nextIndex;
            }
        }


    }

}
