using Leap.Unity.Query;
using System;

namespace Leap.Unity {

  /// <summary>
  /// This is a definition-friendly interface that new "indexable" struct definitions can
  /// implement to inherit a pooling strategy via extension methods that allow the
  /// structs to be 'passed' to IIndexable interfaces without allocation.
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
  /// Wrapper class for using pooled boxes around IIndexableStructs, useful when you
  /// need to pass an IIndexableStruct into a context that requires an IIndexable and
  /// you also need to avoid allocating any garbage.
  /// </summary>
  public class PooledIndexableStructWrapper<Element, IndexableStruct>
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

  public static class IndexableStructWrapperExtensions {

    /// <summary>
    /// If you want to send an IIndexableStruct into a context that expects an
    /// IIndexable, you can use a pooled wrapper around the struct. This method allocates
    /// by explicitly boxing the struct. If you want to avoid allocation, use the pooling
    /// strategy afforded by SpawnPooledWrapper() and RecyclePooledWrapper().
    /// </summary>
    public static PooledIndexableStructWrapper<Element, IndexableStruct>
                    AllocateIndexableWrapper<Element,
                      IndexableStruct>(this IndexableStruct indexableStruct)
                        where IndexableStruct : struct,
                          IIndexableStruct<Element, IndexableStruct> {
      var pooledWrapper = new PooledIndexableStructWrapper<Element, IndexableStruct>();
      pooledWrapper.maybeIndexableStruct = indexableStruct;
      return pooledWrapper;
    }

    /// <summary>
    /// If you want to send an IIndexableStruct into a context that expects an
    /// IIndexable without boxing, you can "convert" it to an IIndexable without
    /// allocating by pooling the wrapper objects instead.
    /// 
    /// This extension method is short-hand for spawning a pooled wrapper around the
    /// struct; pass the wrapper as the IIndexable for your operation within a
    /// <code>try</code> block, and don't forget to call RecyclePooledWrapper() in a
    /// <code>finally</code> block afterward.
    /// </summary>
    public static PooledIndexableStructWrapper<Element, IndexableStruct>
                    SpawnPooledWrapper<Element,
                      IndexableStruct>(this IndexableStruct indexableStruct)
                        where IndexableStruct : struct,
                          IIndexableStruct<Element, IndexableStruct> {
      var pooledWrapper = Pool<PooledIndexableStructWrapper<Element,
                                                            IndexableStruct>>.Spawn();
      pooledWrapper.maybeIndexableStruct = indexableStruct;
      return pooledWrapper;
    }

    /// <summary>
    /// If you want to send an IIndexableStruct into a context that expects an
    /// IIndexable without boxing, you can "convert" it to an IIndexable without
    /// allocating by pooling the wrapper objects instead.
    /// 
    /// This extension method is short-hand for recycling a pooled wrapper around a
    /// struct. It should be called in the <code>finally</code> block after a
    /// <code>try</code> block uses the wrapper as an IIndexable. Be sure to use
    /// SpawnPooledWrapper() to spawn the wrapper from the Pool in the first place.
    /// </summary>
    public static void RecyclePooledWrapper<Element,
                         IndexableStruct>(this PooledIndexableStructWrapper<
                           Element, IndexableStruct> pooledWrapper)
                             where IndexableStruct : struct,
                               IIndexableStruct<Element, IndexableStruct> {
      Pool<PooledIndexableStructWrapper<Element,
                                        IndexableStruct>>.Recycle(pooledWrapper);
    }

  }

  /// <summary>
  /// A two-generic-argument variant of an enumerator that allows an IIndexableStruct
  /// to quickly define an Enumerator that avoids allocation.
  /// </summary>
  public struct IndexableStructEnumerator<Element, IndexableStruct>
                  : IQueryOp<Element>
                  where IndexableStruct : struct,
                                          IIndexableStruct<Element, IndexableStruct> {

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

    public Element Current { get { return maybeIndexable.Value[index]; } }

  }

}
