/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using InteractionEngineUtility;

namespace Leap.Unity.Interaction {

  /**
  * The MoveToControllerVelocity class moves the held object into position
  * using simulated physical forces.
  * @since 4.1.4
  */
  public class MoveToControllerVelocity : IMoveToController {

     /** The maximum allowed velocity in meters per second. */
    [Tooltip("The maximum allowed velocity in meters per second.")]
    [SerializeField]
    protected float _maxVelocity = 6;

    /** Function used to modify the strength of the force used to move the object by the distance to the target position. */
    [Tooltip("Function used to modify the strength of the force used to move the object by the distance to the target position.")]
    [SerializeField]
    protected AnimationCurve _strengthByDistance = new AnimationCurve(new Keyframe(0.0f, 1.0f, 0.0f, 0.0f),
                                                                      new Keyframe(0.02f, 0.3f, 0.0f, 0.0f));

    private float _maxVelocitySqrd;

    /** Initializes the controller based on the object settings. */
    protected override void Init(InteractionBehaviour obj) {
      base.Init(obj);

      _maxVelocitySqrd = _maxVelocity * _obj.Manager.SimulationScale;
      _maxVelocitySqrd *= _maxVelocitySqrd;
    }

    /** Moves the object by applying  forces and torque. */
    public override void MoveTo(ReadonlyList<Hand> hands, PhysicsMoveInfo info, Vector3 solvedPosition, Quaternion solvedRotation) {
      if (info.shouldTeleport) {
        _obj.warper.Teleport(solvedPosition, solvedRotation);
      } else {
        Vector3 targetVelocity = PhysicsUtility.ToLinearVelocity(_obj.warper.RigidbodyPosition, solvedPosition, Time.fixedDeltaTime);
        Vector3 targetAngularVelocity = PhysicsUtility.ToAngularVelocity(_obj.warper.RigidbodyRotation, solvedRotation, Time.fixedDeltaTime);

        float targetSpeedSqrd = targetVelocity.sqrMagnitude;
        if (targetSpeedSqrd > _maxVelocitySqrd) {
          float targetPercent = (_maxVelocity * _obj.Manager.SimulationScale) / Mathf.Sqrt(targetSpeedSqrd);
          targetVelocity *= targetPercent;
          targetAngularVelocity *= targetPercent;
        }

        float followStrength = _strengthByDistance.Evaluate(info.remainingDistanceLastFrame / _obj.Manager.SimulationScale);
        Vector3 lerpedVelocity = Vector3.Lerp(_obj.rigidbody.velocity, targetVelocity, followStrength);
        Vector3 lerpedAngularVelocity = Vector3.Lerp(_obj.rigidbody.angularVelocity, targetAngularVelocity, followStrength);

        Vector3 centerOfMassOffset = _obj.warper.RigidbodyRotation * _obj.rigidbody.centerOfMass;
        _obj.rigidbody.velocity = lerpedVelocity + Vector3.Cross(lerpedAngularVelocity, centerOfMassOffset);
        _obj.rigidbody.angularVelocity = lerpedAngularVelocity;
      }
    }

    /** Sets the physics properties of the object to appropriate values for this move-to method. */
    public override void SetGraspedState() {
      _obj.rigidbody.isKinematic = false;
      _obj.rigidbody.useGravity = false;
      _obj.rigidbody.drag = 0;
      _obj.rigidbody.angularDrag = 0;
    }

    /** Does nothing in this implementation. */
    public override void OnGraspBegin() { }

    /** Does nothing in this implementation. */
    public override void OnGraspEnd() { }

    /** Validates the object's physics settings. */
    public override void Validate() {
      base.Validate();

      if (_obj.IsBeingGrasped && _obj.UntrackedHandCount == 0) {
        Assert.IsFalse(_obj.rigidbody.isKinematic,
                       "Object must not be kinematic when being grasped.");

        Assert.IsFalse(_obj.rigidbody.useGravity,
                       "Object must not be using gravity when being grasped.");

        Assert.AreEqual(_obj.rigidbody.drag, 0,
                        "Object drag must be zero when being grasped.");

        Assert.AreEqual(_obj.rigidbody.angularDrag, 0,
                        "Object angular drag must be zero when being grasped.");
      }
    }
  }
}
