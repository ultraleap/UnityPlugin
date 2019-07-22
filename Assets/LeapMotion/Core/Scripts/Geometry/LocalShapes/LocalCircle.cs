using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Geometry {

  [System.Serializable]
  public struct LocalCircle {

    public Vector3 center;
    public Direction3 direction;
    public float radius;

    public Circle With(Transform t) {
      return new Circle(this, t);
    }

  }

}
