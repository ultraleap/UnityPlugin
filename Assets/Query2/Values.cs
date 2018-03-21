using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public static class Values {

    public static Query<T> Single<T>(T value) {
      var data = ArrayPool<T>.Spawn(1);
      data.array[0] = value;
      return new Query<T>(data, 1);
    }

    public static Query<T> Empty<T>() {
      var data = ArrayPool<T>.Spawn(1);
      return new Query<T>(data, 0);
    }

    public static Query<int> Range(int from, int to, int step = 1, bool endIsExclusive = true) {
      if (step <= 0) {
        throw new ArgumentException("Step must be positive and non-zero.");
      }

      List<int> values = Pool<List<int>>.Spawn();
      try {
        int value = from;
        int sign = Utils.Sign(to - from);

        while (Utils.Sign(value - from) == sign) {
          values.Add(value);
          value += step * sign;
        }

        if (!endIsExclusive && value == to) {
          values.Add(to);
        }

        return new Query<int>(values);
      } finally {
        Pool<List<int>>.Recycle(values);
      }
    }
  }
}
