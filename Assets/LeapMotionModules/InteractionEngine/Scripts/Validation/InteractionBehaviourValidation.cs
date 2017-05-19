/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
