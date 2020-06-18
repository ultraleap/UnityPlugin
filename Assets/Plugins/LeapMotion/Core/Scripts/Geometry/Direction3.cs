/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Geometry {
  
  /// <summary>
  /// A struct very similar to Vector3, but that prevents itself from ever
  /// being converted to Vector3.zero. If the Direction3's components would ever
  /// be un-normalizable (normalize to Vector3.zero), it will instead return
  /// Vector3.forward. (Direction3 implicitly converts to a normalized Vector3.)
  ///
  /// Because of this sanitizing behavior, "default(Direction3)" is stored as
  /// all zero components, but is converted to Vector3.forward upon implicit
  /// conversion to Vector3.
  /// </summary>
  [System.Serializable]
  public struct Direction3 {

    [SerializeField]
    private float x;

    [SerializeField]
    private float y;

    [SerializeField]
    private float z;

    public Direction3(Vector3 v) {
      x = v.x; y = v.y; z = v.z;
    }

    public Direction3(float x, float y, float z) {
      this.x = x; this.y = y; this.z = z;
    }

    /// <summary>
    /// Gets whether this Direction3 will normalize without issue (has nonzero
    /// magnitude), otherwise the conversion to Vector3 will sanitize to
    /// Vector3.forward.
    /// </summary>
    public bool isValid { get { return new Vector3(x, y, z) != Vector3.zero; } }

    /// <summary> Explicitly converts this Direction3 to a Vector3. </summary>
    public Vector3 Vec() {
      return this;
    }

    public static implicit operator Vector3(Direction3 dir) {
      var normalized = new Vector3(dir.x, dir.y, dir.z).normalized;
      if (normalized == Vector3.zero) {
        return Vector3.forward;
      }
      return normalized;
    }

    public static implicit operator Direction3(Vector3 vec) {
      return new Direction3(vec);
    }

    /// <summary>
    /// Returns whether two Direction3s point in the same direction without
    /// normalizing either of the underlying vectors.
    /// 
    /// This method is intended to cheaply match bit-idential Direction3s; it's
    /// subject to precision error if the magnitudes of the underlying vectors
    /// are very different.
    /// </summary>
    public static bool PointsInSameDirection(Direction3 A, Direction3 B) {
      Vector3 aV = new Vector3(A.x, A.y, A.z);
      Vector3 bV = new Vector3(B.x, B.y, B.z);
      return Vector3.Cross(aV, bV).sqrMagnitude == 0f;
    }

  }

}
