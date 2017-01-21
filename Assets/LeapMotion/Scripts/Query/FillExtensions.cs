using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Query {

  public static class FillExtensions {

    public static void Fill<T>(this T[] array, T value) {
      for (int i = 0; i < array.Length; i++) {
        array[i] = value;
      }
    }

    public static void Fill<T>(this T[,] array, T value) {
      for (int i = 0; i < array.GetLength(0); i++) {
        for (int j = 0; j < array.GetLength(1); j++) {
          array[i, j] = value;
        }
      }
    }

    public static void Fill<T>(this List<T> list, T value) {
      for (int i = 0; i < list.Count; i++) {
        list[i] = value;
      }
    }

    public static void Fill<T>(this List<T> list, int count, T value) {
      list.Clear();
      list.Capacity = count;
      for (int i = 0; i < count; i++) {
        list.Add(value);
      }
    }

    public static void Append<T>(this List<T> list, int count, T value) {
      list.Capacity = Mathf.Max(list.Capacity, list.Count + count);
      for (int i = 0; i < count; i++) {
        list.Add(value);
      }
    }
  }
}
