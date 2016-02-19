/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

namespace Leap {

  /** 
   * Extends the Leap Motion Vector class to converting points and directions from the
   * Leap Motion coordinate system into the Unity coordinate system.
   */
  public static class UnityVectorExtension {

    // Leap coordinates are in mm and Unity is in meters. So scale by 1000.
    /** Scale factor from Leap units (millimeters) to Unity units (meters). */
    public const float INPUT_SCALE = 0.001f;
    /** Constant used when converting from right-handed to left-handed axes.*/
    public static readonly Vector3 Z_FLIP = new Vector3(1, 1, 1);

    /** 
     * Converts a direction vector from Leap to Unity. (Does not scale.) 
     * 
     * Changes from the Leap Motion right-hand coordinate convention to the
     * Unity left-handed convention by negating the z-coordinate.
     *
     * @param mirror If true, the vector is reflected along the z axis.
     * @param leap_vector the Leap.Vector object to convert.
     */
    public static Vector3 ToUnity(this Vector leap_vector) {
      return FlipZ(ToVector3(leap_vector));
    }

    /** 
     * Converts a point from Leap to Unity. (Scales.)
     * 
     * Changes from the Leap Motion right-hand coordinate convention to the
     * Unity left-handed convention by negating the z-coordinate. Also scales
     * from Leap Motion millimeter units to Unity meter units by multiplying
     * the vector by .001.
     *
     * @param mirror If true, the vector is reflected along the z axis.
     * @param leap_vector the Leap.Vector object to convert. 
     */
    public static Vector3 ToUnityScaled(this Vector leap_vector) {      
      //return INPUT_SCALE * FlipZ(ToVector3(leap_vector));
      return FlipZ(ToVector3(leap_vector));

    }

    private static Vector3 FlipZ(Vector3 vector) {
      return Vector3.Scale(vector, Z_FLIP);
    }

    public static Vector3 ToVector3(this Vector vector) {
      return new Vector3(vector.x, vector.y, vector.z);
    }
  }

  /**
   * Extends the Leap Mition Matrix class to convert Leap Matrix objects to
   * to Unity Quaternion rotations and translations.
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
     * @param matrix The Leap.Matrix to convert.
     * @param mirror If true, the operation is reflected along the z axis.
     */
    public static Quaternion Rotation(this Matrix matrix) {
      //Vector3 up = matrix.TransformDirection(LEAP_UP).ToUnity(mirror);
      //Vector3 forward = matrix.TransformDirection(LEAP_FORWARD).ToUnity(mirror);
      Vector3 up = matrix.yBasis.ToUnity();
      Vector3 forward = -matrix.zBasis.ToUnity();
      return Quaternion.LookRotation(forward, up);
    }

    /**
     * Converts a Leap Matrix object representing a translation to a 
     * Unity Vector3 object.
     * 
     * @param matrix The Leap.Matrix to convert.
     * @param mirror If true, the operation is reflected along the z axis.
     */
    public static Vector3 Translation(this Matrix matrix, bool mirror = false) {
      return matrix.TransformPoint(LEAP_ORIGIN).ToUnityScaled();
    }
    /**
     * 
     */
    public static Matrix GetLeapMatrix(Transform t) {
      Vector xbasis = new Vector(t.right.x, t.right.y, t.right.z) * t.lossyScale.x * MM_TO_M;
      Vector ybasis = new Vector(t.up.x, t.up.y, t.up.z) * t.lossyScale.y * MM_TO_M;
      Vector zbasis = new Vector(t.forward.x, t.forward.y, t.forward.z) * -t.lossyScale.z * MM_TO_M;
      Vector trans = new Vector(t.position.x, t.position.y, t.position.z);
      return new Matrix(xbasis, ybasis, zbasis, trans);
    }
  }
}
