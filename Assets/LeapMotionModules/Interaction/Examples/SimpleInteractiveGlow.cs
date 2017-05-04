using Leap.Unity;
using Leap.Unity.Interaction;
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

      Color targetColor;

      if (_intObj.isSuspended) {
        // If the object is held by only one hand and that holding hand stops tracking, the
        // object is "suspended." InteractionBehaviour provides suspension callbacks if you'd
        // like the object to, for example, disappear, when the object is suspended.
        // Alternatively you can check "isSuspended" at any time.
        targetColor = Color.red;
      }
      else {
        targetColor = Color.black;
      }

      // "Primary hover" is a special kind of hover state that an InteractionBehaviour can
      // only have if an InteractionHand's thumb, index, or middle finger is closer to it
      // than any other interaction object.
      if (_intObj.isPrimaryHovered && listenForPrimaryHover) {
        targetColor = Color.white;
      }
      else {
        // Of course, any number of objects can be hovered by any number of InteractionHands.
        // InteractionBehaviour provides an API for accessing various interaction-related
        // state information such as the closest hand that is hovering nearby, if the object
        // is hovered at all.
        if (_intObj.isHovered) {
          float glow = Vector3.Distance(_intObj.closestHoveringHand.PalmPosition.ToVector3(), this.transform.position).Map(0F, 0.2F, 1F, 0.0F);
          targetColor = new Color(glow, glow, glow, 1F);
        }
      }

      // We can also check the depressed-or-not-depressed state of InteractionButton objects
      // and assign them a unique color in that case.
      if (_intObj is InteractionButton && (_intObj as InteractionButton).isDepressed) {
        targetColor = Color.blue;
      }

      _material.color = Color.Lerp(_material.color, targetColor, 30F * Time.deltaTime);
    }
  }

}
