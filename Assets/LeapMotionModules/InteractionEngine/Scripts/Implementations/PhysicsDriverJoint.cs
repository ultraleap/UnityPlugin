using UnityEngine;
using System.Collections;
using System;

namespace Leap.Unity.Interaction {

  public class PhysicsDriverJoint : IPhysicsDriver {

    private Rigidbody _rigidbody;
    private FixedJoint _joint;

    public override void DrivePhysics(ReadonlyList<Hand> hands, PhysicsMoveInfo info, Vector3 solvedPosition, Quaternion solvedRotation) {

    }

    public override void OnGrasp() {
      if (_rigidbody == null) {
        GameObject dummyObj = new GameObject("DummyBody");
        _rigidbody = dummyObj.AddComponent<Rigidbody>();
        _joint = dummyObj.AddComponent<FixedJoint>();

        _joint.autoConfigureConnectedAnchor = false;
      }
    }

    public override void OnUngrasp() {

    }
  }
}
