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

    void OnCollisionEnter(Collision collision) {
      InteractionBehaviourBase interactionObj;
      if (collision.rigidbody == null) {
        Debug.LogError("Brush Bone collided with non-rigidbody collider: " + collision.collider.name + "."
                     + "Please enable automatic layer generation in the Interaction Manager, "
                     + "or ensure the Interaction layer only contains Interaction Behaviours.");
      }
      if (interactionHand.interactionManager.rigidbodyRegistry.TryGetValue(collision.rigidbody, out interactionObj)) {
        _lastObjectTouchedMass = collision.rigidbody.mass;
        interactionHand.ContactBoneCollisionEnter(this, interactionObj, false);
      }
    }
    void OnCollisionExit(Collision collision) {
      InteractionBehaviourBase interactionObj;
      if (interactionHand.interactionManager.rigidbodyRegistry.TryGetValue(collision.rigidbody, out interactionObj)) {
        interactionHand.ContactBoneCollisionExit(this, interactionObj, false);
      }
    }
    void OnTriggerEnter(Collider collider) {
      InteractionBehaviourBase interactionObj;
      if (interactionHand.interactionManager.rigidbodyRegistry.TryGetValue(collider.attachedRigidbody, out interactionObj)) {
        interactionHand.ContactBoneCollisionEnter(this, interactionObj, true);
      }
    }
    void OnTriggerExit(Collider collider) {
      InteractionBehaviourBase interactionObj;
      if (interactionHand.interactionManager.rigidbodyRegistry.TryGetValue(collider.attachedRigidbody, out interactionObj)) {
        interactionHand.ContactBoneCollisionExit(this, interactionObj, true);
      }
    }

  }

}