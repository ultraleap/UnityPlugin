using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query2 {

  public static class DirectQueryExtensions {

    public static T Min<T>(this Query<T> query) where T : IComparable<T> {
      return query.Fold((a, b) => a.CompareTo(b) < 0 ? a : b);
    }

    public static K Min<T, K>(this Query<T> query, Func<T, K> selector) where K : IComparable<K> {
      return query.Select(selector).Min();
    }

    public static T Max<T>(this Query<T> query) where T : IComparable<T> {
      return query.Fold((a, b) => a.CompareTo(b) > 0 ? a : b);
    }

    public static K Max<T, K>(this Query<T> query, Func<T, K> selector) where K : IComparable<K> {
      return query.Select(selector).Max();
    }
  }
}
