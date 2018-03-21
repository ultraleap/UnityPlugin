using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {
    public Query<T> Concat(ICollection<T> collection) {
      var result = new Query<T>(_count + collection.Count);
      var dstArray = result._data.array;
      var srcArray = _data.array;

      srcArray.CopyTo(dstArray, 0);
      collection.CopyTo(dstArray, _count);

      Dispose();
      return result;
    }
  }

  public static class ConcatExtension {
    public static Query<T> Concat<T>(this ICollection<T> collection, ICollection<T> other) {
      return new Query<T>(collection).Concat(other);
    }
  }
}
