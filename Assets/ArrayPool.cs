using System;
using System.Collections.Generic;
using UnityEngine;

public static class ArrayPool<T> {
  public static int nextId = 1;
  private static Dictionary<int, Stack<Box>> _bins;

  static ArrayPool() {
    _bins = new Dictionary<int, Stack<Box>>();
    for (int i = 0; i < 32; i++) {
      _bins[1 << i] = new Stack<Box>();
    }
  }

  public static Box Spawn(int minCount) {
    int count = Mathf.NextPowerOfTwo(minCount);
    var bin = _bins[count];

    Box box;
    if (bin.Count > 0) {
      box = bin.Pop();
    } else {
      box = new Box() {
        array = new T[count]
      };
    }

    box.id = nextId++;
    return box;
  }

  public static void Recycle(Box toRecycle) {
    toRecycle.id = -1;
    Array.Clear(toRecycle.array, 0, toRecycle.array.Length);
    int binKey = Mathf.NextPowerOfTwo(toRecycle.array.Length + 1) / 2;
    _bins[binKey].Push(toRecycle);
  }

  public class Box {
    public int id;
    public T[] array;
  }
}
