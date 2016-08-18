using UnityEngine;
using System.Collections;
using System;

namespace Leap.Unity.Interaction {

  /**
  * The ThrowingControllerPalmVelocity class transfers the instantaneous palm velocity of the releasing hand to
  * the object.
  * @since 4.1.4
  */
  public class ThrowingControllerPalmVelocity : IThrowingController {

    [SerializeField]
    private AnimationCurve _throwingVelocityCurve = new AnimationCurve(new Keyframe(0, 1.5f, 0, 0),
                                                                       new Keyframe(2, 1.3f, 0, 0));

    /** Does nothing in this implementation. */
    public override void OnHold(ReadonlyList<Hand> hands) { }

    /** Applies the palm velocity, modified by the _throwingVelocityCurve, to the released object. */
    public override void OnThrow(Hand throwingHand) {
      Vector3 palmVel = throwingHand.PalmVelocity.ToVector3();
      float speed = palmVel.magnitude;
      float multiplier = _throwingVelocityCurve.Evaluate(speed / _obj.Manager.SimulationScale);
      _obj.rigidbody.velocity = palmVel * multiplier;
    }
  }
}
