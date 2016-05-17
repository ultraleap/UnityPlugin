using UnityEngine;
using System.Collections.Generic;

public struct ReadonlyList<T> {
  private readonly List<T> _list;

  public ReadonlyList(List<T> list) {
    _list = list;
  }

  public int Count {
    get {
      return _list.Count;
    }
  }

  public T this[int index] {
    get {
      return _list[index];
    }
  }
}
