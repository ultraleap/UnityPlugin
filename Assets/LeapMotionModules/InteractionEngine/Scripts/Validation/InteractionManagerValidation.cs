using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Linq;

namespace Leap.Unity.Interaction {

  public partial class InteractionManager {

    public void Validate() {
      Assert.AreEqual(isActiveAndEnabled, _hasSceneBeenCreated,
                      "Activation status should always be equal to scene creation status.");

      Assert.AreEqual(isActiveAndEnabled, _scene.pScene != IntPtr.Zero,
                      "Scene ptr should always be non-null when manager is active.");

      assertNonNullWhenActive(_activityManager, "Activity Manager");
      assertNonNullWhenActive(_shapeDescriptionPool, "Shape Description Pool");
      assertNonNullWhenActive(_instanceHandleToBehaviour, "Instance Handle mapping");
      assertNonNullWhenActive(_idToInteractionHand, "Id To Hand mapping");
      assertNonNullWhenActive(_graspedBehaviours, "Grasped behaviour list");

      foreach (var pair in _idToInteractionHand) {
        int id = pair.Key;
        var interactionHand = pair.Value;

        Assert.AreEqual(id, interactionHand.hand.Id,
                        "Id should always map to a hand of the same Id.");

        interactionHand.Validate();
      }


    }

    private void assertNonNullWhenActive(object obj, string name) {
      Assert.AreEqual(isActiveAndEnabled, obj != null,
                      name + " should always be non-null when manager is active.");
    }

    protected partial class InteractionHand {

      public void Validate() {
        Assert.IsTrue(lastTimeUpdated <= Time.unscaledTime,
                      "Last time can never be greater than the current time.");

        Assert.IsTrue(maxSuspensionTime >= 0,
                      "Max suspension time must always be non-negative.");

        Assert.AreEqual(graspedObject != null, graspedObject.IsBeingGrasped,
                        "Hand must always be grasping an object that reports as grasped.");

        Assert.AreEqual(graspedObject != null, graspedObject.IsBeingGraspedByHand(hand.Id),
                        "Grasped object must always report as being grasped by this hand.");

        if (isUntracked) {
          Assert.IsNotNull(graspedObject,
                           "If untracked, must also always be grasping an object.");

          Assert.AreNotEqual(graspedObject.UntrackedHandCount, 0,
                             "If untracked, grasped object must report at least one untracked hand.");

          Assert.IsTrue(graspedObject.UntrackedGraspingHands.Contains(hand.Id),
                        "If untracked, grasped object must report to be grasped by an untracked hand of this id.");
        }

        if (isUserGrasp) {
          Assert.IsNotNull(graspedObject,
                           "If a user grasp is taking place, we must always be grasping an object.");
        }
      }
    }

  }
}
