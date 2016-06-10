using UnityEngine;

namespace Leap.Unity.Interaction {

  public class PhysicsControllerVelocity : IPhysicsController {

    [SerializeField]
    protected float _maxVelocity = 6;

    [SerializeField]
    protected AnimationCurve _strengthByDistance = new AnimationCurve(new Keyframe(0.0f, 1.0f, 0.0f, 0.0f),
                                                                      new Keyframe(0.02f, 0.3f, 0.0f, 0.0f));

    private float _maxVelocitySqrd;

    protected override void Init(InteractionBehaviour obj) {
      base.Init(obj);

      _maxVelocitySqrd = _maxVelocity * _obj.Manager.SimulationScale;
      _maxVelocitySqrd *= _maxVelocitySqrd;
    }

    public override void DrivePhysics(ReadonlyList<Hand> hands, PhysicsMoveInfo info, Vector3 solvedPosition, Quaternion solvedRotation) {
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

    public override void OnGraspBegin() { }

    public override void OnGraspEnd() { }
  }
}
