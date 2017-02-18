using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class ContactBone : MonoBehaviour {

    public InteractionHand interactionHand;
    public Rigidbody body;
    public new Collider collider;
    public Vector3 lastTarget;

    public Vector3 desiredPosition;
    public Quaternion desiredRotation;

    #region Collision Callbacks

    void OnCollisionEnter(Collision collision) {
      interactionHand.ContactBoneCollisionEnter(this, collision);
    }

    void OnCollisionExit(Collision collision) {
      interactionHand.ContactBoneCollisionExit(this, collision);
    }

    void OnTriggerEnter(Collider collider) {
      interactionHand.ContactBoneTriggerEnter(this, collider);
    }

    void OnTriggerExit(Collider collider) {
      interactionHand.ContactBoneTriggerExit(this, collider);
    }

    #endregion

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