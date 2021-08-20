/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Query;
using System;

namespace Leap.Unity {

  /// <summary>
  /// This is a definition-friendly interface that new "indexable" struct definitions can
  /// implement to make it a little easier to implement foreach and Query() operations
  /// for their struct. (You can use the IndexableStructEnumerator for this purpose, you
  /// just have to pass it type arguments that correspond to your struct type.)
  /// 
  /// Unlike IIndexable, IIndexableStruct cannot utilize extension methods to
  /// automatically give consumers of the interface access to foreach and Query
  /// operations because consumption of a struct via an interface parameter forces the
  /// struct to be boxed, which causes allocation. As such, IIndexableStruct does not
  /// directly implement IIndexable.
  /// 
  /// (This all may change in C# 8 when we get traits, but Unity is still in the C# 4
  /// stone age.)
  /// </summary>
  public interface IIndexableStruct<T, ThisIndexableType>
                     where ThisIndexableType : struct,
                                               IIndexableStruct<T, ThisIndexableType> {

    T this[int idx] { get; }

    int Count { get; }

  }

  /// <summary>
  /// Explicit boxing class for IIndexableStructs that implements IIndexable.
  /// 
  /// This is useful when you need to pass an IIndexableStruct into a context that
  /// requires an IIndexable and you also need to avoid allocating any garbage. To avoid
  /// allocation, you can use the generic Pool to pool instances of this class and pass
  /// it around as an IIndexable.
  /// </summary>
  public class BoxedIndexableStruct<Element, IndexableStruct>
                 : IIndexable<Element>,
                   IPoolable
                 where IndexableStruct : struct,
                                         IIndexableStruct<Element, IndexableStruct> {

    /// <summary>
    /// The wrapped indexable struct, or null.
    /// </summary>
    public IndexableStruct? maybeIndexableStruct = null;

    public Element this[int idx] {
      get {
        if (!maybeIndexableStruct.HasValue) {
          throw new NullReferenceException(
            "PooledIndexableStructWrapper failed to index missing "
            + typeof(IndexableStruct).Name
            + "; did you assign its maybeIndexableStruct field?");
        }
        return maybeIndexableStruct.Value[idx];
      }
    }

    public int Count {
      get {
        if (!maybeIndexableStruct.HasValue) { return 0; }
        return maybeIndexableStruct.Value.Count;
      }
    }

    public void OnSpawn() { }

    public void OnRecycle() {
      maybeIndexableStruct = null;
    }
  }

  public static class BoxedIndexableStructExtensions {

    /// <summary>
    /// If you spawned this BoxedIndexableStruct from a Pool, you can call this method
    /// to recycle it back into the pool.
    /// 
    /// If you want to send an IIndexableStruct into a context that expects an
    /// IIndexable without boxing, you can "convert" it to an IIndexable without
    /// allocating by pooling the wrapper objects instead.
    /// 
    /// This extension method is short-hand for recycling a pooled wrapper around a
    /// struct. It should be called in the <code>finally</code> block after a
    /// <code>try</code> block uses the wrapper as an IIndexable. Be sure to use
    /// the Pool for the BoxedIndexableStruct to spawn the wrapper in the first place.
    /// </summary>
    public static void Recycle<Element,
                         IndexableStruct>(this BoxedIndexableStruct<Element,
                           IndexableStruct> pooledWrapper)
                             where IndexableStruct : struct,
                               IIndexableStruct<Element, IndexableStruct> {
      Pool<BoxedIndexableStruct<Element, IndexableStruct>>.Recycle(pooledWrapper);
    }

  }

  /// <summary>
  /// A two-generic-argument variant of an enumerator that allows an IIndexableStruct
  /// to quickly define an Enumerator that avoids allocation.
  /// </summary>
  public struct IndexableStructEnumerator<Element, IndexableStruct>
    where IndexableStruct : struct, IIndexableStruct<Element, IndexableStruct> {

    IndexableStruct? maybeIndexable;
    int index;

    public IndexableStructEnumerator(IndexableStruct indexable) {
      this.maybeIndexable = indexable;
      index = -1;
    }

    public IndexableStructEnumerator<Element, IndexableStruct> GetEnumerator() {
      return this;
    }

    public bool MoveNext() {
      if (!maybeIndexable.HasValue) return false;
      index++; return index < maybeIndexable.Value.Count;
    }

    public void Reset() {
      index = -1;
    }

    public Element Current { get { return maybeIndexable.Value[index]; } }
  }
}
