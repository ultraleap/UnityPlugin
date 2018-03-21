using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query {

  public static class ExtensionOperators {

    public static Query<T> Concat<T>(this Query<T> query, ICollection<T> collection) {
      using (var slice = query.Deconstruct()) {
        var dstArray = ArrayPool<T>.Spawn(slice.Count + collection.Count);

        slice.BackingArray.CopyTo(dstArray, 0);
        collection.CopyTo(dstArray, slice.Count);

        return new Query<T>(dstArray, slice.Count + collection.Count);
      }
    }

    public static Query<T> Concat<T>(this Query<T> query, Query<T> other) {
      using (var slice = query.Deconstruct())
      using (var otherSlice = other.Deconstruct()) {
        var dstArray = ArrayPool<T>.Spawn(slice.Count + otherSlice.Count);

        slice.BackingArray.CopyTo(dstArray, 0);
        otherSlice.BackingArray.CopyTo(dstArray, slice.Count);

        return new Query<T>(dstArray, slice.Count + otherSlice.Count);
      }
    }

    public static Query<T> OfType<T>(this Query<T> query, Type type) {
      using (var slice = query.Deconstruct()) {
        var dstArray = ArrayPool<T>.Spawn(slice.Count);

        int dstCount = 0;
        for (int i = 0; i < slice.Count; i++) {
          if (slice[i] != null &&
              type.IsAssignableFrom(slice[i].GetType())) {
            dstArray[dstCount++] = slice[i];
          }
        }

        return new Query<T>(dstArray, dstCount);
      }
    }

    public static Query<T> Repeat<T>(this Query<T> query, int times) {
      using (var slice = query.Deconstruct()) {
        var dstArray = ArrayPool<T>.Spawn(slice.Count * times);

        for (int i = 0; i < times; i++) {
          Array.Copy(slice.BackingArray, 0, dstArray, i * slice.Count, slice.Count);
        }

        return new Query<T>(dstArray, slice.Count * times);
      }
    }

    public static Query<K> Select<T, K>(this Query<T> query, Func<T, K> selector) {
      using (var slice = query.Deconstruct()) {
        var dstArray = ArrayPool<K>.Spawn(slice.Count);
        for (int i = 0; i < slice.Count; i++) {
          dstArray[i] = selector(slice[i]);
        }

        return new Query<K>(dstArray, slice.Count);
      }
    }

    public static Query<K> SelectMany<T, K>(this Query<T> query, Func<T, ICollection<K>> selector) {
      using (var slice = query.Deconstruct()) {
        int totalCount = 0;
        for (int i = 0; i < slice.Count; i++) {
          totalCount += selector(slice[i]).Count;
        }

        var dstArray = ArrayPool<K>.Spawn(totalCount);

        int targetIndex = 0;
        for (int i = 0; i < slice.Count; i++) {
          var collection = selector(slice[i]);
          collection.CopyTo(dstArray, targetIndex);
          targetIndex += collection.Count;
        }

        return new Query<K>(dstArray, totalCount);
      }
    }

    public static Query<K> SelectMany<T, K>(this Query<T> query, Func<T, Query<K>> selector) {
      using (var slice = query.Deconstruct()) {
        var slices = ArrayPool<Query<K>.QuerySlice>.Spawn(slice.Count);
        int totalCount = 0;
        for (int i = 0; i < slice.Count; i++) {
          slices[i] = selector(slice[i]).Deconstruct();
          totalCount += slices[i].Count;
        }

        var dstArray = ArrayPool<K>.Spawn(totalCount);

        int targetIndex = 0;
        for (int i = 0; i < slice.Count; i++) {
          slices[i].BackingArray.CopyTo(dstArray, targetIndex);
          targetIndex += slices[i].Count;
          slices[i].Dispose();
        }

        ArrayPool<Query<K>.QuerySlice>.Recycle(slices);

        return new Query<K>(dstArray, totalCount);
      }
    }

    public static Query<T> Skip<T>(this Query<T> query, int toSkip) {
      T[] array;
      int count;
      query.Dispose(out array, out count);

      //TODO: write unit tests for all operations to make sure excess array is cleared
      int resultCount = Mathf.Max(count - toSkip, 0);
      Array.Copy(array, toSkip, array, 0, resultCount);
      Array.Clear(array, resultCount, array.Length - resultCount);

      return new Query<T>(array, resultCount);
    }

    public static Query<T> SkipWhile<T>(this Query<T> query, Func<T, bool> predicate) {
      T[] array;
      int count;
      query.Dispose(out array, out count);

      int toSkip = 0;
      while (toSkip < count) {
        if (predicate(array[toSkip])) {
          toSkip++;
        } else {
          break;
        }
      }

      int resultCount = count - toSkip;
      Array.Copy(array, toSkip, array, 0, resultCount);
      Array.Clear(array, resultCount, array.Length - resultCount);

      return new Query<T>(array, resultCount);
    }

    public static Query<T> Take<T>(this Query<T> query, int toTake) {
      T[] array;
      int count;
      query.Dispose(out array, out count);

      count = Mathf.Min(count, toTake);
      Array.Clear(array, count, array.Length - count);

      return new Query<T>(array, count);
    }

    public static Query<T> TakeWhile<T>(this Query<T> query, Func<T, bool> predicate) {
      T[] array;
      int count;
      query.Dispose(out array, out count);

      int takeCount;
      for (takeCount = 0; takeCount < count; takeCount++) {
        if (!predicate(array[takeCount])) {
          break;
        }
      }

      Array.Clear(array, takeCount, array.Length - takeCount);

      return new Query<T>(array, takeCount);
    }

    public static Query<T> Where<T>(this Query<T> query, Func<T, bool> predicate) {
      T[] array;
      int count;
      query.Dispose(out array, out count);

      int writeIndex = 0;
      for (int i = 0; i < count; i++) {
        if (predicate(array[i])) {
          array[writeIndex++] = array[i];
        }
      }

      Array.Clear(array, writeIndex, array.Length - writeIndex);

      return new Query<T>(array, writeIndex);
    }

    public static Query<T> ValidUnityObjs<T>(this ICollection<T> collection) where T : UnityEngine.Object {
      return new Query<T>(collection).ValidUnityObjs();
    }

    public static Query<T> ValidUnityObjs<T>(this Query<T> query) where T : UnityEngine.Object {
      return query.Where(t => {
        UnityEngine.Object obj = t;
        return obj != null;
      });
    }

    public static Query<PrevPair<T>> WithPrevious<T>(this Query<T> query, int offset = 1, bool includeStart = false) {
      using (var slice = query.Deconstruct()) {
        int resultCount = includeStart ? slice.Count : Mathf.Max(0, slice.Count - offset);
        var dstArray = ArrayPool<PrevPair<T>>.Spawn(resultCount);

        int dstIndex = 0;

        if (includeStart) {
          for (int i = 0; i < Mathf.Min(slice.Count, offset); i++) {
            dstArray[dstIndex++] = new PrevPair<T>() {
              value = slice[i],
              prev = default(T),
              hasPrev = false
            };
          }
        }

        for (int i = offset; i < slice.Count; i++) {
          dstArray[dstIndex++] = new PrevPair<T>() {
            value = slice[i],
            prev = slice[i - offset],
            hasPrev = true
          };
        }

        return new Query<PrevPair<T>>(dstArray, resultCount);
      }
    }

    public static Query<V> Zip<T, K, V>(this Query<T> query, ICollection<K> collection, Func<T, K, V> selector) {
      using (var slice = query.Deconstruct()) {
        int resultCount = Mathf.Min(slice.Count, collection.Count);
        var resultArray = ArrayPool<V>.Spawn(resultCount);

        var tmpArray = ArrayPool<K>.Spawn(collection.Count);
        collection.CopyTo(tmpArray, 0);

        for (int i = 0; i < resultCount; i++) {
          resultArray[i] = selector(slice[i], tmpArray[i]);
        }

        ArrayPool<K>.Recycle(tmpArray);

        return new Query<V>(resultArray, resultCount);
      }
    }

    public static Query<V> Zip<T, K, V>(this Query<T> query, Query<K> otherQuery, Func<T, K, V> selector) {
      using (var slice = query.Deconstruct())
      using (var otherSlice = otherQuery.Deconstruct()) {
        int resultCount = Mathf.Min(slice.Count, otherSlice.Count);
        var resultArray = ArrayPool<V>.Spawn(resultCount);

        for (int i = 0; i < resultCount; i++) {
          resultArray[i] = selector(slice[i], otherSlice[i]);
        }

        return new Query<V>(resultArray, resultCount);
      }
    }

    public struct PrevPair<T> {
      /// <summary>
      /// The current element of the sequence
      /// </summary>
      public T value;

      /// <summary>
      /// If hasPrev is true, the element that came before value
      /// </summary>
      public T prev;

      /// <summary>
      /// Does the prev field represent a previous value?  If false,
      /// prev will take the default value of T.
      /// </summary>
      public bool hasPrev;
    }
  }
}
