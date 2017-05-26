using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Interaction;
using Leap.Unity;
using UnityEngine.Events;

[AddComponentMenu("")]
public class InteractionControllerSwitcher : MonoBehaviour {

  public InteractionManager interactionManager;

  [Tooltip("When a controller is tracking and moving, the user is probably "
         + "intending to be using their controller instead of their hand to "
         + "interact with objects. Without this option checked, hands may be "
         + "sorted earlier in the list, which will cause their own movement to "
         + "override controllers, unless they aren't tracked at all.")]
  public bool prioritizeControllers = true;

  public List<InteractionController> LeftHandControllers = new List<InteractionController>();
  public List<InteractionController> RightHandControllers = new List<InteractionController>();

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

	void FixedUpdate () {
    setControllersActive(RightHandControllers, false);
    setControllersActive(LeftHandControllers, true);
  }

  private void refreshControllers() {
    if (interactionManager == null) { interactionManager = FindObjectOfType<InteractionManager>(); }

    // Add new controllers from the Interaction Manager.
    foreach (var controller in interactionManager.interactionControllers) {
      if (controller.isLeft && !LeftHandControllers.Contains(controller) && controller.gameObject.activeInHierarchy && controller.enabled) {
        LeftHandControllers.Add(controller);
      }
      else if (controller.isRight && !RightHandControllers.Contains(controller) && controller.gameObject.activeInHierarchy && controller.enabled) {
        RightHandControllers.Add(controller);
      }
    }

    // Remove old Controllers that no longer exist.
    pruneControllers(LeftHandControllers, expectingLeft: true);
    pruneControllers(RightHandControllers, expectingLeft: false);

    // Prioritize VR controllers over hands for more intuitive switching.
    if (prioritizeControllers) {
      swapSortControllersBeforeHands(LeftHandControllers);
      swapSortControllersBeforeHands(RightHandControllers);
    }

  }

  private void swapSortControllersBeforeHands(List<InteractionController> controllers) {
    if (controllers.Count == 0) return;

    int i = 0;
    int j = controllers.Count - 1;

    do {
      if (controllers[i] is InteractionHand && controllers[j] is InteractionController) {
        Utils.Swap(controllers, i, j);
      }

      i++;
      j--;
    } while (i < j);
  }

  private void pruneControllers(List<InteractionController> controllers, bool expectingLeft) {
    var tempControllers = Pool<List<InteractionController>>.Spawn();

    try {
      foreach (InteractionController switcherController in controllers) {
        bool containsController = interactionManager.interactionControllers.Contains(switcherController);
        if (!containsController || (switcherController.isLeft && !expectingLeft) || !switcherController.gameObject.activeInHierarchy || !switcherController.enabled) {
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
      if (controllers[i] is InteractionVRController) {
        isActive = (controllers[i] as InteractionVRController).trackingProvider.isTracked && controllers[i].isBeingMoved;
      } else if (controllers[i] is InteractionHand) {
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
      } else {
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
