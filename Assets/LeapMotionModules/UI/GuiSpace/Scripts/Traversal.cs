using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Traversal {

  public static void DepthFirst<T>(Transform root, Action<T> onComponent) where T : Component {
    T t = root.GetComponent<T>();
    if (t != null) {
      onComponent(t);
    }

    int childCount = root.childCount;
    for (int i = 0; i < childCount; i++) {
      DepthFirst<T>(root.GetChild(i), onComponent);
    }
  }
}
