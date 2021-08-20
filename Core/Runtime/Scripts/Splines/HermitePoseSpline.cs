/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using Leap.Unity.RuntimeGizmos;
using UnityEngine;

namespace Leap.Unity.Animation {

  /// <summary>
  /// Represents a spline for poses -- positions and rotations -- that travel from one
  /// position and rotation in space to another over a specified time frame.  The two
  /// endpoints are specified, as well as the instantaneous velocity and angular velocity
  /// at those two endpoints.
  /// 
  /// You may ask for the position, rotation, velocity, or angular velocity at any time
  /// along the spline's duration.
  /// </summary>
  [Serializable]
  public struct HermitePoseSpline : ISpline<Pose, Movement>,
                                    ISpline<Vector3, Vector3> {

    public HermiteSpline3 pSpline;
    public HermiteQuaternionSpline qSpline;

    /// <summary>
    /// Constructs a spline by specifying the poses of the two endpoints. The velocity
    /// and angular velocity at each endpoint is zero, and the time range of the spline
    /// is 0 to 1.
    /// </summary>
    public HermitePoseSpline(Pose pose0, Pose pose1) {
      pSpline = new HermiteSpline3(pose0.position, pose1.position);
      qSpline = new HermiteQuaternionSpline(pose0.rotation, pose1.rotation);
    }

    /// <summary>
    /// Constructs a spline by specifying the poses and movements of the two
    /// endpoints. The time range of the spline is 0 to 1.
    /// </summary>
    public HermitePoseSpline(Pose pose0, Pose pose1, Movement move0, Movement move1) {
      pSpline = new HermiteSpline3(pose0.position, pose1.position,
                                   move0.velocity, move1.velocity);
      qSpline = new HermiteQuaternionSpline(pose0.rotation, pose1.rotation,
                                            move0.angularVelocity, move1.angularVelocity);
    }

    /// <summary>
    /// Constructs a spline by specifying the positions and velocities of the two
    /// endpoints. The time range of the spline is 0 to duration.
    /// </summary>
    public HermitePoseSpline(Pose pose0, Pose pose1,
                             Movement move0, Movement move1,
                             float duration) {
      pSpline = new HermiteSpline3(pose0.position, pose1.position,
                                   move0.velocity, move1.velocity,
                                   duration);
      qSpline = new HermiteQuaternionSpline(pose0.rotation, pose1.rotation,
                                            move0.angularVelocity, move1.angularVelocity,
                                            duration);
    }

    /// <summary>
    /// Constructs a spline by specifying the positions, velocities, and times of the
    /// endpoints.
    /// </summary>
    public HermitePoseSpline(float t0, float t1,
                             Pose pose0, Pose pose1,
                             Movement move0, Movement move1) {
      pSpline = new HermiteSpline3(t0, t1,
                                   pose0.position, pose1.position,
                                   move0.velocity, move1.velocity);
      qSpline = new HermiteQuaternionSpline(t0, t1,
                                            pose0.rotation, pose1.rotation,
                                            move0.angularVelocity, move1.angularVelocity);
    }

    /// <summary>
    /// Gets the position at time t along this spline. The time is clamped within the
    /// t0 - t1 range.
    /// </summary>
    public Vector3 PositionAt(float t) {
      return pSpline.PositionAt(t);
    }

    /// <summary>
    /// Gets the rotation at time t along this spline. The time is clamped within the
    /// t0 - t1 range.
    /// </summary>
    public Quaternion RotationAt(float t) {
      return qSpline.RotationAt(t);
    }

    /// <summary>
    /// Gets the pose at time t along this spline. The time is clamped within the t0 - t1
    /// range.
    /// </summary>
    public Pose PoseAt(float t) {
      return new Pose(PositionAt(t), RotationAt(t));
    }

    /// <summary>
    /// Gets the first derivative of position at time t. The time is clamped within the
    /// t0 - t1 range.
    /// </summary>
    public Vector3 VelocityAt(float t) {
      return pSpline.VelocityAt(t);
    }

    /// <summary>
    /// Gets the first derivative of rotation at time t. The time is clamped within the
    /// t0 - t1 range. Angular velocity is encoded as an angle-axis vector.
    /// </summary>
    public Vector3 AngularVelocityAt(float t) {
      return qSpline.AngularVelocityAt(t);
    }

    public Movement MovementAt(float t) {
      return new Movement(VelocityAt(t), AngularVelocityAt(t));
    }

    /// <summary>
    /// Gets both the position and the first derivative of position at time t. The time
    /// is clamped within the t0 - t1 range.
    /// </summary>
    public void PositionAndVelAt(float t, out Vector3 position, out Vector3 velocity) {
      pSpline.PositionAndVelAt(t, out position, out velocity);
    }

    /// <summary>
    /// Gets both the rotation and the first derivative of rotation at time t. The time
    /// is clamped within the t0 - t1 range. Angular velocity is encoded as an angle-axis
    /// vector.
    /// </summary>
    public void RotationAndAngVelAt(float t, out Quaternion rotation,
                                             out Vector3 angularVelocity) {
      qSpline.RotationAndAngVelAt(t, out rotation, out angularVelocity);
    }


    /// <summary>
    /// Gets both the rotation and the first derivative of rotation at time t. The time
    /// is clamped within the t0 - t1 range. Angular velocity is encoded as an angle-axis
    /// vector.
    /// 
    /// Gets both the pose and position/rotation first derivative at time t. The time is
    /// clamped within the t0 - t1 range. Angular velocity is encoded as an angle-axis
    /// vector.
    /// </summary>
    public void PoseAndMovementAt(float t, out Pose pose,
                                           out Movement movement) {
      Vector3 pos, vel, angVel;
      Quaternion rot;
      pSpline.PositionAndVelAt(t, out pos, out vel);
      qSpline.RotationAndAngVelAt(t, out rot, out angVel);
      pose = new Pose(pos, rot);
      movement = new Movement(vel, angVel);
    }

    #region ISpline<Pose, Movement>

    public float minT { get { return pSpline.t0; } }

    public float maxT { get { return pSpline.t1; } }

    public Pose ValueAt(float t) {
      return PoseAt(t);
    }

    public Movement DerivativeAt(float t) {
      return MovementAt(t);
    }

    public void ValueAndDerivativeAt(float t, out Pose value, out Movement deltaValuePerSec) {
      PoseAndMovementAt(t, out value, out deltaValuePerSec);
    }

    #endregion

    #region ISpline<Vector3, Vector3>
    
    float ISpline<Vector3, Vector3>.minT { get { return pSpline.t0; } }

    float ISpline<Vector3, Vector3>.maxT { get { return pSpline.t1; } }

    Vector3 ISpline<Vector3, Vector3>.ValueAt(float t) {
      return PoseAt(t).position;
    }

    Vector3 ISpline<Vector3, Vector3>.DerivativeAt(float t) {
      return MovementAt(t).velocity;
    }

    void ISpline<Vector3, Vector3>.ValueAndDerivativeAt(float t, 
                                                        out Vector3 value,
                                                        out Vector3 deltaValuePerT) {
      Pose pose;
      Movement movement;
      PoseAndMovementAt(t, out pose, out movement);

      value = pose.position;
      deltaValuePerT = movement.velocity;
    }

    #endregion

  }

  public static class HermitePoseSplineExtensions {
    
    public static void DrawPoseSpline(this RuntimeGizmos.RuntimeGizmoDrawer drawer,
                                      HermitePoseSpline spline,
                                      Color? color = null,
                                      float poseGizmoScale = 0.02f,
                                      int splineResolution = 32,
                                      int drawPosePeriod = 8,
                                      bool drawPoses = true,
                                      bool drawSegments = true) {
      if (!color.HasValue) {
        color = LeapColor.brown.WithAlpha(0.4f);
      }
      drawer.color = color.Value;

      var tWidth = spline.maxT - spline.minT;

      Vector3? prevPos = null;
      int counter = 0;
      float tStep = (1f / splineResolution) * tWidth;
      for (float t = spline.minT; t <= spline.minT + tWidth; t += tStep) {
        var pose = spline.PoseAt(t);

        if (counter % drawPosePeriod == 0 && drawPoses) {
          drawer.DrawPose(pose, 0.02f);
        }

        if (prevPos.HasValue && drawSegments) {
          drawer.DrawLine(prevPos.Value, pose.position);
        }

        prevPos = pose.position;
        counter++;
      }
    }

  }
}
