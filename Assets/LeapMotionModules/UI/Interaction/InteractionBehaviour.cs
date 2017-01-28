using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class InteractionBehaviour : InteractionBehaviourBase {

    public enum HoverType {
      Proximity
    }

    public enum TouchType {
      SoftContact,
      CallbacksOnly
    }

    public enum GrabType {
      GrabOrPinch,
      GrabOnly,
      PinchOnly
    }

    [Header("Interaction Types")]
    public bool enableHover = true;
    public HoverType hoverType;
    public bool enableTouch = true;
    public TouchType touchType;
    public bool enableGrab = true;
    public GrabType grabType;

    public override void OnHoverBegin(Hand hand, HoverEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnHoverStay(Hand hand, HoverEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnHoverEnd(Hand hand, HoverEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnPrimaryHoverBegin(Hand hand, HoverEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnPrimaryHoverStay(Hand hand, HoverEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnPrimaryHoverEnd(Hand hand, HoverEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnTouchBegin(Hand hand, TouchEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnTouchStay(Hand hand, TouchEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnTouchEnd(Hand hand, TouchEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnGrab(Hand hand, GrabEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnHold(Hand hand, GrabEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnRelease(Hand hand, GrabEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnSuspend(Hand hand, GrabEventData data) {
      throw new System.NotImplementedException();
    }

    public override void OnResume(Hand hand, GrabEventData data) {
      throw new System.NotImplementedException();
    }

  }

}
