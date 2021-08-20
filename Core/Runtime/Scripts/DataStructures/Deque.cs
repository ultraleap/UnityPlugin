/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity {

  public class Deque<T> {
    private T[] _array;
    private uint _front, _count;
    private uint _indexMask;

    public Deque(int minCapacity = 8) {
      if (minCapacity <= 0) {
        throw new ArgumentException("Capacity must be positive and nonzero.");
      }

      //Find next highest power of two
      int capacity = Mathf.ClosestPowerOfTwo(minCapacity);
      if (capacity < minCapacity) {
        capacity *= 2;
      }

      _array = new T[capacity];
      recalculateIndexMask();
      _front = 0;
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
        _front = 0;
        _count = 0;
      }
    }

    public void PushBack(T t) {
      doubleCapacityIfFull();
      ++_count;
      _array[getBackIndex()] = t;
    }

    public void PushFront(T t) {
      doubleCapacityIfFull();
      ++_count;
      _front = (_front - 1) & _indexMask;
      _array[_front] = t;
    }

    public void PopBack() {
      checkForEmpty("pop back");

      _array[getBackIndex()] = default(T);
      --_count;
    }

    public void PopFront() {
      checkForEmpty("pop front");

      _array[_front] = default(T);
      --_count;
      _front = (_front + 1) & _indexMask;
    }

    public void PopBack(out T back) {
      checkForEmpty("pop back");

      uint backIndex = getBackIndex();
      back = _array[backIndex];
      _array[backIndex] = default(T);
      --_count;
    }

    public void PopFront(out T front) {
      checkForEmpty("pop front");

      front = _array[_front];
      _array[_front] = default(T);
      _front = (_front + 1) & _indexMask;
      --_count;
    }

    public T Front {
      get {
        checkForEmpty("get front");
        return _array[_front];
      }
      set {
        checkForEmpty("set front");
        _array[_front] = value;
      }
    }

    public T Back {
      get {
        checkForEmpty("get back");
        return _array[getBackIndex()];
      }
      set {
        checkForEmpty("set back");
        _array[getBackIndex()] = value;
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
      uint back = getBackIndex();
      for (uint i = 0; i < _array.Length; i++) {
        bool isEmpty;
        if (_count == 0) {
          isEmpty = true;
        } else if (_count == 1) {
          isEmpty = i != _front;
        } else if (_front < back) {
          isEmpty = (i < _front) || (i > back);
        } else {
          isEmpty = (i < _front) && (i > back);
        }

        string element = "";
        if (i == _front) {
          element = "{";
        } else {
          element = " ";
        }

        if (isEmpty) {
          element += ".";
        } else {
          element += _array[i].ToString();
        }

        if (i == back) {
          element += "}";
        } else {
          element += " ";
        }

        debug += element;
      }
      debug += "]";
      return debug;
    }

    private uint getBackIndex() {
      return (_front + _count - 1) & _indexMask;
    }

    private uint getIndex(uint index) {
      return (_front + index) & _indexMask;
    }

    private void doubleCapacityIfFull() {
      if (_count >= _array.Length) {
        T[] newArray = new T[_array.Length * 2];

        uint front = getBackIndex();
        if (_front <= front) {
          //values do not wrap around, we can use a simple copy
          Array.Copy(_array, _front, newArray, 0, _count);
        } else {
          //values do wrap around, we need to use 2 copies
          uint backOffset = (uint)_array.Length - _front;
          Array.Copy(_array, _front, newArray, 0, backOffset);
          Array.Copy(_array, 0, newArray, backOffset, _count - backOffset);
        }

        _front = 0;
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
        throw new IndexOutOfRangeException("The index " + index + " was out of range for the RingBuffer with size " + _count + ".");
      }
    }

    private void checkForEmpty(string actionName) {
      if (_count == 0) {
        throw new InvalidOperationException("Cannot " + actionName + " because the RingBuffer is empty.");
      }
    }
  }
}
