/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using System.IO;
using Leap.Unity.Query;
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

    /// <summary>
    /// Infix sugar for Mathf.Abs(f).
    /// </summary>
    public static float Abs(this float f) {
      return Mathf.Abs(f);
    }

    /// <summary>
    /// Returns a default value if this float is NaN, positive infinity, or
    /// negative infinity. Otherwise returns this float unchanged.
    /// </summary>
    public static float NaNOrInfTo(this float f, float valueIfNaNOrInf) {
      if (float.IsNaN(f) || float.IsNegativeInfinity(f) ||
        float.IsPositiveInfinity(f))
      {
        return valueIfNaNOrInf;
      }
      return f;
    }

    /// <summary> Gets whether the argument float is positive or negative
    /// infinity. </summary>
    public static bool IsInfinity(this float f) {
      return float.IsPositiveInfinity(f) || float.IsNegativeInfinity(f);
    }

    #endregion

    #region Vector2

    /// <summary>
    /// Returns the component-wise absolute value of the vector.
    /// </summary>
    public static Vector2 Abs(this Vector2 v) {
      return new Vector2(Mathf.Abs(v.x), Mathf.Abs(v.y));
    }

    /// <summary>
    /// Converts the Vector2 to a Vector3 with the specified Z value.
    /// </summary>
    public static Vector3 WithZ(this Vector2 v, float z) {
      return new Vector3(v.x, v.y, z);
    }



    public static Vector2 FlippedX(this Vector2 v) {
      return new Vector2(-v.x, v.y);
    }

    public static Vector2 FlippedY(this Vector2 v) {
      return new Vector2(v.x, -v.y);
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
    /// Right-handed infix cross product. (Unity uses left-handed cross products
    /// which can be confusing sometimes.)
    /// </summary>
    public static Vector3 RHCross(this Vector3 a, Vector3 b) {
      return Vector3.Cross(b, a);
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

    /// <summary>
    /// Returns the component-wise exponential of the vector.
    /// </summary>
    public static Vector3 Exp(this Vector3 v) {
      return new Vector3(Mathf.Exp(v.x), Mathf.Exp(v.y), Mathf.Exp(v.z));
    }

    /// <summary>
    /// Returns the component-wise absolute value of the vector.
    /// </summary>
    public static Vector3 Abs(this Vector3 v) {
      return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    }

    /// <summary>
    /// Returns a new vector with the specified Z value.
    /// </summary>
    public static Vector3 WithZ(this Vector3 v, float z) {
      return new Vector3(v.x, v.y, z);
    }

    /// <summary>
    /// Returns a Vector2 with the X and Y components of this Vector3.
    /// </summary>
    public static Vector2 ToVector2(this Vector3 v) {
      return new Vector2(v.x, v.y);
    }
    
    /// <summary>
    /// Returns a default per-component value is the component is NaN,
    /// positive infinity, or negative infinity. Otherwise returns this Vector3
    /// unchanged. Any already non-NaN-or-infinite components remain unchanged.
    /// </summary>
    public static Vector3 NaNOrInfTo(this Vector3 v, float valueIfNaNOrInf) {
      return new Vector3(
        v.x.NaNOrInfTo(valueIfNaNOrInf),
        v.y.NaNOrInfTo(valueIfNaNOrInf),
        v.z.NaNOrInfTo(valueIfNaNOrInf));
    }

    public static bool ContainsNaNOrInf(this Vector3 v) {
      return v.ContainsNaN() ||
        v.x.IsInfinity() || v.y.IsInfinity() ||  v.z.IsInfinity();
    }

    public static Vector3 FlippedX(this Vector3 v) {
      return new Vector3(-v.x, v.y, v.z);
    }

    public static Vector3 FlippedY(this Vector3 v) {
      return new Vector3(v.x, -v.y, v.z);
    }

    public static Vector3 FlippedZ(this Vector3 v) {
      return new Vector3(v.x, v.y, -v.z);
    }

    public static Vector3 ProjectOnPlane(this Vector3 v, Vector3 planeNormal) {
      return Vector3.ProjectOnPlane(v, planeNormal);
    }

    public static Vector3 WithLength(this Vector3 v, float newLength) {
      return v.normalized * newLength;
    }

    public static Vector3 Lerp(this Vector3 a, Vector3 b, float t) {
      return Vector3.Lerp(a, b, t);
    }

    public static Vector3 LerpUnclamped(this Vector3 a, Vector3 b, float t) {
      return Vector3.LerpUnclamped(a, b, t);
    }

    #endregion

    #region List<Vector3>

    public static Vector3 GetCentroid(this List<Vector3> vs) {
      return vs.Query().Fold((v, acc) => v + acc) / vs.Count;
    }

    /// <summary> Sets the points' centroid to the argument value and, in case it's useful, returns the delta that was required to move to the new centroid. </summary>
    public static Vector3 SetCentroid(this List<Vector3> vs, Vector3 c) {
      var oldC = vs.GetCentroid();
      var toNewC = c - oldC;
      for (var i = 0; i < vs.Count; i++) { vs[i] = vs[i] + toNewC; }
      return toNewC;
    }

    #endregion

    #region Vector4

    /// <summary>
    /// Modifies the Vector4's XYZ to match the argument Vector3.
    /// </summary>
    public static Vector3 WithXYZ(this Vector4 v, Vector3 xyz) {
      return new Vector4(xyz.x, xyz.y, xyz.z, v.w);
    }
    /// <summary>
    /// Modifies the Vector4's XYZ to match the argument Vector4's XYZ.
    /// </summary>
    public static Vector3 WithXYZ(this Vector4 v0, Vector4 v1) {
      return new Vector4(v1.x, v1.y, v1.z, v0.w);
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

    /// <summary>
    /// Returns (this * Vector3.left), the -X axis of the rotated frame of this
    /// quaternion.
    /// </summary>
    public static Vector3 GetLeft(this Quaternion q) {
      return q * Vector3.left;
    }

    /// <summary> Infix sugar for Quaternion.Inverse(). </summary>
    public static Quaternion Inverse(this Quaternion q) {
      return Quaternion.Inverse(q);
    }

    /// <summary> Infix for Matrix4x4.Rotate(q); </summary>
    public static Matrix4x4 ToMatrix(this Quaternion q) {
      return Matrix4x4.Rotate(q);
    }

    /// <summary> Infix for Matrix4x4.Rotate(q); </summary>
    public static Matrix4x4 GetMatrix(this Quaternion q) {
      return Matrix4x4.Rotate(q);
    }

    #endregion

    #region Vector2Int

    /// <summary> Returns the smaller of the two vector components. </summary>
    public static int Min(this Vector2Int v) {
      return Mathf.Min(v.x, v.y);
    }

    /// <summary> Returns the larger of the two vector components. </summary>
    public static int Max(this Vector2Int v) {
      return Mathf.Max(v.x, v.y);
    }

    #endregion

    #region Color

    public static Color Lerp(this Color c, Color other, float t) {
      return Color.Lerp(c, other, t);
    }

    public static Color WithValue(this Color c, float newHSVValue) {
      float h, s, v; Color.RGBToHSV(c, out h, out s, out v);
      return Color.HSVToRGB(h, s, newHSVValue);
    }

    public static Color WithSat(this Color c, float newHSVSat) {
      float h, s, v; Color.RGBToHSV(c, out h, out s, out v);
      return Color.HSVToRGB(h, newHSVSat, v);
    }

    public static Color WithHSV(this Color c, float? h = null, float? s = null,
      float? v = null)
    {
      float origH, origS, origV;
      Color.RGBToHSV(c, out origH, out origS, out origV);
      var useH = h.UnwrapOr(origH);
      var useS = s.UnwrapOr(origS);
      var useV = v.UnwrapOr(origV);
      return Color.HSVToRGB(useH, useS, useV);
    }

    public static Color ShiftHue(this Color c, float shift) {
      float origH, origS, origV;
      Color.RGBToHSV(c, out origH, out origS, out origV);
      int limit = 0;
      while (shift < 0f && limit++ != 1000) {
        shift += 1f;
      }
      var h = origH + shift;
      h %= 1f;
      return Color.HSVToRGB(h, origS, origV);
    }

    #endregion

    #region Integer

    /// <summary> Index 0 is the first (least significant) bit. </summary>
    public static bool HasNthBit(this int integer, int bitIdx) {
      return (integer & (1 << bitIdx)) != 0;
    }

    #endregion

    #region String

    /// <summary> Infix sugar for Path.Combine(path0, path1). </summary>
    public static string PathCombine(this string path0, string path1) {
      return Path.Combine(path0, path1);
    }

    /// <summary> Infix sugar for Path.GetDirectoryName(path).
    /// 
    /// Note that this method can be used to move up one level in the file hierarchy every time it is called. </summary>
    public static string GetDirectoryName(this string path) {
      return Path.GetDirectoryName(path);
    }

    #endregion

  }

}
