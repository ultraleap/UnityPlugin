using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {
    public Query<T> Take(int toTake) {
      var result = copyDisposeOriginal();
      result._count = Mathf.Min(result._count, toTake);
      return result;
    }
  }

  public static class TakeExtension {
    public static Query<T> Take<T>(this ICollection<T> collection, int toTake) {
      return new Query<T>(collection).Take(toTake);
    }
  }
}
