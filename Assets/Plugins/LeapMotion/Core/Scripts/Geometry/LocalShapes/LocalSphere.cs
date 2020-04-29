/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Geometry {

  [System.Serializable]
  public struct LocalSphere {

    public Vector3 center;
    public float radius;

    public LocalSphere(Vector3 center, float radius) {
      this.center = center;
      this.radius = radius;
    }
    public LocalSphere(float radius) {
      this.center = Vector3.zero;
      this.radius = radius;
    }

    public Sphere With(Transform transform) {
      return new Sphere(this, transform);
    }

    public static LocalSphere Default() {
      return new LocalSphere(default(Vector3), 1f);
    }

  }

}
