using System;
using System.Collections.Generic;
using DeBox.Teleport.Unity;

namespace DeBox.Teleport.Core
{
    public class TeleportChaoticPacketBuffer : ITeleportPacketBuffer
    {
        [Serializable]
        public class ChaosSettings
        {
            public bool EnableChaos = false;
            public int RandomSeed = 1;
            public float MinDelay = 0.05f;
            public float MaxDelay = 0.2f;
            public int ScrambleBitCount = 3;
            public int ScrambleChance = 30;
            public int DropChance = 30;
        }

        public class QueuedPacket
        {
            public byte[] data;
            public int length;
            public double timestamp;
        }

        public class PacketQueue
        {
            private readonly object _lock = new object();
            private readonly List<QueuedPacket> _queue = new List<QueuedPacket>();

            public int Count { get { return _queue.Count; } }

            public void Enqueue(QueuedPacket entry)
            {
                lock (_lock)
                {
                    _queue.Add(entry);
                    //InsertViaBinarySearch(entry);
                }
            }

            public QueuedPacket Peek()
            {
                return _queue[0];
            }

            public QueuedPacket Dequeue()
            {
                lock (_lock)
                {
                    var item = _queue[0];
                    _queue.RemoveAt(0);
                    return item;
                }
            }

            public void Clear() { _queue.Clear(); }

            private void InsertViaBinarySearch(QueuedPacket entry)
            {
                if (_queue.Count == 0)
                {
                    _queue.Add(entry);
                    return;
                }
                if (entry.timestamp > _queue[_queue.Count - 1].timestamp)
                {
                    _queue.Add(entry);
                    return;
                }
                if (entry.timestamp < _queue[0].timestamp)
                {
                    _queue.Insert(0, entry);
                    return;
                }
                double key = entry.timestamp;
                int minNum = 0;
                int maxNum = _queue.Count - 1;
                int mid = 0;
                while (minNum <= maxNum)
                {
                    
                    mid = (minNum + maxNum) / 2;

                    if (System.Math.Abs(key - _queue[mid].timestamp) <= double.Epsilon)
                    {
                        _queue.Insert(mid + 1, entry);
                        return;
                    }
                    else if (key < _queue[mid].timestamp)
                    {
                        maxNum = mid - 1;
                    }
                    else
                    {
                        minNum = mid + 1;
                    }
                }
                throw new Exception("Failed to perform binary search for: " + key);
            }
        }

        private readonly DateTime _epochStart = new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        private TeleportPacketBuffer _internalBuffer;
        private List<QueuedPacket> _incomingDisorderList;
        private List<QueuedPacket> _outgoingDisorderList;
        private PacketQueue _incomingDelayQueue;
        private PacketQueue _outgoingDelayQueue;
        private Random _random;
        private ChaosSettings _settings;

        public TeleportChaoticPacketBuffer(ChaosSettings settings)
        {
            _internalBuffer = new TeleportPacketBuffer();
            _incomingDisorderList = new List<QueuedPacket>();
            _outgoingDisorderList = new List<QueuedPacket>();
            _incomingDelayQueue = new PacketQueue();
            _outgoingDelayQueue = new PacketQueue();
            _random = new Random(settings.RandomSeed);
            _settings = settings;
        }

        public void ReceiveRawData(byte[] data, int dataLength)
        {
            var timestamp = GetNow() + GetRandomDelayInSeconds();
            bool shouldDrop = ((_random.Next() % 100) < _settings.DropChance) || _incomingDelayQueue.Count > 100000;
            if (shouldDrop)
            {
                return;
            }
            var dataCopy = new byte[data.Length];
            Array.Copy(data, 0, dataCopy, 0, data.Length);
            _incomingDelayQueue.Enqueue(new QueuedPacket() { data = dataCopy, length = dataLength, timestamp = timestamp });
        }
        
        public byte[] CreatePacket(byte channelId, byte[] data, int offset = 0, ushort length = 0)
        {            
            var newData = _internalBuffer.CreatePacket(channelId, data, offset, length);
            bool shouldScramble = ((_random.Next() % 100) < _settings.ScrambleChance);
            if (shouldScramble)
            {
                ScrambleData(newData, length, _settings.ScrambleBitCount);
            }
            return newData;
        }

        public int TryParseNextIncomingPacket(byte[] outBuffer, out byte channelId)
        {
            if (_incomingDelayQueue.Count > 0 && _incomingDelayQueue.Peek().timestamp <= GetNow())
            {
                var entry = _incomingDelayQueue.Dequeue();
                _internalBuffer.ReceiveRawData(entry.data, entry.length);
            }
            return _internalBuffer.TryParseNextIncomingPacket(outBuffer, out channelId);
        }

        private double GetNow()
        {
            return (DateTime.UtcNow - _epochStart).TotalSeconds;
        }

        private double GetRandomDelayInSeconds()
        {
            var range = Math.Abs(_settings.MaxDelay - _settings.MinDelay);
            return (_random.NextDouble() * range) + _settings.MinDelay;
        }

        private void ScrambleData(byte[] data, ushort length, int bitCountToScramble)
        {
            int randomLocation;
            for (var i = 0; i < bitCountToScramble; i++)
            {
                randomLocation = _random.Next() % length;
                data[randomLocation] = ScrambleSingleBitInByte(data[randomLocation]);
            }
        }

        private byte ScrambleSingleBitInByte(byte data)
        {
            var randomBit = _random.Next() % 8;
            var mask = 1 << randomBit;
            var current = data & mask;
            return (byte)(current > 0 ? randomBit & ~mask : randomBit | mask);
        }
    }
}
