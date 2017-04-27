using UnityEngine;

namespace Leap.Unity.UI.Interaction.Internal {

  /// <summary>
  /// Contact Bones store data for the colliders and rigidbodies in each
  /// bone of the contact-related representation of an InteractionHand.
  /// They also notify the InteractionHand of collisions for further
  /// processing.
  /// </summary>
  [AddComponentMenu("")]
  public class ContactBone : MonoBehaviour {

    public InteractionHand interactionHand;
    public Rigidbody body;
    #if UNITY_EDITOR
    new 
    #endif
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
              _lastObjectTouchedAdjustedMass *= 10f;
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