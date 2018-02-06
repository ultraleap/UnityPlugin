using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  public static class SliceExtensions {

    /// <summary>
    /// Creates a slice into the List with an inclusive beginIdx and an _exclusive_
    /// endIdx. A slice with identical begin and end indices would be an empty slice.
    /// 
    /// A slice whose endIdx is smaller than its beginIdx will index backwards along the
    /// underlying List.
    /// 
    /// Not providing either index argument will simply refer to the beginning of the
    /// list (for beginIdx) or to the end of the list (for endIdx).
    /// 
    /// Slices do not allocate, and they provide an enumerator definition so they can be
    /// used in a <code>foreach</code> statement.
    /// </summary>
    public static Slice<T> Slice<T>(this IList<T> list, int beginIdx = -1, int endIdx = -1) {
      if (beginIdx == -1 && endIdx != -1) {
        return new Slice<T>(list, 0, list.Count);
      }
      else if (beginIdx == -1 && endIdx != -1) {
        return new Slice<T>(list, 0, endIdx);
      }
      else if (endIdx == -1 && beginIdx != -1) {
        return new Slice<T>(list, beginIdx, list.Count);
      }
      else {
        return new Slice<T>(list, beginIdx, endIdx);
      }
    }

    public static Slice<T> FromIndex<T>(this IList<T> list, int fromIdx) {
      return Slice(list, fromIdx);
    }

  }

  public struct Slice<T> : IIndexable<T> {

    private IList<T> _list;

    private int _beginIdx;
    private int _endIdx;

    private int _direction;

    /// <summary>
    /// Creates a slice into the List with an inclusive beginIdx and an _exclusive_
    /// endIdx. A slice with identical begin and end indices would be an empty slice.
    /// 
    /// A slice whose endIdx is smaller than its beginIdx will index backwards along the
    /// underlying List.
    /// </summary>
    public Slice(IList<T> list, int beginIdx, int endIdx) {
      _list = list;
      _beginIdx = beginIdx;
      _endIdx = endIdx;
      _direction = beginIdx <= endIdx ? 1 : -1;
    }

    public T this[int index] {
      get {
        if (index > Count - 1) { throw new System.IndexOutOfRangeException(); }
        return _list[_beginIdx + index * _direction];
      }
      set {
        if (index > Count - 1) { throw new System.IndexOutOfRangeException(); }
        _list[_beginIdx + index * _direction] = value;
      }
    }

    public int Count {
      get {
        return (_endIdx - _beginIdx) * _direction;
      }
    }

    public IIndexableEnumerator<T> GetEnumerator() { return this.GetEnumerator(); }

  }

}