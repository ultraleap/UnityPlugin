using System;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public static class ExtensionSingles {

    public static bool All<T>(this Query<T> query, Func<T, bool> predicate) {
      using (var slice = query.Deconstruct()) {

        for (int i = 0; i < slice.Count; i++) {
          if (!predicate(slice[i])) {
            return false;
          }
        }
        return true;

      }
    }

    public static bool AllEqual<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count <= 1) {
          return true;
        }

        var a = slice[0];
        for (int i = 1; i < slice.Count; i++) {
          var b = slice[i];

          if ((a == null) != (b == null)) {
            return false;
          }

          if ((a == null) && (b == null)) {
            continue;
          }

          if (!a.Equals(b)) {
            return false;
          }
        }

        return true;
      }
    }

    public static bool Any<T>(this Query<T> query) {
      return query.Count() > 0;
    }

    public static bool Any<T>(this Query<T> query, Func<T, bool> predicate) {
      using (var slice = query.Deconstruct()) {
        for (int i = 0; i < slice.Count; i++) {
          if (predicate(slice[i])) {
            return true;
          }
        }

        return false;
      }
    }

    public static bool Contains<T>(this Query<T> query, T item) {
      T[] array;
      int count;
      query.Dispose(out array, out count);

      for (int i = 0; i < count; i++) {
        if (array[i].Equals(item)) {
          ArrayPool<T>.Recycle(array);
          return true;
        }
      }

      ArrayPool<T>.Recycle(array);
      return false;
    }

    public static int Count<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        return slice.Count;
      }
    }

    public static int Count<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).Count();
    }

    public static int CountUnique<T>(this Query<T> query) {
      var slice = query.Deconstruct();
      var set = Pool<HashSet<T>>.Spawn();
      try {

        for (int i = 0; i < slice.Count; i++) {
          set.Add(slice[i]);
        }
        return set.Count;

      } finally {
        slice.Dispose();
        set.Clear();
        Pool<HashSet<T>>.Recycle(set);
      }
    }

    public static int CountUnique<T, K>(this Query<T> query, Func<T, K> selector) {
      return query.Select(selector).CountUnique();
    }

    public static T ElementAt<T>(this Query<T> query, int index) {
      using (var slice = query.Deconstruct()) {
        if (index < 0 || index >= slice.Count) {
          throw new IndexOutOfRangeException("The index " + index + " was out of range.  Query only has length of " + slice.Count);
        }
        return slice[index];
      }
    }

    public static T ElementAtOrDefault<T>(this Query<T> query, int index) {
      using (var slice = query.Deconstruct()) {
        if (index < 0 || index >= slice.Count) {
          return default(T);
        }
        return slice[index];
      }
    }

    public static T First<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          throw new InvalidOperationException("The source Query was empty.");
        }
        return slice[0];
      }
    }

    public static T First<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).First();
    }

    public static T FirstOrDefault<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          return default(T);
        }
        return slice[0];
      }
    }

    public static T FirstOrDefault<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).FirstOrDefault();
    }

    public static Maybe<T> FirstOrNone<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          return Maybe.None;
        }
        return Maybe.Some(slice[0]);
      }
    }

    public static Maybe<T> FirstOrNone<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).FirstOrNone();
    }

    public static T Fold<T>(this Query<T> query, Func<T, T, T> foldFunc) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          throw new InvalidOperationException("The source Query was empty.");
        }

        var result = slice[0];
        for (int i = 1; i < slice.Count; i++) {
          result = foldFunc(result, slice[i]);
        }

        return result;
      }
    }

    public static int IndexOf<T>(this Query<T> query, T t) {
      using (var slice = query.Deconstruct()) {
        for (int i = 0; i < slice.Count; i++) {
          if (t == null) {
            if (slice[i] == null) {
              return i;
            }
          } else if (t.Equals(slice[i])) {
            return i;
          }
        }

        return -1;
      }
    }

    public static int IndexOf<T>(this Query<T> query, Func<T, bool> predicate) {
      using (var slice = query.Deconstruct()) {
        for (int i = 0; i < slice.Count; i++) {
          if (predicate(slice[i])) {
            return i;
          }
        }
        return -1;
      }
    }

    public static T Last<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          throw new InvalidOperationException("The source Query was empty.");
        } else {
          return slice[slice.Count - 1];
        }
      }
    }

    public static T Last<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).Last();
    }

    public static T LastOrDefault<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          return default(T);
        } else {
          return slice[slice.Count - 1];
        }
      }
    }

    public static Maybe<T> LastOrNone<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          return Maybe.None;
        } else {
          return Maybe.Some(slice[slice.Count - 1]);
        }
      }
    }

    public static Maybe<T> LastOrNone<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).LastOrNone();
    }

    public static T Max<T>(this Query<T> query) where T : IComparable<T> {
      return query.Fold((a, b) => a.CompareTo(b) > 0 ? a : b);
    }

    public static K Max<T, K>(this Query<T> query, Func<T, K> selector) where K : IComparable<K> {
      return query.Select(selector).Max();
    }

    public static T Min<T>(this Query<T> query) where T : IComparable<T> {
      return query.Fold((a, b) => a.CompareTo(b) < 0 ? a : b);
    }

    public static K Min<T, K>(this Query<T> query, Func<T, K> selector) where K : IComparable<K> {
      return query.Select(selector).Min();
    }

    public static T Single<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count != 1) {
          throw new InvalidOperationException("The Query had a count of " + slice.Count + " instead of a count of 1.");
        } else {
          return slice[0];
        }
      }
    }

    public static T Single<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).Single();
    }

    public static T SingleOrDefault<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count != 1) {
          return default(T);
        } else {
          return slice[0];
        }
      }
    }

    public static T SingleOrDefault<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).SingleOrDefault();
    }

    public static Maybe<T> SingleOrNone<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count != 1) {
          return Maybe.None;
        } else {
          return Maybe.Some(slice[0]);
        }
      }
    }

    public static Maybe<T> SingleOrNone<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).SingleOrNone();
    }

    public static T UniformOrDefault<T>(this Query<T> query) {
      return query.UniformOrNone().valueOrDefault;
    }

    public static Maybe<T> UniformOrNone<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          return Maybe.None;
        }

        var array = slice;
        T reference = array[0];

        for (int i = 1; i < slice.Count; i++) {
          if (reference == null) {
            if (array[i] != null) {
              return Maybe.None;
            }
          } else if (!reference.Equals(array[i])) {
            return Maybe.None;
          }
        }

        return Maybe.Some(reference);
      }
    }

    public static T[] ToArray<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        T[] result = new T[slice.Count];
        Array.Copy(slice.BackingArray, result, slice.Count);
        return result;
      }
    }

    public static void FillArray<T>(this Query<T> query, T[] array, int offset = 0) {
      using (var slice = query.Deconstruct()) {
        Array.Copy(slice.BackingArray, 0, array, offset, slice.Count);
      }
    }

    public static List<T> ToList<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        List<T> result = new List<T>(slice.Count);
        for (int i = 0; i < slice.Count; i++) {
          result.Add(slice[i]);
        }
        return result;
      }
    }

    public static void FillList<T>(this Query<T> query, List<T> list) {
      list.Clear();
      query.AppendList(list);
    }

    public static void AppendList<T>(this Query<T> query, List<T> list) {
      using (var slice = query.Deconstruct()) {
        for (int i = 0; i < slice.Count; i++) {
          list.Add(slice[i]);
        }
      }
    }

    public static HashSet<T> ToHashSet<T>(this Query<T> query) {
      HashSet<T> set = new HashSet<T>();
      query.AppendHashSet(set);
      return set;
    }

    public static void FillHashSet<T>(this Query<T> query, HashSet<T> set) {
      set.Clear();
      query.AppendHashSet(set);
    }

    public static void AppendHashSet<T>(this Query<T> query, HashSet<T> set) {
      using (var slice = query.Deconstruct()) {
        for (int i = 0; i < slice.Count; i++) {
          set.Add(slice[i]);
        }
      }
    }

    public static Dictionary<K, V> ToDictionary<T, K, V>(this Query<T> query, Func<T, K> keySelector, Func<T, V> valueSelector) {
      using (var slice = query.Deconstruct()) {
        Dictionary<K, V> dictionary = new Dictionary<K, V>();

        for (int i = 0; i < slice.Count; i++) {
          dictionary[keySelector(slice[i])] = valueSelector(slice[i]);
        }

        return dictionary;
      }
    }

    public static Dictionary<T, V> ToDictionary<T, V>(this Query<T> query, Func<T, V> valueSelector) {
      return query.ToDictionary(t => t, valueSelector);
    }
  }
}
