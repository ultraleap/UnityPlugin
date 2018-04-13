/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.Animation {

  /// <summary>
  /// Represents a spline that travels from one point in space
  /// to another over a specified time frame.  The two endpoints 
  /// are specified, as well as the instantaneous velocity at 
  /// these two endpoints.
  /// 
  /// You may ask for the position or the velocity at any time
  /// during the splines duration.
  /// </summary>
  [Serializable]
  public struct HermiteSpline3 {
    public float t0, t1;
    public Vector3 pos0, pos1;
    public Vector3 vel0, vel1;

    /// <summary>
    /// Constructs a spline by specifying the positions of the
    /// two endpoints.  The velocity at each endpoint is zero,
    /// and the time range of the spline is 0 to 1.
    /// </summary>
    public HermiteSpline3(Vector3 pos0, Vector3 pos1) {
      t0 = 0;
      t1 = 1;

      vel0 = default(Vector3);
      vel1 = default(Vector3);

      this.pos0 = pos0;
      this.pos1 = pos1;
    }

    /// <summary>
    /// Constructs a spline by specifying the positions and 
    /// velocities of the two endpoints.  The time range of
    /// the spline is 0 to 1.
    /// </summary>
    public HermiteSpline3(Vector3 pos0, Vector3 pos1, Vector3 vel0, Vector3 vel1) {
      t0 = 0;
      t1 = 1;

      this.vel0 = vel0;
      this.vel1 = vel1;

      this.pos0 = pos0;
      this.pos1 = pos1;
    }

    /// <summary>
    /// Constructs a spline by specifying the positions and
    /// velocities of the two endpoints.  The time range of
    /// the spline is 0 to length.
    /// </summary>
    public HermiteSpline3(Vector3 pos0, Vector3 pos1, Vector3 vel0, Vector3 vel1, float length) {
      t0 = 0;
      t1 = length;

      this.vel0 = vel0;
      this.vel1 = vel1;

      this.pos0 = pos0;
      this.pos1 = pos1;
    }

    /// <summary>
    /// Constructs a spline by specifying the positions,
    /// velocities, and times of the endpoints.
    /// </summary>
    public HermiteSpline3(float t0, float t1, Vector3 pos0, Vector3 pos1, Vector3 vel0, Vector3 vel1) {
      this.t0 = t0;
      this.t1 = t1;

      this.vel0 = vel0;
      this.vel1 = vel1;

      this.pos0 = pos0;
      this.pos1 = pos1;
    }

    /// <summary>
    /// Gets the position at time t along this spline.  
    /// The time is clamped within the t0 - t1 range.
    /// </summary>
    public Vector3 PositionAt(float t) {
      float i = Mathf.Clamp01((t - t0) / (t1 - t0));
      float i2 = i * i;
      float i3 = i2 * i;

      Vector3 h00 = (2 * i3 - 3 * i2 + 1) * pos0;
      Vector3 h10 = (i3 - 2 * i2 + i) * (t1 - t0) * vel0;
      Vector3 h01 = (-2 * i3 + 3 * i2) * pos1;
      Vector3 h11 = (i3 - i2) * (t1 - t0) * vel1;

      return h00 + h10 + h01 + h11;
    }

    /// <summary>
    /// Gets the first derivative of position at time t.
    /// The time is clamped within the t0 - t1 range.
    /// </summary>
    public Vector3 VelocityAt(float t) {
      float C00 = t1 - t0;
      float C1 = 1.0f / C00;

      float i, i2;
      float i_, i2_, i3_;
      {
        i = Mathf.Clamp01((t - t0) * C1);
        i_ = C1;

        i2 = i * i;
        i2_ = 2 * i * i_;

        i3_ = i2_ * i + i_ * i2;
      }

      Vector3 h00_ = (i3_ * 2 - i2_ * 3) * pos0;
      Vector3 h10_ = (i3_ - 2 * i2_ + i_) * C00 * vel0;
      Vector3 h01_ = (i2_ * 3 - 2 * i3_) * pos1;
      Vector3 h11_ = (i3_ - i2_) * C00 * vel1;

      return h00_ + h01_ + h10_ + h11_;
    }

    /// <summary>
    /// Gets both the position and the first derivative of position
    /// at time ti.  The time is clamped within the t0 - t1 range.
    /// </summary>
    public void PositionAndVelAt(float t, out Vector3 position, out Vector3 velocity) {
      float C00 = t1 - t0;
      float C1 = 1.0f / C00;

      float i, i2, i3;
      float i_, i2_, i3_;
      {
        i = Mathf.Clamp01((t - t0) * C1);
        i_ = C1;

        i2 = i * i;
        i2_ = 2 * i * i_;

        i3 = i2 * i;
        i3_ = i2_ * i + i_ * i2;
      }

      Vector3 h00 = (2 * i3 - 3 * i2 + 1) * pos0;
      Vector3 h00_ = (i3_ * 2 - i2_ * 3) * pos0;

      Vector3 h10 = (i3 - 2 * i2 + i) * C00 * vel0;
      Vector3 h10_ = (i3_ - 2 * i2_ + i_) * C00 * vel0;

      Vector3 h01 = (3 * i2 - 2 * i3) * pos1;
      Vector3 h01_ = (i2_ * 3 - 2 * i3_) * pos1;

      Vector3 h11 = (i3 - i2) * C00 * vel1;
      Vector3 h11_ = (i3_ - i2_) * C00 * vel1;

      position = h00 + h01 + h10 + h11;
      velocity = h00_ + h01_ + h10_ + h11_;
    }
  }
}
