using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {
    public Query<T> TakeWhile(Func<T, bool> predicate) {
      var result = copyDisposeOriginal();

      int takeCount;
      for (takeCount = 0; takeCount < result._count; takeCount++) {
        if (!predicate(result._data.array[takeCount])) {
          break;
        }
      }

      result._count = takeCount;
      return result;
    }
  }

  public static class TakeWhileExtension {
    public static Query<T> TakeWhile<T>(this ICollection<T> collection, Func<T, bool> predicate) {
      return new Query<T>(collection).TakeWhile(predicate);
    }
  }
}
