using Leap.Unity.Query;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Interaction.Examples {

  /// <summary>
  /// This simple example script disables the InteractionHand script and has event
  /// outputs to drive hiding Leap hand renderers when it detects that an
  /// InteractionXRController is tracked and moving (e.g. it has been picked up).
  /// </summary>
  public class HideInteractionHandWhenControllerMoving : MonoBehaviour {

    public InteractionXRController intCtrl;
    public InteractionHand intHand;

    public UnityEvent OnInteractionHandEnabled;
    public UnityEvent OnInteractionHandDisabled;

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
      if (intCtrl != null && intHand != null) {
        if (intCtrl.isBeingMoved && intHand.enabled) {
          intHand.enabled = false;

          OnInteractionHandDisabled.Invoke();
        }

        if (!intCtrl.isBeingMoved && !intHand.enabled) {
          intHand.enabled = true;

          OnInteractionHandEnabled.Invoke();
        }
      }
    }

  }

}
