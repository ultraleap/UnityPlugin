/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

namespace Leap.Unity {

  public class DelayBuffer<T> {

    private RingBuffer<T> _buffer;

    /// <summary> Returns the underlying Buffer object. </summary>
    public RingBuffer<T> Buffer { get { return _buffer; } }

    public int Count { get { return _buffer.Count; } }

    public bool IsFull { get { return _buffer.IsFull; } }

    public bool IsEmpty { get { return _buffer.IsEmpty; } }

    public int Capacity { get { return _buffer.Capacity; } }

    public void Clear() { _buffer.Clear(); }

    public DelayBuffer(int bufferSize) {
      _buffer = new RingBuffer<T>(bufferSize);
    }

    /// <summary> Returns true if the buffer was full and out "delayedT" will
    /// contain the oldest value in the buffer, otherwise returns false. </summary>
    public bool Add(T t, out T delayedT) {
      bool willOutputValue;
      if (_buffer.IsFull) {
        willOutputValue = true;
        delayedT = _buffer.GetOldest();
      }
      else {
        willOutputValue = false;
        delayedT = default(T);
      }
      _buffer.Add(t);
      return willOutputValue;
    }

  }

}
