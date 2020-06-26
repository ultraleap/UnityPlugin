/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;

namespace Leap.Unity {

  public static class ListAndArrayExtensions {

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

    public static List<T> FillEach<T>(this List<T> list, Func<int, T> generator) {
      for (int i = 0; i < list.Count; i++) {
        list[i] = generator(i);
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

    public static List<T> FillEach<T>(this List<T> list, int count, Func<int, T> generator) {
      list.Clear();
      for (int i = 0; i < count; i++) {
        list.Add(generator(i));
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

    public static void RemoveAtUnordered<T>(this List<T> list, int index) {
      if (list.Count - 1 == index) {
        list.RemoveLast();
      } else {
        list[index] = list.RemoveLast();
      }
    }

    public static void InsertUnordered<T>(this List<T> list, int index, T element) {
      list.Add(list[index]);
      list[index] = element;
    }

    /// <summary>
    /// Removes each element referenced by the 'sortedIndexes' list.  This
    /// is much faster than calling RemoveAt for each index individually.
    /// 
    /// This operation is equivilent to calling RemoveAt for each index in 
    /// the *reverse* order as provided by 'sortedIndexes'.
    /// </summary>
    public static void RemoveAtMany<T>(this List<T> list, List<int> sortedIndexes) {
      if (sortedIndexes.Count == 0) return;

      //If just removing one, might as well use built-in function
      if (sortedIndexes.Count == 1) {
        list.RemoveAt(sortedIndexes[0]);
        return;
      }

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

            //Remove the last elements on the end
            list.RemoveRange(list.Count - index, index);

            return;
          }
        }

        list[to++] = list[from++];
      }
    }

    /// <summary>
    /// Inserts each element in the 'elements' list into each index referenced by the 'sorted'Indexes'
    /// list.  This is much faster than calling Insert for each element individually.  
    /// 
    /// This operation is equivilent to calling Insert for each element in the same order the elements 
    /// are found in the argument list. 
    /// </summary>
    public static void InsertMany<T>(this List<T> list, List<int> sortedIndexes, List<T> elements) {
      if (sortedIndexes.Count == 0) return;

      if (sortedIndexes.Count == 1) {
        list.Insert(sortedIndexes[0], elements[0]);
        return;
      }

      int from = list.Count - 1;

      //First expand array to be large enough to hold all the new elements
      for (int i = 0; i < sortedIndexes.Count; i++) {
        list.Add(default(T));
      }

      int to = list.Count - 1;

      int index = sortedIndexes.Count - 1;

      while (true) {
        while (to == sortedIndexes[index]) {
          list[to--] = elements[index--];

          if (index == -1) {
            return;
          }
        }

        list[to--] = list[from--];
      }
    }
  }
}
