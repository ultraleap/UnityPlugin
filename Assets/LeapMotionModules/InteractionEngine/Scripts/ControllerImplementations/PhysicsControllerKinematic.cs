using System;
using UnityEngine;

namespace Leap.Unity.Interaction {

  public class PhysicsControllerKinematic : IPhysicsController {

    public override void DrivePhysics(ReadonlyList<Hand> hands, PhysicsMoveInfo info, Vector3 solvedPosition, Quaternion solvedRotation) {
      if (info.shouldTeleport) {
        _obj.rigidbody.position = solvedPosition;
        _obj.rigidbody.rotation = solvedRotation;
      } else {
        _obj.rigidbody.MovePosition(solvedPosition);
        _obj.rigidbody.MoveRotation(solvedRotation);
      }
    }

    public override void SetGraspedState() {
      _obj.rigidbody.isKinematic = true;
    }

    public override void OnGrasp() { }

    public override void OnUngrasp() { }
  }
}
