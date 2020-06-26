/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

//#define VALIDATE
using UnityEngine;
using System;

namespace Leap.Unity {

  public interface IMinHeapNode {
    int heapIndex { get; set; }
  }

  public class MinHeap<T> where T : IMinHeapNode, IComparable<T> {

    private T[] _array = new T[4];
    private int _count = 0;

    public int Count {
      get {
        return _count;
      }
    }

    public void Clear() {
      Array.Clear(_array, 0, _count);
      _count = 0;
    }

    public void Insert(T element) {
#if VALIDATE
      validateHeapInternal("Insert");
#endif

      //if the array isn't big enough, expand it
      if (_array.Length == _count) {
        T[] newArray = new T[_array.Length * 2];
        Array.Copy(_array, newArray, _array.Length);
        _array = newArray;
      }

      element.heapIndex = _count;
      _count++;

      bubbleUp(element);
    }

    public void Remove(T element) {
      removeAt(element.heapIndex);
    }

    public T PeekMin() {
      if (_count == 0) {
        throw new Exception("Cannot peek when there are zero elements!");
      }

      return _array[0];
    }

    public T RemoveMin() {
      if (_count == 0) {
        throw new Exception("Cannot Remove Min when there are zero elements!");
      }

      return removeAt(0);
    }

    private T removeAt(int index) {
#if VALIDATE
      validateHeapInternal("Remove At");
#endif

      T ret = _array[index];
      _count--;

      if (_count == 0) {
        return ret;
      }

      var bottom = _array[_count];
      bottom.heapIndex = index;

      int parentIndex = getParentIndex(index);
      if (isValidIndex(parentIndex) && _array[parentIndex].CompareTo(bottom) > 0) {
        bubbleUp(bottom);
      } else {
        bubbleDown(bottom);
      }

      return ret;
    }

    private void bubbleUp(T element) {
      while (true) {
        if (element.heapIndex == 0) {
          break;
        }

        int parentIndex = getParentIndex(element.heapIndex);
        var parent = _array[parentIndex];

        if (parent.CompareTo(element) <= 0) {
          break;
        }

        parent.heapIndex = element.heapIndex;
        _array[element.heapIndex] = parent;

        element.heapIndex = parentIndex;
      }

      _array[element.heapIndex] = element;

#if VALIDATE
      validateHeapInternal("Bubble Up");
#endif
    }

    public bool Validate() {
      return validateHeapInternal("Validation ");
    }

    private void bubbleDown(T element) {
      int elementIndex = element.heapIndex;

      while (true) {
        int leftIndex = getChildLeftIndex(elementIndex);
        int rightIndex = getChildRightIndex(elementIndex);

        T smallest = element;
        int smallestIndex = elementIndex;

        if (isValidIndex(leftIndex)) {
          var leftChild = _array[leftIndex];
          if (leftChild.CompareTo(smallest) < 0) {
            smallest = leftChild;
            smallestIndex = leftIndex;
          }
        } else {
          break;
        }

        if (isValidIndex(rightIndex)) {
          var rightChild = _array[rightIndex];
          if (rightChild.CompareTo(smallest) < 0) {
            smallest = rightChild;
            smallestIndex = rightIndex;
          }
        }

        if (smallestIndex == elementIndex) {
          break;
        }

        smallest.heapIndex = elementIndex;
        _array[elementIndex] = smallest;

        elementIndex = smallestIndex;
      }

      element.heapIndex = elementIndex;
      _array[elementIndex] = element;

#if VALIDATE
      validateHeapInternal("Bubble Down");
#endif
    }

    private bool validateHeapInternal(string operation) {
      for (int i = 0; i < _count; i++) {
        if (_array[i].heapIndex != i) {
          Debug.LogError("Element " + i + " had an index of " + _array[i].heapIndex + " instead, after " + operation);
          return false;
        }

        if (i != 0) {
          var parent = _array[getParentIndex(i)];
          if (parent.CompareTo(_array[i]) > 0) {
            Debug.LogError("Element " + i + " had an incorrect order after " + operation);
            return false;
          }
        }
      }
      return true;
    }

    private static int getChildLeftIndex(int index) {
      return index * 2 + 1;
    }

    private static int getChildRightIndex(int index) {
      return index * 2 + 2;
    }

    private static int getParentIndex(int index) {
      return (index - 1) / 2;
    }

    private bool isValidIndex(int index) {
      return index < _count && index >= 0;
    }
  }
}
