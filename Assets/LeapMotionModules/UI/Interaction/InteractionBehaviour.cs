using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  [RequireComponent(typeof(Rigidbody))]
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
    public RigidbodyWarper rigidbodyWarper;

    void Start() {
      Rigidbody body = GetComponent<Rigidbody>();
      rigidbodyWarper = new RigidbodyWarper(manager, this.transform, body, 0.25F);
    }

    #region Hovering

    public override float GetHoverScore(Hand hand) {
      switch (hoverType) {
        case HoverType.Proximity: default:
          // TODO: Need to get distance from THE COLLIDER. Need to do some good logic based on checking for Rigidbodies and Colliders.
          return Vector3.Distance(this.transform.position, hand.AttentionPosition()).Map(0F, 0.5F, 10F, 0F);
      }
    }

    public Action<Hand> OnHoverBegin = (hand) => { };
    public override void HoverBegin(Hand hand) {
      OnHoverBegin(hand);
    }

    public Action<Hand> OnHoverStay = (hand) => { };
    public override void HoverStay(Hand hand) {
      OnHoverStay(hand);
    }

    public Action<Hand> OnHoverEnd = (hand) => { };
    public override void HoverEnd(Hand hand) {
      OnHoverEnd(hand);
    }

    public Action<Hand> OnPrimaryHoverBegin = (hand) => { };
    public override void PrimaryHoverBegin(Hand hand) {
      OnPrimaryHoverBegin(hand);
    }

    public Action<Hand> OnPrimaryHoverStay = (hand) => { };
    public override void PrimaryHoverStay(Hand hand) {
      OnPrimaryHoverStay(hand);
    }

    public Action<Hand> OnPrimaryHoverEnd = (hand) => { };
    public override void PrimaryHoverEnd(Hand hand) {
      OnPrimaryHoverEnd(hand);
    }

    #endregion

    #region Contact

    public override void ContactBegin(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void ContactStay(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void ContactEnd(Hand hand) {
      throw new System.NotImplementedException();
    }

    #endregion

    #region Grasping

    public Action<Hand> OnGraspBegin = (hand) => { };
    public override void GraspBegin(Hand hand) {
      OnGraspBegin(hand);
    }

    public Action<Hand> OnGraspHold = (hand) => { };
    public override void GraspHold(Hand hand) {
      OnGraspHold(hand);
    }

    public Action<Hand> OnGraspEnd = (hand) => { };
    public override void GraspEnd(Hand hand) {
      OnGraspEnd(hand);
    }

    public override void GraspSuspendObject(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void GraspResumeObject(Hand hand) {
      throw new System.NotImplementedException();
    }

    #endregion

  }

}
