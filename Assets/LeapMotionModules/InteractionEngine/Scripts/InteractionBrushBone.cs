using UnityEngine;
using Leap.Unity.Interaction;

#if UNITY_EDITOR
using UnityEditor;
#endif

// This is a debug script for the editor.  Should not be instanced in game.
namespace Leap.Unity
{
  public class InteractionBrushBone : MonoBehaviour
  {
#if UNITY_EDITOR
    private void OnCollisionEnter(Collision collision)
    {
      GameObject otherObj = collision.collider.gameObject;
      if (otherObj.GetComponentInParent<InteractionBehaviourBase>() == null)
      {
        string thisLabel = gameObject.name + " <layer " + LayerMask.LayerToName(gameObject.layer) + ">";
        string otherLabel = otherObj.name + " <layer " + LayerMask.LayerToName(otherObj.layer) + ">";

        UnityEditor.EditorUtility.DisplayDialog("Collision Error!",
                                                "For interaction to work properly please prevent collision between an InteractionBrushHand "
                                                + "and non-interaction objects. " + thisLabel + ", " + otherLabel,
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
    }
  }
#endif
}
