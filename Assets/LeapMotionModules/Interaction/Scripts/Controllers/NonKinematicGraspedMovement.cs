using InteractionEngineUtility;
using Leap.Unity.Query;
using Leap.Unity.Interaction.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  public class NonKinematicGraspedMovement : IGraspedMovementController {

    protected float _maxVelocity = 6F;

    //protected float _maxVelocityHandSpace = 2F;

    private bool _useLastSolvedPosition = false;
    private Vector3 _lastSolvedPosition = Vector3.zero;
    protected AnimationCurve _strengthByDistance = new AnimationCurve(new Keyframe(0.0f, 1.0f, 0.0f, 0.0f),
                                                                      new Keyframe(0.02f, 0.3f, 0.0f, 0.0f));

    public void MoveTo(Vector3 solvedPosition, Quaternion solvedRotation,
                       InteractionBehaviour intObj, bool justGrasped) {
      Vector3 targetVelocity = PhysicsUtility.ToLinearVelocity(intObj.rigidbody.position, solvedPosition, Time.fixedDeltaTime);
      Vector3 targetAngularVelocity = PhysicsUtility.ToAngularVelocity(intObj.rigidbody.rotation, solvedRotation, Time.fixedDeltaTime);

      // Clamp in hand space
      //Vector3 graspingHandVelocity = interactionObj.graspingHands.Query().First().GetLastTrackedLeapHand().PalmVelocity.ToVector3();
      //targetVelocity -= graspingHandVelocity;

      // Clamp by _maxVelocityHandSpace
      float maxScaledVelocity = _maxVelocity * intObj.manager.SimulationScale;
      float targetSpeedSqrd = targetVelocity.sqrMagnitude;
      if (targetSpeedSqrd > maxScaledVelocity * maxScaledVelocity) {
        float targetPercent = maxScaledVelocity / Mathf.Sqrt(targetSpeedSqrd);
        targetVelocity *= targetPercent;
        targetAngularVelocity *= targetPercent;
      }

      // Back to world space from hand space
      //targetVelocity += graspingHandVelocity;

      _useLastSolvedPosition = !justGrasped;
      float followStrength = 1F;
      if (_useLastSolvedPosition) {
        float remainingDistanceLastFrame = Vector3.Distance(_lastSolvedPosition, intObj.rigidbody.position);
        followStrength = _strengthByDistance.Evaluate(remainingDistanceLastFrame / intObj.manager.SimulationScale);
      }

      Vector3 lerpedVelocity = Vector3.Lerp(intObj.rigidbody.velocity, targetVelocity, followStrength);
      Vector3 lerpedAngularVelocity = Vector3.Lerp(intObj.rigidbody.angularVelocity, targetAngularVelocity, followStrength);

      Vector3 centerOfMassOffset = intObj.rigidbody.rotation * intObj.rigidbody.centerOfMass;
      intObj.rigidbody.velocity = lerpedVelocity + Vector3.Cross(lerpedAngularVelocity, centerOfMassOffset);
      intObj.rigidbody.angularVelocity = lerpedAngularVelocity;

      _lastSolvedPosition = solvedPosition;
    }
  }

}
