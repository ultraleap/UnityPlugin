#if UNITY_EDITOR

using UnityEngine;
using Leap.Unity.Interaction;
using UnityEditor;

// This is a debug script for the editor.  Should not be instanced in game.
namespace Leap.Unity {
  public class InteractionBrushBone : MonoBehaviour {
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
        Debug.Log("For interaction to work properly please prevent collision between an InteractionBrushHand and non-interaction objects. " + ThisLabel() + ", " + ThatLabel(collision));
      }
    }
  }
}

#endif // UNITY_EDITOR
