using UnityEngine.Assertions;
using System.Diagnostics;

namespace Leap.Unity.Interaction {

  public partial class ActivityManager {

    [Conditional("UNITY_ASSERTIONS")]
    public void Validate() {

      Assert.IsTrue(_overlapRadius > 0.0f,
                    "Overlap radius was " + _overlapRadius + " but must be positive and non-zero.");

      Assert.IsTrue(_maxDepth >= 1,
                    "Max depth was " + _maxDepth + " but must be positive and non-zero.");

      foreach (var pair in _registeredBehaviours) {
        var interactionObj = pair.Key;
        var monitor = pair.Value;

        interactionObj.Validate();

        AssertHelper.Implies("IsBeingGrasped", interactionObj.IsBeingGrasped,
                             "IsActive", IsActive(interactionObj));

        Assert.IsTrue(interactionObj.IsRegisteredWithManager,
                      "All registered behaviours must be reported as registered.");

        Assert.IsTrue(interactionObj.isActiveAndEnabled,
                      "All registered behaviours must be active and enabled.");

        Assert.IsTrue(IsRegistered(interactionObj),
                      "Registration status must match the reported status.");

        Assert.AreEqual(monitor != null, IsActive(interactionObj),
                        "Monitor must be non-null for objects reported as active.");
      }

      foreach (var activeObj in _activeBehaviours) {
        Assert.IsTrue(_registeredBehaviours.ContainsKey(activeObj),
                      "Active behaviour must be registered with this activity manager.");
      }

      foreach (var misbehavingObj in _misbehavingBehaviours) {
        Assert.IsTrue(_registeredBehaviours.ContainsKey(misbehavingObj),
                      "All misbehaving objects must be still considered registered.");
      }
    }
  }
}
