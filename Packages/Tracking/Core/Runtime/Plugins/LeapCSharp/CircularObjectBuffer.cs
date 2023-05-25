/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace LeapInternal
{
    //TODO add test for thread safety

    /**
     * A Limited capacity, circular LIFO buffer that wraps around
     * when full. Supports indexing to get older items. Array-backed.
     * *
     * Unlike many collections, objects are never removed, just overwritten when
     * the buffer cycles back to their array location.
     *
     * Object types used must have default parameterless constructor. It should be obvious that
     * such default objects are invalid. I.e. for Leap API objects, the IsValid property should be false.
     */
    public class CircularObjectBuffer<T> where T : new()
    {
        private T[] array;
        private int current = 0;
        private object locker = new object();
        public int Count { get; private set; }
        public int Capacity { get; private set; }
        public bool IsEmpty { get; private set; }

        public CircularObjectBuffer(int capacity)
        {
            Capacity = capacity;
            array = new T[this.Capacity];
            current = 0;
            Count = 0;
            IsEmpty = true;
        }

        /** Put an item at the head of the list. Once full, this will overwrite the oldest item. */
        public virtual void Put(ref T item)
        {
            lock (locker)
            {
                if (!IsEmpty)
                {
                    current++;
                    if (current >= Capacity)
                    {
                        current = 0;
                    }
                }
                if (Count < Capacity)
                    Count++;

                lock (array)
                {
                    array[current] = item;
                }
                IsEmpty = false;
            }
        }

        /** Get the item indexed backward from the head of the list */
        public void Get(out T t, int index = 0)
        {
            lock (locker)
            {
                if (IsEmpty || (index > Count - 1) || index < 0)
                {
                    t = new T(); //default(T);
                }
                else
                {
                    int effectiveIndex = current - index;
                    if (effectiveIndex < 0)
                    {
                        effectiveIndex += Capacity;
                    }

                    t = array[effectiveIndex];
                }
            }
        }

        /** Increase  */
        public void Resize(int newCapacity)
        {
            lock (locker)
            {
                if (newCapacity <= Capacity)
                {
                    return;
                }

                T[] newArray = new T[newCapacity];
                int j = 0;
                for (int i = Count - 1; i >= 0; i--)
                {
                    T t;
                    Get(out t, i);
                    newArray[j++] = t;
                }
                this.array = newArray;
                this.Capacity = newCapacity;
            }
        }
    }
}