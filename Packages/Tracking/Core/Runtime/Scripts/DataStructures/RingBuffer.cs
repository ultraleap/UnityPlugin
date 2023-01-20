/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity
{
    public class RingBuffer<T>
    {

        private T[] arr;
        private int firstIdx = 0;
        private int lastIdx = -1;

        public RingBuffer(int bufferSize)
        {
            bufferSize = System.Math.Max(1, bufferSize);
            arr = new T[bufferSize];
        }

        public int Count
        {
            get
            {
                if (lastIdx == -1) return 0;

                int endIdx = (lastIdx + 1) % arr.Length;

                if (endIdx <= firstIdx) { endIdx += arr.Length; }
                return endIdx - firstIdx;
            }
        }

        public int Capacity
        {
            get { return arr.Length; }
        }

        public bool IsFull
        {
            get
            {
                return lastIdx != -1
                       && ((lastIdx + 1 + arr.Length) % arr.Length) == firstIdx;
            }
        }

        public bool IsEmpty
        {
            get { return lastIdx == -1; }
        }

        /// <summary>
        /// Oldest element is at index 0, youngest is at Count - 1.
        /// </summary>
        public T this[int idx]
        {
            get { return Get(idx); }
            set { Set(idx, value); }
        }

        public void Clear()
        {
            firstIdx = 0;
            lastIdx = -1;
        }

        public void Add(T t)
        {
            if (IsFull)
            {
                firstIdx += 1;
                firstIdx %= arr.Length;
            }
            lastIdx += 1;
            lastIdx %= arr.Length;

            arr[lastIdx] = t;
        }

        /// <summary> Synonym for "Add". </summary>
        public void Push(T t)
        {
            Add(t);
        }

        /// <summary>
        /// Oldest element is at index 0, youngest is at Count - 1.
        /// </summary>
        public T Get(int idx)
        {
            if (idx < 0 || idx > Count - 1)
            {
                Debug.Log("Tried to access index " + idx + " of RingBuffer with count " + Count);
                throw new IndexOutOfRangeException();
            }

            return arr[(firstIdx + idx) % arr.Length];
        }

        public T GetLatest()
        {
            if (Count == 0)
            {
                throw new IndexOutOfRangeException("Can't get latest value in an empty RingBuffer.");
            }

            return Get(Count - 1);
        }

        public T GetOldest()
        {
            if (Count == 0)
            {
                throw new IndexOutOfRangeException("Can't get oldest value in an empty RingBuffer.");
            }

            return Get(0);
        }

        public void Set(int idx, T t)
        {
            if (idx < 0 || idx > Count - 1) { throw new IndexOutOfRangeException(); }

            int actualIdx = (firstIdx + idx) % arr.Length;
            arr[actualIdx] = t;
        }

        public void SetLatest(T t)
        {
            if (Count == 0)
            {
                throw new IndexOutOfRangeException("Can't set latest value in an empty RingBuffer.");
            }

            Set(Count - 1, t);
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[RingBuffer: ");
            for (int i = 0; i < this.Count; i++)
            {
                sb.Append(this[i]);
                sb.Append(", ");
            }
            sb.Length -= 2;
            sb.Append("]");
            return sb.ToString();
        }

        public RingBufferEnumerator GetEnumerator()
        {
            return new RingBufferEnumerator(this);
        }

        public struct RingBufferEnumerator
        {
            private RingBuffer<T> _buffer;
            private int _idx;
            public RingBufferEnumerator(RingBuffer<T> buffer)
            {
                this._buffer = buffer;
                this._idx = -1;
            }
            public bool HasCurrent { get { return _idx >= 0 && _idx < _buffer.Count; } }
            public bool MoveNext()
            {
                if (_idx >= _buffer.Count) { return false; }
                _idx += 1;
                if (_idx < 0 || _idx >= _buffer.Count) { return false; }
                return true;
            }
            public T Current { get { return _buffer[_idx]; } }
        }
    }
}