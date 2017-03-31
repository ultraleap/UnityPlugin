using Leap.Unity.Attributes;
using Leap.Unity.Query;
using Leap.Unity.UI.Interaction.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Leap.Unity.UI.Interaction {

  /// <summary>
  /// InteractionBehaviours are components that enable GameObjects to interact with Leap
  /// hands in a physical intuitive way. By default, they represent objects that can be
  /// poked, prodded, smacked, grasped, and thrown around by Leap hands. They also provide
  /// a thorough public API with callbacks for hovering, contact, and grasping callbacks
  /// for creating feedback mechanisms or overriding the default physical behavior of the object.
  /// In documentation and some method calls, GameObjects with an InteractionBehaviour component
  /// are called interaction objects.
  /// </summary>
  [RequireComponent(typeof(Rigidbody))]
  public class InteractionBehaviour : MonoBehaviour, IInteractionBehaviour {

    public const float MAX_ANGULAR_VELOCITY = 100F;

    #region Public API

    #region Hovering API

    /// <summary> Gets whether any hand is nearby. </summary>
    public bool isHovered             { get { return _hoveringHandsCount > 0; } }

    /// <summary> Gets the closest hand to this object, or null if no hand is nearby. </summary>
    public Hand closestHoveringHand   { get { return _publicClosestHoveringHand; } }

    /// <summary>
    /// Gets whether this object is the primary hover for any Interaction Hand.
    /// </summary>
    public bool isPrimaryHovered      { get { return _primaryHoveringHandsCount > 0; } }

    /// <summary>
    /// Gets the primary hovering hand for this interaction object, if it has one.
    /// A hand is the primary hover for an interaction object only if it is closer to that object
    /// than any other interaction object. If there are multiple such hands, this returns the hand
    /// closest to this object.
    /// </summary>
    public Hand primaryHoveringHand   { get { return _publicClosestPrimaryHoveringHand; } }

    /// <summary>
    /// Called whenever one or more hands have entered the hover activity radius around this
    /// Interaction Behaviour. The hover activity radius is a setting specified in the
    /// Interaction Manager.
    /// </summary>
    /// <remarks>
    /// The provided list will contain only the Interaction Hands that have entered the radius;
    /// refer to OnHoverStay for a list of all Hands that are currently hovering near this object.
    /// 
    /// This method will be called every time a hand begins hovering near an object. To receive a
    /// callback only when this object begins being hovered at all, subscribe to OnObjectHoverBegin.
    /// 
    /// If this method is to be called on a given frame, it will be called after OnHoverEnd
    /// and before OnHoverStay.
    /// </remarks>
    public Action<List<InteractionHand>> OnHoverBegin = (hands) => { };

    /// <summary>
    /// Called during every fixed (physics) frame wherein one or more hands is within the hover
    /// activity radius around the Interaction Behaviour.
    /// </summary>
    /// <remarks>
    /// The provided list will contain all Interaction Hands that are within the hover radius for
    /// this Interaction object.
    /// 
    /// If this method is to be called on a given frame, it will be called after both
    /// OnHoverEnd and OnHoverBegin.
    /// </remarks>
    public Action<List<InteractionHand>> OnHoverStay = (hands) => { };

    /// <summary>
    /// Called when one or more hands have left the hover activity radius around this
    /// Interaction Behaviour.
    /// </summary>
    /// <remarks>
    /// The provided list will only contain the Interaction Hands that have left the radius;
    /// refer to OnHoverStay for a list of all Hands that are currently hovering near this object.
    /// 
    /// This method will be called every time one or more hands ceases hovering near an object on
    /// a given frame. To receive a callback only when the object ceases being hovered at all,
    /// subscribe to OnObjectHoverEnd.
    /// 
    /// If this method is to be called on a given frame, it will be called before OnHoverBegin and
    /// before OnHoverStay.
    /// </remarks>
    public Action<List<InteractionHand>> OnHoverEnd = (hands) => { };

    /// <summary>
    /// Called when the object transitions from having no hands nearby to having one or more
    /// hands nearby.
    /// </summary>
    /// <remarks>
    /// The provided list will only contain the Interaction Hands that have just begun hovering
    /// near the object. For a list of all hands hovering near an object any given frame, refer
    /// to OnHoverStay.
    /// 
    /// Object callbacks are called on a per-object basis. OnHoverBegin will be called every time
    /// a hand begins hovering near an object, but OnObjectHoverBegin will only be called when
    /// the object becomes hovered at all.
    /// 
    /// If this method is to be called on a given frame, it will be called before OnHoverStay,
    /// OnHoverEnd, and OnObjectHoverEnd, and it will be called after OnHoverBegin.
    /// </remarks>
    public Action<List<InteractionHand>> OnObjectHoverBegin = (hands) => { };

    /// <summary>
    /// Called when the object transitions from having one or more hands nearby to having no
    /// hands nearby.
    /// </summary>
    /// <remarks>
    /// The provided list will only contain the Interaction Hands that have just stopped hovering
    /// near the object. For a list of all hands hovering near an object any given frame, refer
    /// to OnHoverStay.
    /// 
    /// Object callbacks are called on a per-object basis. OnHoverEnd will be called every time
    /// a hand stops hovering near an object, but OnObjectHoverBegin will only be called when
    /// the object is no longer being hovered by any hands.
    /// 
    /// If this method is to be called on a given frame, it will be called before OnHoverBegin,
    /// OnObjectHoverBegin, and OnHoverStay, and it will be called after OnHoverEnd.
    /// </remarks>
    public Action<List<InteractionHand>> OnObjectHoverEnd = (hands) => { };

    /// <summary>
    /// Called when the object has become the primary hovered object for one or more
    /// hands. Only one interaction object can be the primary hover for a given hand at a time.
    /// </summary>
    /// <remarks>
    /// The provided list will only contain the Interaction Hands that have just begun primarily
    /// hovering over this object. For a list of all hands primarily hovering over this object,
    /// refer to OnPrimaryHoverStay.
    /// 
    /// This method will be called when any hand begins primarily hovering over this object. To
    /// receive a callback when the object becomes primarily hovered at all, subscribe to
    /// OnObjectPrimaryHoverBegin.
    /// 
    /// If this method is to be called on a given frame, it will be called before OnPrimaryHoverStay,
    /// and it will be called after OnPrimaryHoverEnd.
    /// </remarks>
    public Action<List<InteractionHand>> OnPrimaryHoverBegin = (hands) => { };

    /// <summary>
    /// Called during every fixed (physics) frame in which one or more hands is primarily hovering
    /// over this object. Only one object may be primarily hovered by a given hand at any one time.
    /// </summary>
    /// <remarks>
    /// The provided list will contain all hands for which this object is their primary hovered object.
    /// Primary hovered objects are objects for which a hand's fingertip or palm center is closest to
    /// that object.
    /// 
    /// If this method is to be called on a given frame, it will be called after OnPrimaryHoverStay and
    /// OnPrimaryHoverEnd.
    /// </remarks>
    public Action<List<InteractionHand>> OnPrimaryHoverStay = (hands) => { };

    /// <summary>
    /// Called when the object has ceased being the primary hovered object for one or
    /// more hands. Only one interaction object can be the primary hover for a given hand at one time.
    /// </summary>
    /// <remarks>
    /// The provided list will only contain the Interaction Hands that have just stopped primarily
    /// hovering over this object. For a list of all hands primarily hovering over this object,
    /// refer to OnPrimaryHoverStay.
    /// 
    /// This method is called for every frame that a hand ceases primarily hovering over this object. To
    /// receive a callback when the object ceases being primarily hovered at all, subscribe to
    /// OnObjectPrimaryHoverEnd.
    /// 
    /// If this method is to be called on a given frame, it will be called before OnPrimaryHoverBegin
    /// and OnPrimaryHoverStay.
    /// </remarks>
    public Action<List<InteractionHand>> OnPrimaryHoverEnd = (hands) => { };

    /// <summary>
    /// Called when the object begins being the primary hover of one or more hands, if the object
    /// was not primarily hovered by any hands on the previous frame.
    /// </summary>
    /// <remarks>
    /// Object callbacks are called on a per-object basis. OnPrimaryHoverBegin will be called every time
    /// a hand begins primarily hovering near an object, but OnObjectPrimarytHoverBegin will only be called
    /// when the object begins being the primarily hovered by any hands.
    /// 
    /// If this method is called on a given frame, it will be called before OnPrimaryHoverStay, and it
    /// will be called after OnPrimaryHoverEnd, OnObjectPrimaryHoverEnd, and OnPrimaryHoverBegin.
    /// </remarks>
    public Action<List<InteractionHand>> OnObjectPrimaryHoverBegin = (hands) => { };

    /// <summary>
    /// Called when the object ceases being the primary hover of any hands.
    /// </summary>
    /// <remarks>
    /// Object callbacks are called on a per-object basis. OnPrimaryHoverEnd will be called every time
    /// a hand stops primarily hovering over an object, but OnObjectPrimaryHoverEnd will only be called
    /// when the object is no longer being primarily hovered by any hands.
    /// 
    /// If this method is called on a given frame, it will be called before OnPrimaryHoverStay,
    /// OnPrimaryHoverBegin, OnObjectPrimaryHoverBegin, and it will be called after OnPrimaryHoverEnd.
    /// </remarks>
    public Action<List<InteractionHand>> OnObjectPrimaryHoverEnd = (hands) => { };

    #endregion

    #region Grasping API

    /// <summary> Gets whether this object is grasped by any hand. </summary>
    public override bool isGrasped { get { return _graspCount > 0; } }

    /// <summary> Gets a list of all hands currently grasping this object. </summary>
    public List<InteractionHand> graspingHands { get { return _graspingHandsSet; } }

    /// <summary>
    /// Releases this object from the hand currently grasping it, if it is grasped, and returns true.
    /// If the object was not grasped, this method returns false.  Directly after calling this method,
    /// the object is guaranteed not to be held.
    /// </summary>
    public bool ReleaseFromGrasp() {
      return manager.TryReleaseObjectFromGrasp(this);
    }

    /// <summary>
    /// Called when the hand that is grasping this interaction object loses tracking. This can occur if
    /// the hand leaves the device's field of view, or is fully occluded by another (real-world) object.
    /// 
    /// An object is "suspended" if it is currently grasped by an untracked hand.
    /// 
    /// By default, suspended objects will hang in the air until the hand grasping them resumes tracking.
    /// Subscribe to this callback and OnResume to implement, e.g., the object disappearing and
    /// re-appearing.
    /// 
    /// Grasping a suspended object with a different hand will cease suspension of the object, and will
    /// invoke OnResume, although the input to OnResume will be the newly grasping hand, not the hand that
    /// suspended the object. OnGraspEnd will also be called for the hand that was formerly causing suspension.
    /// </summary>
    public Action<InteractionHand> OnObjectSuspended = (hand) => { };

    /// <summary>
    /// Called when an object ceases being suspended. An object is suspended if it is currently grasped by
    /// an untracked hand. This occurs when the hand grasping an object ceases being tracked.
    /// </summary>
    public Action<InteractionHand> OnObjectResumed = (hand) => { };

    /// <summary>
    /// Called directly after this grasped object's Rigidbody has had its position and rotation set
    /// by the GraspedPoseController (which moves the object realistically with the hand). Subscribe to this
    /// callback if you'd like to override the default behaviour for grasping.
    /// 
    /// Use InteractionBehaviour.Rigidbody.position and InteractionBehaviour.Rigidbody.rotation to modify the
    /// object's position and rotation from the solved position. Setting the object's Transform position and
    /// rotation is not recommended unless you know what you're doing.
    /// </summary>
    /// <remarks>
    /// This method is called after any OnGraspBegin or OnGraspEnd callbacks, but before OnGraspHold.
    /// </remarks>
    public Action<Vector3, Quaternion, Vector3, Quaternion, List<InteractionHand>> OnGraspedMovement
      = (preSolvedPos, preSolvedRot, solvedPos, solvedRot, hands) => { };

    /// <summary>
    /// Called when one or more hands grasp this object during a given frame.
    /// 
    /// Unless allowMultigrasp is set to true, only one hand will ever be grasping an object at any given
    /// time, so the hands list will always be of length one.
    /// </summary>
    /// <remarks>
    /// The argument list will only contain hands that began grasping the object this frame. For a list
    /// of all hands currently grasping the object, subscribe to OnGraspHold or refer to graspingHands.
    /// 
    /// If this method is called on a given frame, it will be called after OnGraspEnd and before OnGraspHold.
    /// </remarks>
    public Action<List<InteractionHand>> OnGraspBegin = (hands) => { };

    /// <summary>
    /// Called every frame during which this object is grasped by one or more hands.
    /// 
    /// Unless allowMultigrasp is set to true, only one hand will ever be grasping an object at any given
    /// time, so the hands list will always be of length one.
    /// </summary>
    /// <remarks>
    /// If this method is called on a given frame, it will be called after all other grasping callbacks.
    /// </remarks>
    public Action<List<InteractionHand>> OnGraspHold = (hands) => { };

    /// <summary>
    /// Called when one of more hands release this object during a given frame.
    /// 
    /// Unless allowMultigrasp is set to true, only one hand will ever be grasping an object at any given
    /// time, so the hands list will always be of length one.
    /// </summary>
    /// <remarks>
    /// The argument list will only contain hands that stopped grasping the object this frame. For a list
    /// of all hands currently grasping the object, subscribe to OnGraspHold or refer to graspingHands.
    /// 
    /// If this method is called on a given frame, it will be before all other grasping callbacks.
    /// </remarks>
    public Action<List<InteractionHand>> OnGraspEnd = (hands) => { };

    /// <summary>
    /// Called when the object is grasped by one or more hands, if the object was not grasped by any
    /// hands on the previous frame.
    /// </summary>
    /// <remarks>
    /// If this method is called on a given frame, it will be called directly after OnGraspBegin.
    /// </remarks>
    public Action<List<InteractionHand>> OnObjectGraspBegin = (hands) => { };

    /// <summary>
    /// Called when the object is no longer grasped by any hands.
    /// </summary>
    /// <remarks>
    /// If this method is called on a given frame, it will be called directly after OnGraspEnd.
    /// </remarks>
    public Action<List<InteractionHand>> OnObjectGraspEnd   = (hands) => { };

    /// <summary>
    /// Returns (approximately) where the argument hand is grasping this object.
    /// If the hand is not currently grasping this object, returns Vector3.zero.
    /// 
    /// This method will log an error if the argument hand is not grasping this object.
    /// </summary>
    public Vector3 GetGraspPoint(InteractionHand intHand) {
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

    /// <summary>
    /// Called when one or more hands begins touching this object.
    /// </summary>
    /// <remarks>
    /// The provided hands list will only contain the hands that began
    /// touching this object. For a list of all hands currently touching
    /// this object, refer to OnContactStay.
    /// </remarks>
    public Action<List<Hand>> OnContactBegin = (hands) => { };

    /// <summary>
    /// Called every frame during which one or more hands is touching this object.
    /// </summary>
    public Action<List<Hand>> OnContactStay = (hands) => { };

    /// <summary>
    /// Called when one or more hands stops touching this object.
    /// </summary>
    /// <remarks>
    /// The provided hands list will only contain the hands that stopped
    /// touching this object. For a list of all hands currently touching
    /// this object, refer to OnContactStay.
    /// </remarks>
    public Action<List<Hand>> OnContactEnd = (hands) => { };

    /// <summary>
    /// Called when this object starts being touched by one or more hands, but was
    /// not touched by any hands during the previous frame.
    /// </summary>
    public Action<List<Hand>> OnObjectContactBegin = (hands) => { };

    /// <summary>
    /// Called when this object stops being touched by any hands.
    /// </summary>
    public Action<List<Hand>> OnObjectContactEnd = (hands) => { };

    #endregion

    #region Forces API

    /// <summary>
    /// Adds a linear acceleration to the center of mass of this object. 
    /// Use this instead of Rigidbody.AddForce() to accelerate an Interaction object.
    /// </summary>
    /// <remarks>
    /// Rigidbody.AddForce() will work in most scenarios, but will produce unexpected
    /// behavior when hands are embedded inside an object. Calling this method instead
    /// solves that problem.
    /// </remarks>
    public void AddLinearAcceleration(Vector3 acceleration) {
      _accumulatedLinearAcceleration += acceleration;
    }

    /// <summary>
    /// Adds an angular acceleration to the center of mass of this object. 
    /// Use this instead of Rigidbody.AddTorque() to add angular acceleration 
    /// to an Interaction object.
    /// </summary>
    /// <remarks>
    /// Rigidbody.AddTorque() will work in most scenarios, but will produce unexpected
    /// behavior when hands are embedded inside an object. Calling this method instead
    /// solves that problem.
    /// </remarks>
    public void AddAngularAcceleration(Vector3 acceleration) {
      _accumulatedAngularAcceleration += acceleration;
    }

    #endregion

    #endregion

    [Tooltip("The Interaction Manager responsible for this interaction object.")]
    [SerializeField]
    private InteractionManager _manager;
    public InteractionManager manager {
      get { return _manager; }
      protected set {
        if (_manager.IsBehaviourRegistered(this)) {
          _manager.UnregisterInteractionBehaviour(this);
        }
        _manager = value;
        if (!manager.IsBehaviourRegistered(this)) {
          _manager.RegisterInteractionBehaviour(this);
        }
      }
    }

    private Rigidbody _rigidbody;
    /// <summary> The Rigidbody associated with this interation object. </summary>
    public new Rigidbody rigidbody { get { return _rigidbody; } protected set { _rigidbody = value; } }

    [Header("Interaction Overrides")]

    [Tooltip("This object will no longer receive hover callbacks if this property is checked.")]
    [SerializeField]
    private bool _ignoreHover = false;
    public bool ignoreHover { get { return _ignoreHover; } set { _ignoreHover = value; } }

    [Tooltip("Hands will not be able to touch this object if this property is checked.")]
    [SerializeField]
    private bool _ignoreContact = false;
    public bool ignoreContact { get { return _ignoreContact; } set { _ignoreContact = value; } }

    [Tooltip("Hands will not be able to grasp this object if this property is checked.")]
    [SerializeField]
    private bool _ignoreGrasping = false;
    public bool ignoreGrasping { get { return _ignoreGrasping; } set { _ignoreGrasping = value; } }

    [Header("Grasp Settings")]

    [Tooltip("Can this object be grasped simultaneously with two or more hands?")]
    [SerializeField]
    [DisableIf("_ignoreGrasping", isEqualTo: true)]
    private bool _allowMultiGrasp = false;
    public bool allowMultiGrasp { get { return _allowMultiGrasp; } set { _allowMultiGrasp = value; } }

    [Tooltip("Should hands move the object as if it is held when the object is grasped? "
           + "Without this property checked, objects will still receive grasp callbacks, "
           + "but you must move them manually via script.")]
    [SerializeField]
    [DisableIf("_ignoreGrasping", isEqualTo: true)]
    private bool _moveObjectWhenGrasped = true;
    public bool moveObjectWhenGrasped { get { return _moveObjectWhenGrasped; } set { _moveObjectWhenGrasped = value; } }

    /// <summary>
    /// When the object is held by an Interaction Hand, how should it move to its
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
    [Tooltip("When the object is held by an Interaction Hand, how should it move to its "
           + "new position? Nonkinematic bodies will collide with other Rigidbodies, so they "
           + "might not reach the target position. Kinematic rigidbodies will always move to the "
           + "target position, ignoring collisions. Inherit will simply use the isKinematic "
           + "state of the Rigidbody from before it was grasped.")]
    [DisableIf("_moveObjectWhenGrasped", isEqualTo: false)]
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

    #region Unity Callbacks

    protected virtual void OnValidate() {
      rigidbody = GetComponent<Rigidbody>();
    }

    protected virtual void Awake() {
      rigidbody = GetComponent<Rigidbody>();
      rigidbody.maxAngularVelocity = MAX_ANGULAR_VELOCITY;
      rigidbodyWarper = new RigidbodyWarper(manager, this.transform, rigidbody, 0.25F);
    }

    protected virtual void OnEnable() {
      if (manager == null) {
        manager = InteractionManager.singleton;

        if (manager == null) {
          Debug.LogError("Interaction Behaviours require an Interaction Manager. Please ensure you have an InteractionManager in your scene.");
          this.enabled = false;
        }
      }

      if (manager != null && !manager.IsBehaviourRegistered(this)) {
        manager.RegisterInteractionBehaviour(this);
      }
    }

    protected virtual void Start() {
      RefreshPositionLockedState();

      InitInternal();
    }

    protected virtual void OnDestroy() {
      manager.UnregisterInteractionBehaviour(this);
    }

    #endregion

    /// <summary>
    /// InteractionManager manually calls method this after all InteractionHands
    /// are updated (via InteractionManager.FixedUpdate).
    /// </summary>
    public override void FixedUpdateObject() {
      FixedUpdateCollisionMode();
      FixedUpdateForces();
    }

    #region Hovering

    private HashSet<InteractionHand> _hoveringHandsSet = new HashSet<InteractionHand>();
    private HashSet<InteractionHand> _primaryHoveringHandsSet = new HashSet<InteractionHand>();

    public float GetDistance(Vector3 worldPosition) {
      return Vector3.Distance(worldPosition, this.transform.position);
    }

    public void BeginHover(InteractionHand hand) {
      _hoveringHandsSet.Add(hand);
    }

    public void EndHover(InteractionHand hand) {
      _hoveringHandsSet.Remove(hand);
    }

    public void BeginPrimaryHover(InteractionHand hand) {
      _primaryHoveringHandsSet.Add(hand);
    }

    public void EndPrimaryHover(InteractionHand hand) {
      _primaryHoveringHandsSet.Remove(hand);
    }

    #endregion

    #region Contact

    private HashSet<InteractionHand> _contactingHandsSet = new HashSet<InteractionHand>();

    public void BeginContact(InteractionHand hand) {
      _contactingHandsSet.Add(hand);
    }

    public void EndContact(InteractionHand hand) {
      _contactingHandsSet.Remove(hand);
    }

    #endregion

    #region Grasping

    private HashSet<InteractionHand> _graspingHandsSet = new HashSet<InteractionHand>();
    private bool _isSuspended;

    public void BeginGrasp(InteractionHand hand) {
      _graspingHandsSet.Add(hand);
    }

    public void EndGrasp(InteractionHand hand) {
      _graspingHandsSet.Remove(hand);
    }

    public bool isSuspended {
      get { return _isSuspended; }
    }

    public void SuspendGraspedObject(InteractionHand hand) {
      _isSuspended = true;
    }

    public void ResumeGraspedObject(InteractionHand hand) {
      _isSuspended = false;
    }

    #endregion

    // TODO: DELETEME
    //#region Callbacks

    //// BeginX / EndX implementations called by InteractionHand modify these sets directly.

    //// These lists are used to provide the data to callbacks, and are used when checking the state of the InteractionBehaviour
    //private List<InteractionHand> _handsBuffer = new List<InteractionHand>(); // for End and Begin callbacks
    //private List<InteractionHand> _hoverStayHands = new List<InteractionHand>();
    //private List<InteractionHand> _primaryHoverStayHands = new List<InteractionHand>();
    //private List<InteractionHand> _contactStayHands = new List<InteractionHand>();
    //private List<InteractionHand> _graspHoldHands = new List<InteractionHand>();

    //// These sets are used to detect changes frame-to-frame
    //private HashSet<InteractionHand> _hoveredLastFrameSet = new HashSet<InteractionHand>();
    //private HashSet<InteractionHand> _primaryHoveredLastFrameSet = new HashSet<InteractionHand>();
    //private HashSet<InteractionHand> _contactingLastFrameSet = new HashSet<InteractionHand>();
    //private HashSet<InteractionHand> _graspingLastFrameSet = new HashSet<InteractionHand>();

    //// TODO: Groooooss. Can I simplify this..??

    //private void FixedUpdateCallbacks() {

    //  //_hoverStayHands.Clear();
    //  //_primaryHoverStayHands.Clear();
    //  //_contactStayHands.Clear();
    //  //_graspHoldHands.Clear();

    //  //// e.g. preparing grasp stay callback, can call grasp end right away
    //  //_handsBuffer.Clear();
    //  //foreach (var hand in _graspingHandsSet) {
    //  //  if (_graspingLastFrameSet.Contains(hand) {
    //  //    _graspHoldHands.Add(hand);
    //  //  }
    //  //  else {
    //  //    _handsBuffer.Add(hand);
    //  //  }
    //  //}
    //  //if (_handsBuffer.Count > 0) { OnGraspEnd(_handsBuffer); }

    //  //// e.g. preparing hover stay callback, can call hover end right away
    //  //_handsBuffer.Clear();
    //  //foreach (var hand in _hoveringHandsSet) {
    //  //  if (_hoveredLastFrameSet.Contains(hand)) {
    //  //    _hoverStayHands.Add(hand);
    //  //  }
    //  //  else {
    //  //    _handsBuffer.Add(hand);
    //  //  }
    //  //}
    //  //if (_handsBuffer.Count > 0) { OnHoverEnd(_handsBuffer); }

    //  //// But note how I have to wait and traverse _graspingHandsSet AGAIN
    //  //// in order to get the necessary hands for OnGraspBegin, because I'm
    //  //// not allowed to call OnGraspBegin until after e.g. HoverEnd and ContactEnd
    //  //// have been taken care of.
    //  //// I _COULD_ solve this problem easily by having completely separate lists
    //  //// for graspEnd, graspBegin, contactEnd, contactBegin, etc. etc. etc.
    //  //// but good lord we already have so many of these per-object

    //  //// Update last-frame state
    //  //_hoveredLastFrameSet.Clear();
    //  //_primaryHoveredLastFrameSet.Clear();
    //  //_contactingLastFrameSet.Clear();
    //  //_graspingLastFrameSet.Clear();
    //  //foreach (var hand in _hoveredLastFrameSet) {
    //  //  _hoveringHandsSet.Add(hand);
    //  //}
    //  //foreach (var hand in _primaryHoveredLastFrameSet) {
    //  //  _primaryHoveredLastFrameSet.Add(hand);
    //  //}
    //  //foreach (var hand in _graspingHandsSet) {
    //  //  _graspingLastFrameSet.Add(hand);
    //  //}
    //  //foreach (var hand in _contactingHandsSet) {
    //  //  _contactingLastFrameSet.Add(hand);
    //  //}
    //}

    //#endregion

    #region OLD interaction code

    #region Hovering

    //// Logistics for providing per-object (instead of per-hand) Hover callbacks.
    //private Hand  _closestHoveringHand = null;
    //private float _closestHoveringHandDistance = float.PositiveInfinity;
    //private Hand  _justStoppedHoveringHand = null; // Provided for OnObjectHoverEnd.
    //private int   _hoveringHandsCountLastFrame = 0;
    //private int   _hoveringHandsCount = 0;

    //private Hand _publicClosestHoveringHand = null;

    //// Runs after InteractionHands have done FixedUpdateHand.
    //private void FixedUpdateHovering() {
    //  if (_hoveringHandsCount > 0) {
    //    if (_hoveringHandsCountLastFrame == 0) {
    //      _publicClosestHoveringHand = _closestHoveringHand;
    //      OnObjectHoverBegin(_closestHoveringHand);
    //    }
    //    else {
    //      _publicClosestHoveringHand = _closestHoveringHand;
    //      OnObjectHoverStay(_closestHoveringHand);
    //    }
    //  }
    //  else if (_hoveringHandsCountLastFrame > 0) {
    //    _closestHoveringHand = null;
    //    OnObjectHoverEnd(_justStoppedHoveringHand);
    //    _publicClosestHoveringHand = null;
    //  }

    //  _hoveringHandsCountLastFrame = _hoveringHandsCount;
    //  _closestHoveringHand = null;
    //  _closestHoveringHandDistance = float.PositiveInfinity;
    //}

    //public override float GetDistance(Vector3 worldPosition) {
    //  // TODO: Should probably get distance from the InteractionBehaviour's colliders. Probably has to wait until 5.6 (Physics.ClosestPoint)
    //  return GetInteractionDistanceToPoint(worldPosition);
    //}

    //public override void HoverBegin(InteractionHand intHand) {
    //  _hoveringHandsCount++;
    //  EvaluateHoverCloseness(hand);

    //  OnHoverBegin(hand);
    //}

    //private void EvaluateHoverCloseness(Hand hand) {
    //  float handDistance = GetInteractionDistanceToPoint(hand.PalmPosition.ToVector3());

    //  if (_closestHoveringHand == null) {
    //    _closestHoveringHand = hand;
    //    _closestHoveringHandDistance = handDistance;
    //  }
    //  else {
    //    if (handDistance < _closestHoveringHandDistance) {
    //      _closestHoveringHand = hand;
    //      _closestHoveringHandDistance = handDistance;
    //    }
    //  }
    //}

    //public override void HoverEnd(Hand hand) {
    //  _hoveringHandsCount--;
    //  if (_hoveringHandsCount == 0) {
    //    _justStoppedHoveringHand = hand;
    //  }

    //  OnHoverEnd(hand);
    //}

    //private Hand _closestPrimaryHoveringHand = null;
    //private float _closestPrimaryHoveringHandDistance = float.PositiveInfinity;
    //private Hand _closestJustStoppedPrimaryHoveringHand = null; // Provided for OnObjectPrimaryHoverEnd.
    //private int _primaryHoveringHandsCountLastFrame = 0;
    //private int _primaryHoveringHandsCount = 0;

    //private Hand _publicClosestPrimaryHoveringHand = null;

    //private void FixedUpdatePrimaryHovering() {
    //  if (_primaryHoveringHandsCount > 0) {
    //    if (_primaryHoveringHandsCountLastFrame == 0) {
    //      _publicClosestPrimaryHoveringHand = _closestPrimaryHoveringHand;
    //      OnObjectPrimaryHoverBegin(_closestPrimaryHoveringHand);
    //    }
    //    else {
    //      _publicClosestPrimaryHoveringHand = _closestPrimaryHoveringHand;
    //      OnObjectPrimaryHoverStay(_closestPrimaryHoveringHand);
    //    }
    //  }
    //  else if (_primaryHoveringHandsCountLastFrame > 0) {
    //    _publicClosestPrimaryHoveringHand = null;
    //    OnObjectPrimaryHoverEnd(_closestJustStoppedPrimaryHoveringHand);
    //  }

    //  _primaryHoveringHandsCountLastFrame = _primaryHoveringHandsCount;
    //  _closestPrimaryHoveringHand = null;
    //  _closestPrimaryHoveringHandDistance = float.PositiveInfinity;
    //}

    //public override void PrimaryHoverBegin(Hand hand) {
    //  EvaluatePrimaryHoverCloseness(hand);
    //  _primaryHoveringHandsCount++;

    //  OnPrimaryHoverBegin(hand);
    //}

    //public override void PrimaryHoverStay(Hand hand) {
    //  EvaluatePrimaryHoverCloseness(hand);

    //  OnPrimaryHoverStay(hand);
    //}

    //private void EvaluatePrimaryHoverCloseness(Hand hand) {
    //  float handDistance = GetInteractionDistanceToPoint(hand.PalmPosition.ToVector3());
    //  if (_primaryHoveringHandsCount == 0 || _closestPrimaryHoveringHand == null) {
    //    _closestPrimaryHoveringHand = hand;
    //    _closestPrimaryHoveringHandDistance = handDistance;
    //  }
    //  else {
    //    if (handDistance < _closestPrimaryHoveringHandDistance) {
    //      _closestPrimaryHoveringHand = hand;
    //      _closestPrimaryHoveringHandDistance = handDistance;
    //    }
    //  }
    //}

    //public override void PrimaryHoverEnd(Hand hand) {
    //  _primaryHoveringHandsCount--;
    //  if (_primaryHoveringHandsCount == 0) {
    //    _closestJustStoppedPrimaryHoveringHand = hand;
    //  }      

    //  OnPrimaryHoverEnd(hand);
    //}

    #endregion

    #region Contact

    //private Hand _closestContactingHand = null;
    //private float _closestContactingHandDistance = float.PositiveInfinity;
    //private Hand _closestJustStoppedContactingHand = null; // Provided for OnObjectContactEnd.
    //int _contactingHandsCount = 0;
    //int _contactingHandsCountLastFrame = 0;

    //private void FixedUpdateContact() {
    //  if (_contactingHandsCount > 0) {
    //    if (_contactingHandsCountLastFrame == 0) {
    //      ObjectContactBegin(_closestContactingHand);
    //    }
    //    else {
    //      ObjectContactStay(_closestContactingHand);
    //    }
    //  }
    //  else if (_contactingHandsCountLastFrame > 0) {
    //    ObjectContactEnd(_closestJustStoppedContactingHand);
    //  }
    //  _contactingHandsCountLastFrame = _contactingHandsCount;
    //  _closestContactingHand = null;
    //  _closestContactingHandDistance = float.PositiveInfinity;
    //}

    //public override void ContactBegin(Hand hand) {
    //  EvaluateContactCloseness(hand);
    //  _contactingHandsCount += 1;

    //  OnContactBegin(hand);
    //}

    //public override void ContactStay(Hand hand) {
    //  EvaluateContactCloseness(hand);

    //  OnContactStay(hand);
    //}

    //private void EvaluateContactCloseness(Hand hand) {
    //  float handDistance = GetInteractionDistanceToPoint(hand.PalmPosition.ToVector3());
    //  if (_contactingHandsCount == 0 || _closestContactingHand == null) {
    //    _closestContactingHand = hand;
    //    _closestContactingHandDistance = handDistance;
    //  }
    //  else {
    //    if (handDistance < _closestContactingHandDistance) {
    //      _closestContactingHand = hand;
    //      _closestContactingHandDistance = handDistance;
    //    }
    //  }
    //}

    //public override void ContactEnd(Hand hand) {
    //  _contactingHandsCount -= 1;
    //  if (_contactingHandsCount == 0) {
    //    _closestJustStoppedContactingHand = hand;
    //  }

    //  OnContactEnd(hand);
    //}

    //public void ObjectContactBegin(Hand hand) {
    //  OnObjectContactBegin(hand);
    //}

    //public void ObjectContactStay(Hand hand) {
    //  OnObjectContactStay(hand);
    //}

    //public void ObjectContactEnd(Hand hand) {
    //  OnObjectContactEnd(hand);
    //}

    #endregion

    #region Grasping

    //private int _graspCount = 0;
    //private bool _moveObjectWhenGrasped__WasEnabledLastFrame;
    //private bool _wasKinematicBeforeGrab;

    //private IGraspedPoseController _graspedPositionController;
    ///// <summary> Gets or sets the grasped pose controller for this Interaction object. </summary>
    //public IGraspedPoseController graspedPoseController {
    //  get {
    //    if (_graspedPositionController == null) {
    //      _graspedPositionController = new KabschGraspedPose(this);
    //    }
    //    return _graspedPositionController;
    //  }
    //  set {
    //    _graspedPositionController = value;
    //  }
    //}

    //private KinematicGraspedMovement    _kinematicHoldingMovement;
    //private NonKinematicGraspedMovement _nonKinematicHoldingMovement;

    //private IThrowController _throwController;
    ///// <summary> Gets or sets the throw controller for this Interaction object. </summary>
    //public IThrowController throwController {
    //  get {
    //    if (_throwController == null) {
    //      _throwController = new SlidingWindowThrow();
    //    }
    //    return _throwController;
    //  }
    //  set {
    //    _throwController = value;
    //  }
    //}

    //private int _graspCountLastFrame = 0;
    //private List<InteractionHand> _graspingIntHands = new List<InteractionHand>(4);
    //private List<Hand> _graspingHandsBuffer = new List<Hand>(4);
    //private List<Hand> _lastFrameGraspingHands = new List<Hand>(4); // for OnMultiGraspEnd
    //private Stack<Hand> _lastFrameGraspingHandObjectStorage = new Stack<Hand>(4); // for OnMultiGraspEnd

    //private void InitGrasping() {
    //  _moveObjectWhenGrasped__WasEnabledLastFrame = moveObjectWhenGrasped;

    //  _kinematicHoldingMovement = new KinematicGraspedMovement();
    //  _nonKinematicHoldingMovement = new NonKinematicGraspedMovement();
    //}

    //private void FixedUpdateGrasping() {
    //  if (!moveObjectWhenGrasped && _moveObjectWhenGrasped__WasEnabledLastFrame) {
    //    graspedPoseController.ClearHands();
    //  }
    //  _moveObjectWhenGrasped__WasEnabledLastFrame = moveObjectWhenGrasped;

    //  _graspingHandsBuffer.Clear();
    //  foreach (var hand in _graspingIntHands.Query().Select(intHand => intHand.GetLastTrackedLeapHand())) {
    //    _graspingHandsBuffer.Add(hand);
    //  }

    //  if (_graspCount > 0) {
    //    if (_graspCountLastFrame == 0) {
    //      OnObjectGraspBegin();
    //    }
    //    else {
    //      OnObjectGraspHold();
    //    }

    //    if (_graspCount > 1) {
    //      if (_graspCountLastFrame <= 1) {
    //        OnMultiGraspBegin(_graspingHandsBuffer);
    //      }
    //      else {
    //        OnMultiGraspHold(_graspingHandsBuffer);
    //      }
    //    }
    //  }
    //  if (_graspCount == 0) {
    //    if (_graspCountLastFrame > 0) {
    //      OnObjectGraspEnd();
    //    }
    //  }
    //  if (_graspCount <= 1) {
    //    if (_graspCountLastFrame > 1) {
    //      OnMultiGraspEnd(_lastFrameGraspingHands);
    //    }
    //  }

    //  _graspCountLastFrame = _graspCount;

    //  // Keep track of grasping hands from last frame; these must be independent
    //  // Hand objects due to pooling by the service provider, but care must be taken
    //  // to not allocate garbage every frame!
    //  while (_lastFrameGraspingHands.Count < _graspingHandsBuffer.Count) {
    //    if (_lastFrameGraspingHandObjectStorage.Count > 0) {
    //      _lastFrameGraspingHands.Add(_lastFrameGraspingHandObjectStorage.Pop());
    //    }
    //    else {
    //      _lastFrameGraspingHandObjectStorage.Push(new Hand());
    //    }
    //  }
    //  while (_lastFrameGraspingHands.Count > _graspingHandsBuffer.Count) {
    //    _lastFrameGraspingHandObjectStorage.Push(_lastFrameGraspingHands[_lastFrameGraspingHands.Count - 1]);
    //    _lastFrameGraspingHands.RemoveAt(_lastFrameGraspingHands.Count - 1);
    //  }
    //  for (int i = 0; i < _graspingHandsBuffer.Count; i++) {
    //    _lastFrameGraspingHands[i].CopyFrom(_graspingHandsBuffer[i]);
    //  }
    //}

    //public override void GraspBegin(Hand hand) {
    //  if (isGrasped && !allowMultiGrasp) {
    //    interactionManager.TryReleaseObjectFromGrasp(this);
    //  }

    //  InteractionHand graspingIntHand = interactionManager.GetInteractionHand(hand);
    //  _graspingIntHands.Add(graspingIntHand);
    //  _graspCount++;

    //  // SnapToHand(hand); // TODO: When you grasp an object, snap the object into a good holding position.

    //  if (moveObjectWhenGrasped) {
    //    graspedPoseController.AddHand(graspingIntHand);
    //  }

    //  // Set kinematic state based on grasping hold movement type
    //  if (_graspCount == 1) {
    //    _wasKinematicBeforeGrab = rigidbody.isKinematic;
    //    switch (graspedMovementType) {
    //      case GraspedMovementType.Inherit: break; // no change
    //      case GraspedMovementType.Kinematic:
    //        rigidbody.isKinematic = true; break;
    //      case GraspedMovementType.Nonkinematic:
    //        rigidbody.isKinematic = false; break;
    //    }
    //  }

    //  OnGraspBegin(hand);
    //}

    //private List<Hand> _graspedHandBuffer = new List<Hand>();
    //public override void GraspHold(Hand hand) {
    //  if (moveObjectWhenGrasped) {
    //    Vector3 origPosition = rigidbody.position; Quaternion origRotation = rigidbody.rotation;
    //    Vector3 newPosition; Quaternion newRotation;
    //    graspedPoseController.GetGraspedPosition(out newPosition, out newRotation);

    //    IGraspedMovementController holdingMovementController = rigidbody.isKinematic ?
    //                                                             (IGraspedMovementController)_kinematicHoldingMovement
    //                                                           : (IGraspedMovementController)_nonKinematicHoldingMovement;
    //    holdingMovementController.MoveTo(newPosition, newRotation, this);

    //    OnGraspedMovement(origPosition, origRotation, newPosition, newRotation, hand);

    //    // TODO: Support providing multiple hands to the throw controller
    //    _graspedHandBuffer.Clear();
    //    _graspedHandBuffer.Add(hand);
    //    throwController.OnHold(this, _graspedHandBuffer);
    //  }

    //  OnGraspHold(hand);
    //}

    //public override void GraspEnd(InteractionHand intHand) {
    //  _graspCount--;

    //  _graspingIntHands.Remove(intHand);

    //  if (moveObjectWhenGrasped) {
    //    graspedPoseController.RemoveHand(intHand);
    //  }

    //  // Revert kinematic state if the grasp has ended
    //  if (_graspCount == 0) {
    //    rigidbody.isKinematic = _wasKinematicBeforeGrab;
    //  }

    //  // If the grasp was causing the object to be suspended, stop being suspended.
    //  if (intHand.GetLeapHand() == null && this.isSuspended) {
    //    this.GraspResumeObject();
    //  }

    //  throwController.OnThrow(this, intHand.GetLastTrackedLeapHand());

    //  OnGraspEnd(intHand.GetLastTrackedLeapHand());
    //}

    //// Suspension / Resume

    //public override void GraspSuspendObject() {
    //  _isSuspended = true;
    //  OnSuspend(this);
    //}

    //public override void GraspResumeObject() {
    //  _isSuspended = false;
    //  OnResume(this);
    //}

    #endregion

    #endregion

    #region Forces

    protected Vector3 _accumulatedLinearAcceleration = Vector3.zero;
    protected Vector3 _accumulatedAngularAcceleration = Vector3.zero;

    public void FixedUpdateForces() {
      if (!isGrasped) {
        //Only apply if non-zero to prevent waking up the body
        if (_accumulatedLinearAcceleration != Vector3.zero) {
          rigidbody.velocity += _accumulatedLinearAcceleration * Time.fixedDeltaTime;
        }

        if (_accumulatedAngularAcceleration != Vector3.zero) {
          rigidbody.angularVelocity += _accumulatedAngularAcceleration * Time.fixedDeltaTime;
        }

        //Reset so we can accumulate for the next frame
        _accumulatedLinearAcceleration = Vector3.zero;
        _accumulatedAngularAcceleration = Vector3.zero;
      }
    }

    #endregion

    #region Internal

    protected Transform[] _childrenArray;

    protected void InitInternal() {
      _childrenArray = GetComponentsInChildren<Transform>(true);

      InitLayer();
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
      if ((rigidbody.constraints & RigidbodyConstraints.FreezePositionX) > 0
       && (rigidbody.constraints & RigidbodyConstraints.FreezePositionY) > 0
       && (rigidbody.constraints & RigidbodyConstraints.FreezePositionZ) > 0) {
        _isPositionLocked = true;
        return;
      }
      else {
        _isPositionLocked = false;

        Joint[] joints = rigidbody.GetComponents<Joint>();
        foreach (var joint in joints) {
          if (joint is FixedJoint) {
            _isPositionLocked = true;
            return;
          }
          if (joint is HingeJoint) {
            _isPositionLocked = true;
            return;
          }
          // if (joint is SpringJoint) {
            // no check required; spring joints never fully lock position.
          // }
          if (joint is CharacterJoint) {
            _isPositionLocked = true;
            return;
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
            _isPositionLocked = true;
            return;
          }
        }
      }
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

      _collisionMode = desiredCollisionMode;
      FixedUpdateLayer();

      Assert.IsTrue((_collisionMode == CollisionMode.Grasped) == isGrasped);
    }

    protected SingleLayer _initialLayer;

    protected void InitLayer() {
      _initialLayer = gameObject.layer;
    }

    protected void FixedUpdateLayer() {
      int layer;

      if (ignoreContact) {
        layer = manager.interactionNoContactLayer;
      }
      else {
        switch (_collisionMode) {
          case CollisionMode.Normal:
            layer = manager.interactionLayer; break;
          case CollisionMode.Grasped:
            layer = manager.interactionNoContactLayer; break;
          default:
            Debug.LogError("Invalid collision mode, can't update layer.");
            return;
        }
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