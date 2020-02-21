using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DeBox.Teleport.Transport
{
    public class SequencedTeleportChannel : BaseTeleportProxyChannel
    {
        private ushort _outgoingSequence;
        private int _lastReceiveIndex;
        private int _lastProcessedReceiveIndex;
        private Dictionary<ushort, TeleportReader> _inbox;

        public SequencedTeleportChannel() : this(new SimpleTeleportChannel()) { }

        public SequencedTeleportChannel(BaseTeleportChannel internalChannel) : base(internalChannel)
        {
            _outgoingSequence = 0;
            _lastReceiveIndex = -1;
            _lastProcessedReceiveIndex = -1;
            _inbox = new Dictionary<ushort, TeleportReader>();
        }

        public override void Receive(TeleportReader reader)
        {            
            var sequenceNumber = reader.ReadUInt16();
            if (_inbox.ContainsKey(sequenceNumber))
            {
                Debug.LogWarning("Got same sequence twice: " + sequenceNumber);
            }
            _inbox[sequenceNumber] = reader;
            if (sequenceNumber > _lastReceiveIndex)
            {
                _lastReceiveIndex = sequenceNumber;
            }
            ProcessInbox();
        }

        public override void Send(TeleportWriter writer, MemoryStream stream, Action<TeleportWriter> serializerFunc)
        {

            writer.Write(_outgoingSequence);
            _outgoingSequence++;            
            InternalChannel.Send(writer, stream, serializerFunc);
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
