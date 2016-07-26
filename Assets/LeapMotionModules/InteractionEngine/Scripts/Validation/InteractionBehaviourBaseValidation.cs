using UnityEngine;
using UnityEngine.Assertions;

namespace Leap.Unity.Interaction {

  public partial class InteractionBehaviourBase {

    public override void Validate() {
      AssertHelper.Implies("isActiveAndEnabled", isActiveAndEnabled,
                           "_isRegisteredWithManager", _isRegisteredWithManager);

      AssertHelper.Implies("_hasShapeDescriptionBeenCreated", _hasShapeDescriptionBeenCreated,
                           "_isRegisteredWithManager", _isRegisteredWithManager);

      AssertHelper.Implies("_hasShapeInstanceHandle", _hasShapeInstanceHandle,
                           "_isRegisteredWithManager", _isRegisteredWithManager);

      foreach (var untrackedId in _untrackedIds) {
        Assert.IsTrue(_graspingIds.Contains(untrackedId),
                      "All untracked ids must be considered grasping.");
      }
    }

  }
}
