using Leap.Unity.UI.Interaction.Internal;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class BrushBone : MonoBehaviour {

    public InteractionHand interactionHand;
    public Rigidbody body;
    public Collider collider;
    public FixedJoint joint;
    public FixedJoint metacarpalJoint;
    public Vector3 lastTarget;

    public Vector3 desiredPosition;
    public Quaternion desiredRotation;

    public float _lastObjectTouchedAdjustedMass;

    void OnCollisionEnter(Collision collision) {
      IInteractionBehaviour interactionObj;
      if (collision.rigidbody == null) {
        Debug.LogError("Brush Bone collided with non-rigidbody collider: " + collision.collider.name + "."
                     + "Please enable automatic layer generation in the Interaction Manager, "
                     + "or ensure the Interaction layer only contains Interaction Behaviours.");
      }

      if (interactionHand.interactionManager.rigidbodyRegistry.TryGetValue(collision.rigidbody, out interactionObj)) {
        _lastObjectTouchedAdjustedMass = collision.rigidbody.mass;
        if (interactionObj is InteractionBehaviour) {
          switch ((interactionObj as InteractionBehaviour).contactForceMode) {
            case InteractionBehaviour.ContactForceModes.Object:
              _lastObjectTouchedAdjustedMass *= 0.1f;
              break;
            case InteractionBehaviour.ContactForceModes.UI:
              _lastObjectTouchedAdjustedMass *= 100f;
              break;
            default:
              _lastObjectTouchedAdjustedMass *= 0.1f;
              break;
          }
        }

        interactionHand.ContactBoneCollisionEnter(this, interactionObj, false);
      }
    }
    void OnCollisionExit(Collision collision) {
      IInteractionBehaviour interactionObj;
      if (interactionHand.interactionManager.rigidbodyRegistry.TryGetValue(collision.rigidbody, out interactionObj)) {
        interactionHand.ContactBoneCollisionExit(this, interactionObj, false);
      }
    }
    void OnTriggerEnter(Collider collider) {
      if (collider.attachedRigidbody == null) {
        Debug.LogError("Brush Bone collided with non-rigidbody collider trigger: " + collider.name + "."
                     + "Please enable automatic layer generation in the Interaction Manager, "
                     + "or ensure the Interaction layer only contains Interaction Behaviours.");
      }
      IInteractionBehaviour interactionObj;
      if (interactionHand.interactionManager.rigidbodyRegistry.TryGetValue(collider.attachedRigidbody, out interactionObj)) {
        interactionHand.ContactBoneCollisionEnter(this, interactionObj, true);
      }
    }
    void OnTriggerExit(Collider collider) {
      IInteractionBehaviour interactionObj;
      if (interactionHand.interactionManager.rigidbodyRegistry.TryGetValue(collider.attachedRigidbody, out interactionObj)) {
        interactionHand.ContactBoneCollisionExit(this, interactionObj, true);
      }
    }

  }

}