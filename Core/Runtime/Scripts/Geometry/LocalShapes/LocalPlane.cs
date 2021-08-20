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

  [System.Serializable]
  /// <summary>
  /// A transformless Plane defined by a position and normal vector.
  /// </summary>
  public struct LocalPlane {

    public Vector3 position;
    public Vector3 normal;

    public LocalPlane(Vector3 position, Vector3 normal) {
      this.position = position;
      this.normal = normal;
    }

    public Plane With(Transform t) {
      return new Plane(position, normal, t);
    }

  }

  public static class LocalPlaneExtensions {

    /// <summary>
    /// Returns a transformless LocalPlane corresponding to the world-space plane of this
    /// Rect.
    /// </summary>
    public static LocalPlane ToWorldPlane(this Rect rect) {
      var pose = rect.pose;
      return new LocalPlane(pose.position, pose.rotation * Rect.PLANE_NORMAL);
    }

    /// <summary>
    /// Returns a transformless LocalPlane corresponding to the local-space plane of this
    /// Rect.
    /// </summary>
    public static LocalPlane ToLocalPlane(this Rect rect) {
      var pose = rect.pose;
      return new LocalPlane(Vector3.zero, Rect.PLANE_NORMAL);
    }

  }

}
