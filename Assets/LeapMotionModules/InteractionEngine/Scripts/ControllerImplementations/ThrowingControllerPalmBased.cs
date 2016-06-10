using UnityEngine;
using System.Collections;
using System;

namespace Leap.Unity.Interaction {

  public class ThrowingControllerPalmBased : IThrowingController {

    [SerializeField]
    private AnimationCurve _throwingVelocityCurve;

    public override void OnHold(ReadonlyList<Hand> hands) { }

    public override void OnThrow(Hand throwingHand) {
      Vector3 palmVel = throwingHand.PalmVelocity.ToVector3();
      float speed = palmVel.magnitude;
      float multiplier = _throwingVelocityCurve.Evaluate(speed / _obj.Manager.SimulationScale);
      _obj.rigidbody.velocity = palmVel * multiplier;
    }
  }
}
