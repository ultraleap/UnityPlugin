/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections;

namespace Leap.Unity.Query {

  public static class QueryConversionExtensions {

    /// <summary>
    /// Constructs a new Query from the given Collection.
    /// </summary>
    public static Query<T> Query<T>(this ICollection<T> collection) {
      return new Query<T>(collection);
    }

    /// <summary>
    /// Constructs a new Query from the given IEnumerable
    /// </summary>
    public static Query<T> Query<T>(this IEnumerable<T> enumerable) {
      List<T> list = Pool<List<T>>.Spawn();
      try {
        list.AddRange(enumerable);
        return new Query<T>(list);
      } finally {
        list.Clear();
        Pool<List<T>>.Recycle(list);
      }
    }

    /// <summary>
    /// Constructs a new Query from the given IEnumerator
    /// </summary>
    public static Query<T> Query<T>(this IEnumerator<T> enumerator) {
      List<T> list = Pool<List<T>>.Spawn();
      try {
        while (enumerator.MoveNext()) {
          list.Add(enumerator.Current);
        }
        return new Query<T>(list);
      } finally {
        list.Clear();
        Pool<List<T>>.Recycle(list);
      }
    }

    /// <summary>
    /// Constructs a new Query from the given two dimensional array.
    /// </summary>
    public static Query<T> Query<T>(this T[,] array) {
      var dst = ArrayPool<T>.Spawn(array.GetLength(0) * array.GetLength(1));
      int dstIndex = 0;
      for (int i = 0; i < array.GetLength(0); i++) {
        for (int j = 0; j < array.GetLength(1); j++) {
          dst[dstIndex++] = array[i, j];
        }
      }

      return new Query<T>(dst, array.GetLength(0) * array.GetLength(1));
    }
  }

  /// <summary>
  /// A Query object is a type of immutable ordered collection of elements that can be 
  /// used to perform useful queries.  These queries are very similar to LINQ style
  /// queries, providing useful methods such as Where, Select, Concat, etc....
  /// 
  /// The Query struct and its interfaces use a pooling strategy backed by ArrayPool
  /// to incur an amortized cost of zero GC allocations.
  /// 
  /// A Query struct is immutable, and so cannot be modified once it has been created.
  /// You can use a query in few ways:
  ///  - The simplest way is to call an operator method such as Where or Select.  These
  ///    methods CONSUME the query to produce a new query.  Trying to use the original
  ///    query after it has been consumed will cause a runtime error.
  ///  - The next way is to call a collapsing operator, which will consume the query
  ///    and produce a non-query value or other side-effect.  Examples of collapsing 
  ///    operators are First, Last, or ElementAt.
  ///  - The last way to use a query is to Deconstruct it, by calling a Deconstruct
  ///    method to destroy the query and get access to its underlying data.  You
  ///    will be responsible for cleaning up or disposing the data you get.
  /// </summary>
  public struct Query<T> {
    private T[] _array;
    private int _count;

    private Validator _validator;

    /// <summary>
    /// Constructs a new query given a source array and a count.  The query assumes
    /// ownership of the array, so you should not use it or store it 
    /// after the query is constructed.
    /// </summary>
    public Query(T[] array, int count) {
      if (array == null) {
        throw new ArgumentNullException("array");
      }

      if (count < 0) {
        throw new ArgumentException("Count must be non-negative, but was " + count);
      }

      if (count > array.Length) {
        throw new ArgumentException("Count was " + count + " but the provided array only had a length of " + array.Length);
      }

      _array = array;
      _count = count;
      _validator = Validator.Spawn();
    }

    /// <summary>
    /// Constructs a new query of the given collection.
    /// </summary>
    public Query(ICollection<T> collection) {
      _array = ArrayPool<T>.Spawn(collection.Count);
      _count = collection.Count;
      collection.CopyTo(_array, 0);

      _validator = Validator.Spawn();
    }

    /// <summary>
    /// Constructs a query that is an exact copy of another query.
    /// </summary>
    public Query(Query<T> other) {
      other._validator.Validate();

      _array = ArrayPool<T>.Spawn(other._count);
      _count = other._count;
      Array.Copy(other._array, _array, _count);

      _validator = Validator.Spawn();
    }

    //Certain operators cannot be implemented as extension methods due to the way
    //the generic arguments are to be consumed by the user, so there are implemented
    //directly here in the Query class.
    #region DIRECT IMPLEMENTED OPERATORS

