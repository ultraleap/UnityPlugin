/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using Leap.Unity.Infix;
using UnityEngine;

namespace Leap.Unity.Geometry {

  [System.Serializable]
  public struct Frustum {

    [SerializeField]
    private float _angle;
    public float angle {
      get { _angle = Mathf.Clamp(_angle, -179f, 179f); return _angle; }
      set { _angle = Mathf.Clamp(value, -179f, 179f); }
    }
    public float near;
    public float far;
    public Transform transform;

    public Frustum(float angle, float near = 0.10f, float far = 0.50f,
      Transform transform = null)
    {
      this._angle = angle; this.near = near; this.far = far;
      this.transform = transform;
    }

    public void Draw(Drawer drawer) {
      var forward = Vector3.forward;
      var alpha = 0.3f;
      var color = LeapColor.cerulean.WithAlpha(alpha);

      // Near surface.
      var nearVec = forward * near;
      var nearRad = Mathf.Tan(angle/2f * Mathf.Deg2Rad) * near;
      var nearCircle = new Geometry.Circle(center: nearVec,
        direction: forward, radius: nearRad, transform: transform);
      nearCircle.Draw(drawer, color);

      // Far surface.
      var farVec = forward * far;
      var farRad = Mathf.Tan(angle/2f * Mathf.Deg2Rad) * far;
      var farCircle = new Geometry.Circle(center: farVec,
        direction: forward, radius: farRad, transform: transform);
      farCircle.Draw(drawer, color);

      var nearCirclePoints = nearCircle.Points(7);
      var farCirclePoints = farCircle.Points(7);
      for (var i = 0; i < 7; i++) {
        nearCirclePoints.MoveNext();
        farCirclePoints.MoveNext();
        drawer.Line(nearCirclePoints.Current, farCirclePoints.Current);
      }

      for (var rMult = 0.8f; rMult > 0.1f; rMult -= 0.2f) {
        drawer.color = color.WithAlpha(rMult * rMult * alpha);
        nearCircle.radius = nearRad * rMult;
        nearCircle.Draw(drawer);
      }
      
      for (var rMult = 0.8f; rMult > 0.1f; rMult -= 0.2f) {
        drawer.color = color.WithAlpha(rMult * rMult * alpha);
        farCircle.radius = farRad * rMult;
        farCircle.Draw(drawer);
      }
    }

  }

}
