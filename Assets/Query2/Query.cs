using System;
using System.Collections.Generic;
using System.Collections;

namespace Leap.Unity.Query2 {

  public static class DeprecatedQueryExtension {

    [Obsolete("The Query call is no longer needed.", error: false)]
    public static ICollection<T> Query<T>(this ICollection<T> collection) {
      return collection;
    }
  }

  public partial struct Query<T> : IDisposable, IEnumerable<T>, ICollection<T> {
    private ArrayPool<T>.Box _data;
    private int _dataId;

    private int _count;

    private Query(int capacity) {
      _data = ArrayPool<T>.Spawn(capacity);
      _dataId = _data.id;
      _count = capacity;
    }

    public Query(ArrayPool<T>.Box data, int count) {
      _data = data;
      _dataId = _data.id;
      _count = count;
    }

    public Query(ICollection<T> collection) {
      _data = ArrayPool<T>.Spawn(collection.Count);
      _dataId = _data.id;
      _count = collection.Count;
      collection.CopyTo(_data.array, 0);
    }

    public Query(Query<T> other) {
      other.checkValid();

      _data = ArrayPool<T>.Spawn(other._count);
      _dataId = _data.id;
      _count = other._count;

      Array.Copy(other._data.array, _data.array, _count);
    }

    public void Dispose() {
      checkValid();

      ArrayPool<T>.Recycle(_data);
      _data = null;
      _count = 0;
    }

    public Enumerator GetEnumerator() {
      checkValid();
      return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() {
      return GetEnumerator();
    }

    private Query<T> copyDisposeOriginal() {
      checkValid();

      var copy = this;

      int nextId = ArrayPool<T>.nextId++;
      copy._dataId = nextId;
      copy._data.id = nextId;

      return copy;
    }

    private void checkValid() {
      if (_dataId == 0) {
        throw new InvalidOperationException("This Query is not valid, you cannot construct a Query using the default constructor.");
      }

      if (_data == null || _data.id != _dataId) {
        throw new InvalidOperationException("This Query has already been disposed.  A Query can only be used once before it is automatically disposed.");
      }
    }

    #region ICOLLECTION IMPLEMENTATION
    int ICollection<T>.Count {
      get {
        return _count;
      }
    }

    bool ICollection<T>.IsReadOnly {
      get {
        return true;
      }
    }

    public bool Contains(T item) {
      checkValid();
      using (this) {
        var array = _data.array;
        for (int i = 0; i < _count; i++) {
          if (array[i].Equals(item)) {
            return true;
          }
        }

        return false;
      }
    }

    public void CopyTo(T[] array, int arrayIndex) {
      checkValid();
      using (this) {
        Array.Copy(_data.array, 0, array, arrayIndex, _count);
      }
    }

    void ICollection<T>.Add(T item) {
      throw new InvalidOperationException();
    }

    void ICollection<T>.Clear() {
      throw new InvalidOperationException();
    }

    bool ICollection<T>.Remove(T item) {
      throw new InvalidOperationException();
    }
    #endregion

    public struct Enumerator : IEnumerator<T> {
      private Query<T> _query;
      private int _nextIndex;

      public T Current { get; private set; }

      public Enumerator(Query<T> query) {
        _query = query;
        _nextIndex = 0;
        Current = default(T);
      }

      object IEnumerator.Current {
        get {
          return Current;
        }
      }

      public bool MoveNext() {
        _query.checkValid();

        if (_nextIndex >= _query._count) {
          return false;
        }

        Current = _query._data.array[_nextIndex];
        _nextIndex++;
        return true;
      }

      public void Dispose() {
        _query.Dispose();
      }

      public void Reset() { throw new InvalidOperationException(); }
    }
  }
}
