using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query {

  public static class FillExtensions {

    public static T[] Fill<T>(this T[] array, T value) {
      for (int i = 0; i < array.Length; i++) {
        array[i] = value;
      }
      return array;
    }

    public static T[] Fill<T>(this T[] array, Func<T> constructor) {
      for (int i = 0; i < array.Length; i++) {
        array[i] = constructor();
      }
      return array;
    }

    public static T[,] Fill<T>(this T[,] array, T value) {
      for (int i = 0; i < array.GetLength(0); i++) {
        for (int j = 0; j < array.GetLength(1); j++) {
          array[i, j] = value;
        }
      }
      return array;
    }

    public static List<T> Fill<T>(this List<T> list, T value) {
      for (int i = 0; i < list.Count; i++) {
        list[i] = value;
      }
      return list;
    }

    public static List<T> Fill<T>(this List<T> list, int count, T value) {
      list.Clear();
      for (int i = 0; i < count; i++) {
        list.Add(value);
      }
      return list;
    }

    public static List<T> FillEach<T>(this List<T> list, Func<T> generator) {
      for (int i = 0; i < list.Count; i++) {
        list[i] = generator();
      }
      return list;
    }

    public static List<T> FillEach<T>(this List<T> list, int count, Func<T> generator) {
      list.Clear();
      for (int i = 0; i < count; i++) {
        list.Add(generator());
      }
      return list;
    }

    public static List<T> Append<T>(this List<T> list, int count, T value) {
      for (int i = 0; i < count; i++) {
        list.Add(value);
      }
      return list;
    }

    public static T RemoveLast<T>(this List<T> list) {
      T last = list[list.Count - 1];
      list.RemoveAt(list.Count - 1);
      return last;
    }

    /// <summary>
    /// If the element exists in the list, the first instance is replaced 
    /// with the last element of the list.
    /// </summary>
    public static bool RemoveUnordered<T>(this List<T> list, T element) {
      for (int i = 0; i < list.Count; i++) {
        if (list[i].Equals(element)) {
          list[i] = list.RemoveLast();
          return true;
        }
      }
      return false;
    }

    /// <summary>
    /// Removes each element references by the 'sortedIndexes' list.  This
    /// is much faster than calling RemoveAt for each index individually.
    /// </summary>
    public static void RemoveAtMany<T>(this List<T> list, List<int> sortedIndexes) {
      if (sortedIndexes.Count == 0) return;

      int to = sortedIndexes[0];
      int from = to;
      int index = 0;

      while (true) {
        while (from == sortedIndexes[index]) {
          from++;
          index++;

          if (index == sortedIndexes.Count) {
            //Copy remaining
            while (from < list.Count) {
              list[to++] = list[from++];
            }
            return;
          }
        }

        list[to++] = list[from++];
      }
    }
  }
}
