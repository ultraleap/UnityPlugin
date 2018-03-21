using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {
    public Query<T> Repeat(int times) {
      var result = new Query<T>(_count * times);
      var srcArray = _data.array;
      var dstArray = result._data.array;

      for (int i = 0; i < times; i++) {
        srcArray.CopyTo(dstArray, i * _count);
      }

      Dispose();
      return result;
    }
  }

  public static class RepeatExtension {
    public static Query<T> Repeat<T>(this ICollection<T> collection, int times) {
      return new Query<T>(collection).Repeat(times);
    }
  }
}
