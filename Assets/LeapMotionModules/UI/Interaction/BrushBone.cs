using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class BrushBone : MonoBehaviour {

    public InteractionHand interactionHand;
    public Rigidbody body;
    public new Collider collider;
    public FixedJoint joint;
    public FixedJoint metacarpalJoint;
    public Vector3 lastTarget;

    public Vector3 desiredPosition;
    public Quaternion desiredRotation;

    public float _lastObjectTouchedMass;

    #region Collision Callbacks

    void OnCollisionEnter(Collision collision) {
      InteractionBehaviourBase interactionObj;
      if (interactionHand.interactionManager.RigidbodyRegistry.TryGetValue(collision.rigidbody, out interactionObj)) {
        _lastObjectTouchedMass = collision.rigidbody.mass;
        interactionHand.ContactBoneCollisionEnter(this, interactionObj);
      }
    }
    void OnCollisionExit(Collision collision) {
      InteractionBehaviourBase interactionObj;
      if (interactionHand.interactionManager.RigidbodyRegistry.TryGetValue(collision.rigidbody, out interactionObj)) {
        interactionHand.ContactBoneCollisionExit(this, interactionObj);
      }
    }
    void OnTriggerEnter(Collider collider) {
      InteractionBehaviourBase interactionObj;
      if (interactionHand.interactionManager.RigidbodyRegistry.TryGetValue(collider.attachedRigidbody, out interactionObj)) {
        interactionHand.ContactBoneCollisionEnter(this, interactionObj);
      }
    }
    void OnTriggerExit(Collider collider) {
      InteractionBehaviourBase interactionObj;
      if (interactionHand.interactionManager.RigidbodyRegistry.TryGetValue(collider.attachedRigidbody, out interactionObj)) {
        interactionHand.ContactBoneCollisionExit(this, interactionObj);
      }
    }

    #endregion

  }

}