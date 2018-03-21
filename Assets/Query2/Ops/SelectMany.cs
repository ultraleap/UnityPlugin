using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {
    public Query<K> SelectMany<K>(Func<T, ICollection<K>> selector) {
      int totalCount = 0;
      var array = _data.array;
      for (int i = 0; i < _count; i++) {
        totalCount += selector(array[i]).Count;
      }

      var result = new Query<K>(totalCount);

      int targetIndex = 0;
      for (int i = 0; i < _count; i++) {
        var collection = selector(array[i]);
        collection.CopyTo(result._data.array, targetIndex);
        targetIndex += collection.Count;
      }

      Dispose();
      return result;
    }
  }

  public static class SelectManyExtension {
    public static Query<K> SelectMany<T, K>(this ICollection<T> collection, Func<T, ICollection<K>> selector) {
      return new Query<T>(collection).SelectMany(selector);
    }
  }
}
