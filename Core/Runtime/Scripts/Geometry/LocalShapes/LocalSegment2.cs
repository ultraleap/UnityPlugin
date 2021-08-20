/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Infix;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Geometry {

  public struct LocalSegment2 {

    public Vector2 a, b;

    public LocalSegment2(Vector2 a, Vector2 b) {
      this.a = a;
      this.b = b;
    }

    /// <summary>
    /// Given a point _on the segment_, parameterizes that point into a value such that
    /// a + (b - a).magnitude * value = b.
    /// </summary>
    public float Parameterize(Vector2 pointOnSegment) {
      if ((a - b).sqrMagnitude < float.Epsilon) return 0f;
      return (pointOnSegment - a).magnitude / (b - a).magnitude;
    }

    //public LocalSegment3 WithZ(float z) {
    //  return new LocalSegment3(a.WithZ(z), b.WithZ(z));
    //}

  }

}
