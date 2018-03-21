using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {
    public Query<T> Skip(int toSkip) {
      var result = copyDisposeOriginal();

      var array = result._data.array;
      result._count = Mathf.Max(result._count - toSkip, 0);
      for (int i = 0; i < result._count; i++) {
        array[i] = array[i + toSkip];
      }

      return result;
    }
  }

  public static class SkipExtension {
    public static Query<T> Skip<T>(this ICollection<T> collection, int toSkip) {
      return new Query<T>(collection).Skip(toSkip);
    }
  }
}
