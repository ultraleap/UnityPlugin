using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class ContactBone : MonoBehaviour {

    public InteractionHand interactionHand;
    public Rigidbody body;
    public new Collider collider;
    public Vector3 lastTarget;

    public Vector3 desiredPosition;
    public Quaternion desiredRotation;

    // TODO:
    #region rename weird "Triggering" silliness

    // Once the contact bone becomes dislocated, it then remains dislocated until it
    // stops triggering and then the _dislocatedCounter expires.
    private const int DISLOCATED_BONE_COOLDOWN = 30;
    private int _dislocatedCounter = DISLOCATED_BONE_COOLDOWN;

    public void StartTriggering() {
      collider.isTrigger = true;
      _dislocatedCounter = 0;
    }

    public bool UpdateTriggering() {
      if (_dislocatedCounter < DISLOCATED_BONE_COOLDOWN) {
        _dislocatedCounter += 1;
        if (_dislocatedCounter == DISLOCATED_BONE_COOLDOWN) {
          collider.isTrigger = false;
          return false;
        }
        return true;
      }
      return false;
    }

    #endregion

  }

}