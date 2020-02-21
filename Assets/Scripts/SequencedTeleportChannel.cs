using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DeBox.Teleport.Debugging;
using DeBox.Collections;

namespace DeBox.Teleport.Transport
{
    public class SequencedTeleportChannel : BaseTeleportProxyChannel
    {
        private class OutboxItem
        {
            public byte[] data;
            public long lastSendTime;
        }

        private class InboxItem
        {
            public byte[] data;
            public int startIndex;
            public int length;
        }


        private ushort _outgoingSequence;
        private int _lastReceiveIndex;
        private int _lastProcessedReceiveIndex;
        private Dictionary<ushort, InboxItem> _inbox;
        private Dictionary<ushort, OutboxItem> _outbox;
        private bool _sendAcks;
        private object _outboxLock;

        public SequencedTeleportChannel() : this(new SimpleTeleportChannel()) { }

        public SequencedTeleportChannel(BaseTeleportChannel internalChannel) : this(internalChannel, false)
        {            
        }

        public SequencedTeleportChannel(BaseTeleportChannel internalChannel, bool sendAcks) : base(internalChannel)
        {
            _sendAcks = sendAcks;
            _outgoingSequence = 0;
            _lastReceiveIndex = -1;
            _lastProcessedReceiveIndex = -1;
            _inbox = new Dictionary<ushort, InboxItem>();
            _outbox = new Dictionary<ushort, OutboxItem>();
            _outboxLock = new object();
        }

        public override void Receive(byte[] data, int startIndex, int length)
        {
            //Debug.LogError("RECV: " + TeleportDebugUtils.DebugString(reader));            
            var processedLength = 0;
            var sequenceNumber = BitConverter.ToUInt16(data, startIndex);
            processedLength += sizeof(ushort);
            if (_sendAcks && sequenceNumber == ushort.MaxValue)
            {                
                Debug.LogError("GOT ACK!");
                lock (_outboxLock)
                {
                    sequenceNumber = BitConverter.ToUInt16(data, startIndex + processedLength);
                    _outbox.Remove(sequenceNumber);
                }                
                return;
            }
            if (_inbox.ContainsKey(sequenceNumber))
            {
                Debug.LogWarning("Got same sequence twice: " + sequenceNumber);
            }
            _inbox[sequenceNumber] = new InboxItem() { data = data, startIndex = startIndex, length = length - processedLength };
            if (sequenceNumber > _lastReceiveIndex)
            {
                _lastReceiveIndex = sequenceNumber;
            }
            if (_sendAcks)
            {
                byte[] ackData = new byte[4];
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
                lock (_outboxLock)
                {
                    _outbox[_outgoingSequence] = new OutboxItem() { data = newData, lastSendTime = DateTime.UtcNow.Ticks };
                }
            }
            _outgoingSequence++;
            InternalChannel.Send(newData);
            ProcessOutbox();
        }

        private void ProcessOutbox()
        {
            if (_outbox.Count == 0)
            {
                return;
            }
            ushort seqId;
            OutboxItem outboxItem;
            lock (_outboxLock)
            {
                foreach (var p in _outbox)
                {
                    seqId = p.Key;
                    outboxItem = p.Value;
                    if (outboxItem.lastSendTime < DateTime.UtcNow.Ticks - 10000000)                    
                    {
                        outboxItem.lastSendTime = DateTime.UtcNow.Ticks;
                        InternalChannel.Send(outboxItem.data);
                    }
                }
            }
        }

        private void ProcessInbox()
        {
            if (_inbox.Count == 0)
            {
                return;
            }
            InboxItem inboxItem;
            ushort nextIndex;
            while (_lastReceiveIndex > _lastProcessedReceiveIndex)
            {
                nextIndex = (ushort)(_lastProcessedReceiveIndex + 1);
                if (!_inbox.TryGetValue(nextIndex, out inboxItem))
                {
                    break;
                }
                
                InternalChannel.Receive(inboxItem.data, inboxItem.startIndex, inboxItem.length);
                _lastProcessedReceiveIndex = nextIndex;
            }
        }


    }

}
