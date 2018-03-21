using System.Collections.Generic;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {

    public Query<K> OfType<K>() where K : T {
      var result = new Query<K>(_count);
      var dstArray = result._data.array;
      var srcArray = _data.array;

      int dstCount = 0;
      for (int i = 0; i < _count; i++) {
        if (srcArray[i] is K) {
          dstArray[dstCount++] = (K)srcArray[i];
        }
      }

      Dispose();
      return result;
    }
  }

  public static class OfTypeExtension {
    public static Query<K> OfType<T, K>(this ICollection<T> collection) where K : T {
      return new Query<T>(collection).OfType<K>();
    }
  }
}
