using UnityEngine;

namespace Leap.Unity.Interaction {

  public class PhysicsDriverVelocity : IPhysicsController {

    [SerializeField]
    protected float _maxVelocity = 6;

    [SerializeField]
    protected AnimationCurve _strengthByDistance;

    public override void DrivePhysics(ReadonlyList<Hand> hands, PhysicsMoveInfo info, Vector3 solvedPosition, Quaternion solvedRotation) {
      if (info.shouldTeleport) {
        _obj.rigidbody.position = solvedPosition;
        _obj.rigidbody.rotation = solvedRotation;
      } else {
        Vector3 deltaPos = solvedPosition - _obj.rigidbody.position;
        Quaternion deltaRot = solvedRotation * Quaternion.Inverse(_obj.rigidbody.rotation);

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
        _obj.rigidbody.velocity = Vector3.Lerp(_obj.rigidbody.velocity, targetVelocity, followStrength);
        _obj.rigidbody.angularVelocity = Vector3.Lerp(_obj.rigidbody.angularVelocity, targetAngularVelocity, followStrength);
      }
    }

    public override void SetGraspedState() {
      _obj.rigidbody.isKinematic = false;
      _obj.rigidbody.useGravity = false;
      _obj.rigidbody.drag = 0;
      _obj.rigidbody.angularDrag = 0;
    }

    public override void OnGrasp() { }

    public override void OnUngrasp() { }
  }
}
