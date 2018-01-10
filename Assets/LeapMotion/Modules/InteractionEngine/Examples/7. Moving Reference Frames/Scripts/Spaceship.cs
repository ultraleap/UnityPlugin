/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity;
using Leap.Unity.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Examples {

  /// <summary>
  /// The spaceship in this example is a kinematic rigidbody with a force API, but having
  /// a rigidbody on your Interaction Manager's reference frame is entirely optional. Any
  /// moving transform can provide a frame of reference for the Interaction Manager and
  /// encompassing interfaces.
  ///
  /// This script provides a "velocity" and "angular velocity" abstraction for the
  /// AutopilotSystem in the example, and moves the ship's transform appropriately.
  ///
  /// The important thing when using a moving reference frame is that you move the
  /// Interaction Manager's transform before its FixedUpdate runs. This script uses
  /// the manager's OnPrePhysicalUpdate to ensure this, which it calls at the beginning
  /// of its FixedUpdate.
  /// </summary>
  [AddComponentMenu("")]
  public class Spaceship : MonoBehaviour {

    private Rigidbody _body;
    /// <summary>
    /// The ship contains Colliders, so it is given a kinematic Rigidbody to prevent
    /// the overhead of moving Colliders every frame.
    ///
    /// Setting the rigidbody's position is not important for the Interaction Manager;
    /// only the transform's positional movement establishes a moving reference frame!
    /// </summary>
    public
    #if UNITY_EDITOR
    new
    #endif
    Rigidbody rigidbody { get { return _body; } }

    private float _mass = 10F;

    private Vector3 _velocity;
    public Vector3 velocity {
      get { return _velocity; }
      set { _velocity = value; }
    }

    public Vector3 shipAlignedVelocity {
      get { return Quaternion.Inverse(this.transform.rotation) * _velocity; }
    }

    private Vector3 _angularVelocity;
    public Vector3 angularVelocity {
      get { return _angularVelocity; }
      set { _angularVelocity = value; }
    }

    public Vector3 shipAlignedAngularVelocity {
      get { return Quaternion.Inverse(this.transform.rotation) * _angularVelocity; }
    }

    private Vector3 _accumulatedForce;
    private Vector3 _accumulatedTorque;

    void Awake() {
      _body = GetComponent<Rigidbody>();
      _body.mass = _mass;
      _body.isKinematic = true;
    }

    void Start() {
      // The ship is moved in the manager's OnPrePhysicalUpdate callback, which ensures
      // (1) the ship's transform is updated before the Interaction Manager runs, and
      // (2) the ship's transform is updated in FixedUpdate.
      //
      // The Interaction Manager takes into account how it has moved since its last
      // update, and informs interaction controllers appropriately, allowing
      // interfaces to function properly.

      InteractionManager.instance.OnPrePhysicalUpdate += updateShipPhysics;
    }

    private void updateShipPhysics() {
      // Update velocity.
      Vector3 acceleration = _accumulatedForce / _mass;
      _velocity += acceleration * Time.deltaTime;

      _accumulatedForce = Vector3.zero;

      // Update position.
      Vector3 newPosition = this.transform.position + _velocity * Time.deltaTime;
      this.transform.position = newPosition;


      // Update angular velocity.
      Vector3 eulerAcceleration = _accumulatedTorque;
      _angularVelocity += eulerAcceleration * Time.deltaTime;

      _accumulatedTorque = Vector3.zero;

      // Update rotation.
      Quaternion newRotation = Quaternion.Euler(_angularVelocity * Time.deltaTime) * this.transform.rotation;
      this.transform.rotation = newRotation;

      // Sync transforms with the Physics engine so Rigidbody changes reflect
      // the movement of the ship. (Required for 2017.3 and newer.)
#if UNITY_2017_3_OR_NEWER
      Physics.SyncTransforms();
#endif
    }

    #region Ship Forces API

    public void AddForce(Vector3 force) {
      _accumulatedForce += force;
    }

    public void AddForceAtPosition(Vector3 force, Vector3 position) {
      Vector3 toCenterOfMass = this.transform.TransformPoint(_body.centerOfMass) - position;

      float forceCMAngle = Vector3.Angle(force, toCenterOfMass);

      // Linear force
      Vector3 linForce = force * Mathf.Cos(forceCMAngle);
      _accumulatedForce += linForce;

      // Torque
      Vector3 torqueVector = force * Mathf.Sin(forceCMAngle) * toCenterOfMass.magnitude;

      _accumulatedTorque += Vector3.Cross(torqueVector, toCenterOfMass);
    }

    public void AddShipAlignedTorque(Vector3 shipAlignedTorque) {
      _accumulatedTorque += this.transform.rotation * shipAlignedTorque;
    }

    public void AddShipAlignedForce(Vector3 shipAlignedForce) {
      _accumulatedForce += this.transform.rotation * shipAlignedForce;
    }

    #endregion

  }

}

