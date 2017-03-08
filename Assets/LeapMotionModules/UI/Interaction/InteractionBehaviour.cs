using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Leap.Unity.UI.Interaction {

  [RequireComponent(typeof(Rigidbody))]
  public class InteractionBehaviour : InteractionBehaviourBase {

    /// <summary> Gets whether any hand is nearby. </summary>
    public bool isHovered             { get { return _hoveringHandsCount > 0; } }

    /// <summary> Gets the closest hand to this object, or null if no hand is nearby. </summary>
    public Hand closestHoveringHand   { get { return _publicClosestHoveringHand; } }

    /// <summary> Gets this object's primary hovering hand, or null if no hand is primarily
    /// hovering over this interaction object. (That is, closer to this object than to any other object.) </summary>
    public bool isPrimaryHovered      { get { return _primaryHoveringHandsCount > 0; } }

    /// <summary> Returns the primary hovering hand for this interaction object, if it has one.
    /// A hand is the primary hover for an interaction object only if it is closer to that object
    /// than any other interaction object. If there are multiple such hands, this returns the hand
    /// closest to this object. </summary>
    public Hand primaryHoveringHand   { get { return _publicClosestPrimaryHoveringHand; } }

    public Action<Hand> OnHoverBegin = (hand) => { };
    public Action<Hand> OnHoverStay  = (hand) => { };
    public Action<Hand> OnHoverEnd   = (hand) => { };

    // TODO: Document OnObjectHover callbacks and how they differ from OnHover callbacks
    public Action<Hand> OnObjectHoverBegin = (closestHand) => { };
    public Action<Hand> OnObjectHoverStay  = (closestHand) => { };
    public Action<Hand> OnObjectHoverEnd   = (closestHand) => { };

    public Action<Hand>                      OnGraspBegin             = (hand) => { };
    public Action<Vector3, Quaternion, Hand> OnPreGraspedMovement     = (preSolvePos, preSolveRot, hand) => { };
    public Action<Vector3, Quaternion, Hand> OnGraspedMovement    = (solvedPos, solvedRot, hand) => { };
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

    public Action<Hand> OnObjectContactBegin = (closestHand) => { };
    public Action<Hand> OnObjectContactStay = (closestHand) => { };
    public Action<Hand> OnObjectContactEnd = (closestHand) => { };

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
    public enum GraspedMovementType {
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
    public GraspedMovementType graspedMovementType;

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
      RefreshPositionLockedState();
      InitGrasping();

      InitInternal();
    }

    /// <summary>
    /// InteractionManager manually calls this directly
    /// after all InteractionHands are updated (in FixedUpdate).
    /// 
    /// Hovering uses its update to provide per-object (as opposed to per-hand)
    /// hover callbacks, e.g. OnObjectHoverStay(), which fires once per frame
    /// while the object is hovered by any number of hands greater than zero.
    /// 
    /// Grasping uses its update to support changing moveObjectWhenGrasping at
    /// runtime.
    /// </summary>
    public override void FixedUpdateObject() {
      FixedUpdateHovering();
      FixedUpdatePrimaryHovering();
      FixedUpdateContact();
      FixedUpdateGrasping();

      FixedUpdateCollisionMode();
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

    private Hand _publicClosestHoveringHand = null;

    // Runs after InteractionHands have done FixedUpdateHand.
    private void FixedUpdateHovering() {
      if (_hoveringHandsCount > 0) {
        if (_hoveringHandsCountLastFrame == 0) {
          _publicClosestHoveringHand = _closestHoveringHand;
          OnObjectHoverBegin(_closestHoveringHand);
        }
        else {
          _publicClosestHoveringHand = _closestHoveringHand;
          OnObjectHoverStay(_closestHoveringHand);
        }
      }
      else if (_hoveringHandsCountLastFrame > 0) {
        _closestHoveringHand = null;
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

    private Hand _publicClosestPrimaryHoveringHand = null;

    private void FixedUpdatePrimaryHovering() {
      if (_primaryHoveringHandsCount > 0) {
        if (_primaryHoveringHandsCountLastFrame == 0) {
          _publicClosestPrimaryHoveringHand = _closestPrimaryHoveringHand;
          OnObjectPrimaryHoverBegin(_closestPrimaryHoveringHand);
        }
        else {
          _publicClosestPrimaryHoveringHand = _closestPrimaryHoveringHand;
          OnObjectPrimaryHoverStay(_closestPrimaryHoveringHand);
        }
      }
      else if (_primaryHoveringHandsCountLastFrame > 0) {
        _publicClosestPrimaryHoveringHand = null;
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

    private Hand _closestContactingHand = null;
    private float _closestContactingHandDistance = float.PositiveInfinity;
    private Hand _closestJustStoppedContactingHand = null; // Provided for OnObjectContactEnd.
    int _contactingHandsCount = 0;
    int _contactingHandsCountLastFrame = 0;

    private void FixedUpdateContact() {
      if (_contactingHandsCount > 0) {
        if (_contactingHandsCountLastFrame == 0) {
          ObjectContactBegin(_closestContactingHand);
        }
        else {
          ObjectContactStay(_closestContactingHand);
        }
      }
      else if (_contactingHandsCountLastFrame > 0) {
        ObjectContactEnd(_closestJustStoppedContactingHand);
      }
      _contactingHandsCountLastFrame = _contactingHandsCount;
      _closestContactingHand = null;
      _closestContactingHandDistance = float.PositiveInfinity;
    }

    public override void ContactBegin(Hand hand) {
      EvaluateContactCloseness(hand);
      _contactingHandsCount += 1;

      OnContactBegin(hand);
    }

    public override void ContactStay(Hand hand) {
      EvaluateContactCloseness(hand);

      OnContactStay(hand);
    }

    private void EvaluateContactCloseness(Hand hand) {
      float handDistance = GetInteractionDistanceToPoint(hand.PalmPosition.ToVector3());
      if (_contactingHandsCount == 0 || _closestContactingHand == null) {
        _closestContactingHand = hand;
        _closestContactingHandDistance = handDistance;
      }
      else {
        if (handDistance < _closestContactingHandDistance) {
          _closestContactingHand = hand;
          _closestContactingHandDistance = handDistance;
        }
      }
    }

    public override void ContactEnd(Hand hand) {
      _contactingHandsCount -= 1;
      if (_contactingHandsCount == 0) {
        _closestJustStoppedContactingHand = hand;
      }

      OnContactEnd(hand);
    }

    public void ObjectContactBegin(Hand hand) {
      OnObjectContactBegin(hand);
    }

    public void ObjectContactStay(Hand hand) {
      OnObjectContactStay(hand);
    }

    public void ObjectContactEnd(Hand hand) {
      OnObjectContactEnd(hand);
    }

    #endregion

    #region Grasping

    private int _graspCount = 0;
    private bool _moveObjectWhenGraspedEnabledLastFrame;
    private bool _wasKinematicBeforeGrab;

    private IGraspedPositionController _graspedPositionController;
    private IGraspedPositionController GraspedPositionController {
      get {
        if (_graspedPositionController == null) {
          _graspedPositionController = new KabschGraspedPosition(this);
        }
        return _graspedPositionController;
      }
      set {
        _graspedPositionController = value;
      }
    }

    private KinematicGraspedMovement    _kinematicHoldingMovement;
    private NonKinematicGraspedMovement _nonKinematicHoldingMovement;

    private void InitGrasping() {
      _moveObjectWhenGraspedEnabledLastFrame = moveObjectWhenGrasped;

      _kinematicHoldingMovement = new KinematicGraspedMovement();
      _nonKinematicHoldingMovement = new NonKinematicGraspedMovement();
    }

    private void FixedUpdateGrasping() {
      if (!moveObjectWhenGrasped && _moveObjectWhenGraspedEnabledLastFrame) {
        GraspedPositionController.ClearHands();
      }

      _moveObjectWhenGraspedEnabledLastFrame = moveObjectWhenGrasped;
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
        Debug.LogWarning("Two-handed grasping is not yet supported!"
          + "This warning ever appearing is indicative of a bug because the lack of support means "
          + "that _graspCount should NEVER become larger than 1.");
      }
      
      // Set kinematic state based on grasping hold movement type
      _wasKinematicBeforeGrab = Rigidbody.isKinematic;
      switch (graspedMovementType) {
        case GraspedMovementType.Inherit: break; // no change
        case GraspedMovementType.Kinematic:
          Rigidbody.isKinematic = true; break;
        case GraspedMovementType.Nonkinematic:
          Rigidbody.isKinematic = false; break;
      }

      // SnapToHand(hand); // TODO: When you grasp an object, snap the object into a good holding position.

      if (moveObjectWhenGrasped) {
        GraspedPositionController.AddHand(interactionManager.GetInteractionHand(hand.Handedness()));
      }

      OnGraspBegin(hand);
    }

    public override void GraspHold(Hand hand) {
      if (moveObjectWhenGrasped) {
        OnPreGraspedMovement(Rigidbody.position, Rigidbody.rotation, hand);

        Vector3 newPosition; Quaternion newRotation;
        GraspedPositionController.GetGraspedPosition(out newPosition, out newRotation);

        IGraspedMovementController holdingMovementController;
        if (Rigidbody.isKinematic) {
          holdingMovementController = _kinematicHoldingMovement;
        }
        else {
          holdingMovementController = _nonKinematicHoldingMovement;
        }
        holdingMovementController.MoveTo(newPosition, newRotation, this);

        OnGraspedMovement(newPosition, newRotation, hand);
      }

      OnGraspHold(hand);
    }

    public override void GraspEnd(Hand hand) {
      _graspCount--;

      // Revert kinematic state
      Rigidbody.isKinematic = _wasKinematicBeforeGrab;

      if (moveObjectWhenGrasped) {
        GraspedPositionController.RemoveHand(interactionManager.GetInteractionHand(hand.Handedness()));
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

    #region Internal

    protected Transform[] _childrenArray;

    protected void InitInternal() {
      _childrenArray = GetComponentsInChildren<Transform>(true);

      FixedUpdateLayer();
    }

    #region Locked Position Checking

    private bool _isPositionLocked = false;

    /// <summary>
    /// Returns whether the InteractionBehaviour has its position fully locked
    /// by its Rigidbody settings or by any attached PhysX Joints.
    /// 
    /// If the state of the underlying Rigidbody or Joints changes what this value
    /// should be, it will not automatically update (as an optimization) at runtime;
    /// instead, manually call RefreshPositionLockedState(). This is because the
    /// type-checks required are relatively expensive and mustn't occur every frame.
    /// </summary>
    public bool IsPositionLocked { get { return _isPositionLocked; } }

    /// <summary>
    /// Call this method if the InteractionBehaviour's Rigidbody becomes or unbecomes
    /// fully positionally locked (X, Y, Z) or if a Joint attached to the Rigidbody
    /// no longer locks its position (e.g. by being destroyed or disabled).
    /// </summary>
    public void RefreshPositionLockedState() {
      if ((_body.constraints & RigidbodyConstraints.FreezePositionX) > 0
       && (_body.constraints & RigidbodyConstraints.FreezePositionY) > 0
       && (_body.constraints & RigidbodyConstraints.FreezePositionZ) > 0) {
        _isPositionLocked = true;
        return;
      }
      else {
        Joint[] joints = _body.GetComponents<Joint>();
        foreach (var joint in joints) {
          FixedJoint fixedJoint = joint as FixedJoint;
          if (fixedJoint != null) {
            _isPositionLocked = true; return;
          }
          HingeJoint hinge = joint as HingeJoint;
          if (hinge != null) {
            _isPositionLocked = true; return;
          }
          // SpringJoint -- no check required, spring joints never fully lock position.
          CharacterJoint charJoint = joint as CharacterJoint;
          if (charJoint != null) {
            _isPositionLocked = true; return;
          }
          ConfigurableJoint configJoint = joint as ConfigurableJoint;
          if (configJoint != null
            && (configJoint.xMotion == ConfigurableJointMotion.Locked
              || (configJoint.xMotion == ConfigurableJointMotion.Limited
                && configJoint.linearLimit.limit == 0F))
            && (configJoint.yMotion == ConfigurableJointMotion.Locked
              || (configJoint.yMotion == ConfigurableJointMotion.Limited
                && configJoint.linearLimit.limit == 0F))
            && (configJoint.zMotion == ConfigurableJointMotion.Locked
              || (configJoint.zMotion == ConfigurableJointMotion.Limited
                && configJoint.linearLimit.limit == 0F))) {
            _isPositionLocked = true; return;
          }
        }
      }
      _isPositionLocked = false;
    }

    #endregion

    #region Interaction Layers 

    protected enum CollisionMode {
      Normal,
      Grasped
    }
    protected CollisionMode _collisionMode;

    protected void FixedUpdateCollisionMode() {
      CollisionMode desiredCollisionMode = CollisionMode.Normal;
      if (IsGrasped) {
        desiredCollisionMode = CollisionMode.Grasped;
      }
      //else if (_dislocatedBrushCounter < DISLOCATED_BRUSH_COOLDOWN || (_CollisionMode != CollisionMode.Normal && _minHandDistance <= 0.0f)) {
      //  desiredCollisionMode = CollisionMode.SOFT;
      //}

      if (_collisionMode != desiredCollisionMode) {
        _collisionMode = desiredCollisionMode;
        FixedUpdateLayer();
      }

      Assert.IsTrue((_collisionMode == CollisionMode.Grasped) == IsGrasped);
    }

    protected void FixedUpdateLayer() {
      int layer;
      if (_collisionMode != CollisionMode.Normal) {
        layer = interactionManager.GraspedObjectLayer;
      }
      else {
        layer = interactionManager.InteractionLayer;
      }

      if (gameObject.layer != layer) {
        for (int i = 0; i < _childrenArray.Length; i++) {
          _childrenArray[i].gameObject.layer = layer;
        }
      }
    }

    #endregion

    #endregion

  }

}