    /// <summary>
    /// Returns a new Query representing only the items of the current Query that
    /// are of a specific type.
    /// 
    /// For example
    ///   ("A", 1, null, 5.0f, 900, "hello").Query().OfType<string>()
    /// would result in
    ///   ("A", "hello")
    /// </summary>
    public Query<K> OfType<K>() where K : T {
      _validator.Validate();

      var dstArray = ArrayPool<K>.Spawn(_count);

      int dstCount = 0;
      for (int i = 0; i < _count; i++) {
        if (_array[i] is K) {
          dstArray[dstCount++] = (K)_array[i];
        }
      }

      Dispose();
      return new Query<K>(dstArray, dstCount);
    }

    /// <summary>
    /// Returns a new Query representing the current query sequence where each element is cast
    /// to a new type.
    /// </summary>
    public Query<K> Cast<K>() where K : class {
      return this.Select(item => item as K);
    }

    #endregion

    /// <summary>
    /// Disposes of the resources that this query holds.  The Query cannot be 
    /// used after this method is called.
    /// </summary>
    public void Dispose() {
      _validator.Validate();

      ArrayPool<T>.Recycle(_array);
      Validator.Invalidate(_validator);

      _array = null;
      _count = 0;
    }

    /// <summary>
    /// Deconstructs this Query into its base elements, the array and the count.
    /// The caller assumes ownership of the array and is responsible for managing
    /// its lifecycle.  The Query cannot be used after this method is called.
    /// </summary>
    public void Deconstruct(out T[] array, out int count) {
      _validator.Validate();

      array = _array;
      count = _count;

      Validator.Invalidate(_validator);
      _array = null;
      _count = 0;
    }

    /// <summary>
    /// Deconstructs this Query into a simple QuerySlice construct.  This is
    /// simply a utility overload of the regular Deconstruct method.  The
    /// user is still responsible for managing the memory lifecycle of the returned
    /// slice.  The Query cannot be used after this method is called.
    /// </summary>
    public QuerySlice Deconstruct() {
      T[] array;
      int count;
      Deconstruct(out array, out count);

      return new QuerySlice(array, count);
    }

    public Enumerator GetEnumerator() {
      _validator.Validate();

      T[] array;
      int count;
      Deconstruct(out array, out count);

      return new Enumerator(array, count);
    }

    public struct Enumerator : IEnumerator<T> {
      private T[] _array;
      private int _count;
      private int _nextIndex;

      public T Current { get; private set; }

      public Enumerator(T[] array, int count) {
        _array = array;
        _count = count;
        _nextIndex = 0;

        Current = default(T);
      }

      object IEnumerator.Current {
        get {
          if (_nextIndex == 0) {
            throw new InvalidOperationException();
          }

          return Current;
        }
      }

      public bool MoveNext() {
        if (_nextIndex >= _count) {
          return false;
        }

        Current = _array[_nextIndex++];
        return true;
      }

      public void Dispose() {
        ArrayPool<T>.Recycle(_array);
      }

      public void Reset() { throw new InvalidOperationException(); }
    }

    public struct QuerySlice : IDisposable {
      public readonly T[] BackingArray;
      public readonly int Count;

      public QuerySlice(T[] array, int count) {
        BackingArray = array;
        Count = count;
      }

      public T this[int index] {
        get {
          return BackingArray[index];
        }
      }

      public void Dispose() {
        ArrayPool<T>.Recycle(BackingArray);
      }
    }

    private struct Validator {
      private static int _nextId = 1;

      private Id _idRef;
      private int _idValue;

      public void Validate() {
        if (_idValue == 0) {
          throw new InvalidOperationException("This Query is not valid, you cannot construct a Query using the default constructor.");
        }

        if (_idRef == null || _idRef.value != _idValue) {
          throw new InvalidOperationException("This Query has already been disposed.  A Query can only be used once before it is automatically disposed.");
        }
      }

      public static Validator Spawn() {
        Id id = Pool<Id>.Spawn();
        id.value = _nextId++;

        return new Validator() {
          _idRef = id,
          _idValue = id.value
        };
      }

      public static void Invalidate(Validator validator) {
        validator._idRef.value = -1;
        Pool<Id>.Recycle(validator._idRef);
      }

      private class Id {
        public int value;
      }
    }
  }
}
