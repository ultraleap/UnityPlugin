/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Geometry {

  [System.Serializable]
  public struct LocalFrustum {

    [SerializeField]
    private float _angle;
    public float angle {
      get { _angle = Mathf.Clamp(_angle, -179f, 179f); return _angle; }
      set { _angle = Mathf.Clamp(value, -179f, 179f); }
    }
    public float near;
    public float far;

    public static LocalFrustum Default { get {
      return new LocalFrustum(90f, 0.10f, 0.50f);
    }}

    public LocalFrustum(float angle, float near = 0.10f, float far = 0.50f) {
      this._angle = angle;
      this.near = near;
      this.far = far;
    }

    public Frustum With(Transform t) {
      return new Frustum(angle, near, far, t);
    }

  }

}
