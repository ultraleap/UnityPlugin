using Leap.Unity.Animation;
using UnityEngine;

namespace Leap.Unity.Animation {

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
    public static HermiteSpline3 ToCHS(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, bool centripetal = true) {
      Vector3 v1, v2;
      CalculateCatmullRomParameterization(p0, p1, p2, p3, out v1, out v2, centripetal);

      return new HermiteSpline3(p1, p2, v1, v2);
    }

    /// <summary>
    /// Calculates the endpoint derivatives at p1 and p2 define a Hermite spline
    /// equivalent to a centripetal Catmull-Rom spline defined by the four input points.
    /// </summary>
    private static void CalculateCatmullRomParameterization(
      Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3,
      out Vector3 v1, out Vector3 v2,
      bool centripetal = true) {
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
        aV1 = Mathq.Log(Quaternion.Inverse(q0) * q2) / 2f;
        aV2 = Mathq.Log(Quaternion.Inverse(q1) * q3) / 2f;
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

  }

  /// <summary>
  /// Quaternion math.
  /// </summary>
  public static class Mathq {

    #region Exponential Quaternion Map

    // Quaternion CHS reference:
    // Kim, Kim, and Shin, 1995.
    // A General Construction Scheme for Unit Quaternion Curves with Simple High Order
    // Derivatives.
    // http://graphics.cs.cmu.edu/nsp/course/15-464/Fall05/papers/kimKimShin.pdf

    // also see Slide 41 of:
    // https://www.cs.indiana.edu/ftp/hanson/Siggraph01QuatCourse/quatvis2.pdf
    // for converting 

    /// <summary>
    /// Exponential Quaternion Map function. Maps Euclidean 3D input space to a
    /// Quaternion for spline math.
    /// 
    /// Some helpful references:
    /// http://graphics.cs.cmu.edu/nsp/course/15-464/Fall05/papers/kimKimShin.pdf
    /// https://www.cs.indiana.edu/ftp/hanson/Siggraph01QuatCourse/quatvis2.pdf
    /// </summary>
    public static Quaternion Exp(Vector3 angleAxisVector) {
      var angle = angleAxisVector.magnitude;
      var axis = angleAxisVector / angle;
      return Quaternion.AngleAxis(angle, axis);
    }

    /// <summary>
    /// Exponential Quaternion Map function (inverse). Maps a Quaternion to reduced
    /// Euclidean 3D space for spline math.
    /// 
    /// Some helpful references:
    /// http://graphics.cs.cmu.edu/nsp/course/15-464/Fall05/papers/kimKimShin.pdf
    /// https://www.cs.indiana.edu/ftp/hanson/Siggraph01QuatCourse/quatvis2.pdf
    /// </summary>
    public static Vector3 Log(Quaternion quaternion) {
      return quaternion.ToAngleAxisVector();
    }

    #endregion
  }

}