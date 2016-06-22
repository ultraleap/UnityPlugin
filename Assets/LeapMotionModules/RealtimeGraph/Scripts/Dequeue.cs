using System;
using UnityEngine;

namespace Leap.Unity.Graphing {

  public class Dequeue<T> {
    private T[] _array;
    private uint _back, _count;
    private uint _indexMask;

    public Dequeue(int minCapacity = 8) {
      if (minCapacity <= 0) {
        throw new ArgumentException("Capacity must be positive and nonzero.");
      }

      int capacity = Mathf.ClosestPowerOfTwo(minCapacity);
      if (capacity < minCapacity) {
        capacity *= 2;
      }

      _array = new T[capacity];
      recalculateIndexMask();
      _back = 0;
      _count = 0;
    }

    public int Count {
      get {
        return (int)_count;
      }
    }

    public void Clear() {
      if (_count != 0) {
        Array.Clear(_array, 0, _array.Length);
        _back = 0;
        _count = 0;
      }
    }

    public void PushFront(T t) {
      expandIfNeeded();
      _count++;
      Front = t;
    }

    public void PushBack(T t) {
      expandIfNeeded();
      _count++;
      _back = (_back - 1) & _indexMask;
      Back = t;
    }

    public void PopFront() {
      checkForEmpty("pop front");

      Front = default(T);
      --_count;
    }

    public void PopBack() {
      checkForEmpty("pop back");

      Back = default(T);
      _count--;
      _back = (_back + 1) & _indexMask;
    }

    public void PopFront(out T front) {
      checkForEmpty("pop front");

      uint frontIndex = getFrontIndex();
      front = _array[frontIndex];
      _array[frontIndex] = default(T);
      --_count;
    }

    public void PopBack(out T back) {
      checkForEmpty("pop back");

      back = _array[_back];
      _array[_back] = default(T);
      _back = (_back + 1) & _indexMask;
      --_count;
    }

    public T Back {
      get {
        checkForEmpty("get back");
        return _array[_back];
      }
      set {
        checkForEmpty("set back");
        _array[_back] = value;
      }
    }

    public T Front {
      get {
        checkForEmpty("get front");
        return _array[getFrontIndex()];
      }
      set {
        checkForEmpty("set front");
        _array[getFrontIndex()] = value;
      }
    }

    public T this[int index] {
      get {
        uint uindex = (uint)index;
        checkForValidIndex(uindex);
        return _array[getIndex(uindex)];
      }
      set {
        uint uindex = (uint)index;
        checkForValidIndex(uindex);
        _array[getIndex(uindex)] = value;
      }
    }

    public string ToDebugString() {
      string debug = "[";
      uint front = (_back + _count - 1) & _indexMask;
      for (uint i = 0; i < _array.Length; i++) {
        bool isEmpty;
        if (_count == 0) {
          isEmpty = true;
        } else if (_count == 1) {
          isEmpty = i != _back;
        } else if (_back < front) {
          isEmpty = (i < _back) || (i > front);
        } else {
          isEmpty = (i < _back) && (i > front);
        }

        string element = "";
        if (i == _back) {
          element = "{";
        } else {
          element = " ";
        }

        if (isEmpty) {
          element += ".";
        } else {
          element += _array[i].ToString();
        }

        if (i == front) {
          element += "}";
        } else {
          element += " ";
        }

        debug += element;
      }
      debug += "]";
      return debug;
    }

    private uint getFrontIndex() {
      return (_back + _count - 1) & _indexMask;
    }

    private uint getIndex(uint index) {
      return (_back + index) & _indexMask;
    }

    private void expandIfNeeded() {
      if (_count >= _array.Length) {
        T[] newArray = new T[_array.Length * 2];

        uint front = getFrontIndex();
        if (_back <= front) {
          //values do not wrap around, we can use a simple copy
          Array.Copy(_array, _back, newArray, 0, _count);
        } else {
          //values do wrap around, we need to use 2 copies
          uint backOffset = (uint)_array.Length - _back;
          Array.Copy(_array, _back, newArray, 0, backOffset);
          Array.Copy(_array, 0, newArray, backOffset, _count - backOffset);
        }

        _back = 0;
        _array = newArray;
        recalculateIndexMask();
      }
    }

    private void recalculateIndexMask() {
      //array length is always power of 2, so length-1 is the bitmask we need
      //8 = 1000
      //7 = 0111 = mask for values 0-7
      _indexMask = (uint)_array.Length - 1;
    }

    private void checkForValidIndex(uint index) {
      if (index >= _count) {
        throw new IndexOutOfRangeException("The index " + index + " was out of range for the Dequeue with size " + _count + ".");
      }
    }

    private void checkForEmpty(string actionName) {
      if (_count == 0) {
        throw new InvalidOperationException("Cannot " + actionName + " because the Dequeue is empty.");
      }
    }
  }
}
