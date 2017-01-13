using System.Collections.Generic;

namespace Leap.Unity.Query {

  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IEnumerator<QueryType> {
    private QueryOp op;

    public QueryWrapper(QueryOp op) {
      this.op = op;
    }

    public QueryOp GetEnumerator() {
      return op;
    }
  }

  public static class QueryConversionExtensions {
    public static QueryWrapper<T, IEnumerator<T>> Query<T>(this IEnumerable<T> enumerable) {
      return new QueryWrapper<T, IEnumerator<T>>(enumerable.GetEnumerator());
    }

    public static QueryWrapper<T, List<T>.Enumerator> Query<T>(this List<T> list) {
      return new QueryWrapper<T, List<T>.Enumerator>(list.GetEnumerator());
    }

    public static QueryWrapper<KeyValuePair<K, V>, Dictionary<K, V>.Enumerator> Query<T, K, V>(this Dictionary<K, V> dictionary) {
      return new QueryWrapper<KeyValuePair<K, V>, Dictionary<K, V>.Enumerator>(dictionary.GetEnumerator());
    }

    public static QueryWrapper<T, HashSet<T>.Enumerator> Query<T>(this HashSet<T> hashSet) {
      return new QueryWrapper<T, HashSet<T>.Enumerator>(hashSet.GetEnumerator());
    }

    public static QueryWrapper<T, Queue<T>.Enumerator> Query<T>(this Queue<T> queue) {
      return new QueryWrapper<T, Queue<T>.Enumerator>(queue.GetEnumerator());
    }
  }
}
