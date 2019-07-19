/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;

namespace Leap.Unity {

  public class ProduceConsumeBuffer<T> {
    
    private T[] _buffer;
    private uint _bufferMask;
    private uint _head, _tail;

    /// <summary>
    /// Constructs a new produce consumer buffer of at least a certain capacity.
    /// Once the buffer is created, the capacity cannot be modified.
    /// 
    /// If the minimum capacity is a power of two, it will be used as the actual
    /// capacity. If the minimum capacity is not a power of two, the next
    /// highest power of two will be used as the capacity.  This behavior is an
    /// optimization, Internally this class uses a bitwise AND operation instead
    /// of a slower modulus operation for indexing, which only is possible if
    /// the array length is a power of two.
    /// </summary>
    public ProduceConsumeBuffer(int minCapacity) {
      if (minCapacity <= 0) {
        throw new ArgumentOutOfRangeException(
          "The capacity of the ProduceConsumeBuffer must be positive and " +
          "non-zero.");
      }

      int capacity;
      int closestPowerOfTwo = Mathf.ClosestPowerOfTwo(minCapacity);
      if (closestPowerOfTwo == minCapacity) {
        capacity = minCapacity;
      } else {
        if (closestPowerOfTwo < minCapacity) {
          capacity = closestPowerOfTwo * 2;
        } else {
          capacity = closestPowerOfTwo;
        }
      }

      _buffer = new T[capacity];
      _bufferMask = (uint)(capacity - 1);
      _head = 0;
      _tail = 0;
    }

    /// <summary>
    /// Returns the maximum number of elements that the buffer can hold.
    /// </summary>
    public int Capacity {
      get {
        return _buffer.Length;
      }
    }

    /// <summary>
    /// Returns the current number of elements that are held inside the buffer.
    /// </summary>
    public int Count {
      get {
        int tail = (int)_tail;
        int head = (int)_head;

        if (tail < head) {
          tail += Capacity;
        }

        return tail - head;
      }
    }

    /// <summary>
    /// Tries to enqueue a value into the buffer. If the buffer is already full,
    /// this method will perform no action and return false. This method is only
    /// safe to be called from a single producer thread.
    /// </summary>
    public bool TryEnqueue(ref T t) {
      uint nextTail = (_tail + 1) & _bufferMask;
      if (nextTail == _head) return false;

      _buffer[_tail] = t;
      _tail = nextTail;
      return true;
    }

    /// <summary>
    /// Tries to enqueue a value into the buffer. If the buffer is already full,
    /// this method will perform no action and return false. This method is only
    /// safe to be called from a single producer thread.
    /// </summary>
    public bool TryEnqueue(T t) {
      return TryEnqueue(ref t);
    }

    /// <summary>
    /// Tries to get the next element that would be dequeued from this buffer.
    /// If there is no element yet, this method will return false. If there is
    /// an element ready to be dequeued, it will be copied to the out param and
    /// this method will return true.
    /// 
    /// This method is only safe to be called from a single consumer thread.
    /// </summary>
    public bool TryPeek(out T t) {
      if (Count == 0) {
        t = default(T);
        return false;
      } else {
        // No risk of an enqueue corrupting this element 
        // since we don't modify head or tail, an enqueue targeting this element
        // would fail.
        t = _buffer[_head];
        return true;
      }
    }

    /// <summary>
    /// Tries to dequeue a value off of the buffer. If the buffer is empty this
    /// method will perform no action and return false. This method is only safe
    /// to be called from a single consumer thread.
    /// </summary>
    public bool TryDequeue(out T t) {
      if (_tail == _head) {
        t = default(T);
        return false;
      }

      t = _buffer[_head];
      _head = (_head + 1) & _bufferMask;
      return true;
    }

    /// <summary>
    /// Tries to dequeue a value off of the buffer. If the buffer is empty this
    /// method will perform no action and return false. This method is only safe
    /// to be called from a single consumer thread.
    /// </summary>
    public bool TryDequeue() {
      if (_tail == _head) {
        return false;
      }

      _head = (_head + 1) & _bufferMask;
      return true;
    }

    /// <summary>
    /// Tries to dequeue all values off of the buffer, returning the most
    /// recently added element. If there was an element found, this method will
    /// return true, else it will return false.
    /// </summary>
    public bool TryDequeueAll(out T mostRecent) {
      if (!TryDequeue(out mostRecent)) {
        return false;
      }

      T temp;
      while (TryDequeue(out temp)) {
        mostRecent = temp;
      }

      return true;
    }
  }
}
