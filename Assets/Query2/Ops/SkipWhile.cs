using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {
    public Query<T> SkipWhile(Func<T, bool> predicate) {
      var result = copyDisposeOriginal();

      var array = result._data.array;

      int toSkip = 0;
      while (toSkip < result._count) {
        if (predicate(array[toSkip])) {
          toSkip++;
        } else {
          break;
        }
      }

      result._count -= toSkip;
      for (int i = 0; i < result._count; i++) {
        array[i] = array[i + toSkip];
      }

      return result;
    }
  }

  public static class SkipWhileExtension {
    public static Query<T> SkipWhile<T>(this ICollection<T> collection, Func<T, bool> predicate) {
      return new Query<T>(collection).SkipWhile(predicate);
    }
  }
}
