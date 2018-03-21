using System;
using System.Collections.Generic;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {
    public Query<T> Where(Func<T, bool> predicate) {
      var copy = copyDisposeOriginal();

      var array = copy._data.array;
      var count = copy._count;

      int writeIndex = 0;
      for (int i = 0; i < count; i++) {
        if (predicate(array[i])) {
          array[writeIndex++] = array[i];
        }
      }

      copy._count = writeIndex;
      while (writeIndex < count) {
        array[writeIndex++] = default(T);
      }

      return copy;
    }
  }

  public static class WhereExtension {
    public static Query<T> Where<T>(this ICollection<T> collection, Func<T, bool> predicate) {
      return new Query<T>(collection).Where(predicate);
    }
  }
}