using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;
using Leap.Unity;

public class InteractionControllerSwitcher : MonoBehaviour {
  public InteractionManager interactionManager;
  public List<InteractionController> LeftHandControllers = new List<InteractionController>();
  public List<InteractionController> RightHandControllers = new List<InteractionController>();

  void OnValidate() {
    if (interactionManager == null) { interactionManager = FindObjectOfType<InteractionManager>(); }

    //Add new controllers from the Interaction Manager
    foreach (InteractionController managerController in interactionManager.interactionControllers) {
      if(managerController.isLeft && !LeftHandControllers.Contains(managerController) && managerController.gameObject.activeInHierarchy && managerController.enabled) {
        LeftHandControllers.Add(managerController);
      } else if (managerController.isRight && !RightHandControllers.Contains(managerController) && managerController.gameObject.activeInHierarchy && managerController.enabled) {
        RightHandControllers.Add(managerController);
      }
    }

    //Remove old Controllers that no longer exist
    PruneControllers(LeftHandControllers);
    PruneControllers(RightHandControllers);
  }

  void PruneControllers(List<InteractionController> controllers) {
    foreach (InteractionController switcherController in controllers) {
      bool containsController = false;
      foreach (InteractionController managerController in interactionManager.interactionControllers) {
        if (switcherController == managerController) {
          containsController = true;
        }
      }
      if (switcherController.isRight || !containsController || !switcherController.gameObject.activeInHierarchy || !switcherController.enabled) {
        LeftHandControllers.Remove(switcherController);
      }
    }
  }

	void FixedUpdate () {
    SetControllersActive(RightHandControllers, false);
    SetControllersActive(LeftHandControllers, true);
  }

  void SetControllersActive(List<InteractionController> controllers, bool isLeft) {
    bool foundATrackedController = false;
    for (int i = 0; i < controllers.Count; i++) {
      bool isActive = false;
      if (controllers[i] is InteractionVRController) {
        isActive = (controllers[i] as InteractionVRController).trackingProvider.isTracked && controllers[i].isBeingMoved;
      } else if (controllers[i] is InteractionHand) {
        isActive = (isLeft ? Hands.Left : Hands.Right) != null && controllers[i].isBeingMoved;
      }

      if (!foundATrackedController && isActive) {
        foundATrackedController = true;
        if (!controllers[i].enabled) {
          controllers[i].enabled = true;
        }
      } else {
        if (controllers[i].enabled) {
          controllers[i].enabled = false;
        }
      }
    }
  }

  void OnDrawGizmosSelected() {
    if (!Application.isPlaying) {
      OnValidate();
    }
  }
}
