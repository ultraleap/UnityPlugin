/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity {

  public struct Movement {

    /// <summary>
    /// The linear velocity of this Movement.
    /// </summary>
    public Vector3 velocity;

    /// <summary>
    /// Angular velocity expressed as an angle-axis vector with angle equal to the length
    /// of the vector in degrees.
    /// </summary>
    public Vector3 angularVelocity;

    public static readonly Movement identity = new Movement();

    public Movement inverse {
      get { return new Movement(-velocity, -angularVelocity); }
    }

    public static Movement operator *(Movement movement, float multiplier) {
      return new Movement(movement.velocity * multiplier,
                          movement.angularVelocity * multiplier);
    }

    public static Movement operator /(Movement movement, float divisor) {
      return movement * (1f / divisor);
    }

    public Pose ToPose() {
      var angVelMag = angularVelocity.magnitude;
      return new Pose(velocity,
        Quaternion.AngleAxis(angVelMag, angularVelocity / angVelMag));
    }

    public static Movement operator +(Movement movement0, Movement movement1) {
      return new Movement(movement0.velocity + movement1.velocity,
                          movement0.angularVelocity + movement1.angularVelocity);
    }
    
    /// <summary>
    /// Constructs a linear Movement involving no rotation.
    /// </summary>
    public Movement(Vector3 velocity) {
      this.velocity = velocity;
      this.angularVelocity = Vector3.zero;
    }
    
    /// <summary>
    /// Constructs a Movement with a specified linear velocity and an angular velocity.
    /// </summary>
    public Movement(Vector3 velocity, Vector3 angularVelocity) {
      this.velocity = velocity;
      this.angularVelocity = angularVelocity;
    }

    /// <summary>
    /// Returns the Movement necessary to go from Pose p0 to Pose p1 in dt seconds.
    /// You can ignore the time parameter if you wish simply to store delta positions
    /// and angle-axis vector rotations.
    /// </summary>
    public Movement(Pose fromPose, Pose toPose, float dt = 1f) {
      Vector3 deltaPosition = toPose.position - fromPose.position;
      Quaternion deltaRotation = Quaternion.Inverse(fromPose.rotation) * toPose.rotation;

      this.velocity = deltaPosition / dt;
      this.angularVelocity = deltaRotation.ToAngleAxisVector() / dt;
    }

    #region Accelerations

    /// <summary>
    /// Discretely integrates this Movement's velocity by a linear acceleration over
    /// deltaTime.
    /// </summary>
    public void Integrate(Vector3 linearAcceleration,
                          float deltaTime) {
      velocity += linearAcceleration * deltaTime;
    }

    /// <summary>
    /// Discretely integrates this Movement's velocity and angular velocity by both a
    /// linear acceleration term and an angular acceleration term and a deltaTime.
    /// </summary>
    public void Integrate(Vector3 linearAcceleration,
                          Vector3 angularAcceleration,
                          float deltaTime) {
      velocity += linearAcceleration * deltaTime;
      angularVelocity += angularAcceleration * deltaTime;
    }

    #endregion

  }

  public struct KinematicState {

    public Pose pose;
    public Movement movement;

    public KinematicState(Pose pose, Movement movement) {
      this.pose = pose;
      this.movement = movement;
    }

    public void Integrate(float deltaTime) {
      pose = pose.Integrated(movement, deltaTime);
    }

    public void Integrate(Vector3 linearAcceleration,
                          float deltaTime) {
      movement.Integrate(linearAcceleration, deltaTime);
      pose = pose.Integrated(movement, deltaTime);
    }

    public void Integrate(Vector3 linearAcceleration,
                          Vector3 angularAcceleration,
                          float deltaTime) {
      movement.Integrate(linearAcceleration, angularAcceleration, deltaTime);
      pose = pose.Integrated(movement, deltaTime);
    }

  }

  public static class MovementExtensions {

    public static Pose Integrated(this Pose thisPose, Movement movement, float deltaTime) {
      thisPose.position = movement.velocity * deltaTime + thisPose.position;

      if (movement.angularVelocity.sqrMagnitude > 0.00001f) {
        var angVelMag = movement.angularVelocity.magnitude;
        thisPose.rotation = Quaternion.AngleAxis(angVelMag * deltaTime,
                                                 movement.angularVelocity / angVelMag)
                            * thisPose.rotation;
      }

      return thisPose;
    }

  }

}
