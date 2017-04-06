using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  #region Transform

  public static class TransformExtensions {

    [ThreadStatic]
    private static ChildrenEnumerator s_childEnum = new ChildrenEnumerator();
    public static ChildrenEnumerator GetChildEnumerator(this Transform t) {
      s_childEnum.ResetWithNewTransform(t);
      return s_childEnum;
    }

  }

  public class ChildrenEnumerator : IEnumerator<Transform> {
    private Transform _t;
    private int _idx;
    private int _count;

    public IEnumerator<Transform> GetEnumerator() { return this; }

    /// <summary>
    /// If the parameterless constructor is called, be sure to call
    /// ResetWithNewTransform(t) before attempting to enumerate.
    /// </summary>
    public ChildrenEnumerator() { }

    public ChildrenEnumerator(Transform t) {
      ResetWithNewTransform(t);
    }

    public void ResetWithNewTransform(Transform t) {
      _t = t;
      _idx = -1;
      _count = t == null ? 0 : t.childCount;
    }

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

    object IEnumerator.Current { get { return Current; } }
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