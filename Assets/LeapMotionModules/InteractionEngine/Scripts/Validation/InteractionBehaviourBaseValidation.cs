
namespace Leap.Unity.Interaction {

  public partial class InteractionBehaviourBase {

    public override void Validate() {
      AssertHelper.Implies("isActiveAndEnabled", isActiveAndEnabled,
                           "_isRegisteredWithManager", _isRegisteredWithManager);

      AssertHelper.Implies("_hasShapeDescriptionBeenCreated", _hasShapeDescriptionBeenCreated,
                           "_isRegisteredWithManager", _isRegisteredWithManager);

      AssertHelper.Implies("_hasShapeInstanceHandle", _hasShapeInstanceHandle,
                           "_isRegisteredWithManager", _isRegisteredWithManager);

      foreach(var untrackedId in _untrackedIds) {
        AssertHelper.Contains(untrackedId, _graspingIds, "An untracked id must always be considered grasping.");
      }
    }
  }
}
