/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.Animation {

  /// <summary>
  /// Represents a spline for the rotation of a rigid body from one orientation in space
  /// to another over a specified time frame.  The two endpoints are specified, as well
  /// as the instantaneous angular velocity at those two endpoints.
  /// 
  /// You may ask for the position, rotation, velocity, or angular velocity at any time
  /// along the spline's duration.
  /// </summary>
  [Serializable]
  public struct HermiteQuaternionSpline : ISpline<Quaternion, Vector3> {

    public float t0, t1;
    public Quaternion rot0, rot1;
    public Vector3 angVel0, angVel1;

    /// <summary>
    /// Constructs a Quaternion spline by specifying the rotations at the two endpoints.
    /// The angular velocity at each endpoint is zero, and the time range of the spline
    /// is 0 to 1.
    /// </summary>
    public HermiteQuaternionSpline(Quaternion rot0, Quaternion rot1) {
      t0 = 0;
      t1 = 1;

      this.angVel0 = default(Vector3);
      this.angVel1 = default(Vector3);

      this.rot0 = rot0;
      this.rot1 = rot1;
    }
    
    /// <summary>
    /// Constructs a Quaternion spline by specifying the rotations (as quaternions) and
    /// angular velocities (as axis * angle vectors) at the endpoints.
    /// 
    /// The time range of the spline is 0 to 1.
    /// </summary>
    public HermiteQuaternionSpline(Quaternion rot0, Quaternion rot1,
                                   Vector3 angVel0, Vector3 angVel1) {
      t0 = 0;
      t1 = 1;

      this.angVel0 = angVel0;
      this.angVel1 = angVel1;

      this.rot0 = rot0;
      this.rot1 = rot1;
    }

    /// <summary>
    /// Constructs a Quaternion spline by specifying the rotations (as quaternions) and
    /// angular velocities (as axis * angle vectors) at the endpoints.
    /// 
    /// The time range of the spline is 0 to duration.
    /// </summary>
    public HermiteQuaternionSpline(Quaternion rot0, Quaternion rot1,
                                   Vector3 angVel0, Vector3 angVel1,
                                   float duration) {
      t0 = 0;
      t1 = duration;

      this.angVel0 = angVel0;
      this.angVel1 = angVel1;

      this.rot0 = rot0;
      this.rot1 = rot1;
    }

    /// <summary>
    /// Constructs a spline by specifying the rotations, angular velocities, and times of
    /// the endpoints.
    /// </summary>
    public HermiteQuaternionSpline(float t0, float t1,
                                   Quaternion rot0, Quaternion rot1,
                                   Vector3 angVel0, Vector3 angVel1) {
      this.t0 = t0;
      this.t1 = t1;

      this.angVel0 = angVel0;
      this.angVel1 = angVel1;

      this.rot0 = rot0.ToNormalized();
      this.rot1 = rot1.ToNormalized();
    }

    /// <summary>
    /// Gets the rotation at time t along this spline. The time is clamped within the
    /// t0 - t1 range.
    /// </summary>
    public Quaternion RotationAt(float t) {
      if (t > t1) {
        float i = ((t - t0) / (t1 - t0)) - 1f;
        return rot1 * Mathq.Exp(angVel1 * i); //unsure of the ordering here...
      } else {
        float i = Mathf.Clamp01((t - t0) / (t1 - t0));
        float i2 = i * i;
        float i3 = i2 * i;

        float dt = t1 - t0;

        var oneThird = 1 / 3f;

        var w1 = Quaternion.Inverse(rot0) * angVel0 * dt * oneThird;
        var w3 = Quaternion.Inverse(rot1) * angVel1 * dt * oneThird;
        var w2 = Mathq.Log(Mathq.Exp(-w1)
                      * Quaternion.Inverse(rot0)
                      * rot1
                      * Mathq.Exp(-w3));

        var beta1 = i3 - (3 * i2) + (3 * i);
        var beta2 = -2 * i3 + 3 * i2;
        var beta3 = i3;

        return rot0 * Mathq.Exp(w1 * beta1) * Mathq.Exp(w2 * beta2) * Mathq.Exp(w3 * beta3);
      }
      // A cubic Bezier quaternion curve can be used to define a Hermite quaternion curve
      // which interpolates two end unit quaternions, q_a and q_b, and two angular
      // velocities omega_a and omega_b.
      //
      // var q_at_t = q_0 * PRODUCT(i = 1, i < 3, exp(omega_i * beta_q(i, t))
      // "where beta_q(i, t) is beta_q(i, 3, t) for i = 1, 2, 3."
      // 
      // q coefficients:
      // q_0 = q_a
      // q_1 = q_a * exp(omega_a / 3)
      // q_2 = q_b * exp(omega_b / 3)^-1
      // q_3 = q_b
      //
      // omega coefficients:
      // omega_1 = omega_a / 3
      // omega_2 = log(exp(omega_a / 3)^-1 * q_a^-1 * q_b * exp(omega_b / 3)^-1)
      // omega_3 = omega_b / 3
      //
      // Dependencies/definitions:
      // "Bernstein basis", referred to as "beta"
      // beta(i, n, t) = (n Choose i) * (1 - t)^(n - i) * t^i
      // The form used in the formula are the cumulative basis functions:
      // beta_q(i, n, t) = SUM(j = i, n, beta(i, n, t))
      //
      // The Exponential Mapping exp(v) and its Inverse
      // "The exponential map can be interpreted as a mapping from the angular velocity
      // vector (measured in S^3) into the unit quaternion which represents the rotation."
      // ...
      // "Given a vector v = theta * v_norm (in R^3), the exponential:
      // exp(v) = SUM(i = 0, i -> inf, v^i) = (cos(theta), v_norm * sin(theta)) in S^3
      // becomes the unit quaternion which represents the rotation by angle 2*theta
      // about the axis v_norm, where v^i is computed using the quaternion multiplication."
      // 
      // In other words... The Exponential converts a VECTOR represented by an ANGLE
      // and a normalized AXIS to a quaternion. A Unity function for this:
      // exp(v) -> Q := Quaternion.AngleAxis(v.magnitude, v.normalized);
      //
      // Its inverse map, log, would thus be the conversion from a Quaternion to an
      // Angle-Axis representation. Quaternion.ToAngleAxisVector():
      // log(Q) -> v := Q.ToAngleAxisVector() -> v
    }

    /// <summary>
    /// Gets the first derivative of rotation at time t. The time is clamped within the
    /// t0 - t1 range. Angular velocity is encoded as an angle-axis vector.
    /// </summary>
    public Vector3 AngularVelocityAt(float t) {
      float i = Mathf.Clamp01((t - t0) / (t1 - t0));
      float i2 = i * i;
      float i3 = i2 * i;

      float dt = t1 - t0;

      var oneThird = 1 / 3f;

      var w1 = Quaternion.Inverse(rot0) * angVel0 * dt * oneThird;
      var w3 = Quaternion.Inverse(rot1) * angVel1 * dt * oneThird;
      var w2 = (Mathq.Exp(-w1)
                * Quaternion.Inverse(rot0)
                * rot1
                * Mathq.Exp(-w3)).ToAngleAxisVector();

      var beta1 = i3 - (3 * i2) + (3 * i);
      var beta2 = -2 * i3 + 3 * i2;
      //var beta3 = i3;

      // Derivatives of beta1, beta2, beta3
      var dotBeta1 = 3 * i2 - 5 * i + 3;
      var dotBeta2 = -6 * i2 + 6 * i;
      var dotBeta3 = 3 * i2;

      var rot0_times_w1beta1 = rot0 * Mathq.Exp(w1 * beta1);

      return
        rot0 * w1 * dotBeta1 +
        rot0_times_w1beta1 * w2 * dotBeta2 +
        rot0_times_w1beta1 * Mathq.Exp(w2 * beta2) * w3 * dotBeta3;
    }

    /// <summary>
    /// Gets both the rotation and the first derivative of rotation at time t. The time
    /// is clamped within the t0 - t1 range. Angular velocity is encoded as an angle-axis
    /// vector.
    /// </summary>
    public void RotationAndAngVelAt(float t, out Quaternion rotation,
                                             out Vector3 angularVelocity) {
      float i = Mathf.Clamp01((t - t0) / (t1 - t0));
      float i2 = i * i;
      float i3 = i2 * i;

      float dt = t1 - t0;

      var oneThird = 1 / 3f;

      var w1 = Quaternion.Inverse(rot0) * angVel0 * dt * oneThird;
      var w3 = Quaternion.Inverse(rot1) * angVel1 * dt * oneThird;
      var w2 = Mathq.Log(Mathq.Exp(-w1)
                    * Quaternion.Inverse(rot0)
                    * rot1
                    * Mathq.Exp(-w3));

      var beta1 = i3 - (3 * i2) + (3 * i);
      var beta2 = -2 * i3 + 3 * i2;
      var beta3 = i3;

      rotation =
        rot0 * Mathq.Exp(w1 * beta1) * Mathq.Exp(w2 * beta2) * Mathq.Exp(w3 * beta3);

      // Derivatives of beta1, beta2, beta3
      var dotBeta1 = 3 * i2 - 5 * i + 3;
      var dotBeta2 = -6 * i2 + 6 * i;
      var dotBeta3 = 3 * i2;

      var rot0_times_w1beta1 = rot0 * Mathq.Exp(w1 * beta1);

      angularVelocity =
        rot0 * w1 * dotBeta1 +
        rot0_times_w1beta1 * w2 * dotBeta2 +
        rot0_times_w1beta1 * Mathq.Exp(w2 * beta2) * w3 * dotBeta3;
    }

    #region ISpline<Quaternion, Vector3>

    public float minT { get { return t0; } }

    public float maxT { get { return t1; } }

    public Quaternion ValueAt(float t) {
      return RotationAt(t);
    }

    public Vector3 DerivativeAt(float t) {
      return AngularVelocityAt(t);
    }

    public void ValueAndDerivativeAt(float t, out Quaternion value,
                                              out Vector3 deltaValuePerSec) {
      RotationAndAngVelAt(t, out value, out deltaValuePerSec);
    }

    #endregion

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
