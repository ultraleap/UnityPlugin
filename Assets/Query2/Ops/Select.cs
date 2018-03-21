using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {
    public Query<K> Select<K>(Func<T, K> selector) {
      var result = new Query<K>(_count);

      var srcArray = _data.array;
      var dstArray = result._data.array;
      for (int i = 0; i < _count; i++) {
        dstArray[i] = selector(srcArray[i]);
      }

      Dispose();
      return result;
    }

    public Query<K> Cast<K>() where K : T {
      return Select(item => (K)item);
    }
  }

  public static class SelectExtension {
    public static Query<K> Select<T, K>(this ICollection<T> collection, Func<T, K> selector) {
      return new Query<T>(collection).Select(selector);
    }

    public static Query<K> Cast<T, K>(this ICollection<T> collection) where K : T {
      return new Query<T>(collection).Cast<K>();
    }
  }
}
