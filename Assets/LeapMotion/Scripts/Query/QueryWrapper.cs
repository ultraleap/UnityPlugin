using System;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public partial struct QueryWrapper<QueryType, QueryOp> : IDisposable where QueryOp : IEnumerator<QueryType> {

    private enum State : byte {
      Ready,
      ReturnedEnumerator,
      AlreadyDisposed
    }

    private QueryOp _op;
    private State _state;

    public QueryWrapper(QueryOp op) {
      _op = op;
      _state = State.Ready;
    }

    public QueryOp GetEnumerator() {
      return thisAndConsume._op;
    }

    public void Dispose() {
      if (_state == State.AlreadyDisposed) {
        throw new InvalidOperationException("This QueryWrapper has already been disposed");
      }

      _state = State.AlreadyDisposed;
      _op.Dispose();
    }

    private QueryWrapper<QueryType, QueryOp> thisAndConsume {
      get {
        switch (_state) {
          case State.AlreadyDisposed:
            throw new InvalidOperationException("This QueryWrapper has already been disposed.");
          case State.ReturnedEnumerator:
            throw new InvalidOperationException("Get Enumerator has already been called for this QueryWrapper.");
        }

        _state = State.ReturnedEnumerator;
        return this;
      }
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
