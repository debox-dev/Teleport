using System;

namespace DeBox.Teleport.Core
{
    public class ArrayQueue<T> 
    {
        private const int ENQUEUE_INDEX_FULL_INDECATION = -1;
        private T[] _queue;
        private int _enqueueIndex;
        private int _dequeueIndex;
        private object _queueLock;

        public ArrayQueue(int maxSize)
        {
            _queue = new T[maxSize];
            _dequeueIndex = 0;
            _dequeueIndex = 0;
            _queueLock = new object();
        }

        public int Length => _queue.Length;

        public bool IsFull => _enqueueIndex == ENQUEUE_INDEX_FULL_INDECATION;

        public int Count => _enqueueIndex >= _dequeueIndex ? _enqueueIndex - _dequeueIndex : _queue.Length - (_dequeueIndex - _enqueueIndex - 1);        

        public T Peek() { return _queue[_dequeueIndex]; }

        public void ClearFast() { _enqueueIndex = _dequeueIndex; }

        public void Enqueue(T item)
        {
            lock (_queueLock)
            {
                if (_enqueueIndex == ENQUEUE_INDEX_FULL_INDECATION)
                {
                    throw new Exception("Queue is full");
                }
                _queue[_enqueueIndex] = item;
                _enqueueIndex = GetNextIndex(_enqueueIndex);
                if (_enqueueIndex == _dequeueIndex)
                {
                    _enqueueIndex = ENQUEUE_INDEX_FULL_INDECATION;
                }
            }
        }

        public T Dequeue()
        {
            T item;
            lock (_queueLock)
            {
                if (_dequeueIndex == _enqueueIndex)
                {
                    throw new Exception("Queue is empty");
                }
                item = _queue[_dequeueIndex];
                _queue[_dequeueIndex] = default(T);
                _dequeueIndex = GetNextIndex(_dequeueIndex);
                return item;
            }
        }

        public void Clear()
        {
            while (Count > 0)
            {
                Dequeue();
            }
        }    

        private int GetNextIndex(int index)
        {
            return ((index + 1) % _queue.Length);
        }
    }
}

