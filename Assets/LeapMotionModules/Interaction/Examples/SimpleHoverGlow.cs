/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This simple script changes the color of an InteractionBehaviour as
/// a function of its distance to the palm of the closest hand that is
/// hovering nearby.
/// </summary>
[AddComponentMenu("")]
[RequireComponent(typeof(InteractionBehaviour))]
public class SimpleHoverGlow : MonoBehaviour {

  public bool listenForPrimaryHover = false;

  private Material _material;

  private InteractionBehaviour _intObj;

  void Start() {
    _intObj = GetComponent<InteractionBehaviour>();

    Renderer renderer = GetComponent<Renderer>();
    if (renderer == null) {
      renderer = GetComponentInChildren<Renderer>();
    }
    if (renderer != null) {
      _material = renderer.material;
    }
  }

  void Update() {
    if (_material != null) {
      if (_intObj.isSuspended) {
        // If the object is held by only one hand and that holding hand stops tracking, the
        // object is "suspended." InteractionBehaviour provides suspension callbacks if you'd
        // like the object to, for example, disappear, when the object is suspended.
        // Alternatively you can check "isSuspended" at any time.
        _material.color = Color.red;
      }
      else {
        _material.color = Color.black;
      }

      // "Primary hover" is a special kind of hover state that an InteractionBehaviour can
      // only have if an InteractionHand's thumb, index, or middle finger is closer to it
      // than any other interaction object.
      if (_intObj.isPrimaryHovered && listenForPrimaryHover) {
        _material.color = Color.white;
      }
      else {
        // Of course, any number of objects can be hovered by any number of InteractionHands.
        // InteractionBehaviour provides an API for accessing various interaction-related
        // state information such as the closest hand that is hovering nearby, if the object
        // is hovered at all.
        if (_intObj.isHovered) {
          float glow = Vector3.Distance(_intObj.closestHoveringHand.PalmPosition.ToVector3(), this.transform.position).Map(0F, 0.2F, 1F, 0.0F);
          _material.color = new Color(glow, glow, glow, 1F);
        }
      }
    }
  }

}
