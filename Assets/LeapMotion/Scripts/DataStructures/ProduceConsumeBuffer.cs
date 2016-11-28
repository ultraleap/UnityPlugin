using UnityEngine;
using System;

namespace Leap.Unity {

  public class ProduceConsumeBuffer<T> {
    private T[] _buffer;
    private uint _bufferMask;
    private uint _head, _tail;

    /// <summary>
    /// Constructs a new produce consumer buffer of a given capacity.  This capacity
    /// is fixed and cannot be changed after the buffer is created.  This capacity
    /// must be a power of two.
    /// 
    /// The power of two requirement is an optimization.  Internally this class can
    /// use a bitwise AND operation instead of a slower modulus operation for indexing
    /// if the length of the buffer is a power of two.
    /// </summary>
    public ProduceConsumeBuffer(int capacity) {
      if (capacity <= 0) {
        throw new ArgumentOutOfRangeException("The capacity of the ProduceConsumeBuffer must be positive and non-zero.");
      }

      if (Mathf.ClosestPowerOfTwo(capacity) != capacity) {
        throw new ArgumentException("The capacity of the ProduceConsumeBuffer must be a power of two.");
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
    /// Tries to enqueue a value into the buffer.  If the buffer is already full, this
    /// method will perform no action and return false.  This method is only safe to
    /// be called from a single producer thread.
    /// </summary>
    public bool TryEnqueue(ref T t) {
      uint nextTail = (_tail + 1) & _bufferMask;
      if (nextTail == _head) return false;

      _buffer[_tail] = t;
      _tail = nextTail;
      return true;
    }

    /// <summary>
    /// Tries to dequeue a value off of the buffer.  If the buffer is empty this method
    /// will perform no action and return false.  This method is only safe to be
    /// called from a single consumer thread.
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
  }
}
