/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

namespace Leap.Unity.Interaction {

  public partial class InteractionBehaviourBase {

    public override void Validate() {
      AssertHelper.Implies("isActiveAndEnabled", isActiveAndEnabled,
                           "_isRegisteredWithManager", _isRegisteredWithManager);

      foreach(var untrackedId in _untrackedIds) {
        AssertHelper.Contains(untrackedId, _graspingIds, "An untracked id must always be considered grasping.");
      }
    }
  }
}
