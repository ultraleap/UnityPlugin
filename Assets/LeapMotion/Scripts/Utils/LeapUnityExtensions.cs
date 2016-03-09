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
     * In previous version prior 4.0.0 this function performed a conversion to Unity's left-handed coordinate system, and now does not.
     * 
     * @param matrix The Leap.Matrix to convert.
     * @param mirror If true, the operation is reflected along the z axis.
     */
    public static Quaternion Rotation(this Matrix matrix) {
      Vector3 up = matrix.yBasis.ToVector3();
      Vector3 forward = -matrix.zBasis.ToVector3();
      return Quaternion.LookRotation(forward, up);
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
