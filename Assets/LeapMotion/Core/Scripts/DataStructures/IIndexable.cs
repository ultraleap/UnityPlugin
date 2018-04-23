/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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

    public static Query<T> Query<T>(this IIndexable<T> indexable) {
      var arr = ArrayPool<T>.Spawn(indexable.Count);
      for (int i = 0; i < indexable.Count; i++) {
        arr[i] = indexable[i];
      }
      return new Query<T>(arr, indexable.Count);
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
