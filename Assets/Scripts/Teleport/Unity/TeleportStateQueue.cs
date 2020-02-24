using System.Collections.Generic;

namespace DeBox.Teleport.Unity
{
    public class TeleportStateQueue
    {
        public class TeleportQueueEntry
        {
            public float Timestamp;
            public ITeleportState[] States;
        }

        private List<TeleportQueueEntry> _queue;
        private bool _autoclear;

        public TeleportStateQueue()
        {
            _queue = new List<TeleportQueueEntry>();
            _autoclear = false;
        }

        public void EnqueueEntry(float timestamp, ITeleportState[] states)
        {
            var entry = new TeleportQueueEntry()
            {
                Timestamp = timestamp,
                States = states,
            };
            InsertViaBinarySearch(entry);
        }

        public ITeleportState[] GetInterpolatedStates(float timestamp)
        {
            int firstEntryIndex = 0;
            int secondEntryIndex;
            if (_queue.Count == 0)
            {
                return new ITeleportState[0];
            }
            if (_queue.Count == 1)
            {
                return _queue[0].States;
            }
            if (timestamp > _queue[_queue.Count - 1].Timestamp)
            {
                return _queue[_queue.Count - 1].States;
            }

            while (_queue[firstEntryIndex + 1].Timestamp < timestamp && firstEntryIndex < _queue.Count)
            {
                firstEntryIndex++;
            }
            secondEntryIndex = firstEntryIndex + 1;
            if (secondEntryIndex >= _queue.Count)
            {
                return _queue[_queue.Count - 1].States;
            }
            var statesByInstanceId = new Dictionary<ushort, ITeleportState>();
            var entry1 = _queue[firstEntryIndex];
            var entry2 = _queue[secondEntryIndex];
            var interpolatePct = (timestamp - entry1.Timestamp) / (entry2.Timestamp - entry1.Timestamp);
            foreach (var state in entry2.States)
            {
                statesByInstanceId[state.InstanceId] = state;
            }
            foreach (var state in entry1.States)
            {
                if (!statesByInstanceId.ContainsKey(state.InstanceId))
                {
                    continue;
                }
                statesByInstanceId[state.InstanceId] = state.Interpolate(statesByInstanceId[state.InstanceId], interpolatePct);
            }
            
            if (_autoclear)
            {
                for (int i = 0; i < firstEntryIndex - 1; i++)
                {
                    _queue.RemoveAt(0);
                }
            }
            return new List<ITeleportState>(statesByInstanceId.Values).ToArray();
        }

        private void InsertViaBinarySearch(TeleportQueueEntry entry)
        {
            if (_queue.Count == 0)
            {
                _queue.Add(entry);
                return;
            }
            if (entry.Timestamp > _queue[_queue.Count - 1].Timestamp)
            {
                _queue.Add(entry);
                return;
            }
            float key = entry.Timestamp;
            int minNum = 0;
            int maxNum = _queue.Count - 1;
            while (minNum <= maxNum)
            {
                int mid = (minNum + maxNum) / 2;
                if (System.Math.Abs(key - _queue[mid].Timestamp) < float.Epsilon)
                {
                    _queue.Insert(mid + 1, entry);
                    return;
                }
                else if (key < _queue[mid].Timestamp)
                {
                    maxNum = mid - 1;
                }
                else
                {
                    minNum = mid + 1;
                }
            }
            throw new System.Exception("Failed to binary search");
        }
    }

}
