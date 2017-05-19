/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Leap.Unity {

  public static class Utils {

    #region Generic Utils

    /// <summary>
    /// Swaps the references of a and b.  Note that you can pass
    /// in references to array elements if you want!
    /// </summary>
    public static void Swap<T>(ref T a, ref T b) {
      T temp = a;
      a = b;
      b = temp;
    }

    /// <summary>
    /// Utility extension to swap the elements at index a and index b.
    /// </summary>
    public static void Swap<T>(this List<T> list, int a, int b) {
      T temp = list[a];
      list[a] = list[b];
      list[b] = temp;
    }

    /// <summary>
    /// Utility extension to swap the elements at index a and index b.
    /// </summary>
    public static void Swap<T>(this T[] array, int a, int b) {
      Swap(ref array[a], ref array[b]);
    }

    /// <summary>
    /// System.Array.Reverse is actually suprisingly complex / slow.  This
    /// is a basic generic implementation of the reverse algorithm.
    /// </summary>
    public static void Reverse<T>(this T[] array) {
      int mid = array.Length / 2;
      int i = 0;
      int j = array.Length;
      while (i < mid) {
        array.Swap(i++, --j);
      }
    }

    public static void DoubleCapacity<T>(ref T[] array) {
      T[] newArray = new T[array.Length * 2];
      Array.Copy(array, newArray, array.Length);
      array = newArray;
    }

    // http://stackoverflow.com/a/19317229/2471635
    /// <summary>
    /// Returns whether this type implements the argument interface type.
    /// If the argument type is not an interface, returns false.
    /// </summary>
    public static bool ImplementsInterface(this Type type, Type ifaceType) {
      Type[] intf = type.GetInterfaces();
      for (int i = 0; i < intf.Length; i++) {
        if (intf[i] == ifaceType) {
          return true;
        }
      }
      return false;
    }

    #endregion

    #region Math Utils

    /// <summary>
    /// Returns a vector that is perpendicular to this vector.
    /// The returned vector will have the same length as the
    /// input vector.
    /// </summary>
    public static Vector2 Perpendicular(this Vector2 vector) {
      return new Vector2(vector.y, -vector.x);
    }

    /// <summary>
    /// Returns a vector that is perpendicular to this vector.
    /// The returned vector is not guaranteed to be a unit vector,
    /// nor is its length guaranteed to be the same as the source
    /// vector's.
    /// </summary>
    public static Vector3 Perpendicular(this Vector3 vector) {
      float x2 = vector.x * vector.x;
      float y2 = vector.y * vector.y;
      float z2 = vector.z * vector.z;

      float mag0 = z2 + x2;
      float mag1 = y2 + x2;
      float mag2 = z2 + y2;

      if (mag0 > mag1) {
        if (mag0 > mag2) {
          return new Vector3(-vector.z, 0, vector.x);
        } else {
          return new Vector3(0, vector.z, -vector.y);
        }
      } else {
        if (mag1 > mag2) {
          return new Vector3(vector.y, -vector.x, 0);
        } else {
          return new Vector3(0, vector.z, -vector.y);
        }
      }
    }

    #endregion

    #region Orientation Utils

    /// <summary>
    /// Similar to Unity's Transform.LookAt(), but resolves the forward vector of this
    /// Transform to point away from the argument Transform.
    /// 
    /// Useful for billboarding Quads and UI elements whose forward vectors should match
    /// rather than oppose the Main Camera.
    /// </summary>
    public static void LookAwayFrom(this Transform thisTransform, Transform transform) {
      thisTransform.rotation = Quaternion.LookRotation(thisTransform.position - transform.position, Vector3.up);
    }

    /// <summary>
    /// Similar to Unity's Transform.LookAt(), but resolves the forward vector of this
    /// Transform to point away from the argument Transform.
    /// 
    /// Allows specifying an upwards parameter; this is passed as the upwards vector to the Quaternion.LookRotation.
    /// </summary>
    /// <param name="thisTransform"></param>
    /// <param name="transform"></param>
    public static void LookAwayFrom(this Transform thisTransform, Transform transform, Vector3 upwards) {
      thisTransform.rotation = Quaternion.LookRotation(thisTransform.position - transform.position, upwards);
    }

    #endregion

    #region Physics Utils

    public static void IgnoreCollisions(GameObject first, GameObject second, bool ignore = true) {
      if (first == null || second == null)
        return;

      Collider[] first_colliders = first.GetComponentsInChildren<Collider>();
      Collider[] second_colliders = second.GetComponentsInChildren<Collider>();

      for (int i = 0; i < first_colliders.Length; ++i) {
        for (int j = 0; j < second_colliders.Length; ++j) {
          if (first_colliders[i] != second_colliders[j] &&
              first_colliders[i].enabled && second_colliders[j].enabled) {
            Physics.IgnoreCollision(first_colliders[i], second_colliders[j], ignore);
          }
        }
      }
    }

    #endregion

    #region Collider Utils

    public static Vector3 GetDirection(this CapsuleCollider capsule) {
      switch (capsule.direction) {
        case 0: return Vector3.right;
        case 1: return Vector3.up;
        case 2: default: return Vector3.forward;
      }
    }

    /// <summary>
    /// Manipulates capsule.transform.position, capsule.transform.rotation, and capsule.height
    /// so that the line segment defined by the capsule connects world-space points a and b.
    /// </summary>
    public static void SetCapsulePoints(this CapsuleCollider capsule, Vector3 a, Vector3 b) {
      capsule.center = Vector3.zero;

      capsule.transform.position = (a + b) / 2F;

      Vector3 capsuleDirection = capsule.GetDirection();

      Vector3 capsuleDirWorldSpace = capsule.transform.TransformDirection(capsuleDirection);
      Quaternion necessaryRotation = Quaternion.FromToRotation(capsuleDirWorldSpace, a - capsule.transform.position);
      capsule.transform.rotation = necessaryRotation * capsule.transform.rotation;

      Vector3 aCapsuleSpace = capsule.transform.InverseTransformPoint(a);
      float capsuleSpaceDistToA = aCapsuleSpace.magnitude;
      capsule.height = (capsuleSpaceDistToA + capsule.radius) * 2;
    }

    #endregion

    #region Gizmo Utils

    public static void DrawCircle(Vector3 center,
                           Vector3 normal,
                           float radius,
                           Color color,
                           int quality = 32,
                           float duration = 0,
                           bool depthTest = true) {
      Vector3 planeA = Vector3.Slerp(normal, -normal, 0.5f);
      DrawArc(360, center, planeA, normal, radius, color, quality);
    }

    /* Adapted from: Zarrax (http://math.stackexchange.com/users/3035/zarrax), Parametric Equation of a Circle in 3D Space?, 
     * URL (version: 2014-09-09): http://math.stackexchange.com/q/73242 */
    public static void DrawArc(float arc,
                           Vector3 center,
                           Vector3 forward,
                           Vector3 normal,
                           float radius,
                           Color color,
                           int quality = 32) {

      Gizmos.color = color;
      Vector3 right = Vector3.Cross(normal, forward).normalized;
      float deltaAngle = arc / quality;
      Vector3 thisPoint = center + forward * radius;
      Vector3 nextPoint = new Vector3();
      for (float angle = 0; Mathf.Abs(angle) <= Mathf.Abs(arc); angle += deltaAngle) {
        float cosAngle = Mathf.Cos(angle * Constants.DEG_TO_RAD);
        float sinAngle = Mathf.Sin(angle * Constants.DEG_TO_RAD);
        nextPoint.x = center.x + radius * (cosAngle * forward.x + sinAngle * right.x);
        nextPoint.y = center.y + radius * (cosAngle * forward.y + sinAngle * right.y);
        nextPoint.z = center.z + radius * (cosAngle * forward.z + sinAngle * right.z);
        Gizmos.DrawLine(thisPoint, nextPoint);
        thisPoint = nextPoint;
      }
    }

    public static void DrawCone(Vector3 origin,
                           Vector3 direction,
                           float angle,
                           float height,
                           Color color,
                           int quality = 4,
                           float duration = 0,
                           bool depthTest = true) {

      float step = height / quality;
      for (float q = step; q <= height; q += step) {
        DrawCircle(origin + direction * q, direction, Mathf.Tan(angle * Constants.DEG_TO_RAD) * q, color, quality * 8, duration, depthTest);
      }
    }

    #endregion

  }

}
