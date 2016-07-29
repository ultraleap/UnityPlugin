using UnityEngine;
using Leap.Unity.Interaction;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Interaction {
  public class InteractionBrushBone : MonoBehaviour {

    // Used by InteractionBrushHand:
    public InteractionManager manager;
    public Rigidbody capsuleBody;
    public CapsuleCollider capsuleCollider;
    public Vector3 lastTarget;

    // Once the brush becomes dislocated, it then remains dislocated until it
    // stops triggering and then the _dislocatedCounter expires.
    private const int DISLOCATED_BRUSH_COOLDOWN = 3;
    private int _dislocatedCounter = DISLOCATED_BRUSH_COOLDOWN;

    public void startTriggering() {
      capsuleCollider.isTrigger = true;
      _dislocatedCounter = 0;
    }

    public bool updateTriggering() {
      if (_dislocatedCounter < DISLOCATED_BRUSH_COOLDOWN) {
        if (++_dislocatedCounter == DISLOCATED_BRUSH_COOLDOWN) {
          capsuleCollider.isTrigger = false;
          return false;
        }
        return true;
      }
      return false;
    }

    private void tryNotify(Collider other) {
      IInteractionBehaviour ib = other.GetComponentInParent<IInteractionBehaviour>();
      if (ib) {
        manager.EnsureActive(ib);
        _dislocatedCounter = 0;
        ib.NotifyBrushDislocated();
      }
    }

    protected void OnTriggerEnter(Collider other) {
      tryNotify(other);
    }

    protected void OnTriggerStay(Collider other) {
      tryNotify(other);
    }

#if UNITY_EDITOR
    private string ThisLabel() {
      return gameObject.name + " <layer " + LayerMask.LayerToName(gameObject.layer) + ">";
    }

    private string ThatLabel(Collision collision) {
      GameObject otherObj = collision.collider.gameObject;
      return otherObj.name + " <layer " + LayerMask.LayerToName(otherObj.layer) + ">";
    }

    private void OnCollisionEnter(Collision collision) {
      GameObject otherObj = collision.collider.gameObject;
      if (otherObj.GetComponentInParent<InteractionBehaviourBase>() == null) {
        Debug.LogError("For interaction to work properly please prevent collision between an InteractionBrushHand and non-interaction objects. " + ThisLabel() + ", " + ThatLabel(collision));
      }
    }
#endif // UNITY_EDITOR
  }
}
