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

    /// <summary>
    /// When the object is grasped, what should it do? FollowHand utilizes a Kabsch
    /// solve algorithm to move the object with the hand. Or, choose DoNothing and
    /// use the grasp events to specify your own behavior.
    /// </summary>
    public enum GraspedHoldBehavior {
      FollowHand,
      DoNothing
    }
    [Tooltip("When the object is grasped, what should it do? FollowHand utilizes a Kabsch "
           + "solve algorithm to move the object with the hand. Or, choose DoNothing and "
           + "use the grasp events to specify your own behavior.")]
    public GraspedHoldBehavior graspedHoldBehavior;

    /// <summary>
    /// When the object is moved by the FollowHand behavior, how should it move to its
    /// new position? Nonkinematic bodies will collide with other Rigidbodies, so they
    /// might not reach the target position. Kinematic rigidbodies will always move to the
    /// target position, ignoring collisions. Inherit will simply use the isKinematic
    /// state of the Rigidbody from before it was grasped.
    /// </summary>
    public enum GraspedHoldMovementType {
      Inherit,
      Kinematic,
      Nonkinematic
    }
    [DisableIf("graspedHoldBehavior", isEqualTo: GraspedHoldBehavior.DoNothing)]
    [Tooltip("When the object is moved by the FollowHand behavior, how should it move to its"
      + "new position? Nonkinematic bodies will collide with other Rigidbodies, so they "
      + "might not reach the target position. Kinematic rigidbodies will always move to the "
      + "target position, ignoring collisions. Inherit will simply use the isKinematic "
      + "state of the Rigidbody from before it was grasped.")]
    public GraspedHoldMovementType graspedHoldMovementType;

    /// <summary> The RigidbodyWarper manipulates the graphical (but not physical) position
    /// of grasped objects based on the movement of the Leap hand so they appear move with less latency. </summary>
    [HideInInspector]
    public RigidbodyWarper rigidbodyWarper;

    [Header("Advanced Settings")]
    [Tooltip("")]
    public bool GraspHoldWarpingEnabled = true;

    private Rigidbody _body;

    void Start() {
      interactionManager.RegisterInteractionBehaviour(this);

      _body = GetComponent<Rigidbody>();
      rigidbodyWarper = new RigidbodyWarper(interactionManager, this.transform, _body, 0.25F);

      InitGrasping();
    }

    /// <summary>
    /// InteractionManager manually calls this directly
    /// after all InteractionHands are updated (in FixedUpdate).
    /// 
    /// These methods fire per-object interaction events, e.g., OnObjectHoverStay,
    /// in contrast to methods like OnHoverStay, which fire per-hand.
    /// Events like OnObjectHoverStay only fire once per FixedUpdate, no matter
    /// how many hands are hovering over the object.
    /// </summary>
    public override void FixedUpdateObject() {
      // Fire per-object interaction events
      // (As opposed to per-hand interaction events, which are handled by InteractionHand).
      FixedUpdateObjectHovering();
      //FixedUpdateObjectContact();  // Contact not yet implemented.
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
      // TODO: Need to get distance from the InteractionBehaviour's colliders. Probably has to wait until 5.6 (Physics.ClosestPoint)
      return Vector3.Distance(this.transform.position, hand.PalmPosition.ToVector3()).Map(0F, interactionManager.WorldHoverActivationRadius, 10F, 0F);
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

    private IGraspedHoldBehaviour     _graspedHoldBehaviour;
    private IGraspedHoldBehaviour GraspedHoldBehaviour {
      get {
        if (_graspedHoldBehaviour == null) {
          _graspedHoldBehaviour = new KabschHoldBehaviour();
        }
        return _graspedHoldBehaviour;
      }
    }
    private IGraspedMovementBehaviour _graspedMovementBehaviour;

    private void InitGrasping() {
      _graspedHoldBehaviour = new KabschHoldBehaviour();
      //_graspedMovementBehaviour = new 
    }

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

      SnapToHand(hand);

      OnGraspBegin(hand);
    }

    // Not yet implemented.
    private void SnapToHand(Hand hand) {
      // TODO: When you grasp an object, snap the object into a good holding position.
    }

    public override void GraspHold(Hand hand) {
      if (graspedHoldBehavior == GraspedHoldBehavior.FollowHand) {
        //KabschFollow(hand);
      }

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
