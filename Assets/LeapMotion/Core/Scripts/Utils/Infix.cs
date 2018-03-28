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

    /// <summary>
    /// Infix sugar for Mathf.Clamp01(f).
    /// </summary>
    public static float Clamped01(this float f) {
      return Mathf.Clamp01(f);
    }

    /// <summary>
    /// Infix sugar for Mathf.Clamp(f, min, max).
    /// </summary>
    public static float Clamped(this float f, float min, float max) {
      return Mathf.Clamp(f, min, max);
    }

    #endregion

    #region Vector3
    
    /// <summary>
    /// Rightward syntax for applying a Quaternion rotation to this vector; literally
    /// returns byQuaternion * thisVector -- does NOT modify the input vector.
    /// </summary>
    public static Vector3 RotatedBy(this Vector3 thisVector, Quaternion byQuaternion) {
      return byQuaternion * thisVector;
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
    /// Infix sugar for Vector3.Dot(a, b).
    /// </summary>
    public static float Dot(this Vector3 a, Vector3 b) {
      return Vector3.Dot(a, b);
    }

    /// <summary>
    /// Infix sugar for Vector3.Cross(a, b).
    /// </summary>
    public static Vector3 Cross(this Vector3 a, Vector3 b) {
      return Vector3.Cross(a, b);
    }

    /// <summary>
    /// Infix sugar for Vector3.Angle(a, b).
    /// </summary>
    public static float Angle(this Vector3 a, Vector3 b) {
      return Vector3.Angle(a, b);
    }

    /// <summary>
    /// Infix sugar for Vector3.SignedAngle(a, b).
    /// </summary>
    public static float SignedAngle(this Vector3 a, Vector3 b, Vector3 axis) {
      float sign = Vector3.Dot(Vector3.Cross(a,b), axis) < 0f ? -1f : 1f;
      return sign * Vector3.Angle(a, b);
    }

    #endregion

    #region Quaternion

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
    /// Returns (this * Vector3.forward), the z-axis of the rotated frame of this
    /// quaternion.
    /// </summary>
    public static Vector3 GetForward(this Quaternion q) {
      return q * Vector3.forward;
    }

    #endregion

  }

}
