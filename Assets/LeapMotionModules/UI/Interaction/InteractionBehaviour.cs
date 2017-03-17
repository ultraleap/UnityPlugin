using Leap.Unity.Attributes;
using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Leap.Unity.UI.Interaction {

  [RequireComponent(typeof(Rigidbody))]
  public class InteractionBehaviour : InteractionBehaviourBase {

    #region Public API

    #region Hovering API

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
    public Action<Hand> OnObjectHoverStay = (closestHand) => { };
    public Action<Hand> OnObjectHoverEnd = (closestHand) => { };

    public Action<Hand> OnPrimaryHoverBegin = (hand) => { };
    public Action<Hand> OnPrimaryHoverStay = (hand) => { };
    public Action<Hand> OnPrimaryHoverEnd = (hand) => { };

    public Action<Hand> OnObjectPrimaryHoverBegin = (closestHand) => { };
    public Action<Hand> OnObjectPrimaryHoverStay = (closestHand) => { };
    public Action<Hand> OnObjectPrimaryHoverEnd = (closestHand) => { };

    #endregion

    #region Grasping API

    public override bool isGrasped { get { return _graspCount > 0; } }

    /// <summary> Called directly after the grasped object's Rigidbody has had its position and rotation set
    /// by the GraspedPoseController (which moves the object realistically with the hand). Subscribe to this
    /// callback if you'd like to override the default behaviour for grasping. 
    /// 
    /// Use InteractionBehaviour.Rigidbody.position InteractionBehaviour.Rigidbody.rotation to modify the
    /// object's position and rotation from the solved position. Setting the object's Transform position and
    /// rotation is not recommended unless you know what you're doing. </summary>
    public Action<Vector3, Quaternion, Vector3, Quaternion, Hand> OnGraspedMovement
      = (preSolvedPos, preSolvedRot, solvedPos, solvedRot, hand) => { };

    /// <summary> Called when any hand grasps this interaction object, even if the object is already held by another hand. </summary>
    public Action<Hand> OnGraspBegin = (hand) => { };
    public Action<Hand> OnGraspHold  = (hand) => { };
    public Action<Hand> OnGraspEnd   = (hand) => { };

    // TODO: Implement!
    /// <summary> Called when the object is grasped by a hand. If multi-handed grasping is disabled, will fire End and Begin
    /// if the object is grasped by a different hand, released by the first. If multi-handed grasping is enabled, Begin will
    /// fire only once on the first grasp, and End will only fire once there are no more hands grasping the object. </summary>
    public Action OnObjectGraspBegin = () => { };
    public Action OnObjectGraspHold  = () => { };
    public Action OnObjectGraspEnd   = () => { };

    /// <summary> Called when the number of objects grasping this object exceeds 1. This can only occur if multi-grasp is
    /// enabled for this object. End will be called when the number of hands grasping the object becomes 1 or fewer. </summary>
    public Action<List<Hand>> OnMultiGraspBegin = (hands) => { };
    public Action<List<Hand>> OnMultiGraspHold  = (hands) => { };
    public Action<List<Hand>> OnMultiGraspEnd   = (hands) => { };

    /// <summary> Returns (approximately) where the argument hand is grasping this object.
    /// If the hand is not currently grasping this object, returns Vector3.zero. </summary>
    public Vector3 GetGraspPoint(Hand hand) {
      InteractionHand intHand = interactionManager.GetInteractionHand(hand);
      if (intHand.IsGrasping(this)) {
        return intHand.GetGraspPoint();
      }
      else {
        Debug.LogError("Cannot get this object's grasp point: It is not currently grasped by an InteractionHand.");
        return Vector3.zero;
      }
    }

    #endregion

    #region Contact API

    public Action<Hand> OnContactBegin = (hand) => { };
    public Action<Hand> OnContactStay  = (hand) => { };
    public Action<Hand> OnContactEnd   = (hand) => { };

    public Action<Hand> OnObjectContactBegin = (closestHand) => { };
    public Action<Hand> OnObjectContactStay  = (closestHand) => { };
    public Action<Hand> OnObjectContactEnd   = (closestHand) => { };

    #endregion

    #endregion

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
    /// TODO: This is not actually implemented.
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

    protected override void Start() {
      base.Start();

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
    /// Grasping uses its update to provide per-object (as opposed to per-hand)
    /// grasp callbacks, e.g. OnObjectGraspBegin(), similarly. It will also
    /// fire multi-grasp callbacks, e.g. OnMultiGraspBegin(), but only if
    /// multiHandGrasping is enabled.
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
        _publicClosestHoveringHand = null;
      }

      _hoveringHandsCountLastFrame = _hoveringHandsCount;
      _closestHoveringHand = null;
      _closestHoveringHandDistance = float.PositiveInfinity;
    }

    public override float GetDistance(Vector3 worldPosition) {
      // TODO: Should probably get distance from the InteractionBehaviour's colliders. Probably has to wait until 5.6 (Physics.ClosestPoint)
      return GetInteractionDistanceToPoint(worldPosition);
    }

    public override void HoverBegin(Hand hand) {
      _hoveringHandsCount++;
      EvaluateHoverCloseness(hand);

      OnHoverBegin(hand);
    }

    public override void HoverStay(Hand hand) {
      EvaluateHoverCloseness(hand);

      OnHoverStay(hand);
    }

    private void EvaluateHoverCloseness(Hand hand) {
      float handDistance = GetInteractionDistanceToPoint(hand.PalmPosition.ToVector3());

      if (_closestHoveringHand == null) {
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
    private bool _moveObjectWhenGrasped__WasEnabledLastFrame;
    private bool _wasKinematicBeforeGrab;

    private IGraspedPoseController _graspedPositionController;
    private IGraspedPoseController GraspedPoseController {
      get {
        if (_graspedPositionController == null) {
          _graspedPositionController = new KabschGraspedPose(this);
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
      _moveObjectWhenGrasped__WasEnabledLastFrame = moveObjectWhenGrasped;

      _kinematicHoldingMovement = new KinematicGraspedMovement();
      _nonKinematicHoldingMovement = new NonKinematicGraspedMovement();
    }

    private int _graspCountLastFrame = 0;
    private List<InteractionHand> _graspingIntHandsCache = new List<InteractionHand>(4);
    private List<Hand> _graspingHandsCache = new List<Hand>(4);
    private List<Hand> _lastFrameGraspingHandsCache = new List<Hand>(4); // for OnMultiGraspEnd

    private void FixedUpdateGrasping() {
      if (!moveObjectWhenGrasped && _moveObjectWhenGrasped__WasEnabledLastFrame) {
        GraspedPoseController.ClearHands();
      }
      _moveObjectWhenGrasped__WasEnabledLastFrame = moveObjectWhenGrasped;

      _graspingHandsCache.Clear();
      foreach (var hand in _graspingIntHandsCache.Query().Select(intHand => intHand.GetLeapHand())) {
        _graspingHandsCache.Add(hand);
      }

      if (_graspCount > 0) {
        if (_graspCountLastFrame == 0) {
          OnObjectGraspBegin();
        }
        else {
          OnObjectGraspHold();
        }

        if (_graspCount > 1) {
          if (_graspCountLastFrame <= 1) {
            OnMultiGraspBegin(_graspingHandsCache);
          }
          else {
            OnMultiGraspHold(_graspingHandsCache);
          }
        }
      }
      if (_graspCount == 0) {
        if (_graspCountLastFrame > 0) {
          OnObjectGraspEnd();
        }
      }
      if (_graspCount <= 1) {
        if (_graspCountLastFrame > 1) {
          OnMultiGraspEnd(_lastFrameGraspingHandsCache);
        }
      }

      _graspCountLastFrame = _graspCount;
      _lastFrameGraspingHandsCache.Clear();
      foreach (var hand in _graspingHandsCache) {
        _lastFrameGraspingHandsCache.Add(hand);
      }
    }

    public override void GraspBegin(Hand hand) {
      if (isGrasped && !allowMultiGrasp) {
        interactionManager.ReleaseObjectFromGrasp(this);
      }

      InteractionHand graspingIntHand = interactionManager.GetInteractionHand(hand);
      _graspingIntHandsCache.Add(graspingIntHand);
      _graspCount++;

      // SnapToHand(hand); // TODO: When you grasp an object, snap the object into a good holding position.

      if (moveObjectWhenGrasped) {
        GraspedPoseController.AddHand(graspingIntHand);
      }

      // Set kinematic state based on grasping hold movement type
      if (_graspCount == 1) {
        _wasKinematicBeforeGrab = Rigidbody.isKinematic;
        switch (graspedMovementType) {
          case GraspedMovementType.Inherit: break; // no change
          case GraspedMovementType.Kinematic:
            Rigidbody.isKinematic = true; break;
          case GraspedMovementType.Nonkinematic:
            Rigidbody.isKinematic = false; break;
        }
      }

      OnGraspBegin(hand);
    }

    public override void GraspHold(Hand hand) {
      if (moveObjectWhenGrasped) {
        Vector3 origPosition = Rigidbody.position; Quaternion origRotation = Rigidbody.rotation;
        Vector3 newPosition; Quaternion newRotation;
        GraspedPoseController.GetGraspedPosition(out newPosition, out newRotation);

        IGraspedMovementController holdingMovementController = Rigidbody.isKinematic ?
                                                                 (IGraspedMovementController)_kinematicHoldingMovement
                                                               : (IGraspedMovementController)_nonKinematicHoldingMovement;
        holdingMovementController.MoveTo(newPosition, newRotation, this);

        OnGraspedMovement(origPosition, origRotation, newPosition, newRotation, hand);
      }

      OnGraspHold(hand);
    }

    public override void GraspEnd(Hand hand) {
      _graspCount--;

      InteractionHand graspingIntHand = interactionManager.GetInteractionHand(hand);
      _graspingIntHandsCache.Remove(graspingIntHand);

      if (moveObjectWhenGrasped) {
        GraspedPoseController.RemoveHand(graspingIntHand);
      }

      // Revert kinematic state if the grasp has ended
      if (_graspCount == 0) {
        Rigidbody.isKinematic = _wasKinematicBeforeGrab;
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
    /// This is useful for the GraspedMovementController to determine whether
    /// it should attempt to move the interaction object or merely rotate it.
    /// 
    /// If the state of the underlying Rigidbody or Joints changes what this value
    /// should be, it will not automatically update (as an optimization) at runtime;
    /// instead, manually call RefreshPositionLockedState(). This is because the
    /// type-checks required are relatively expensive and mustn't occur every frame.
    /// </summary>
    public bool isPositionLocked { get { return _isPositionLocked; } }

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
      if (isGrasped) {
        desiredCollisionMode = CollisionMode.Grasped;
      }
      //else if (_dislocatedBrushCounter < DISLOCATED_BRUSH_COOLDOWN || (_CollisionMode != CollisionMode.Normal && _minHandDistance <= 0.0f)) {
      //  desiredCollisionMode = CollisionMode.SOFT;
      //}

      if (_collisionMode != desiredCollisionMode) {
        _collisionMode = desiredCollisionMode;
        FixedUpdateLayer();
      }

      Assert.IsTrue((_collisionMode == CollisionMode.Grasped) == isGrasped);
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