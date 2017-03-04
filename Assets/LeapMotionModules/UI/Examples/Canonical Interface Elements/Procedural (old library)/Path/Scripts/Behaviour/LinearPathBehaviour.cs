using System;
using UnityEngine;

namespace Procedural.DynamicPath {

  public class LinearPathBehaviour : PathBehaviourBase {

    [SerializeField]
    private float _length;

    public float Length {
      get {
        return _length;
      }
      set {
        _length = value;
      }
    }

    public override IPath Path {
      get {
        return new LinearPath(transform.position, transform.position + transform.forward * _length);
      }
    }
  }
}
