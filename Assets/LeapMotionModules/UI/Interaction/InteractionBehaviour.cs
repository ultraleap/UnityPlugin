using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class InteractionBehaviour : InteractionBehaviourBase {

    public enum HoverType {
      Proximity
    }

    public enum ContactType {
      SoftContact,
      CallbacksOnly
    }

    public enum GrabType {
      GrabOrPinch,
      GrabOnly,
      PinchOnly
    }

    [Header("Interaction Settings")]
    [DisableIf("enableHovering", isNotEqualTo: true)]
    public HoverType hoverType;
    [DisableIf("enableContact", isNotEqualTo: true)]
    public ContactType touchType;
    [DisableIf("enableGrasping", isNotEqualTo: true)]
    public GrabType grabType;

    /// <summary> The RigidbodyWarper manipulates the graphical (but not physical) position
    /// of grasped objects based on the movement of the Leap hand so they appear move with less latency. </summary>
    [HideInInspector]
    public Leap.Unity.Interaction.RigidbodyWarper rigidbodyWarper;


    #region Hovering

    public override float GetHoverScore(Hand hand) {
      switch (hoverType) {
        case HoverType.Proximity: default:
          // TODO: Need to get distance from THE COLLIDER. Need to do some good logic based on checking for Rigidbodies and Colliders.
          return Vector3.Distance(this.transform.position, hand.AttentionPosition()).Map(0F, 0.5F, 10F, 0F);
      }
    }

    public Action<Hand> OnHoverBeginEvent = (hand) => { };
    public override void OnHoverBegin(Hand hand) {
      OnHoverBeginEvent(hand);
    }

    public Action<Hand> OnHoverStayEvent = (hand) => { };
    public override void OnHoverStay(Hand hand) {
      OnHoverStayEvent(hand);
    }

    public Action<Hand> OnHoverEndEvent = (hand) => { };
    public override void OnHoverEnd(Hand hand) {
      OnHoverEndEvent(hand);
    }

    public Action<Hand> OnPrimaryHoverBeginEvent = (hand) => { };
    public override void OnPrimaryHoverBegin(Hand hand) {
      OnPrimaryHoverBeginEvent(hand);
    }

    public Action<Hand> OnPrimaryHoverStayEvent = (hand) => { };
    public override void OnPrimaryHoverStay(Hand hand) {
      OnPrimaryHoverStayEvent(hand);
    }

    public Action<Hand> OnPrimaryHoverEndEvent = (hand) => { };
    public override void OnPrimaryHoverEnd(Hand hand) {
      OnPrimaryHoverEndEvent(hand);
    }

    #endregion

    #region Contact

    public override void OnContactBegin(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnContactStay(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnContactEnd(Hand hand) {
      throw new System.NotImplementedException();
    }

    #endregion

    #region Grasping

    public override void OnGraspBegin(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnGraspHold(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnGraspRelease(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnSuspend(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void OnResume(Hand hand) {
      throw new System.NotImplementedException();
    }

    #endregion

  }

}
