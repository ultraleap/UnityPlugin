using System;
using Leap.Unity.Query;
using System.Collections.Generic;

namespace Leap.Unity {

  public interface IIndexable<T> {

    T this[int idx] { get; }

    int Count { get; }

  }

  public static class IIndexableExtensions {

    public static IIndexableEnumerator<T> GetEnumerator<T>(this IIndexable<T> indexable) {
      return new IIndexableEnumerator<T>(indexable);
    }

    public static QueryWrapper<T, IIndexableEnumerator<T>> Query<T>(this IIndexable<T> indexable) {
      return new QueryWrapper<T, IIndexableEnumerator<T>>(GetEnumerator(indexable));
    }

  }

  public struct IIndexableEnumerator<T> : IQueryOp<T> {

    IIndexable<T> indexable;
    int index;

    public IIndexableEnumerator(IIndexable<T> indexable) {
      this.indexable = indexable;
      index = -1;
    }

    public IIndexableEnumerator<T> GetEnumerator() { return this; }

    public bool MoveNext() { if (indexable == null) return false;
                             index++; return index < indexable.Count; }

    public bool TryGetNext(out T t) {
      var hasNext = MoveNext();
      if (!hasNext) {
        t = default(T);
        return false;
      }
      else {
        t = Current;
        return true;
      }
    }

    public void Reset() {
      index = -1;
    }

    public T Current { get { return indexable[index]; } }

  }

}