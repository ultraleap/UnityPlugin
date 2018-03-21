using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query {

  public static class Values {

    public static Query<T> Single<T>(T value) {
      var array = ArrayPool<T>.Spawn(1);
      array[0] = value;
      return new Query<T>(array, 1);
    }

    public static Query<T> Repeat<T>(T value, int times) {
      var array = ArrayPool<T>.Spawn(times);
      for (int i = 0; i < times; i++) {
        array[i] = value;
      }
      return new Query<T>(array, times);
    }

    public static Query<T> Empty<T>() {
      var array = ArrayPool<T>.Spawn(0);
      return new Query<T>(array, 0);
    }

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
