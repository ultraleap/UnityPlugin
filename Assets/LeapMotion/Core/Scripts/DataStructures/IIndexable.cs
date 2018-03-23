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
  }

  public struct IndexableEnumerator<Element> {
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

    public void Reset() {
      index = -1;
    }

    public Element Current { get { return indexable[index]; } }

  }

}
