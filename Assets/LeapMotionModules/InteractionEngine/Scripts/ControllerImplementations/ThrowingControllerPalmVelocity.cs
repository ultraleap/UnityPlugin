using UnityEngine;
using System.Collections;
using System;

namespace Leap.Unity.Interaction {

  public class ThrowingControllerPalmVelocity : IThrowingController {

    [Tooltip("Maps the release speed into a multiplier which is used to scale the release velocity.")]
    [SerializeField]
    private AnimationCurve _velocityScaleCurve = new AnimationCurve(new Keyframe(0, 1, 0, 0),
                                                                    new Keyframe(3, 1.5f, 0, 0));

    public override void OnHold(ReadonlyList<Hand> hands) { }

    public override void OnThrow(Hand throwingHand) {
      Vector3 palmVel = throwingHand.PalmVelocity.ToVector3();
      float speed = palmVel.magnitude;
      float multiplier = _velocityScaleCurve.Evaluate(speed / _obj.Manager.SimulationScale);
      _obj.rigidbody.velocity = palmVel * multiplier;
    }
  }
}
