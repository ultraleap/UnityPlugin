/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections.Generic;

namespace Leap.Unity {
  using Query;

  /// <summary>
  /// A simple wrapper around HashSet to provide readonly access.
  /// Useful when you want to return a HashSet to someone but you want
  /// to make sure they don't muck it up!
  /// </summary>
  public struct ReadonlyHashSet<T> {
    private readonly HashSet<T> _set;

    public ReadonlyHashSet(HashSet<T> set) {
      _set = set;
    }

    public int Count {
      get {
        return _set.Count;
      }
    }

    public HashSet<T>.Enumerator GetEnumerator() {
      return _set.GetEnumerator();
    }

    public bool Contains(T obj) {
      return _set.Contains(obj);
    }

    public Query<T> Query() {
      return _set.Query();
    }

    public static implicit operator ReadonlyHashSet<T>(HashSet<T> set) {
      return new ReadonlyHashSet<T>(set);
    }

    public static implicit operator ReadonlyHashSet<T>(SerializableHashSet<T> set) {
      return (HashSet<T>)set;
    }
  }
}
