/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
    /** This InteractionBrushBone's Collider. */
    public Collider col;
    /** This InteractionBrushBone's FixedJoint. */
    public FixedJoint joint;
    /** This InteractionBrushBone's Metacarpal FixedJoint. */
    public FixedJoint metacarpalJoint;
    /** This InteractionBrushBone's target position. */
    public Vector3 lastTarget;
    /** The mass of the last object this InteractionBrushBone touched. */
    public float massOfLastTouchedObject = 1f;

    public void DisableColliderTemporarily(float seconds) {
      StartCoroutine(TemporaryDisable(seconds));
    }

    private IEnumerator TemporaryDisable(float seconds) {
      col.isTrigger = true;
      yield return new WaitForSecondsRealtime(seconds);
      col.isTrigger = false;
    }

    private void OnCollisionEnter(Collision collision) {
      massOfLastTouchedObject = collision.collider.attachedRigidbody.mass;
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
