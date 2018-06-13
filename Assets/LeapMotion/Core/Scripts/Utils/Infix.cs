/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Infix {

  /// <summary>
  /// Unity math operations like Vector3.Dot put in extension methods so they can be used
  /// in as infix operations, e.g., a.Dot(b).
  /// </summary>
  public static class Infix {
    
    #region Float

    /// <summary> Infix Mathf.Abs. Does not modify the input float. </summary>
    public static float Abs(this float f) {
      return Mathf.Abs(f);
    }

    /// <summary>
    /// Infix method for Mathf.Clamp(f, min, max). Does not modify the input float.
    /// </summary>
    public static float Clamped(this float f, float min, float max) {
      return Mathf.Clamp(f, min, max);
    }

    /// <summary>
    /// Infix method for Mathf.Clamp01(f). Does not modify the input float.
    /// </summary>
    public static float Clamped01(this float f) {
      return Mathf.Clamp01(f);
    }

    /// <summary>
    /// If the input is NaN, negative infinity, or positivty infinity, the argument
    /// value is returned, otherwise the original float is returned.
    /// </summary>
    public static float NaNOrInfTo(this float f, float valueIfNaNOrInf) {
      if (float.IsNaN(f) || float.IsNegativeInfinity(f) || float.IsPositiveInfinity(f)) {
        return valueIfNaNOrInf;
      }
      return f;
    }

    /// <summary> Returns the input times itself. </summary>
    public static float Squared(this float f) {
      return f * f;
    }

    #endregion

    #region Vector3

    /// <summary> Component-wise absolute value. </summary>
    public static Vector3 Abs(this Vector3 v) {
      return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    /// <summary>
    /// Infix sugar for Vector3.Angle(a, b).
    /// </summary>
    public static float Angle(this Vector3 a, Vector3 b) {
      return Vector3.Angle(a, b);
    }

    /// <summary>
    /// Returns the values of this vector clamped component-wise with minimums from minV
    /// and maximums from maxV.
    /// </summary>
    public static Vector3 Clamped(this Vector3 v, Vector3 minV, Vector3 maxV) {
      return new Vector3(Mathf.Clamp(v.x, minV.x, maxV.x),
                         Mathf.Clamp(v.y, minV.y, maxV.y),
                         Mathf.Clamp(v.z, minV.z, maxV.z));
    }

    /// <summary>
    /// Infix sugar for Vector3.Cross(a, b).
    /// </summary>
    public static Vector3 Cross(this Vector3 a, Vector3 b) {
      return Vector3.Cross(a, b);
    }

    /// <summary>
    /// Infix sugar for Vector3.Dot(a, b).
    /// </summary>
    public static float Dot(this Vector3 a, Vector3 b) {
      return Vector3.Dot(a, b);
    }

    /// <summary>
    /// Infix sugar for Vector3.MoveTowards(a, b).
    /// 
    /// Returns this position moved towards the argument position, up to but no more than
    /// the max distance from the original position specified by maxDistanceDelta.
    /// </summary>
    public static Vector3 MovedTowards(this Vector3 thisPosition,
                                      Vector3 otherPosition,
                                      float maxDistanceDelta) {
      return Vector3.MoveTowards(thisPosition, otherPosition, maxDistanceDelta);
    }
    
    /// <summary>
    /// Component-wise sanitizes NaNs or Infinity values to the argument value,
    /// leaving real-number components unaffected.
    /// </summary>
    public static Vector3 NaNOrInfTo(this Vector3 v, float valueIfNaNOrInf) {
      return new Vector3(v.x.NaNOrInfTo(valueIfNaNOrInf), v.y.NaNOrInfTo(valueIfNaNOrInf),
        v.z.NaNOrInfTo(valueIfNaNOrInf));
    }

    /// <summary>
    /// Returns this vector rotated around a point by an angle around an axis direction.
    /// </summary>
    public static Vector3 RotatedAround(this Vector3 v, 
                                        Vector3 aroundPoint, float angle, Vector3 axis) {
      var vFromPoint = v - aroundPoint;
      vFromPoint = Quaternion.AngleAxis(angle, axis) * vFromPoint;
      return aroundPoint + vFromPoint;
    }
    
    /// <summary>
    /// Infix method for applying a Quaternion rotation to this vector; literally
    /// returns byQuaternion * thisVector. Does *not* modify the input vector.
    /// </summary>
    public static Vector3 RotatedBy(this Vector3 thisVector, Quaternion byQuaternion) {
      return byQuaternion * thisVector;
    }

    /// <summary>
    /// Infix sugar for Vector3.SignedAngle(a, b).
    /// </summary>
    public static float SignedAngle(this Vector3 a, Vector3 b, Vector3 axis) {
      float sign = Vector3.Dot(Vector3.Cross(a,b), axis) < 0f ? -1f : 1f;
      return sign * Vector3.Angle(a, b);
    }

    /// <summary>
    /// Infix method to convert a Vector3 to a Vector2.
    /// </summary>
    public static Vector2 ToVector2(this Vector3 v) {
      return v;
    }

    #endregion

    #region Vector2

    /// <summary>
    /// Component-wise absolute value. Does not modify the input vector.
    /// </summary>
    public static Vector2 Abs(this Vector2 v) {
      return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
    }

    /// <summary>
    /// Returns the values of this vector clamped component-wise with minimums from minV
    /// and maximums from maxV.
    /// </summary>
    public static Vector2 Clamped(this Vector2 v, Vector2 minV, Vector2 maxV) {
      return new Vector2(Mathf.Clamp(v.x, minV.x, maxV.x),
                         Mathf.Clamp(v.y, minV.y, maxV.y));
    }

    #endregion

    #region Quaternion

    /// <summary>
    /// Returns (this * Vector3.forward), the z-axis of the rotated frame of this
    /// quaternion.
    /// </summary>
    public static Vector3 GetForward(this Quaternion q) {
      return q * Vector3.forward;
    }

    /// <summary>
    /// Returns (this * Vector3.right), the x-axis of the rotated frame of this
    /// quaternion.
    /// </summary>
    public static Vector3 GetRight(this Quaternion q) {
      return q * Vector3.right;
    }

    /// <summary>
    /// Returns (this * Vector3.up), the y-axis of the rotated frame of this quaternion.
    /// </summary>
    public static Vector3 GetUp(this Quaternion q) {
      return q * Vector3.up;
    }

    /// <summary>
    /// Returns Quaternion.Inverse(this).
    /// </summary>
    public static Quaternion Inverse(this Quaternion q) {
      return Quaternion.Inverse(q);
    }

    #endregion

    #region Color

    public static Color Lerp(this Color a, Color b, float t) {
      return Color.Lerp(a, b, t);
    }

    #endregion

  }

}
