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

    // TODO: Document OnObjectHover callbacks and how they differ from OnHover callbacks
    public Action<Hand> OnObjectHoverBegin = (closestHand) => { };
    public Action<Hand> OnObjectHoverStay  = (closestHand) => { };
    public Action<Hand> OnObjectHoverEnd   = (closestHand) => { };

    public Action<Hand> OnGraspBegin = (hand) => { };
    public Action<Hand> OnGraspHold  = (hand) => { };
    public Action<Hand> OnGraspEnd   = (hand) => { };

    // TODO: When two-handed grasping becomes a thing, implement these.
    //public Action       OnObjectGraspBegin = () => { };
    //public Action       OnObjectGraspHold  = () => { };
    //public Action       OnObjectGraspEnd   = () => { };

    // TODO: Primary hover not totally good yet. Needs work.
    public Action<Hand> OnPrimaryHoverBegin = (hand) => { };
    public Action<Hand> OnPrimaryHoverStay  = (hand) => { };
    public Action<Hand> OnPrimaryHoverEnd   = (hand) => { };

    [Tooltip("Should hands move the object as if it is held when the object is grasped? "
           + "Use OnPostHoldMovement to constrain the object's motion while held, or "
           + "set this property to false to specify your own behavior entirely in "
           + "OnGraspHold or OnObjectGraspHold.")]
    public bool moveObjectWhenGrasped = true;

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
    [DisableIf("moveObjectWhenGrasped", isEqualTo: false)]
    [Tooltip("When the object is moved by a holding InteractionHand, how should it move to its"
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
    [Tooltip("Warping manipulates the graphical (but not physical) position of grasped objects "
           + "based on the movement of the Leap hand so the objects appear to move with less latency.")]
    public bool graspHoldWarpingEnabled = true; // TODO: Warping not yet implemented.

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
    /// Hovering uses this to provide per-object (as opposed to per-hand)
    /// hover callbacks, e.g. OnObjectHoverStay(), which fires once per frame
    /// while the object is hovered by any number of hands greater than zero.
    /// 
    /// Grasping uses this to do support changing moveObjectWhenGrasping at
    /// runtime.
    /// </summary>
    public override void FixedUpdateObject() {
      FixedUpdateHovering();
      //FixedUpdateContact();  // Contact not yet implemented.
      FixedUpdateGrasping(); // Not yet necessary (two-handed grabbing NYI).
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

    private void FixedUpdateHovering() {
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
    private bool _wasMovingObjectWhenGraspedLastFrame;

    private IGraspedHoldBehaviour _graspedHoldBehaviour;
    // TODO: Investigate the best way to allow custom holding behavior specification.
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
      _wasMovingObjectWhenGraspedLastFrame = moveObjectWhenGrasped;
      //_graspedMovementBehaviour = new // TODO: implement IGraspedMovementBehaviour.
    }

    private void FixedUpdateGrasping() {
      if (!moveObjectWhenGrasped && _wasMovingObjectWhenGraspedLastFrame) {
        _graspedHoldBehaviour.ClearHands();
      }

      _wasMovingObjectWhenGraspedLastFrame = moveObjectWhenGrasped;
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

      // SnapToHand(hand); // TODO: When you grasp an object, snap the object into a good holding position.

      if (moveObjectWhenGrasped) {
        GraspedHoldBehaviour.AddHand(interactionManager.GetInteractionHand(hand.Handedness()));
      }

      OnGraspBegin(hand);
    }

    public override void GraspHold(Hand hand) {
      if (moveObjectWhenGrasped) {
        Vector3 newPosition;
        Quaternion newRotation;
        _graspedHoldBehaviour.GetHoldingPose(out newPosition, out newRotation);
        //_graspedMovementBehaviour.
      }

      OnGraspHold(hand);
    }

    public override void GraspEnd(Hand hand) {
      _graspCount--;

      if (moveObjectWhenGrasped) {
        _graspedHoldBehaviour.RemoveHand(interactionManager.GetInteractionHand(hand.Handedness()));
      }

      OnGraspEnd(hand);
    }

    // TODO: Implement suspend/resume callbacks and example behavior.
    public override void GraspSuspendObject(Hand hand) {
      throw new System.NotImplementedException();
    }

    public override void GraspResumeObject(Hand hand) {
      throw new System.NotImplementedException();
    }

    #endregion

  }

}
