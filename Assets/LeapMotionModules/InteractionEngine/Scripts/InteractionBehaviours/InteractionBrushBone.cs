#if UNITY_EDITOR

using UnityEngine;
using Leap.Unity.Interaction;
using UnityEditor;

// This is a debug script for the editor.  Should not be instanced in game.
namespace Leap.Unity
{
  public class InteractionBrushBone : MonoBehaviour
  {
    private int _collisionCounter = 0;
    private float _collisionMassTotal = 0.0f;

    // Should be called by hand every fixed update.
    public float getAverageContactingMass()
    {
      if (_collisionCounter == 0)
        return 0.0f;

      float result = _collisionMassTotal / (float)_collisionCounter;
      _collisionCounter = 0;
      _collisionMassTotal = 0.0f;
      return result;
    }

    void OnCollisionStay(Collision collisionInfo)
    {
      // Doing this every frame prevents issues when the mass of other objects changes.

      Rigidbody otherRigidbody = collisionInfo.collider.attachedRigidbody;
      if(otherRigidbody) {
        ++_collisionCounter;
        _collisionMassTotal += otherRigidbody.mass;
      }
    }

#if UNITY_EDITOR
    private string ThisLabel()
    {
      return gameObject.name + " <layer " + LayerMask.LayerToName(gameObject.layer) + ">";
    }

    private string ThatLabel(Collision collision)
    {
      GameObject otherObj = collision.collider.gameObject;
      return otherObj.name + " <layer " + LayerMask.LayerToName(otherObj.layer) + ">";
    }

    private void OnCollisionEnter(Collision collision)
    {
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
#endif
  }
}

#endif // UNITY_EDITOR
