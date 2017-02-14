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

    public Action<Hand>                      OnGraspBegin             = (hand) => { };
    public Action<Vector3, Quaternion, Hand> OnPreHoldingMovement     = (preSolvePos, preSolveRot, hand) => { };
    public Action<Vector3, Quaternion, Hand> OnPostHoldingMovement    = (solvedPos, solvedRot, hand) => { };
    public Action<Hand>                      OnGraspHold              = (hand) => { };
    public Action<Hand>                      OnGraspEnd               = (hand) => { };

    // TODO: When two-handed grasping becomes a thing, implement these.
    //public Action       OnObjectGraspBegin = () => { };
    //public Action       OnObjectGraspHold  = () => { };
    //public Action       OnObjectGraspEnd   = () => { };

    public Action<Hand> OnPrimaryHoverBegin = (hand) => { };
    public Action<Hand> OnPrimaryHoverStay  = (hand) => { };
    public Action<Hand> OnPrimaryHoverEnd   = (hand) => { };

    public Action<Hand> OnObjectPrimaryHoverBegin = (closestHand) => { };
    public Action<Hand> OnObjectPrimaryHoverStay  = (closestHand) => { };
    public Action<Hand> OnObjectPrimaryHoverEnd   = (closestHand) => { };

    public Action<Hand> OnContactBegin = (hand) => { };
    public Action<Hand> OnContactStay  = (hand) => { };
    public Action<Hand> OnContactEnd   = (hand) => { };

    //public Action<Hand> OnObjectContactBegin = (closestHand) => { };
    //public Action<Hand> OnObjectContactStay  = (closestHand) => { };
    //public Action<Hand> OnObjectContactEnd   = (closestHand) => { };

    [Tooltip("Should hands move the object as if it is held when the object is grasped? "
           + "Use OnPostHoldingMovement to constrain the object's motion while held, or "
           + "set this property to false to specify your own behavior entirely in "
           + "OnGraspHold or OnObjectGraspHold.")]
    [DisableIf("_interactionManagerIsNull", isEqualTo: true)]
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
    [Tooltip("When the object is moved by a holding InteractionHand, how should it move to its "
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
    public bool graspHoldWarpingEnabled__curIgnored = true; // TODO: Warping not yet implemented.

    protected override void Awake() {
      base.Awake();
      rigidbodyWarper = new RigidbodyWarper(interactionManager, this.transform, _body, 0.25F);
    }

    void Start() {
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
      FixedUpdatePrimaryHovering();
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

    // Runs after InteractionHands have done FixedUpdateHand.
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

    private Hand _closestPrimaryHoveringHand = null;
    private float _closestPrimaryHoveringHandDistance = float.PositiveInfinity;
    private Hand _closestJustStoppedPrimaryHoveringHand = null; // Provided for OnObjectPrimaryHoverEnd.
    private int _primaryHoveringHandsCountLastFrame = 0;
    private int _primaryHoveringHandsCount = 0;

    private void FixedUpdatePrimaryHovering() {
      if (_primaryHoveringHandsCount > 0) {
        if (_primaryHoveringHandsCountLastFrame == 0) {
          OnObjectPrimaryHoverBegin(_closestPrimaryHoveringHand);
        }
        else {
          OnObjectPrimaryHoverStay(_closestPrimaryHoveringHand);
        }
      }
      else if (_primaryHoveringHandsCountLastFrame > 0) {
        OnObjectPrimaryHoverEnd(_closestJustStoppedPrimaryHoveringHand);
      }

      _primaryHoveringHandsCountLastFrame = _primaryHoveringHandsCount;
      _closestPrimaryHoveringHand = null;
      _closestPrimaryHoveringHandDistance = float.PositiveInfinity;
    }

    public override float GetDistance(Vector3 worldPosition) {
      // TODO: Need to get distance from the InteractionBehaviour's colliders. Probably has to wait until 5.6 (Physics.ClosestPoint)
      return GetInteractionDistanceToPoint(worldPosition);
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
      float handDistance = GetInteractionDistanceToPoint(hand.PalmPosition.ToVector3());
      if (_hoveringHandsCount == 0 || _closestHoveringHand == null) {
        _closestHoveringHand = hand;
        _closestHoveringHandDistance = handDistance;
      }
      else {
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
      EvaluatePrimaryHoverCloseness(hand);
      _primaryHoveringHandsCount++;

      OnPrimaryHoverBegin(hand);
    }

    public override void PrimaryHoverStay(Hand hand) {
      EvaluatePrimaryHoverCloseness(hand);

      OnPrimaryHoverStay(hand);
    }

    private void EvaluatePrimaryHoverCloseness(Hand hand) {
      float handDistance = GetInteractionDistanceToPoint(hand.PalmPosition.ToVector3());
      if (_primaryHoveringHandsCount == 0 || _closestPrimaryHoveringHand == null) {
        _closestPrimaryHoveringHand = hand;
        _closestPrimaryHoveringHandDistance = handDistance;
      }
      else {
        if (handDistance < _closestPrimaryHoveringHandDistance) {
          _closestPrimaryHoveringHand = hand;
          _closestPrimaryHoveringHandDistance = handDistance;
        }
      }
    }

    public override void PrimaryHoverEnd(Hand hand) {
      _primaryHoveringHandsCount--;
      if (_primaryHoveringHandsCount == 0) {
        _closestJustStoppedPrimaryHoveringHand = hand;
      }      

      OnPrimaryHoverEnd(hand);
    }

    #endregion

    #region Contact

    public override void ContactBegin(Hand hand) {
      OnContactBegin(hand);
    }

    public override void ContactStay(Hand hand) {
      OnContactStay(hand);
    }

    public override void ContactEnd(Hand hand) {
      OnContactEnd(hand);
    }

    #endregion

    #region Grasping

    private int _graspCount = 0;
    private bool _wasMovingObjectWhenGraspedLastFrame;
    private bool _wasKinematicBeforeGrab;

    private IHoldingPoseController _graspedPositionBehaviour;
    private IHoldingPoseController GraspedPositionBehaviour {
      get {
        if (_graspedPositionBehaviour == null) {
          _graspedPositionBehaviour = new KabschHoldingPose(this);
        }
        return _graspedPositionBehaviour;
      }
    }

    private KinematicHoldingMovement    _kinematicHoldingMovement;
    private NonKinematicHoldingMovement _nonKinematicHoldingMovement;

    private void InitGrasping() {
      _wasMovingObjectWhenGraspedLastFrame = moveObjectWhenGrasped;

      _kinematicHoldingMovement = new KinematicHoldingMovement();
      _nonKinematicHoldingMovement = new NonKinematicHoldingMovement();
    }

    private void FixedUpdateGrasping() {
      if (!moveObjectWhenGrasped && _wasMovingObjectWhenGraspedLastFrame) {
        GraspedPositionBehaviour.ClearHands();
      }

      _wasMovingObjectWhenGraspedLastFrame = moveObjectWhenGrasped;
    }

    public override bool IsGrasped {
      get { return _graspCount > 0; }
    }

    public override void GraspBegin(Hand hand) {
      if (IsGrasped && !allowsTwoHandedGrasp__curIgnored) {
        interactionManager.ReleaseObjectFromGrasp(this);
      }
      _graspCount++;

      // TODO: Make two-handed grasping a thing.
      if (_graspCount > 1) {
        Debug.LogWarning("Two-handed grasping is not yet supported!");
      }
      
      // Set kinematic state based on grasping hold movement type
      _wasKinematicBeforeGrab = Rigidbody.isKinematic;
      switch (graspedHoldMovementType) {
        case GraspedHoldMovementType.Inherit: break; // no change
        case GraspedHoldMovementType.Kinematic:
          Rigidbody.isKinematic = true; break;
        case GraspedHoldMovementType.Nonkinematic:
          Rigidbody.isKinematic = false; break;
      }

      // SnapToHand(hand); // TODO: When you grasp an object, snap the object into a good holding position.

      if (moveObjectWhenGrasped) {
        GraspedPositionBehaviour.AddHand(interactionManager.GetInteractionHand(hand.Handedness()));
      }

      OnGraspBegin(hand);
    }

    public override void GraspHold(Hand hand) {
      if (moveObjectWhenGrasped) {
        OnPreHoldingMovement(Rigidbody.position, Rigidbody.rotation, hand);

        Vector3 newPosition; Quaternion newRotation;
        GraspedPositionBehaviour.GetHoldingPose(out newPosition, out newRotation);

        IHoldingMovementController holdingMovementController;
        if (Rigidbody.isKinematic) {
          holdingMovementController = _kinematicHoldingMovement;
        }
        else {
          holdingMovementController = _nonKinematicHoldingMovement;
        }
        holdingMovementController.MoveTo(newPosition, newRotation, this);

        OnPostHoldingMovement(newPosition, newRotation, hand);
      }

      OnGraspHold(hand);
    }

    public override void GraspEnd(Hand hand) {
      _graspCount--;

      // Revert kinematic state
      Rigidbody.isKinematic = _wasKinematicBeforeGrab;

      if (moveObjectWhenGrasped) {
        GraspedPositionBehaviour.RemoveHand(interactionManager.GetInteractionHand(hand.Handedness()));
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
