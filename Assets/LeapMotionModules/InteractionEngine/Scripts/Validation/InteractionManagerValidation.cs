/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Linq;
using System.Diagnostics;

namespace Leap.Unity.Interaction {

  public partial class InteractionManager {

    [Conditional("UNITY_ASSERTIONS")]
    public void Validate() {
      AssertHelper.Implies("_hasSceneBeenCreated", _hasSceneBeenCreated,
                           "isActiveAndEnabled", isActiveAndEnabled);

      assertNonNullWhenActive(_activityManager, "Activity Manager");
      assertNonNullWhenActive(_idToInteractionHand, "Id To Hand mapping");
      assertNonNullWhenActive(_graspedBehaviours, "Grasped behaviour list");

      _activityManager.Validate();

      foreach (var pair in _idToInteractionHand) {
        int id = pair.Key;
        var interactionHand = pair.Value;

        if (interactionHand.hand != null) {
          Assert.AreEqual(id, interactionHand.hand.Id,
                          "Id should always map to a hand of the same Id.");
        }

        interactionHand.Validate();
      }

      foreach (var graspedObj in _graspedBehaviours) {
        assertIsRegisteredWithThisManager(graspedObj);

        Assert.IsTrue(graspedObj.IsBeingGrasped,
                      "All grasped objects must report as being grasped.");

        foreach (var graspingId in graspedObj.GraspingHands) {
          Assert.IsTrue(_idToInteractionHand.ContainsKey(graspingId),
                        "Must be reporting as grasped by a hand we are tracking.");

          Assert.AreEqual(_idToInteractionHand[graspingId].graspedObject, graspedObj,
                          "Must be grasped by the hand that it is reporting to be grasped by.");
        }

        foreach (var untrackedId in graspedObj.UntrackedGraspingHands) {
          Assert.IsTrue(_idToInteractionHand.ContainsKey(untrackedId),
                        "Must be reporting as grasped by an untracked hand we are tracking.");

          Assert.AreEqual(_idToInteractionHand[untrackedId].graspedObject, graspedObj,
                          "Must be grasped by the hand that it is reporting to be grasped by.");

          Assert.IsTrue(_idToInteractionHand[untrackedId].isUntracked,
                        "Hand that is reported to be untracked must actually be untracked.");
        }
      }
    }

    private void assertIsRegisteredWithThisManager(IInteractionBehaviour interactionObj) {
      Assert.IsTrue(interactionObj.IsRegisteredWithManager,
                    "Object must be registered with a manager.");

      Assert.AreEqual(interactionObj.Manager, this,
                      "Object must be registered with this manager.");

      Assert.IsTrue(interactionObj.isActiveAndEnabled,
                    "Object must be active and enabled.");
    }

    private void assertNonNullWhenActive(object obj, string name) {
      AssertHelper.Implies(obj != null, isActiveAndEnabled,
                           name + " should always be non-null when manager is active.");
    }

    protected partial class InteractionHand {

      [Conditional("UNITY_ASSERTIONS")]
      public void Validate() {
        Assert.IsTrue(lastTimeUpdated <= Time.unscaledTime,
                      "Last time can never be greater than the current time.");

        Assert.IsTrue(maxSuspensionTime >= 0,
                      "Max suspension time must always be non-negative.");

        if (graspedObject != null) {
          Assert.IsTrue(graspedObject.IsBeingGrasped,
                        "Hand must always be grasping an object that reports as grasped.");

          Assert.IsTrue(graspedObject.IsBeingGraspedByHand(hand.Id),
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
}
