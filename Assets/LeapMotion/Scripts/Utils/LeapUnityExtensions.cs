/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2016.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

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
  }

  /**
   * Unity extentions for the Leap Motion Matrix class.
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
     * Converts a Leap Matrix object representing a rotation to a 
     * Unity Quaternion.
     * 
     * In previous version prior 4.0.0 this function performed a conversion to Unity's left-handed coordinate system, and now does not.
     * 
     * @returns A Unity Quaternion representing the rotation.
     */
    public static Quaternion Rotation(this Matrix matrix) {
      Vector3 up = matrix.yBasis.ToVector3();
      Vector3 forward = -matrix.zBasis.ToVector3();
      return Quaternion.LookRotation(forward, up);
    }

    /**
     * Extracts a transform matrix containing translation, rotation, and scale from a Unity Transform object and 
     * returns a Leap Motion Matrix object.
     * Use this matrix to transform Leap Motion tracking data to the Unity world relative to the
     * specified transform.
     *
     * In addition to applying the translation, rotation, and scale from the Transform object, the returned 
     * transformation changes the coordinate system from right- to left-handed and converts units from millimeters to meters
     * by scaling.
     * @returns A Leap.Matrix object representing the specified transform from Leap Motion into Unity space.
     */
    public static Matrix GetLeapMatrix(this Transform t) {
      Vector xbasis = new Vector(t.right.x, t.right.y, t.right.z) * t.lossyScale.x * MM_TO_M;
      Vector ybasis = new Vector(t.up.x, t.up.y, t.up.z) * t.lossyScale.y * MM_TO_M;
      Vector zbasis = new Vector(t.forward.x, t.forward.y, t.forward.z) * -t.lossyScale.z * MM_TO_M;
      Vector trans = new Vector(t.position.x, t.position.y, t.position.z);
      return new Matrix(xbasis, ybasis, zbasis, trans);
    }
  }
}
