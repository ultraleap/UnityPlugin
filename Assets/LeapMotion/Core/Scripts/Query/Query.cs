using System;
using System.Collections.Generic;
using System.Collections;

namespace Leap.Unity.Query {

  public static class QueryConversionExtensions {

    public static Query<T> Query<T>(this ICollection<T> collection) {
      return new Query<T>(collection);
    }

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

    public static Query<T> Query<T>(this IEnumerator<T> enumerator) {
      List<T> list = Pool<List<T>>.Spawn();
      try {
        while (enumerator.MoveNext()) {
          list.Add(enumerator.Current);
        }
        return new Query<T>(list);
      } finally {
        Pool<List<T>>.Recycle(list);
      }
    }

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

  public partial struct Query<T> {
    private T[] _array;
    private int _count;

    private Validator _validator;

    public Query(T[] array, int count) {
      _array = array;
      _count = count;
      _validator = Validator.Spawn();
    }

    public Query(ICollection<T> collection) {
      _array = ArrayPool<T>.Spawn(collection.Count);
      _count = collection.Count;
      collection.CopyTo(_array, 0);

      _validator = Validator.Spawn();
    }

    public Query(Query<T> other) {
      other._validator.Validate();

      _array = ArrayPool<T>.Spawn(other._count);
      _count = other._count;
      Array.Copy(other._array, _array, _count);

      _validator = Validator.Spawn();
    }

    public void Dispose() {
      _validator.Validate();

      ArrayPool<T>.Recycle(_array);
      Validator.Invalidate(_validator);

      _array = null;
      _count = 0;
    }

    public void Dispose(out T[] array, out int count) {
      _validator.Validate();

      array = _array;
      count = _count;

      Validator.Invalidate(_validator);
      _array = null;
      _count = 0;
    }

    public QuerySlice Deconstruct() {
      T[] array;
      int count;
      Dispose(out array, out count);

      return new QuerySlice(array, count);
    }

    public Enumerator GetEnumerator() {
      _validator.Validate();

      T[] array;
      int count;
      Dispose(out array, out count);

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
