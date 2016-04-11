
/******************************************************************************\
     * Copyright (C) 2012-2016 Leap Motion, Inc. All rights reserved.               *
     * Leap Motion proprietary and confidential. Not for distribution.              *
     * Use subject to the terms of the Leap Motion SDK Agreement available at       *
     * https://developer.leapmotion.com/sdk_agreement, or another agreement         *
     * between Leap Motion and you, your company or other organization.             *
\******************************************************************************/
namespace Leap
{
  using System;
  using System.Collections.Generic;
  using System.Runtime.InteropServices;

  /**
   * The LeapQuaternion struct represents a rotation in three-dimensional space.
   */
  public struct LeapQuaternion :
    IEquatable<LeapQuaternion>
  {
    /**
     * Creates a new LeapQuaternion with the specified component values.
     */
    public LeapQuaternion(float x, float y, float z, float w) :
      this()
    {
      this.x = x;
      this.y = y;
      this.z = z;
      this.w = w;
    }

    /**
     * Copies the specified LeapQuaternion.
     */
    public LeapQuaternion(LeapQuaternion quaternion) :
      this()
    {
      x = quaternion.x;
      y = quaternion.y;
      z = quaternion.z;
      w = quaternion.w;
    }

    /**
     * Copies the specified LEAP_QUATERNION.
     */
    public LeapQuaternion(LeapInternal.LEAP_QUATERNION quaternion) :
      this()
    {
      x = quaternion.x;
      y = quaternion.y;
      z = quaternion.z;
      w = quaternion.w;
    }

    /**
     * Returns a string containing this quaternion in a human readable format: (x, y, z).
     */
    public override string ToString()
    {
      return "(" + x + ", " + y + ", " + z + ", " + w + ")";
    }

    /**
     * Compare LeapQuaternion equality component-wise.
     */
    public bool Equals(LeapQuaternion v)
    {
      return x.NearlyEquals(v.x) && y.NearlyEquals(v.y) && z.NearlyEquals(v.z) && w.NearlyEquals(v.w);
    }
    public override bool Equals(Object obj)
    {
      return obj is LeapQuaternion && Equals((LeapQuaternion)obj);
    }

    /**
     * Returns true if all of the quaternion's components are finite.  If any
     * component is NaN or infinite, then this returns false.
     */
    public bool IsValid()
    {
      return !(float.IsNaN(x) || float.IsInfinity(x) ||
               float.IsNaN(y) || float.IsInfinity(y) ||
               float.IsNaN(z) || float.IsInfinity(z) ||
               float.IsNaN(w) || float.IsInfinity(w));
    }

    /**
     * The x component.
     */
    public float x;

    /**
     * The y component.
     */
    public float y;

    /**
     * The z component.
     */
    public float z;

    /**
     * The w component.
     */
    public float w;

    /**
     * The magnitude, or length, of this quaternion.
     * @returns The length of this quaternion.
     */
    public float Magnitude
    {
      get { return (float)Math.Sqrt(x * x + y * y + z * z + w * w); }
    }

    /**
     * The square of the magnitude, or length, of this quaternion.
     *
     * @returns The square of the length of this quaternion.
     */
    public float MagnitudeSquared
    {
      get { return x * x + y * y + z * z + w * w; }
    }

    /**
     * A normalized copy of this quaternion.
     *
     * @returns A LeapQuaternion object with a length of one.
     */
    public LeapQuaternion Normalized
    {
      get
      {
        float denom = this.MagnitudeSquared;
        if (denom <= Leap.Constants.EPSILON)
        {
          return LeapQuaternion.Identity;
        }
        denom = 1.0f / (float)Math.Sqrt(denom);
        return new LeapQuaternion(x * denom, y * denom, z * denom, w * denom);
      }
    }

    /**
     * Concatenates the rotation described by this quaternion with the one provided
     * and returns the result.
     *
     * @returns A LeapQuaternion containging the product.
     */
    public LeapQuaternion Multiply(LeapQuaternion rhs)
    {
      return new LeapQuaternion(
        w * rhs.x + x * rhs.w + y * rhs.z - z * rhs.y,
        w * rhs.y + y * rhs.w + z * rhs.x - x * rhs.z,
        w * rhs.z + z * rhs.w + x * rhs.y - y * rhs.x,
        w * rhs.w - x * rhs.x - y * rhs.y - z * rhs.z);
    }

    public static readonly LeapQuaternion Identity = new LeapQuaternion(0, 0, 0, 1);

    public override int GetHashCode()
    {
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
  }// end of LeapQuaternion class
} //end namespace
