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
        UnityEditor.EditorUtility.DisplayDialog("Collision Error!",
                                                "For interaction to work properly please prevent collision between an InteractionBrushHand "
                                                + "and non-interaction objects. " + ThisLabel() + ", " + ThatLabel(collision),
                                                "Ok");
        Debug.Break();
      }

      /* GRASP IS TRIGGERING THIS:


            if (otherObj.GetComponentInParent<Rigidbody>() == null || otherObj.GetComponentInParent<Rigidbody>().isKinematic) {
              UnityEditor.EditorUtility.DisplayDialog("Collision Error!",
                                                      "For interaction to work properly please require collisions be with a Rigidbody that is not kinematic"
                                                      + ThisLabel() + ", " + ThatLabel(collision),
                                                      "Ok");
              Debug.Break();
            }

            PhysicMaterial material = otherObj.GetComponentInParent<Collider>().material;
            if (material == null)
            {
              UnityEditor.EditorUtility.DisplayDialog("Collision Error!",
                                                      "For interaction to work properly please provide a material for all objects touching an InteractionBrushHand."
                                                      + "Name:" + otherObj.gameObject.name,
                                                      "Ok");
              Debug.Break();
            }
      */
    }
  }
}

#endif // UNITY_EDITOR
