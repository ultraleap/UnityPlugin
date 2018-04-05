/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;

namespace Leap.Unity.Query {

  public static class QueryCollapseExtensions {

    /// <summary>
    /// Returns true if all elements in the Query satisfy the predicate.
    /// </summary>
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

    /// <summary>
    /// Returns true if all elements in the Query are equal to the same value.
    /// Will always return true for Queries with one or zero elements.
    /// </summary>
    public static bool AllEqual<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count <= 1) {
          return true;
        }

        var comparer = EqualityComparer<T>.Default;

        var first = slice[0];
        for (int i = 1; i < slice.Count; i++) {
          if (!comparer.Equals(first, slice[i])) {
            return false;
          }
        }

        return true;
      }
    }

    /// <summary>
    /// Returns true if the Query has any elements in it.
    /// </summary>
    public static bool Any<T>(this Query<T> query) {
      return query.Count() > 0;
    }

    /// <summary>
    /// Returns true if any elements in the Query satisfy the predicate.
    /// </summary>
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

    /// <summary>
    /// Returns the average of a Query of floats.
    /// </summary>
    public static float Average(this Query<float> query) {
      using (var slice = query.Deconstruct()) {
        float sum = 0;
        for (int i = 0; i < slice.Count; i++) {
          sum += slice[i];
        }
        return sum / slice.Count;
      }
    }

    /// <summary>
    /// Returns the average of a Query of doubles.
    /// </summary>
    public static double Average(this Query<double> query) {
      using (var slice = query.Deconstruct()) {
        double sum = 0;
        for (int i = 0; i < slice.Count; i++) {
          sum += slice[i];
        }
        return sum / slice.Count;
      }
    }

    /// <summary>
    /// Returns true if any element in the Query is equal to a specific value.
    /// </summary>
    public static bool Contains<T>(this Query<T> query, T item) {
      T[] array;
      int count;
      query.Deconstruct(out array, out count);

      var comparer = EqualityComparer<T>.Default;
      for (int i = 0; i < count; i++) {
        if (comparer.Equals(item, array[i])) {
          ArrayPool<T>.Recycle(array);
          return true;
        }
      }

      ArrayPool<T>.Recycle(array);
      return false;
    }

    /// <summary>
    /// Returns the number of elements in the Query.
    /// </summary>
    public static int Count<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        return slice.Count;
      }
    }

    /// <summary>
    /// Returns the number of elements in the Query that satisfy a predicate.
    /// </summary>
    public static int Count<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).Count();
    }

    /// <summary>
    /// Counts the number of distinct elements in the Query.
    /// </summary>
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

    /// <summary>
    /// Returns the number of distinct elements in the Query once it has been mapped
    /// using a selector function.
    /// </summary>
    public static int CountUnique<T, K>(this Query<T> query, Func<T, K> selector) {
      return query.Select(selector).CountUnique();
    }

    /// <summary>
    /// Returns the element at a specific index in the Query.  Will throw an error
    /// if the Query has no element at that index.
    /// </summary>
    public static T ElementAt<T>(this Query<T> query, int index) {
      using (var slice = query.Deconstruct()) {
        if (index < 0 || index >= slice.Count) {
          throw new IndexOutOfRangeException("The index " + index + " was out of range.  Query only has length of " + slice.Count);
        }
        return slice[index];
      }
    }

    /// <summary>
    /// Returns the element at a specific index in the Query.  Will return
    /// the default value if the Query has no element at that index.
    /// </summary>
    public static T ElementAtOrDefault<T>(this Query<T> query, int index) {
      using (var slice = query.Deconstruct()) {
        if (index < 0 || index >= slice.Count) {
          return default(T);
        }
        return slice[index];
      }
    }

    /// <summary>
    /// Returns the first element in the Query.  Will throw an error if there are
    /// no elements in the Query.
    /// </summary>
    public static T First<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          throw new InvalidOperationException("The source Query was empty.");
        }
        return slice[0];
      }
    }

    /// <summary>
    /// Returns the first element in the Query that satisfies a predicate.  Will
    /// throw an error if there is no such element.
    /// </summary>
    public static T First<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).First();
    }

    /// <summary>
    /// Returns the first element in the Query.  Will return the default value
    /// if the Query is empty.
    /// </summary>
    public static T FirstOrDefault<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          return default(T);
        }
        return slice[0];
      }
    }

    /// <summary>
    /// Returns the first element in the Query that satisfies a predicate.  Will return
    /// the default value if there is no such element.
    /// </summary>
    public static T FirstOrDefault<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).FirstOrDefault();
    }

    /// <summary>
    /// Returns Some value that represents the first value in the Query, or None
    /// if there is no such value.
    /// </summary>
    public static Maybe<T> FirstOrNone<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          return Maybe.None;
        }
        return Maybe.Some(slice[0]);
      }
    }

    /// <summary>
    /// Returns the Some value representing the first value that satisfies the predicate,
    /// or None if there is no such value.
    /// </summary>
    public static Maybe<T> FirstOrNone<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).FirstOrNone();
    }

    /// <summary>
    /// Folds all of the elements in the Query into a single element, using a fold function.
    /// Will throw an error if there are no elements in the Query.
    /// 
    /// The fold function takes in the current folded value, and the next item to fold in.
    /// It returns the result of folding the item into the current folded value.  For example,
    /// you can use the Fold operation to implement a sum:
    /// 
    /// var sum = numbers.Query().Fold((a,b) => a + b);
    /// </summary>
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

    /// <summary>
    /// Returns the index of the first element that is equal to a specific value.  Will return
    /// a negative index if there is no such element.
    /// </summary>
    public static int IndexOf<T>(this Query<T> query, T t) {
      using (var slice = query.Deconstruct()) {
        var comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < slice.Count; i++) {
          if (comparer.Equals(t, slice[i])) {
            return i;
          }
        }

        return -1;
      }
    }

    /// <summary>
    /// Returns the index of the first element to satisfy a predicate.  Will return a negative
    /// index if there is no such element.
    /// </summary>
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

    /// <summary>
    /// Returns the last element in the Query.  Will throw an error if the Query is empty.
    /// </summary>
    public static T Last<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          throw new InvalidOperationException("The source Query was empty.");
        } else {
          return slice[slice.Count - 1];
        }
      }
    }

    /// <summary>
    /// Returns the last element in the Query that satisfies a predicate.  Will throw an error
    /// if there is no such element.
    /// </summary>
    public static T Last<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).Last();
    }

    /// <summary>
    /// Returns the last element in the Query.  Will return the default value if the Query is empty.
    /// </summary>
    public static T LastOrDefault<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          return default(T);
        } else {
          return slice[slice.Count - 1];
        }
      }
    }

    /// <summary>
    /// Returns the last element in the Query that satisfies a predicate.  Will return
    /// the default value if there is no such element.
    /// </summary>
    public static T LastOrDefault<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).LastOrDefault();
    }

    /// <summary>
    /// Returns Some value that represents the last value in the Query, or None
    /// if there is no such value.
    /// </summary>
    public static Maybe<T> LastOrNone<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          return Maybe.None;
        } else {
          return Maybe.Some(slice[slice.Count - 1]);
        }
      }
    }

    /// <summary>
    /// Returns the Some value representing the last value that satisfies the predicate,
    /// or None if there is no such value.
    /// </summary>
    public static Maybe<T> LastOrNone<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).LastOrNone();
    }

    /// <summary>
    /// Returns the largest element in the Query.
    /// </summary>
    public static T Max<T>(this Query<T> query) where T : IComparable<T> {
      return query.Fold(FoldDelegate<T>.max);
    }

    /// <summary>
    /// Returns the largest element in the Query after it has been mapped using a selector function.
    /// </summary>
    public static K Max<T, K>(this Query<T> query, Func<T, K> selector) where K : IComparable<K> {
      return query.Select(selector).Max();
    }

    /// <summary>
    /// Returns the smallest element in the Query.
    /// </summary>
    public static T Min<T>(this Query<T> query) where T : IComparable<T> {
      return query.Fold(FoldDelegate<T>.min);
    }

    /// <summary>
    /// Returns the smallest element in the Query after it has been mapped using a selector function.
    /// </summary>
    public static K Min<T, K>(this Query<T> query, Func<T, K> selector) where K : IComparable<K> {
      return query.Select(selector).Min();
    }

    /// <summary>
    /// Returns the first and only element in the Query.  Will throw an error if the length of the 
    /// Query is anything other than 1.
    /// </summary>
    public static T Single<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count != 1) {
          throw new InvalidOperationException("The Query had a count of " + slice.Count + " instead of a count of 1.");
        } else {
          return slice[0];
        }
      }
    }

    /// <summary>
    /// Returns the first and only element in the Query that satisfies the predicate.  Will throw
    /// an error if the number of such elements is anything other than 1.
    /// </summary>
    public static T Single<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).Single();
    }

    /// <summary>
    /// Returns the first and only element in the Query.  Will return the default value if the number
    /// of elements is anything other than 1.
    /// </summary>
    public static T SingleOrDefault<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count != 1) {
          return default(T);
        } else {
          return slice[0];
        }
      }
    }

    /// <summary>
    /// Returns the first and only element in the Query that satisfies the predicate.  Will return
    /// the default value if the number of such elements is anything other than 1.
    /// </summary>
    public static T SingleOrDefault<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).SingleOrDefault();
    }

    /// <summary>
    /// Returns the first and only element in the Query.  Will return None if the number
    /// of elements is anything other than 1.
    /// </summary>
    public static Maybe<T> SingleOrNone<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count != 1) {
          return Maybe.None;
        } else {
          return Maybe.Some(slice[0]);
        }
      }
    }

    /// <summary>
    /// Returns the first and only element in the Query that satisfies the predicate.  Will return
    /// None if the number of such elements is anything other than 1.
    /// </summary>
    public static Maybe<T> SingleOrNone<T>(this Query<T> query, Func<T, bool> predicate) {
      return query.Where(predicate).SingleOrNone();
    }

    /// <summary>
    /// Returns the sum of a Query of ints.
    /// </summary>
    public static int Sum(this Query<int> query) {
      return query.Fold((a, b) => a + b);
    }

    /// <summary>
    /// Returns the sum of a Query of floats.
    /// </summary>
    public static float Sum(this Query<float> query) {
      return query.Fold((a, b) => a + b);
    }

    /// <summary>
    /// Returns the sum of a Query of doubles.
    /// </summary>
    public static double Sum(this Query<double> query) {
      return query.Fold((a, b) => a + b);
    }

    /// <summary>
    /// Returns the single value that is present in the entire Query.  If there is more
    /// than one value in the Query or there are no values at all, this method will return
    /// the default value.
    /// </summary>
    public static T UniformOrDefault<T>(this Query<T> query) {
      return query.UniformOrNone().valueOrDefault;
    }

    /// <summary>
    /// Returns Some single value that is present in the entire Query.  If there is more
    /// than one value in the Query or there are no values at all, this method will return
    /// None.
    /// </summary>
    public static Maybe<T> UniformOrNone<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        if (slice.Count == 0) {
          return Maybe.None;
        }

        var array = slice;
        T reference = array[0];

        var comparer = EqualityComparer<T>.Default;
        for (int i = 1; i < slice.Count; i++) {
          if (!comparer.Equals(reference, slice[i])) {
            return Maybe.None;
          }
        }

        return Maybe.Some(reference);
      }
    }

    /// <summary>
    /// Converts the Query into an array.
    /// </summary>
    public static T[] ToArray<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        T[] result = new T[slice.Count];
        Array.Copy(slice.BackingArray, result, slice.Count);
        return result;
      }
    }

    /// <summary>
    /// Copies the elements of the Query into an array.  Can optionally specify the offset into the array
    /// where to copy.
    /// </summary>
    public static void FillArray<T>(this Query<T> query, T[] array, int offset = 0) {
      using (var slice = query.Deconstruct()) {
        Array.Copy(slice.BackingArray, 0, array, offset, slice.Count);
      }
    }

    /// <summary>
    /// Converts the Query into a list.
    /// </summary>
    public static List<T> ToList<T>(this Query<T> query) {
      using (var slice = query.Deconstruct()) {
        List<T> result = new List<T>(slice.Count);
        for (int i = 0; i < slice.Count; i++) {
          result.Add(slice[i]);
        }
        return result;
      }
    }

    /// <summary>
    /// Fills a given list with the elements in this Query.  The list is cleared before the fill happens.
    /// </summary>
    public static void FillList<T>(this Query<T> query, List<T> list) {
      list.Clear();
      query.AppendList(list);
    }

    /// <summary>
    /// Appends the elements in this Query to the end of a given list.
    /// </summary>
    public static void AppendList<T>(this Query<T> query, List<T> list) {
      using (var slice = query.Deconstruct()) {
        for (int i = 0; i < slice.Count; i++) {
          list.Add(slice[i]);
        }
      }
    }

    /// <summary>
    /// Converts the Query into a HashSet.
    /// </summary>
    public static HashSet<T> ToHashSet<T>(this Query<T> query) {
      HashSet<T> set = new HashSet<T>();
      query.AppendHashSet(set);
      return set;
    }

    /// <summary>
    /// Fills a given HashSet with the elements in the Query.  The set is cleared before the fill happens.
    /// </summary>
    public static void FillHashSet<T>(this Query<T> query, HashSet<T> set) {
      set.Clear();
      query.AppendHashSet(set);
    }

    /// <summary>
    /// Appends the elements in this Query into the given HashSet.
    /// </summary>
    public static void AppendHashSet<T>(this Query<T> query, HashSet<T> set) {
      using (var slice = query.Deconstruct()) {
        for (int i = 0; i < slice.Count; i++) {
          set.Add(slice[i]);
        }
      }
    }

    /// <summary>
    /// Converts the Query into a Dictionary using a specific key selector and value selector.
    /// </summary>
    public static Dictionary<K, V> ToDictionary<T, K, V>(this Query<T> query, Func<T, K> keySelector, Func<T, V> valueSelector) {
      using (var slice = query.Deconstruct()) {
        Dictionary<K, V> dictionary = new Dictionary<K, V>();

        for (int i = 0; i < slice.Count; i++) {
          dictionary[keySelector(slice[i])] = valueSelector(slice[i]);
        }

        return dictionary;
      }
    }

    /// <summary>
    /// Converts the Query into a Dictionary using the query elements as keys, and using a value selector
    /// to select the value tied to the key.
    /// </summary>
    public static Dictionary<T, V> ToDictionary<T, V>(this Query<T> query, Func<T, V> valueSelector) {
      return query.ToDictionary(t => t, valueSelector);
    }

    private static class FoldDelegate<T> where T : IComparable<T> {
      public readonly static Func<T, T, T> max = (a, b) => a.CompareTo(b) > 0 ? a : b;
      public readonly static Func<T, T, T> min = (a, b) => a.CompareTo(b) < 0 ? a : b;
    }
  }
}
