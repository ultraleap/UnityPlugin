using UnityEngine.Assertions;

namespace Leap.Unity.Interaction {

  public partial class InteractionBehaviour {

    public override void Validate() {
      base.Validate();

      bool shouldShadowStateMatch = true;

      //If being grasped, actual state might be different than shadow state.
      if (IsBeingGrasped) {
        shouldShadowStateMatch = false;
      }

      //If in soft contact mode, actual state might be different than shadow state.
      if (_contactMode == ContactMode.SOFT) {
        shouldShadowStateMatch = false;
      }

      if (shouldShadowStateMatch) {
        Assert.AreEqual(_rigidbody.isKinematic, _isKinematic,
                        "Kinematic shadow state must match actual kinematic state.");

        Assert.AreEqual(_rigidbody.useGravity, _useGravity,
                        "Gravity shadow state must match actual gravity state.");
      }

      if (IsRegisteredWithManager) {
        _controllers.Validate();
      }
    }

  }
}
