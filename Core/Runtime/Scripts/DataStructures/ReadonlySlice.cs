/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  public static class ReadonlySliceExtensions {

    /// <summary>
    /// Creates a readonlySlice into the ReadonlyList with an inclusive beginIdx and an _exclusive_
    /// endIdx. A readonlySlice with identical begin and end indices would be an empty readonlySlice.
    /// 
    /// A readonlySlice whose endIdx is smaller than its beginIdx will index backwards along the
    /// underlying ReadonlyList.
    /// 
    /// Not providing either index argument will simply refer to the beginning of the
    /// list (for beginIdx) or to the end of the list (for endIdx).
    /// 
    /// ReadonlySlices do not allocate, and they provide an enumerator definition so they can be
    /// used in a <code>foreach</code> statement.
    /// </summary>
    public static ReadonlySlice<T> ReadonlySlice<T>(this ReadonlyList<T> list, int beginIdx = -1, int endIdx = -1) {
      if (beginIdx == -1 && endIdx == -1) {
        return new ReadonlySlice<T>(list, 0, list.Count);
      }
      else if (beginIdx == -1 && endIdx != -1) {
        return new ReadonlySlice<T>(list, 0, endIdx);
      }
      else if (endIdx == -1 && beginIdx != -1) {
        return new ReadonlySlice<T>(list, beginIdx, list.Count);
      }
      else {
        return new ReadonlySlice<T>(list, beginIdx, endIdx);
      }
    }

    public static ReadonlySlice<T> FromIndex<T>(this ReadonlyList<T> list, int fromIdx) {
      return ReadonlySlice(list, fromIdx);
    }

  }

  public struct ReadonlySlice<T> : IIndexableStruct<T, ReadonlySlice<T>> {

    private ReadonlyList<T> _list;

    private int _beginIdx;
    private int _endIdx;

    private int _direction;

    /// <summary>
    /// Creates a readonlySlice into the ReadonlyList with an inclusive beginIdx and an _exclusive_
    /// endIdx. A readonlySlice with identical begin and end indices would be an empty readonlySlice.
    /// 
    /// A readonlySlice whose endIdx is smaller than its beginIdx will index backwards along the
    /// underlying ReadonlyList.
    /// </summary>
    public ReadonlySlice(ReadonlyList<T> list, int beginIdx, int endIdx) {
      _list = list;
      _beginIdx = beginIdx;
      _endIdx = endIdx;
      _direction = beginIdx <= endIdx ? 1 : -1;
    }

    public T this[int index] {
      get {
        if (index < 0 || index > Count - 1) { throw new IndexOutOfRangeException(); }
        return _list[_beginIdx + index * _direction];
      }
    }

    public int Count {
      get {
        return (_endIdx - _beginIdx) * _direction;
      }
    }

    #region foreach and Query()

    public IndexableStructEnumerator<T, ReadonlySlice<T>> GetEnumerator() {
      return new IndexableStructEnumerator<T, ReadonlySlice<T>>(this);
    }

    public Query<T> Query() {
      T[] array = ArrayPool<T>.Spawn(Count);
      for (int i = 0; i < Count; i++) {
        array[i] = this[i];
      }
      return new Query<T>(array, Count);
    }

    #endregion

  }

}
