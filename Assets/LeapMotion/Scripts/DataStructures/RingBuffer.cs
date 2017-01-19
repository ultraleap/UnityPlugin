using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity {

  public class RingBuffer<T> {

    private T[] arr;
    private int firstIdx = 0;
    private int lastIdx = 0;

    public RingBuffer(int bufferSize) {
      arr = new T[bufferSize];
    }

    public int Length {
      get {
        if (lastIdx == -1) return 0;
        int diff = (lastIdx + 1) - firstIdx;
        if (diff <= 0) return diff + arr.Length;
        return diff;
      }
    }

    public bool IsFull {
      get { return Length == arr.Length; }
    }

    public void Clear() {
      lastIdx = -1;
      firstIdx = 0;
    }

    public void Add(T t) {
      if (IsFull) {
        firstIdx += 1;
        firstIdx %= arr.Length;
      }
      lastIdx += 1;
      lastIdx %= arr.Length;

      arr[lastIdx] = t;
    }

    /// <summary>
    /// Oldest element is at index 0, youngest is at Length - 1.
    /// </summary>
    public T Get(int idx) {
      return arr[(firstIdx + idx) % arr.Length];
    }

    public T GetLatest() {
      return Get(Length - 1);
    }

    public T GetOldest() {
      return Get(0);
    }

    public void Set(int idx, T t) {
      int actualIdx = (firstIdx + idx) % arr.Length;
      arr[actualIdx] = t;
    }

    public void SetLatest(T t) {
      Set(Length - 1, t);
    }

    public RingBufferEnumerator GetEnumerator() {
      return new RingBufferEnumerator(this);
    }

    public class RingBufferEnumerator : IEnumerator<T>, System.Collections.IEnumerator {

      private RingBuffer<T> _ringBuffer;
      private int idx = -1;

      public RingBufferEnumerator(RingBuffer<T> ringBuffer) {
        _ringBuffer = ringBuffer;
      }

      object IEnumerator.Current {
        get { return (object)Current; }
      }

      public T Current {
        get {
          if (idx >= 0) {
            return _ringBuffer.Get(idx);
          }
          else throw new System.IndexOutOfRangeException();
        }
      }

      public bool MoveNext() {
        idx += 1;
        if (idx >= _ringBuffer.Length) return false;
        return true;
      }

      public void Reset() {
        idx = -1;
      }

      public void Dispose() { }
    }

  }

}