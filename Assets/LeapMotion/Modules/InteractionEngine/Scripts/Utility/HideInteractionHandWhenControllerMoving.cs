using Leap.Unity.Query;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Interaction.Examples {

  /// <summary>
  /// This simple example script disables the InteractionHand script and has event
  /// outputs to drive hiding Leap hand renderers when it detects that an
  /// InteractionXRController is tracked and moving (e.g. it has been picked up).
  /// 
  /// It also does some basic checks with hand distance so that you can see your hands
  /// when you put the controller down and you hide the hands when they're obviously
  /// holding the controller (e.g. tracked as very close to the controller).
  /// </summary>
  public class HideInteractionHandWhenControllerMoving : MonoBehaviour {

    public InteractionXRController intCtrl;
    public InteractionHand intHand;

    public UnityEvent OnInteractionHandEnabled;
    public UnityEvent OnInteractionHandDisabled;
    
    private float _handSeparationDistance = 0.23f;
    private float _handHoldingDistance = 0.18f;

    private void Reset() {
      if (intCtrl == null) {
        intCtrl = GetComponent<InteractionXRController>();
      }
      if (intCtrl != null && intHand == null && this.transform.parent != null) {
        intHand = this.transform.parent.GetChildren().Query()
          .Select(t => t.GetComponent<InteractionHand>())
          .Where(h => h != null)
          .FirstOrDefault(h => h.isLeft == intCtrl.isLeft);
      }
    }

    private void Update() {
      if (intCtrl != null && intCtrl.isActiveAndEnabled && intHand != null) {
        var shouldIntHandBeEnabled = !intCtrl.isBeingMoved;

        if (intCtrl.isTracked) {
          var handPos = intHand.position;
          var ctrlPos = intCtrl.position;
          var handControllerDistanceSqr = (handPos - ctrlPos).sqrMagnitude;

          // Also allow the hand to be active if it's far enough away from the controller.
          if (handControllerDistanceSqr > _handSeparationDistance * _handSeparationDistance) {
            shouldIntHandBeEnabled = true;
          }

          // Prevent the hand from being active if it's very close to the controller.
          if (handControllerDistanceSqr < _handHoldingDistance * _handHoldingDistance) {
            shouldIntHandBeEnabled = false;
          }
        }

        if (shouldIntHandBeEnabled && !intHand.enabled) {
          intHand.enabled = true;

          OnInteractionHandEnabled.Invoke();
        }

        if (!shouldIntHandBeEnabled && intHand.enabled) {
          intHand.enabled = false;

          OnInteractionHandDisabled.Invoke();
        }
      }
    }

  }

}
