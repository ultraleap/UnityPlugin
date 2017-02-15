using System;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  /// <summary>
  /// The wrapper for all leap queries.  These queries are meant to mirror the queries present in the System.Linq.Enumerable
  /// class.  These queries are meant to be functionaly identical, but allocate zero garbage, both during the generation of
  /// the query, as well as the execution.  The speed is also aimed to be as fast or faster.
  ///
  /// There is some difference between using Linq and using Leap queries. One is that is you must prefix your query with
  /// a call to a Query() method if you are starting with an external data structure.  So for example if you want to query a
  /// list, your method call would look something like this:
  ///
  /// myList.Query().Where(someCondition).First();
  ///
  /// The other major difference is that you can only use a query once.  So the following will not work:
  ///
  /// var myQuery = myList.Query().Where(someCondition);
  ///
  /// foreach(var item in myQuery) { }
  /// foreach(var item in myQuery) { } // will not work, query has already been used!
  ///
  /// All of these limitations and changes are made in the name of performance and to reduce garbage allocated to zero, while
  /// hopefully not impacting the productivity of the user too much.
  /// </summary>
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
