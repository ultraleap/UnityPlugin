/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Animation;
using UnityEngine;

namespace Leap.Unity.Splines {

  /// <summary>
  /// Interpolate between points using Catmull-Rom splines.
  /// </summary>
  public static class CatmullRom {

    /// <summary>
    /// Construct a HermiteSpline3 equivalent to the centripetal Catmull-Rom spline
    /// defined by the four input points.
    /// 
    /// Check out this Catmull-Rom implementation for the source on this:
    /// https://stackoverflow.com/a/23980479/2471635
    /// </summary>
    public static HermiteSpline3 ToCHS(
      Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
      bool centripetal = true)
    {
      Vector3 v1, v2;
      CalculateCatmullRomParameterization(p0, p1, p2, p3, out v1, out v2,
        centripetal);

      return new HermiteSpline3(p1, p2, v1, v2);
    }

    /// <summary>
    /// Calculates the endpoint derivatives at p1 and p2 define a Hermite spline
    /// equivalent to a centripetal Catmull-Rom spline defined by the four input points.
    /// </summary>
    private static void CalculateCatmullRomParameterization(
      Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
      out Vector3 v1, out Vector3 v2,
      bool centripetal = true)
    {
      v1 = Vector3.zero;
      v2 = Vector3.zero;

      if (!centripetal) {
        // (Uniform Catmull-Rom)
        v1 = (p2 - p0) / 2f;
        v2 = (p3 - p1) / 2f;
      } else {
        // Centripetal Catmull-Rom
        var dt0 = Mathf.Pow((p0 - p1).sqrMagnitude, 0.25f);
        var dt1 = Mathf.Pow((p1 - p2).sqrMagnitude, 0.25f);
        var dt2 = Mathf.Pow((p2 - p3).sqrMagnitude, 0.25f);

        // Check for repeated points.
        if (dt1 < 1e-4f) dt1 = 1.0f;
        if (dt0 < 1e-4f) dt0 = dt1;
        if (dt2 < 1e-4f) dt2 = dt1;

        // Centripetal Catmull-Rom
        v1 = (p1 - p0) / dt0 - (p2 - p0) / (dt0 + dt1) + (p2 - p1) / dt1;
        v2 = (p2 - p1) / dt1 - (p3 - p1) / (dt1 + dt2) + (p3 - p2) / dt2;

        // Rescale for [0, 1] parameterization
        v1 *= dt1;
        v2 *= dt1;
      }
    }

    /// <summary>
    /// Construct a HermiteQuaternionSpline equivalent to the centripetal Catmull-Rom
    /// spline defined by the four input rotations.
    /// </summary>
    public static HermiteQuaternionSpline ToQuaternionCHS(
      Quaternion q0, Quaternion q1, Quaternion q2, Quaternion q3,
      bool centripetal = true)
    {
      Vector3 aV1, aV2;
      CalculateCatmullRomParameterization(q0, q1, q2, q3, out aV1, out aV2, centripetal);

      return new HermiteQuaternionSpline(q1, q2, aV1, aV2);
    }

    /// <summary>
    /// Calculates the endpoint derivatives at q1 and q2 that define a Hermite spline
    /// equivalent to a centripetal Catmull-Rom spline defined by the four input
    /// rotations. "aV" refers to angular velocity and represents an angle-axis vector.
    /// </summary>
    private static void CalculateCatmullRomParameterization(Quaternion q0, Quaternion q1,
                                                            Quaternion q2, Quaternion q3,
                                                            out Vector3 aV1,
                                                            out Vector3 aV2,
                                                            bool centripetal = true) {
      aV1 = Vector3.zero;
      aV2 = Vector3.zero;

      if (!centripetal) {
        // (Uniform Catmull-Rom)
        aV1 = Mathq.Log(Quaternion.Inverse(q0) * q2)/2f;
        aV2 = Mathq.Log(Quaternion.Inverse(q1) * q3)/2f;
      } else {
        // Centripetal Catmull-Rom
        // Handy little trick for using Euclidean-3 spline math on Quaternions, see
        // slide 41 of
        // https://www.cs.indiana.edu/ftp/hanson/Siggraph01QuatCourse/quatvis2.pdf
        // "The trick: Where you would see 'x0 + t(x1 - x0)' in a Euclidean spline,
        // replace (x1 - x0) by log((q0)^-1 * q1) "

        // Centripetal Catmull-Rom parameterization
        var dt0 = Mathf.Pow(Mathq.Log(Quaternion.Inverse(q1) * q0).sqrMagnitude, 0.25f);
        var dt1 = Mathf.Pow(Mathq.Log(Quaternion.Inverse(q2) * q1).sqrMagnitude, 0.25f);
        var dt2 = Mathf.Pow(Mathq.Log(Quaternion.Inverse(q3) * q2).sqrMagnitude, 0.25f);

        // Check for repeated points.
        if (dt1 < 1e-4f) dt1 = 1.0f;
        if (dt0 < 1e-4f) dt0 = dt1;
        if (dt2 < 1e-4f) dt2 = dt1;

        // A - B -> Mathq.Log(Quaternion.Inverse(B) * A)

        // Centripetal Catmull-Rom
        aV1 = Mathq.Log(Quaternion.Inverse(q0) * q1) / dt0
            - Mathq.Log(Quaternion.Inverse(q0) * q2) / (dt0 + dt1)
            + Mathq.Log(Quaternion.Inverse(q1) * q2) / dt1;
        aV2 = Mathq.Log(Quaternion.Inverse(q1) * q2) / dt1
            - Mathq.Log(Quaternion.Inverse(q1) * q3) / (dt1 + dt2)
            + Mathq.Log(Quaternion.Inverse(q2) * q3) / dt2;

        // Rescale for [0, 1] parameterization
        aV1 *= dt1;
        aV2 *= dt1;
      }
    }

