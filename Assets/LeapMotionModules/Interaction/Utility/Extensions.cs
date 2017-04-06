using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  #region Transform

  public static class TransformExtensions {

    public static ChildrenEnumerator GetChildren(this Transform t) {
      return new ChildrenEnumerator(t);
    }

  }

  public struct ChildrenEnumerator {
    private Transform _t;
    private int _idx;
    private int _count;

    public ChildrenEnumerator(Transform t) {
      _t = t;
      _idx = -1;
      _count = t.childCount;
    }

    public ChildrenEnumerator GetEnumerator() { return this; }

    public bool MoveNext() {
      if (_idx < _count) _idx += 1;
      if (_idx == _count) { return false; }
      else { return true; }
    }
    public Transform Current {
      get { return _t == null ? null : _t.GetChild(_idx); }
    }
    public void Reset() {
      _idx = -1;
      _count = _t.childCount;
    }
    public void Dispose() { }
  }

  #endregion

  #region Vector3

  public static class Vector3Extensions {

    public static Vector3 ConstrainToSegment(this Vector3 position, Vector3 a, Vector3 b) {
      Vector3 ba = b - a;
      return Vector3.Lerp(a, b, Vector3.Dot(position - a, ba) / ba.sqrMagnitude);
    }

  }

  #endregion

}