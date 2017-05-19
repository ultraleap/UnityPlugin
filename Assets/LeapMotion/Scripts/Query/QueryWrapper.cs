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
  /// The wrapper for all leap queries.  These queries are meant to mirror the queries present in the 
  /// System.Linq.Enumerable class.  These queries are meant to be functionaly identical, but allocate 
  /// zero garbage, both during the generation of the query, as well as the execution.  The speed is 
  /// also aimed to be as fast or faster.
  ///
  /// There is one big difference between using Linq and using Leap queries.You must prefix your query 
  /// with a call to a Query() method if you are starting with an external data structure.  So for 
  /// example if you want to query a list, your method call would look something like this:
  ///
  /// myList.Query().Where(someCondition).First();
  /// </summary>
  public partial struct QueryWrapper<QueryType, QueryOp> where QueryOp : IQueryOp<QueryType> {
    private QueryOp _op;

    /// <summary>
    /// Returns the actual query operation wrapped by this wrapper.  This operation always
    /// implements IQueryOp, which is an interface similar to IEnumerator.  You usually
    /// do not need to access this op directly to use the query.  Instead you can simply
    /// foreach over this wrapper, or use one of the direct query operations like First,
    /// ToList, Any, ect...
    /// </summary>
    public QueryOp op {
      get {
        return _op;
      }
    }

    /// <summary>
    /// Constructs a new wrapper given a specific query operation.
    /// </summary>
    public QueryWrapper(QueryOp op) {
      _op = op;
    }

    /// <summary>
    /// Returns an enumerator object that is able to enumerate through the query operation
    /// wrapped by this wrapper.  You can call this directly and step through the result 
    /// by using MoveNext/Current, or use it indirectly by using the foreach construct.
    /// </summary>
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

  /// <summary>
  /// The interface all query operations must follow.  It is a modified version of the 
  /// IEnumerator interface, optimized for speed and conciseness.
  /// </summary>
  public interface IQueryOp<T> {

    /// <summary>
    /// Tries to get the next value in the sequence.  If this method returns true,
    /// the next value will be placed into the out parameter t.  If this method
    /// returns false, the sequence is at an end and t will be the default value
    /// of T.
    /// 
    /// Once TryGetNext returns false, it can NEVER return true again until the
    /// Reset operator is called.
    /// </summary>
    bool TryGetNext(out T t);

    /// <summary>
    /// Resets the internal state of this query operation to the begining of
    /// the sequence.
    /// </summary>
    void Reset();
  }

  /// <summary>
  /// Data structures require special conversion operations to turn them into 
  /// objects that implement IQueryOp.  These conversions can unfortunately not
  /// be done automatically, which is the reason for the call to Query().  All
  /// Query() calls are housed in this class for ease of use.
  /// </summary>
  public static class QueryConversionExtensions {

    /// <summary>
    /// Converts an IList object into a query operation, and returns a query wrapper
    /// that wraps this new operation.
    /// </summary>
    public static QueryWrapper<T, ListQueryOp<T>> Query<T>(this IList<T> list) {
      return new QueryWrapper<T, ListQueryOp<T>>(new ListQueryOp<T>(list));
    }

    /// <summary>
    /// Converts a Dictionary object into a query operation, and returns a query wrapper
    /// that wraps this new operation.
    /// </summary>
    public static QueryWrapper<KeyValuePair<K, V>, EnumerableQueryOp<KeyValuePair<K, V>, Dictionary<K, V>.Enumerator>> Query<T, K, V>(this Dictionary<K, V> dictionary) {
      return new QueryWrapper<KeyValuePair<K, V>, EnumerableQueryOp<KeyValuePair<K, V>, Dictionary<K, V>.Enumerator>>(new EnumerableQueryOp<KeyValuePair<K, V>, Dictionary<K, V>.Enumerator>(dictionary.GetEnumerator()));
    }

    /// <summary>
    /// Converts a HashSet object into a query operation, and returns a query wrapper
    /// that wraps this new operation.
    /// </summary>
    public static QueryWrapper<T, EnumerableQueryOp<T, HashSet<T>.Enumerator>> Query<T>(this HashSet<T> hashSet) {
      return new QueryWrapper<T, EnumerableQueryOp<T, HashSet<T>.Enumerator>>(new EnumerableQueryOp<T, HashSet<T>.Enumerator>(hashSet.GetEnumerator()));
    }

    /// <summary>
    /// Converts a Queue object into a query operation, and returns a query wrapper
    /// that wraps this new operation.
    /// </summary>
    public static QueryWrapper<T, EnumerableQueryOp<T, Queue<T>.Enumerator>> Query<T>(this Queue<T> queue) {
      return new QueryWrapper<T, EnumerableQueryOp<T, Queue<T>.Enumerator>>(new EnumerableQueryOp<T, Queue<T>.Enumerator>(queue.GetEnumerator()));
    }

    /// <summary>
    /// Generic fallback for calling Query on any IEnumerator.
    /// 
    /// IMPORTANT!  Since this uses the IEnumerator interface, it MUST create a small allocation
    /// during the call to GetEnumerator that cannot be avoided.
    /// </summary>
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
