/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap {
  using System;

  /// <summary>
  /// The LeapQuaternion struct represents a rotation in three-dimensional space.
  /// @since 3.1.2
  /// </summary>
  [Serializable]
  public struct LeapQuaternion :
    IEquatable<LeapQuaternion> {

    /// <summary>
    /// Creates a new LeapQuaternion with the specified component values.
    /// @since 3.1.2
    /// </summary>
    public LeapQuaternion(float x, float y, float z, float w) :
      this() {
      this.x = x;
      this.y = y;
      this.z = z;
      this.w = w;
    }

    /// <summary>
    /// Copies the specified LeapQuaternion.
    /// @since 3.1.2
    /// </summary>
    public LeapQuaternion(LeapQuaternion quaternion) :
      this() {
      x = quaternion.x;
      y = quaternion.y;
      z = quaternion.z;
      w = quaternion.w;
    }

    /// <summary>
    /// Copies the specified LEAP_QUATERNION.
    /// @since 3.1.2
    /// </summary>
    public LeapQuaternion(LeapInternal.LEAP_QUATERNION quaternion) :
      this() {
      x = quaternion.x;
      y = quaternion.y;
      z = quaternion.z;
      w = quaternion.w;
    }

    /// <summary>
    /// Returns a string containing this quaternion in a human readable format: (x, y, z).
    /// @since 3.1.2
    /// </summary>
    public override string ToString() {
      return "(" + x + ", " + y + ", " + z + ", " + w + ")";
    }

    /// <summary>
    /// Compare LeapQuaternion equality component-wise.
    /// @since 3.1.2
    /// </summary>
    public bool Equals(LeapQuaternion v) {
      return x.NearlyEquals(v.x) && y.NearlyEquals(v.y) && z.NearlyEquals(v.z) && w.NearlyEquals(v.w);
    }
    public override bool Equals(Object obj) {
      return obj is LeapQuaternion && Equals((LeapQuaternion)obj);
    }

    /// <summary>
    /// Returns true if all of the quaternion's components are finite.  If any
    /// component is NaN or infinite, then this returns false.
    /// @since 3.1.2
    /// </summary>
    public bool IsValid() {
      return !(float.IsNaN(x) || float.IsInfinity(x) ||
               float.IsNaN(y) || float.IsInfinity(y) ||
               float.IsNaN(z) || float.IsInfinity(z) ||
               float.IsNaN(w) || float.IsInfinity(w));
    }

    public float x;
    public float y;
    public float z;
    public float w;

    /// <summary>
    /// The magnitude, or length, of this quaternion.
    /// @since 3.1.2
    /// </summary>
    public float Magnitude {
      get { return (float)Math.Sqrt(x * x + y * y + z * z + w * w); }
    }

    /// <summary>
    /// The square of the magnitude, or length, of this quaternion.
    /// @since 3.1.2
    /// </summary>
    public float MagnitudeSquared {
      get { return x * x + y * y + z * z + w * w; }
    }

    /// <summary>
    /// A normalized copy of this quaternion.
    /// @since 3.1.2
    /// </summary>
    public LeapQuaternion Normalized {
      get {
        float denom = MagnitudeSquared;
        if (denom <= Constants.EPSILON) {
          return Identity;
        }
        denom = 1.0f / (float)Math.Sqrt(denom);
        return new LeapQuaternion(x * denom, y * denom, z * denom, w * denom);
      }
    }

    /// <summary>
    /// Concatenates the rotation described by this quaternion with the one provided
    /// and returns the result.
    /// @since 3.1.2
    /// </summary>
    public LeapQuaternion Multiply(LeapQuaternion rhs) {
      return new LeapQuaternion(
        w * rhs.x + x * rhs.w + y * rhs.z - z * rhs.y,
        w * rhs.y + y * rhs.w + z * rhs.x - x * rhs.z,
        w * rhs.z + z * rhs.w + x * rhs.y - y * rhs.x,
        w * rhs.w - x * rhs.x - y * rhs.y - z * rhs.z);
    }

    /// <summary>
    /// The identity quaternion.
    /// @since 3.1.2 
    /// </summary>
    public static readonly LeapQuaternion Identity = new LeapQuaternion(0, 0, 0, 1);

    public override int GetHashCode() {
      unchecked // Overflow is fine, just wrap
      {
        int hash = 17;
        hash = hash * 23 + x.GetHashCode();
        hash = hash * 23 + y.GetHashCode();
        hash = hash * 23 + z.GetHashCode();
        hash = hash * 23 + w.GetHashCode();

        return hash;
      }
    }
  }
}
