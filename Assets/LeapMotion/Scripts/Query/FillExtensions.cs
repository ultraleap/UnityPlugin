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
  }
}
