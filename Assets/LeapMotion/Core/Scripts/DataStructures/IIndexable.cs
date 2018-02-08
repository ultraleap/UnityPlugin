using System;
using Leap.Unity.Query;
using System.Collections.Generic;

namespace Leap.Unity {

  /// <summary>
  /// This easy-to-implement interface represents the ability to index into a collection
  /// of elements of type T.
  /// 
  /// Optionally, it's also very easy to add allocation-free Query() and
  /// <code>foreach</code> support to your IIndexable, even if your implementing type is
  /// a struct.
  /// 
  /// (Extension methods on any IIndexable enables Query and GetEnumerator calls, but C#
  /// limitations prevent allocation-free type inference in this case.)
  /// 
  /// To support <code>foreach</code>, define a public GetEnumerator() method that
  /// returns an IIndexableEnumerator:
  /// <example>
  /// public IIndexableEnumerator‹T, MyIndexable‹T›› GetEnumerator() {
  ///   return this.GetEnumerator‹T, MyIndexable‹T››();
  /// }
  /// </example>
  /// 
  /// To support Query()ing the IIndexable, define a Query() method that returns a
  /// QueryWrapper over the IIndexableEnumerator:
  /// <example>
  /// public QueryWrapper‹T, IIndexableEnumerator‹T, MyIndexable‹T››› Query() {
  ///   return this.Query‹T, Slice‹T››();
  /// }
  /// </example>
  /// 
  /// </summary>
  public interface IIndexable<T> {

    T this[int idx] { get; }

    int Count { get; }

  }

  public static class IIndexableExtensions {

    public static IIndexableEnumerator<Element, Indexable>
                    GetEnumerator<Element, Indexable>(this Indexable indexable)
                      where Indexable : IIndexable<Element> {
      return new IIndexableEnumerator<Element, Indexable>(indexable);
    }

    public static QueryWrapper<Element, IIndexableEnumerator<Element, Indexable>>
                    Query<Element, Indexable>(this Indexable indexable)
                      where Indexable : IIndexable<Element> {
      return new QueryWrapper<Element, IIndexableEnumerator<Element, Indexable>>(
                   GetEnumerator<Element, Indexable>(indexable));
    }

  }

  public struct IIndexableEnumerator<Element, IndexableOverElement>
                  : IQueryOp<Element>
                  where IndexableOverElement : IIndexable<Element> {

    IndexableOverElement indexable;
    int index;

    public IIndexableEnumerator(IndexableOverElement indexable) {
      this.indexable = indexable;
      index = -1;
    }

    public IIndexableEnumerator<Element, IndexableOverElement> GetEnumerator() {
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