using System;
using Leap.Unity.Query;
using System.Collections.Generic;

namespace Leap.Unity {

  /// <summary>
  /// This easy-to-implement interface represents the ability to index into a collection
  /// of elements of type T.
  /// 
  /// The other nice thing is that you get foreach and more complex Query support for any
  /// IIndexable if you call Query(). The Query call is allocation-free.
  /// 
  /// You can also get <code>foreach</code> directly on your Indexable implementer if you
  /// have it define a "GetEnumerator()" method that simply returns the result of the
  /// "GetEnumerator()" extension method provided for all Indexables. (Unfortunately,
  /// C# isn't smart enough to recognize the extension method automatically for the
  /// purposes of <code>foreach</code>.)
  /// </summary>
  /// <example>
  /// <code>foreach (var element in myIndexable.Query()) { /* ... */ }</code>
  /// </example>
  public interface IIndexable<T> {

    T this[int idx] { get; }

    int Count { get; }

  }

  public static class IIndexableExtensions {

    public static IIndexableEnumerator<T>
                    GetEnumerator<T>(this IIndexable<T> indexable) {
      return new IIndexableEnumerator<T>(indexable);
    }


    public static QueryWrapper<T, IIndexableEnumerator<T>>
                    Query<T>(this IIndexable<T> indexable) {
      return new QueryWrapper<T, IIndexableEnumerator<T>>(GetEnumerator(indexable));
    }

  }

  public struct IIndexableEnumerator<Element> : IQueryOp<Element> {

    IIndexable<Element> indexable;
    int index;

    public IIndexableEnumerator(IIndexable<Element> indexable) {
      this.indexable = indexable;
      index = -1;
    }

    public IIndexableEnumerator<Element> GetEnumerator() {
      return this;
    }

    public bool MoveNext() {
      if (indexable == null) return false;
      index++; return index < indexable.Count;
    }

    public bool TryGetNext(out Element t) {
      var hasNext = MoveNext();
      if (!hasNext) {
        t = default(Element);
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

    public Element Current { get { return indexable[index]; } }

  }

}