    public static HermitePoseSpline ToPoseCHS(Pose p0, Pose p1, Pose p2, Pose p3) {
      Vector3 v1, v2, aV1, aV2;
      CalculateCatmullRomParameterization(p0.position, p1.position,
                                          p2.position, p3.position,
                                          out v1, out v2);
      CalculateCatmullRomParameterization(p0.rotation, p1.rotation,
                                          p2.rotation, p3.rotation,
                                          out aV1, out aV2);

      return new HermitePoseSpline(p1, p2, new Movement(v1, aV1), new Movement(v2, aV2));
    }

    #region Archived code -- legacy code relies on this. It is incorrect though!

    // TODO: The below isn't ACTUALLY a centripetal Catmull-Rom spline, it's just a
    // spline parameterization -- centripetal-ness requires choosing "time values" that
    // correspond to the distances between the four positions. The implementation
    // above is better.
    // deleeeeete it!

    /// <summary>
    /// Performs centripetal Catmull-Rom interpolation between the second and third
    /// spline points and writes evenly-distributed points along the spline into interpolatedPoints.
    /// </summary>
    public static void InterpolatePoints(Vector3[] fourPositions, float[] timeValues, ref Vector3[] outPoints) {
      InterpolatePoints(fourPositions, timeValues, ref outPoints, numPoints: outPoints.Length);
    }

    /// <summary>
    /// Performs NOT centripetal Catmull-Rom interpolation between the second and third
    /// spline points and writes evenly-distributed points along the spline into
    /// interpolatedPoints. Use numPoints to specify the number of points to write in
    /// the output buffer.
    /// 
    /// The first point will be the first input position; the last point will be the last
    /// input position.
    /// </summary>
    public static void InterpolatePoints(Vector3[] fourPositions, float[] timeValues,
                                         ref Vector3[] outPoints, int numPoints) {
      for (int i = 0; i < numPoints; i++) {
        outPoints[i] = Interpolate(fourPositions, timeValues, i * (1F / (numPoints - 1)));
      }
    }

    /// <summary> Evaluates the Catmull-Rom spline between p1 and p2 given points P and times T at time t. </summary>
    // http://stackoverflow.com/questions/9489736/catmull-rom-curve-with-no-cusps-and-no-self-intersections
    public static Vector3 Interpolate(Vector3[] P, float[] T, float t) {
      Vector3 L01 = P[0] * (T[1] - t) / (T[1] - T[0]) + P[1] * (t - T[0]) / (T[1] - T[0]);
      Vector3 L12 = P[1] * (T[2] - t) / (T[2] - T[1]) + P[2] * (t - T[1]) / (T[2] - T[1]);
      Vector3 L23 = P[2] * (T[3] - t) / (T[3] - T[2]) + P[3] * (t - T[2]) / (T[3] - T[2]);
      Vector3 L012 = L01 * (T[2] - t) / (T[2] - T[0]) + L12  * (t - T[0]) / (T[2] - T[0]);
      Vector3 L123 = L12 * (T[3] - t) / (T[3] - T[1]) + L23  * (t - T[1]) / (T[3] - T[1]);
      Vector3 C12 = L012 * (T[2] - t) / (T[2] - T[1]) + L123 * (t - T[1]) / (T[2] - T[1]);
      return C12;
    }

    #endregion

  }

}
