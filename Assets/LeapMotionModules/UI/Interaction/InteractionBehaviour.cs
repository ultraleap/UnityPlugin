using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  [RequireComponent(typeof(Rigidbody))]
  public class InteractionBehaviour : InteractionBehaviourBase {

    public Action<Hand> OnHoverBegin = (hand) => { };
    public Action<Hand> OnHoverStay  = (hand) => { };
    public Action<Hand> OnHoverEnd   = (hand) => { };

    public Action<Hand> OnObjectHoverBegin = (closestHand) => { };
    public Action<Hand> OnObjectHoverStay  = (closestHand) => { };
    public Action<Hand> OnObjectHoverEnd   = (closestHand) => { };

    public Action<Hand> OnGraspBegin = (hand) => { };
    public Action<Hand> OnGraspHold  = (hand) => { };
    public Action<Hand> OnGraspEnd   = (hand) => { };

    // TODO: When two-handed grasping becomes a thing, implement these.
    //public Action       OnObjectGraspBegin = () => { };
    //public Action       OnObjectGraspStay  = () => { };
    //public Action       OnObjectGraspEnd   = () => { };

    // TODO: Primary hover not totally good yet. Needs work.
    public Action<Hand> OnPrimaryHoverBegin = (hand) => { };
    public Action<Hand> OnPrimaryHoverStay  = (hand) => { };
    public Action<Hand> OnPrimaryHoverEnd   = (hand) => { };

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

    public HoverType hoverType;
    public ContactType touchType;
    public GrabType grabType;

    /// <summary> The RigidbodyWarper manipulates the graphical (but not physical) position
    /// of grasped objects based on the movement of the Leap hand so they appear move with less latency. </summary>
    [HideInInspector]
    public RigidbodyWarper rigidbodyWarper;

    void Start() {
      interactionManager.RegisterInteractionBehaviour(this);

      Rigidbody body = GetComponent<Rigidbody>();
      rigidbodyWarper = new RigidbodyWarper(interactionManager, this.transform, body, 0.25F);
    }

    /// <summary> InteractionManager manually calls this directly
    /// after all InteractionHands are updated (in FixedUpdate).
    /// 
    /// These methods fire per-object interaction events, e.g., OnObjectHoverStay,
    /// in contrast to methods like OnHoverStay, which fire per-hand.
    /// Events like OnObjectHoverStay only fire once per FixedUpdate, no matter
    /// how many hands are hovering over the object. </summary>
    public override void FixedUpdateObject() {
      // Fire per-object interaction events
      // (As opposed to per-hand interaction events, which are handled by InteractionHand).
      FixedUpdateObjectHovering();
      //FixedUpdateObjectContact(); // Contact not yet implemented.
      //FixedUpdateObjectGrasping(); // Not yet necessary (two-handed grabbing NYI).
    }

    // TODO: Currently this gets the distance from the point to this transform, but this will
    // need to incorporate distance to the rigidbody.
    private float GetInteractionDistanceToPoint(Vector3 point) {
      return Vector3.Distance(point, this.transform.position);
    }

    #region Hovering

    // Logistics for providing per-object (instead of per-hand) Hover callbacks.
    private Hand  _closestHoveringHand = null;
    private float _closestHoveringHandDistance = float.PositiveInfinity;
    private Hand  _closestJustStoppedHoveringHand = null; // Provided for OnObjectHoverEnd.
    private int   _hoveringHandsCountLastFrame = 0;
    private int   _hoveringHandsCount = 0;

    private void FixedUpdateObjectHovering() {
      if (_hoveringHandsCount > 0) {
        if (_hoveringHandsCountLastFrame == 0) {
          OnObjectHoverBegin(_closestHoveringHand);
        }
        else {
          OnObjectHoverStay(_closestHoveringHand);
        }
      }
      else if (_hoveringHandsCountLastFrame > 0) {
        OnObjectHoverEnd(_closestJustStoppedHoveringHand);
      }

      _hoveringHandsCountLastFrame = _hoveringHandsCount;
      _closestHoveringHand = null;
      _closestHoveringHandDistance = float.PositiveInfinity;
    }

    public override float GetHoverScore(Hand hand) {
      switch (hoverType) {
        case HoverType.Proximity: default:
          // TODO: Need to get distance from THE COLLIDER. Need to do some good logic based on checking for Rigidbodies and Colliders.
          return Vector3.Distance(this.transform.position, hand.PalmPosition.ToVector3()).Map(0F, 0.5F, 10F, 0F);
      }
    }

    public override void HoverBegin(Hand hand) {
      EvaluateHoverCloseness(hand);
      _hoveringHandsCount++;

      OnHoverBegin(hand);
    }

    public override void HoverStay(Hand hand) {
      EvaluateHoverCloseness(hand);

      OnHoverStay(hand);
    }

    private void EvaluateHoverCloseness(Hand hand) {
      if (_hoveringHandsCount == 0 || _closestHoveringHand == null) {
        _closestHoveringHand = hand;
      }
      else {
        float handDistance = GetInteractionDistanceToPoint(hand.PalmPosition.ToVector3());
        if (handDistance < _closestHoveringHandDistance) {
          _closestHoveringHand = hand;
          _closestHoveringHandDistance = handDistance;
        }
      }
    }

    public override void HoverEnd(Hand hand) {
      _hoveringHandsCount--;
      if (_hoveringHandsCount == 0) {
        _closestJustStoppedHoveringHand = hand;
      }

      OnHoverEnd(hand);
    }

    public override void PrimaryHoverBegin(Hand hand) {
      OnPrimaryHoverBegin(hand);
    }

    public override void PrimaryHoverStay(Hand hand) {
      OnPrimaryHoverStay(hand);
    }

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

    private int _graspCount = 0;

    public override bool IsGrasped {
      get { return _graspCount > 0; }
    }

    public override void GraspBegin(Hand hand) {
      if (IsGrasped && !allowsTwoHandedGrasp) {
        interactionManager.ReleaseObjectFromGrasp(this);
      }
      _graspCount++;

      // TODO: Make two-handed grasping a thing.
      if (_graspCount > 1) {
        Debug.LogWarning("Two-handed grasping is not yet supported!");
      }

      OnGraspBegin(hand);
    }

    public override void GraspHold(Hand hand) {
      OnGraspHold(hand);
    }

    public override void GraspEnd(Hand hand) {
      _graspCount--;

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
