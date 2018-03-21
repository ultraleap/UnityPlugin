/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Leap.Unity {

  /// <summary>
  /// A simple wrapper around HashSet to provide readonly access.
  /// Useful when you want to return a HashSet to someone but you want
  /// to make sure they don't muck it up!
  /// </summary>
  public struct ReadonlyHashSet<T> : ICollection<T> {
    private readonly HashSet<T> _set;

    public ReadonlyHashSet(HashSet<T> set) {
      _set = set;
    }

    public int Count {
      get {
        return _set.Count;
      }
    }

    public bool IsReadOnly {
      get {
        return true;
      }
    }

    public HashSet<T>.Enumerator GetEnumerator() {
      return _set.GetEnumerator();
    }

    public bool Contains(T obj) {
      return _set.Contains(obj);
    }

    public void Add(T item) {
      throw new System.NotImplementedException();
    }

    public void Clear() {
      throw new System.NotImplementedException();
    }

    public void CopyTo(T[] array, int arrayIndex) {
      throw new System.NotImplementedException();
    }

    public bool Remove(T item) {
      throw new System.NotImplementedException();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
      throw new System.NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      throw new System.NotImplementedException();
    }

    public static implicit operator ReadonlyHashSet<T>(HashSet<T> set) {
      return new ReadonlyHashSet<T>(set);
    }

    public static implicit operator ReadonlyHashSet<T>(SerializableHashSet<T> set) {
      return (HashSet<T>)set;
    }
  }
}
