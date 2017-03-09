using UnityEngine;
using Leap.Unity.Interaction;
using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Interaction {

  /**
  * The InteractiveBrushBone class is a physics element of an InteractiveBrushHand object.
  * It contains Unity physics components that are controlled by the Interaction Engine.
  * @since 4.1.3
  */
  public class InteractionBrushBone : MonoBehaviour {

    // Used by InteractionBrushHand:
    /** The active InteractionManager. */
    public InteractionManager manager;
    /** This InteractiveBrushBone's RigidBody. */
    public Rigidbody body;
    /** This InteractiveBrushBone's Collider. */
    public Collider col;
    /** This InteractiveBrushBone's target position. */
    public Vector3 lastTarget;

    public void DisableColliderTemporarily(float seconds) {
      StartCoroutine(TemporaryDisable(seconds));
    }
    private IEnumerator TemporaryDisable(float seconds) {
      col.isTrigger = true;
      yield return new WaitForSecondsRealtime(seconds);
      col.isTrigger = false;
    }

#if UNITY_EDITOR
    private string ThisLabel() {
      return gameObject.name + " <layer " + LayerMask.LayerToName(gameObject.layer) + ">";
    }

    private string ThatLabel(Collision collision) {
      GameObject otherObj = collision.collider.gameObject;
      return otherObj.name + " <layer " + LayerMask.LayerToName(otherObj.layer) + ">";
    }
#endif // UNITY_EDITOR
  }
}
