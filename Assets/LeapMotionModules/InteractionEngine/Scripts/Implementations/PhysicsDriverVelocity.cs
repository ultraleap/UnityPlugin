using UnityEngine;
using System.Collections;
using System;

namespace Leap.Unity.Interaction {

  public class PhysicsDriverVelocity : IPhysicsController {

    [SerializeField]
    protected float _maxVelocity = 6;

    [SerializeField]
    protected AnimationCurve _strengthByDistance;

    public override void DrivePhysics(ReadonlyList<Hand> hands, PhysicsMoveInfo info, Vector3 solvedPosition, Quaternion solvedRotation) {
      if (info.shouldTeleport) {
        _obj.Rigidbody.position = solvedPosition;
        _obj.Rigidbody.rotation = solvedRotation;
      } else {
        Vector3 deltaPos = solvedPosition - _obj.Rigidbody.position;
        Quaternion deltaRot = solvedRotation * Quaternion.Inverse(_obj.Rigidbody.rotation);

        Vector3 deltaAxis;
        float deltaAngle;
        deltaRot.ToAngleAxis(out deltaAngle, out deltaAxis);

        Vector3 targetVelocity = deltaPos / Time.fixedDeltaTime;
        Vector3 targetAngularVelocity = deltaAxis * deltaAngle * Mathf.Deg2Rad / Time.fixedDeltaTime;

        if (targetVelocity.sqrMagnitude > float.Epsilon) {
          float targetSpeed = targetVelocity.magnitude;
          float actualSpeed = Mathf.Min(_maxVelocity, targetSpeed);
          float targetPercent = actualSpeed / targetSpeed;

          targetVelocity *= targetPercent;
          targetAngularVelocity *= targetPercent;
        }

        float followStrength = _strengthByDistance.Evaluate(info.remainingDistanceLastFrame);
        _obj.Rigidbody.velocity = Vector3.Lerp(_obj.Rigidbody.velocity, targetVelocity, followStrength);
        _obj.Rigidbody.angularVelocity = Vector3.Lerp(_obj.Rigidbody.angularVelocity, targetAngularVelocity, followStrength);
      }
    }

    public override void SetGraspedState() {
      _obj.Rigidbody.isKinematic = false;
      _obj.Rigidbody.useGravity = false;
      _obj.Rigidbody.drag = 0;
      _obj.Rigidbody.angularDrag = 0;
    }

    public override void OnGrasp() { }

    public override void OnUngrasp() { }
  }
}
