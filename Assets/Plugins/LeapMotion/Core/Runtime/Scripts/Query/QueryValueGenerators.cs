/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public static class Values {

    /// <summary>
    /// Returns a Query containing a single element.
    /// </summary>
    public static Query<T> Single<T>(T value) {
      var array = ArrayPool<T>.Spawn(1);
      array[0] = value;
      return new Query<T>(array, 1);
    }

    /// <summary>
    /// Returns a Query containing a single element repeated
    /// 0 or more times.
    /// </summary>
    public static Query<T> Repeat<T>(T value, int times) {
      var array = ArrayPool<T>.Spawn(times);
      for (int i = 0; i < times; i++) {
        array[i] = value;
      }
      return new Query<T>(array, times);
    }

    /// <summary>
    /// Returns a Query containing no elements.
    /// </summary>
    public static Query<T> Empty<T>() {
      var array = ArrayPool<T>.Spawn(0);
      return new Query<T>(array, 0);
    }

    /// <summary>
    /// Returns a Query containing integers that range from one value to another.  You can
    /// optionally specify the step to used when moving along the range, as well as specifying
    /// whether or not the final value of the range should be included or not.
    /// 
    /// For Example:
    ///  Range(0, 10)           = 0,1,2,3,4,5,6,7,8,9
    ///  Range(0, 10, 2)        = 0,2,4,6,8
    ///  Range(0, 10, 2, false) = 0,2,4,6,8,10
    ///  Range(10, 0)           = 10,9,8,7,6,5,4,3,2,1
    ///  Range(-1,1,false)      = -1,0,1
    /// </summary>
    public static Query<int> Range(int from, int to, int step = 1, bool endIsExclusive = true) {
      if (step <= 0) {
        throw new ArgumentException("Step must be positive and non-zero.");
      }

      List<int> values = Pool<List<int>>.Spawn();
      try {
        int value = from;
        int sign = Utils.Sign(to - from);

        if (sign != 0) {
          while (Utils.Sign(to - value) == sign) {
            values.Add(value);
            value += step * sign;
          }
        }

        if (!endIsExclusive && value == to) {
          values.Add(to);
        }

        return new Query<int>(values);
      } finally {
        values.Clear();
        Pool<List<int>>.Recycle(values);
      }
    }
  }
}
