using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {
    public Query<V> Zip<K, V>(ICollection<K> collection, Func<T, K, V> selector) {
      int resultCount = Mathf.Min(_count, collection.Count);
      var result = new Query<V>(resultCount);
      var resultArray = result._data.array;

      var tmpArray = ArrayPool<K>.Spawn(collection.Count);
      collection.CopyTo(tmpArray.array, 0);

      var array = _data.array;
      for (int i = 0; i < resultCount; i++) {
        resultArray[i] = selector(array[i], tmpArray.array[i]);
      }

      ArrayPool<K>.Recycle(tmpArray);
      Dispose();

      return result;
    }
  }

  public static class ZipExtension {
    public static Query<V> Zip<T, K, V>(this ICollection<T> collectionA, ICollection<K> collectionB, Func<T, K, V> selector) {
      return new Query<T>(collectionA).Zip(collectionB, selector);
    }
  }
}
