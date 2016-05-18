/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2016.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using LeapInternal;

namespace Leap.Unity {

  /**
   * Unity extentions for Leap Vector class.
   */
  public static class UnityVectorExtension {

    /**
    * Converts a Leap Vector object to a UnityEngine Vector3 object.
    *
    * Does not convert to the Unity left-handed coordinate system or scale
    * the coordinates from millimeters to meters.
    * @returns The Unity Vector3 object with the same coordinate values as the Leap.Vector.
    */
    public static Vector3 ToVector3(this Vector vector) {
      return new Vector3(vector.x, vector.y, vector.z);
    }

    public static Vector3 ToVector3(this LEAP_VECTOR vector) {
      return new Vector3(vector.x, vector.y, vector.z);
    }

    /**
    * Converts a Leap Vector object to a UnityEngine Vector4 object.
    *
    * Does not convert to the Unity left-handed coordinate system or scale
    * the coordinates from millimeters to meters.
    * @returns The Unity Vector4 object with the same coordinate values as the Leap.Vector.
    */
    public static Vector4 ToVector4(this Vector vector) {
      return new Vector4(vector.x, vector.y, vector.z, 0.0f);
    }

    /**
    * Converts a UnityEngine Vector3 object to a Leap Vector object.
    *
    * Does not convert to the Unity left-handed coordinate system or scale
    * the coordinates from millimeters to meters.
    * @returns The Leap Vector object with the same coordinate values as the UnityEngine.Vector.
    */
    public static Vector ToVector(this Vector3 vector) {
      return new Vector(vector.x, vector.y, vector.z);
    }

    public static LEAP_VECTOR ToCVector(this Vector3 vector) {
      LEAP_VECTOR cVector = new LEAP_VECTOR();
      cVector.x = vector.x;
      cVector.y = vector.y;
      cVector.z = vector.z;
      return cVector;
    }
  }

  /**
   * Unity extentions for Leap Quaternion class.
   */
  public static class UnityQuaternionExtension {
    /**
    * Converts a LeapQuaternion object to a UnityEngine.Quaternion object.
    *
    * @returns The UnityEngine Quaternion object with the same values as the LeapQuaternion.
    */
    public static Quaternion ToQuaternion(this LeapQuaternion q) {
      return new Quaternion(q.x, q.y, q.z, q.w);
    }

    public static Quaternion ToQuaternion(this LEAP_QUATERNION q) {
      return new Quaternion(q.x, q.y, q.z, q.w);
    }

    /**
    * Converts a UnityEngine.Quaternion object to a LeapQuaternion object.
    *
    * @returns The LeapQuaternion object with the same values as the UnityEngine.Quaternion.
    */
    public static LeapQuaternion ToLeapQuaternion(this Quaternion q) {
      return new LeapQuaternion(q.x, q.y, q.z, q.w);
    }

    public static LEAP_QUATERNION ToCQuaternion(this Quaternion q) {
      LEAP_QUATERNION cQuaternion = new LEAP_QUATERNION();
      cQuaternion.x = q.x;
      cQuaternion.y = q.y;
      cQuaternion.z = q.z;
      cQuaternion.w = q.w;
      return cQuaternion;
    }
  }

  /**
   * Unity extentions for the Leap Motion LeapTransform class.
   */
  public static class UnityMatrixExtension {
    /** Up in the Leap coordinate system.*/
    public static readonly Vector LEAP_UP = new Vector(0, 1, 0);
    /** Forward in the Leap coordinate system.*/
    public static readonly Vector LEAP_FORWARD = new Vector(0, 0, -1);
    /** The origin point in the Leap coordinate system.*/
    public static readonly Vector LEAP_ORIGIN = new Vector(0, 0, 0);
    /** Conversion factor for millimeters to meters. */
    public static readonly float MM_TO_M = 1e-3f;

    /**
      * Converts a LeapTransform representing a rotation to a Unity Quaternion without
      * depending on the LeapTransform having a valid Quaternion.
      *
      * In previous version prior 4.0.0 this function performed a conversion to Unity's left-handed coordinate system, and now does not.
      *
      * @returns A Unity Quaternion representing the rotation.
      */
    public static Quaternion CalculateRotation(this LeapTransform trs) {
      Vector3 up = trs.yBasis.ToVector3();
      Vector3 forward = -trs.zBasis.ToVector3();
      return Quaternion.LookRotation(forward, up);
    }

    /**
     * Extracts a transform matrix containing translation, rotation, and scale from a Unity Transform object and
     * returns a Leap Motion LeapTransform object.
     * Use this matrix to transform Leap Motion tracking data to the Unity world relative to the
     * specified transform.
     *
     * In addition to applying the translation, rotation, and scale from the Transform object, the returned
     * transformation changes the coordinate system from right- to left-handed and converts units from millimeters to meters
     * by scaling.
     * @returns A Leap.LeapTransform object representing the specified transform from Leap Motion into Unity space.
     */
    public static LeapTransform GetLeapMatrix(this Transform t) {
      Vector scale = new Vector(t.lossyScale.x * MM_TO_M, t.lossyScale.y * MM_TO_M, t.lossyScale.z * MM_TO_M);
      LeapTransform transform = new LeapTransform(t.position.ToVector(), t.rotation.ToLeapQuaternion(), scale);
      transform.MirrorZ(); // Unity is left handed.
      return transform;
    }
  }
}
