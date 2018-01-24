/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity;
using Leap.Unity.Interaction;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// This MonoBehaviour is used in the Interaction Engine example scenes to provide a
  /// simple heuristic for when to switch between a held controller and a Leap hand.
  /// Your mileage may vary.
  /// </summary>
  [AddComponentMenu("")]
  public class InteractionControllerSwitcher : MonoBehaviour {

    public InteractionManager interactionManager;

    public List<InteractionController> leftHandControllers = new List<InteractionController>();
    public List<InteractionController> rightHandControllers = new List<InteractionController>();

    public UnityEvent OnLeftHandActive = new UnityEvent();
    public UnityEvent OnLeftHandInactive = new UnityEvent();
    public UnityEvent OnRightHandActive = new UnityEvent();
    public UnityEvent OnRightHandInactive = new UnityEvent();

    void OnValidate() {
      refreshControllers();
    }

    void OnDrawGizmosSelected() {
      if (!Application.isPlaying) {
        OnValidate();
      }
    }

    void Awake() {
      refreshControllers();
    }

    void FixedUpdate() {
      setControllersActive(rightHandControllers, false);
      setControllersActive(leftHandControllers, true);
    }

    private void refreshControllers() {
      if (interactionManager == null) { interactionManager = FindObjectOfType<InteractionManager>(); }

      // Add new controllers from the Interaction Manager.
      foreach (var controller in interactionManager.interactionControllers) {
        if (controller.isLeft && !leftHandControllers.Contains(controller) && controller.gameObject.activeInHierarchy && controller.enabled) {
          addController(leftHandControllers, controller);
        }
        else if (controller.isRight && !rightHandControllers.Contains(controller) && controller.gameObject.activeInHierarchy && controller.enabled) {
          addController(rightHandControllers, controller);
        }
      }

      // Remove old Controllers that no longer exist.
      pruneControllers(leftHandControllers, expectingLeft: true);
      pruneControllers(rightHandControllers, expectingLeft: false);
    }

    /// <summary>
    /// Adds the InteractionController, but inserts it before any InteractionHands if it's
    /// an InteractionVRController.
    /// </summary>
    private void addController(List<InteractionController> controllers, InteractionController toAdd) {
      int insertionIndex = -1;
      if (toAdd is InteractionXRController) {
        for (int i = 0; i < controllers.Count; i++) {
          if (controllers[i] is InteractionHand) {
            insertionIndex = i; break;
          }
        }
      }

      if (insertionIndex > -1) {
        controllers.Insert(insertionIndex, toAdd);
      }
      else {
        controllers.Add(toAdd);
      }
    }

    private void pruneControllers(List<InteractionController> controllers, bool expectingLeft) {
      var tempControllers = Pool<List<InteractionController>>.Spawn();

      try {
        foreach (InteractionController switcherController in controllers) {
          bool containsController = interactionManager.interactionControllers.Contains(switcherController);
          if (!containsController
              || (switcherController.isLeft && !expectingLeft)
              || (switcherController.isRight && expectingLeft)
              || !switcherController.gameObject.activeInHierarchy
              || !switcherController.enabled) {
            tempControllers.Add(switcherController);
          }
        }

        foreach (var controller in tempControllers) {
          controllers.Remove(controller);
        }
      }
      finally {
        tempControllers.Clear();
        Pool<List<InteractionController>>.Recycle(tempControllers);
      }
    }

    private void setControllersActive(List<InteractionController> controllers, bool isLeft) {
      bool foundATrackedController = false;
      for (int i = 0; i < controllers.Count; i++) {
        bool isActive = false;
        if (controllers[i] is InteractionXRController) {
          isActive = (controllers[i] as InteractionXRController).trackingProvider.isTracked && controllers[i].isBeingMoved;
        }
        else if (controllers[i] is InteractionHand) {
          isActive = (isLeft ? Hands.Left : Hands.Right) != null && controllers[i].isBeingMoved;
        }

        if (!foundATrackedController && isActive) {
          foundATrackedController = true;
          if (!controllers[i].enabled) {
            controllers[i].enabled = true;

            if (controllers[i] is InteractionHand) {
              if (isLeft) {
                OnLeftHandActive.Invoke();
              }
              else {
                OnRightHandActive.Invoke();
              }
            }
          }
        }
        else {
          if (controllers[i].enabled) {
            controllers[i].enabled = false;

            if (controllers[i] is InteractionHand) {
              if (isLeft) {
                OnLeftHandInactive.Invoke();
              }
              else {
                OnRightHandInactive.Invoke();
              }
            }
          }
        }
      }
    }

  }

}
