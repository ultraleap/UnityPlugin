/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
