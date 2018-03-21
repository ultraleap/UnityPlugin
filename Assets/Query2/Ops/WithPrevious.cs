using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public partial struct Query<T> {

    public Query<PrevPair<T>> WithPrevious(int offset = 1, bool includeStart = false) {
      int resultCount = includeStart ? _count : Mathf.Max(0, _count - offset);
      var result = new Query<PrevPair<T>>(resultCount);
      var dstArray = result._data.array;
      var srcArray = _data.array;

      int dstIndex = 0;

      if (includeStart) {
        for (int i = 0; i < Mathf.Min(_count, offset); i++) {
          dstArray[dstIndex++] = new PrevPair<T>() {
            value = srcArray[i],
            prev = default(T),
            hasPrev = false
          };
        }
      }

      for (int i = offset; i < _count; i++) {
        dstArray[dstIndex++] = new PrevPair<T>() {
          value = srcArray[i],
          prev = srcArray[i - offset],
          hasPrev = true
        };
      }

      Dispose();
      return result;
    }
  }

  public static class WithPreviousExtension {
    public static Query<PrevPair<T>> WithPrevious<T>(this ICollection<T> collection, int offset = 1, bool includeStart = false) {
      return new Query<T>(collection).WithPrevious(offset, includeStart);
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