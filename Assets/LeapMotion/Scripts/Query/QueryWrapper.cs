/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {
    private QueryOp _op;

    public QueryOp op {
      get {
        return _op;
      }
    }

    public QueryWrapper(QueryOp op) {
      _op = op;
    }

    public Enumerator GetEnumerator() {
      return new Enumerator(_op);
    }

    public struct Enumerator {
      private QueryOp _op;
      private QueryType _current;

      public Enumerator(QueryOp op) {
        _op = op;
        _current = default(QueryType);
      }

      public bool MoveNext() {
        return _op.TryGetNext(out _current);
      }

      public QueryType Current {
        get {
          return _current;
        }
      }
    }
  }

  public interface IQueryOp<T> {
    bool TryGetNext(out T t);
    void Reset();
  }

  public static class QueryConversionExtensions {

    public static QueryWrapper<T, ListQueryOp<T>> Query<T>(this IList<T> list) {
      return new QueryWrapper<T, ListQueryOp<T>>(new ListQueryOp<T>(list));
    }

    public static QueryWrapper<KeyValuePair<K, V>, EnumerableQueryOp<KeyValuePair<K, V>, Dictionary<K, V>.Enumerator>> Query<T, K, V>(this Dictionary<K, V> dictionary) {
      return new QueryWrapper<KeyValuePair<K, V>, EnumerableQueryOp<KeyValuePair<K, V>, Dictionary<K, V>.Enumerator>>(new EnumerableQueryOp<KeyValuePair<K, V>, Dictionary<K, V>.Enumerator>(dictionary.GetEnumerator()));
    }

    public static QueryWrapper<T, EnumerableQueryOp<T, HashSet<T>.Enumerator>> Query<T>(this HashSet<T> hashSet) {
      return new QueryWrapper<T, EnumerableQueryOp<T, HashSet<T>.Enumerator>>(new EnumerableQueryOp<T, HashSet<T>.Enumerator>(hashSet.GetEnumerator()));
    }

    public static QueryWrapper<T, EnumerableQueryOp<T, Queue<T>.Enumerator>> Query<T>(this Queue<T> queue) {
      return new QueryWrapper<T, EnumerableQueryOp<T, Queue<T>.Enumerator>>(new EnumerableQueryOp<T, Queue<T>.Enumerator>(queue.GetEnumerator()));
    }

    public static QueryWrapper<T, EnumerableQueryOp<T, IEnumerator<T>>> Query<T>(this IEnumerator<T> enumerator) {
      return new QueryWrapper<T, EnumerableQueryOp<T, IEnumerator<T>>>(new EnumerableQueryOp<T, IEnumerator<T>>(enumerator));
    }

    /// <summary>
    /// Generic fallback for calling Query on any IEnumerable.
    /// 
    /// IMPORTANT!  Since this uses the IEnumerable interface, it MUST create a small allocation
    /// during the call to GetEnumerator that cannot be avoided.
    /// </summary>
    public static QueryWrapper<T, EnumerableQueryOp<T, IEnumerator<T>>> Query<T>(this IEnumerable<T> enumerable) {
      return new QueryWrapper<T, EnumerableQueryOp<T, IEnumerator<T>>>(new EnumerableQueryOp<T, IEnumerator<T>>(enumerable.GetEnumerator()));
    }

    public struct ListQueryOp<T> : IQueryOp<T> {
      private IList<T> _list;
      private int _index;

      public ListQueryOp(IList<T> list) {
        _list = list;
        _index = 0;
      }

      public bool TryGetNext(out T t) {
        if (_index >= _list.Count) {
          t = default(T);
          return false;
        } else {
          t = _list[_index++];
          return true;
        }
      }

      public void Reset() {
        _index = 0;
      }
    }

    public struct EnumerableQueryOp<T, Enumerable> : IQueryOp<T>
      where Enumerable : IEnumerator<T> {
      private Enumerable _source;

      public EnumerableQueryOp(Enumerable source) {
        _source = source;
      }

      public bool TryGetNext(out T t) {
        if (_source.MoveNext()) {
          t = _source.Current;
          return true;
        } else {
          t = default(T);
          return false;
        }
      }

      public void Reset() {
        _source.Reset();
      }
    }
  }
}
