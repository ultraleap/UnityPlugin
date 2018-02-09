using Leap.Unity.Query;

namespace Leap.Unity {

  /// <summary>
  /// This easy-to-implement interface represents the ability to index into a collection
  /// of elements of type T. IIndexables inherit Query() via an extension method.
  /// 
  /// IIndexable is fire-and-forget if your implementer is a reference type (class). If
  /// the implementing type is a struct, be mindful of boxing, and consider using
  /// IIndexableStruct and pooling instead.
  /// </summary>
  public interface IIndexable<T> {

    T this[int idx] { get; }

    int Count { get; }

  }

  public static class IIndexableExtensions {

    public static IndexableEnumerator<T> GetEnumerator<T>(this IIndexable<T> indexable) {
      return new IndexableEnumerator<T>(indexable);
    }

    /// <summary>
    /// Returns a QueryWrapper suitable for Query operations around this IIndexable.
    /// You can also call this to quickly declare a <code>foreach</code> statement over
    /// elements in the IIndexable, even if you don't actually call any Query operations.
    /// 
    /// If you call this method on a struct that implements IIndexable, the struct will
    /// be boxed, resulting in garbage allocation. Consider IIndexableStruct instead,
    /// which provides easy access to pooling methods to avoid allocation while still
    /// allowing a struct to be wrapped as an IIndexable.
    /// </summary>
    public static QueryWrapper<T, IndexableEnumerator<T>>
                    Query<T>(this IIndexable<T> indexable) {
      return new QueryWrapper<T, IndexableEnumerator<T>>(GetEnumerator(indexable));
    }

  }

  public struct IndexableEnumerator<Element> : IQueryOp<Element> {

    IIndexable<Element> indexable;
    int index;

    public IndexableEnumerator(IIndexable<Element> indexable) {
      this.indexable = indexable;
      index = -1;
    }

    public IndexableEnumerator<Element> GetEnumerator() {
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
