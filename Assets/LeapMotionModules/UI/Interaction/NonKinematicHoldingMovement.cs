using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class NonKinematicHoldingMovement : IHoldingMovementController {

    protected float _maxVelocity = 6F;

    //protected AnimationCurve _strengthByDistance = new AnimationCurve(new Keyframe(0.0f, 1.0f, 0.0f, 0.0f),
    //                                                                  new Keyframe(0.02f, 0.3f, 0.0f, 0.0f));

    public void MoveTo(Vector3 solvedPosition, Quaternion solvedRotation,
                       InteractionBehaviour interactionObj) {
      Vector3 targetVelocity = PhysicsUtility.ToLinearVelocity(
        interactionObj.Rigidbody.position, solvedPosition, Time.fixedDeltaTime);
      Vector3 targetAngularVelocity = PhysicsUtility.ToAngularVelocity(
        interactionObj.Rigidbody.rotation, solvedRotation, Time.fixedDeltaTime);

      //float maxScaledVelocity = _maxVelocity * interactionObj.interactionManager.SimulationScale;
      //float targetSpeedSqrd = targetVelocity.sqrMagnitude;
      //if (targetSpeedSqrd > maxScaledVelocity * maxScaledVelocity) {
      //  float targetPercent = maxScaledVelocity / Mathf.Sqrt(targetSpeedSqrd);
      //  targetVelocity *= targetPercent;
      //  targetAngularVelocity *= targetPercent;
      //}

      // TODO: Investigate whether followStrength needs to be re-implemented here.
      // Requires a pretty unclean passing of the distance to target position on a one-frame delay.
      // Could just be done via internal state here.

      Vector3 centerOfMassOffset = interactionObj.Rigidbody.rotation * interactionObj.Rigidbody.centerOfMass;
      interactionObj.Rigidbody.velocity = targetVelocity + Vector3.Cross(targetAngularVelocity, centerOfMassOffset);
      interactionObj.Rigidbody.angularVelocity = targetAngularVelocity;
    }
  }

}
