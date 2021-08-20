/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Infix;
using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Geometry {
  
  public struct LocalSegment3 {

    public Vector3 a, b;

    public LocalSegment3(Vector3 a, Vector3 b) {
      this.a = a;
      this.b = b;
    }

    /// <summary>
    /// Given a point _on the segment_, parameterizes that point into a value such that
    /// a + (b - a).magnitude * value = b.
    /// </summary>
    public float Parameterize(Vector3 pointOnSegment) {
      if ((a - b).sqrMagnitude < float.Epsilon) return 0f;
      return (pointOnSegment - a).magnitude / (b - a).magnitude;
    }

    public Vector3 Evaluate(float t) {
      var ab = b - a;
      return a + ab * t;
    }

    public LocalSegment3 Transform(Matrix4x4 m, bool fullMultiply = false) {
      if (fullMultiply) {
        return new LocalSegment3(m.MultiplyPoint(a), m.MultiplyPoint(b));
      }
      else {
        return new LocalSegment3(m.MultiplyPoint3x4(a), m.MultiplyPoint3x4(b));
      }
    }

    #region Collision

    /// <summary>
    /// Returns the squared distance between this line segment and the rect.
    /// </summary>
    public float Intersect(Rect rect) {
      return Collision.Intersect(this, rect);
    }

    #endregion

    #region Runtime Gizmos

    public void DrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      drawer.DrawLine(a, b);
    }

    #endregion

  }

}
