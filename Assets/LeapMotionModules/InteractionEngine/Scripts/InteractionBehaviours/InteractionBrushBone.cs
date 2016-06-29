using UnityEngine;
using Leap.Unity.Interaction;
using UnityEditor;

// This is a debug script for the editor.  Should not be instanced in game.
namespace Leap.Unity.Interaction {
  public class InteractionBrushBone : MonoBehaviour {
    // Operated by InteractionBrushHand.
    public Rigidbody capsuleBody;
    public CapsuleCollider capsuleCollider;
    public Vector3 lastTarget;
    public int dislocationCounter = 0;
    public int triggerCounter = 0;
    public InteractionBrushHand brushHand = null;
    public int boneArrayIndex = -1;

    protected void OnTriggerEnter(Collider other) {
      IInteractionBehaviour ib = other.GetComponentInParent<IInteractionBehaviour>();
      if(ib) {
//        ib.NotifyBrushTriggerEnter();
        ++triggerCounter;
      }
    }

    protected void OnTriggerExit(Collider other) {
      IInteractionBehaviour ib = other.GetComponentInParent<IInteractionBehaviour>();
      if(ib) {
//        ib.NotifyBrushTriggerExit();
        --triggerCounter;
      }
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
