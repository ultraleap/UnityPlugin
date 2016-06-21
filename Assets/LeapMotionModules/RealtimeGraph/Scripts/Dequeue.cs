using System;

namespace Leap.Unity.Graphing {

  public class Dequeue<T> {
    private T[] _array;
    private int _back, _count;

    public Dequeue(int capacity = 8) {
      _array = new T[capacity];
      _back = 0;
      _count = 0;
    }

    public int Count {
      get {
        return _count;
      }
    }

    public void Clear() {
      Array.Clear(_array, 0, _array.Length);
      _back = 0;
      _count = 0;
    }

    public void PushFront(T t) {
      expandIfNeeded();
      _count++;
      Front = t;
    }

    public void PushBack(T t) {
      expandIfNeeded();
      _count++;
      _back = (_back + _array.Length - 1) % _array.Length;
      Back = t;
    }

    public void PopFront() {
      Front = default(T);
      _count--;
    }

    public void PopBack() {
      Back = default(T);
      _count--;
      _back = (_back + 1) % _array.Length;
    }

    public void PopFront(out T front) {
      int frontIndex = getFrontIndex();
      front = _array[frontIndex];
      _array[frontIndex] = default(T);
      _count--;
    }

    public void PopBack(out T back) {
      back = _array[_back];
      _array[_back] = default(T);
      _count--;
      _back = (_back + 1) % _array.Length;
    }

    public T Back {
      get {
        return _array[_back];
      }
      set {
        _array[_back] = value;
      }
    }

    public T Front {
      get {
        return _array[getFrontIndex()];
      }
      set {
        _array[getFrontIndex()] = value;
      }
    }

    public T this[int index] {
      get {
        return _array[getIndex(index)];
      }
      set {
        _array[getIndex(index)] = value;
      }
    }

    private int getFrontIndex() {
      return (_back + _count - 1) % _array.Length;
    }

    private int getIndex(int index) {
      return (_back + index) % _array.Length;
    }

    private void expandIfNeeded() {
      if (_count >= _array.Length) {
        T[] newArray = new T[_array.Length * 2];
        for (int i = 0; i < _count; i++) {
          newArray[i] = _array[(_back + i) % _array.Length];
        }
        _array = newArray;
      }
    }
  }
}
