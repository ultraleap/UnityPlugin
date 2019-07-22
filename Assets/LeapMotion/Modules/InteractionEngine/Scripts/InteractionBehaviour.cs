/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using InteractionEngineUtility;
using Leap.Unity.Attributes;
using Leap.Unity.Interaction.Internal;
using Leap.Unity.Query;
using Leap.Unity.Space;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  public enum ContactForceMode { Object, UI };

  /// <summary>
  /// InteractionBehaviours are components that enable GameObjects to interact with
  /// interaction controllers (InteractionControllerBase) in a physically intuitive way.
  /// 
  /// By default, they represent objects that can be poked, prodded, smacked, grasped,
  /// and thrown around by Interaction controllers, including Leap hands. They also
  /// provide a thorough public API with settings and hovering, contact, and grasping
  /// callbacks for creating physical interfaces or overriding the default physical
  /// behavior of the object.
  /// 
  /// In documentation and some method calls, GameObjects with an InteractionBehaviour
  /// component may be referred to as interaction objects.
  /// </summary>
  [RequireComponent(typeof(Rigidbody))]
  public class InteractionBehaviour : MonoBehaviour, IInteractionBehaviour {

    public const float MAX_ANGULAR_VELOCITY = 100F;

    #region Public API

    #region Hovering API

    /// <summary> Gets whether any interaction controller is nearby. </summary>
    public bool isHovered { get { return _hoveringControllers.Count > 0; } }

    /// <summary>
    /// Gets the closest interaction controller to this object, or null if no controller is nearby.
    /// Leap hands and supported VR controllers both count as "controllers" for the purposes of
    /// this getter.
    /// </summary>
    public InteractionController closestHoveringController {
      get {
        return _closestHoveringController;
      }
    }

    /// <summary> Gets the closest Leap hand to this object, or null if no hand is nearby. </summary>
    public Hand closestHoveringHand {
      get {
        return _closestHoveringHand == null ? null
                                            : _closestHoveringHand.leapHand;
      }
    }

    /// <summary>
    /// Gets the distance from this object to the palm of the closest hand to this object,
    /// or float.PositiveInfinity of no hand is nearby.
    /// </summary>
    public float closestHoveringControllerDistance {
      get {
        return _closestHoveringControllerDistance;
      }
    }

    /// <summary>
    /// Gets all of the interaction controllers hovering near this object, whether they
    /// are Leap hands or supported VR controllers.
    /// </summary>
    public ReadonlyHashSet<InteractionController> hoveringControllers {
      get {
        return _hoveringControllers;
      }
    }

    /// <summary>
    /// Gets whether this object is the primary hover for any interaction controller.
    /// </summary>
    public bool isPrimaryHovered { get { return _primaryHoveringControllers.Count > 0; } }

    /// <summary>
    /// Gets the closest primary hovering interaction controller for this object, if it has one.
    /// An interaction controller can be a Leap hand or a supported VR controller. Any of these
    /// controllers can be the primary hover for this interaction object only if the controller is
    /// closer to it than any other interaction object. If there are multiple such controllers,
    /// this getter will return the closest one.
    /// </summary>
    public InteractionController primaryHoveringController {
      get {
        return _closestPrimaryHoveringController;
      }
    }

    /// <summary>
    /// Gets the set of all interaction controllers primarily hovering over this object.
    /// </summary>
    public ReadonlyHashSet<InteractionController> primaryHoveringControllers {
      get {
        return _primaryHoveringControllers;
      }
    }

    /// <summary>
    /// Gets the primary hovering hand for this interaction object, if it has one.
    /// A hand is the primary hover for an interaction object only if it is closer to that object
    /// than any other interaction object. If there are multiple such hands, returns the hand
    /// closest to this object.
    /// </summary>
    public Hand primaryHoveringHand {
      get {
        return _closestPrimaryHoveringHand == null ? null
                                                   : _closestPrimaryHoveringHand.leapHand;
      }
    }

    /// <summary>
    /// Gets the finger that is currently primarily hovering over this object, of the closest
    /// primarily hovering hand. Will return null if this object is not currently any Leap 
    /// hand's primary hover.
    /// </summary>
    public Finger primaryHoveringFinger {
      get {
        if (!isPrimaryHovered) return null;
        return _closestPrimaryHoveringHand.leapHand
                  .Fingers[_closestPrimaryHoveringHand.primaryHoveringPointIndex];
      }
    }

    /// <summary>
    /// Gets the position of the primaryHoverPoint on the primary hovering interaction
    /// controller that is primarily hovering over this object. For example, if the primarily
    /// hovering controller is a Leap hand, this will be the position of the fingertip that
    /// is closest to this object.
    /// </summary>
    public Vector3 primaryHoveringControllerPoint {
      get {
        if (!isPrimaryHovered) return Vector3.zero;
        return primaryHoveringController.primaryHoveringPoint;
      }
    }

    /// <summary>
    /// Gets the distance to the primary hover point whose controller is primarily hovering over this
    /// object. For example, if the primary hovering controller is a Leap hand, this will return the
    /// distance to the fingertip that is closest to this object.
    /// 
    /// If this object is not the primary hover of any interaction controller, returns positive infinity.
    /// </summary>
    public float primaryHoverDistance {
      get {
        if (!isPrimaryHovered) return float.PositiveInfinity;
        return primaryHoveringController.primaryHoverDistance;
      }
    }

    #region Hover Events

    /// <summary>
    /// Called when the object becomes hovered by any nearby interaction controllers. The hover activity
    /// radius is a setting specified by the Interaction Manager.
    /// </summary>
    /// <remarks>
    /// If this event is to be fired on a given frame, it will be called before OnHoverStay,
    /// OnPerControllerHoverEnd, and OnHoverEnd, and it will be called after OnPerControllerHoverBegin.
    /// </remarks>
    public Action OnHoverBegin;

    /// <summary>
    /// Called when the object stops being hovered by any nearby interaction controllers. The hover activity
    /// radius is a setting specified by the Interaction Manager.
    /// </summary>
    /// <remarks>
    /// If this event is to be fired on a given frame, it will be called before OnPerControllerHoverBegin,
    /// OnHoverBegin, and OnHoverStay, and it will be called after OnPerControllerHoverEnd.
    /// </remarks>
    public Action OnHoverEnd;

    /// <summary>
    /// Called during every fixed (physics) frame in which one or more interaction controller is
    /// within the hover activity radius around this object. The hover activity radius is a setting
    /// specified by the Interaction Manager.
    /// </summary>
    /// <remarks>
    /// "Stay" methods are always called after their "Begin" and "End" counterparts.
    /// </remarks>
    public Action OnHoverStay;

    /// <summary>
    /// Called whenever an interaction controller enters the hover activity radius around this
    /// interaction object. The hover activity radius is a setting specified by the Interaction Manager.
    /// </summary>
    /// <remarks>
    /// If this event is to be fired on a given frame, it will be called after OnPerControllerHandHoverEnd
    /// and before OnHoverStay.
    /// </remarks>
    public Action<InteractionController> OnPerControllerHoverBegin;

    /// <summary>
    /// Called whenever an interaction controller leaves the hover activity radius around this
    /// interaction object. The hover activity radius is a setting specified by the Interaction Manager.
    /// </summary>
    /// <remarks>
    /// If this event is to be fired on a given frame, it will be called before OnPerControllerHoverBegin
    /// and before OnHoverStay.
    /// </remarks>
    public Action<InteractionController> OnPerControllerHoverEnd;

    #endregion

    #region Primary Hover Events

    /// <summary>
    /// Called when the object becomes primarily hovered by any interaction controllers, if the object
    /// was not primarily hovered by any controllers on the previous frame.
    /// </summary>
    /// <remarks>
    /// If this event is fired on a given frame, it will be called before OnPrimaryHoverStay, and it
    /// will be called after OnPrimaryHoverEnd.
    /// </remarks>
    public Action OnPrimaryHoverBegin;

    /// <summary>
    /// Called when the object ceases being the primary hover of any interaction controllers, if the
    /// object was primarily hovered by one or more controllers on the previous frame.
    /// </summary>
    /// <remarks>
    /// If this event is fired on a given frame, it will be called before OnPrimaryHoverStay and
    /// OnPrimaryHoverBegin.
    /// </remarks>
    public Action OnPrimaryHoverEnd;

    /// <summary>
    /// Called every fixed (physics) frame in which one or more interaction controllers is primarily
    /// hovering over this object. Only one object may be the primary hover of a given controller at
    /// any one time.
    /// </summary>
    /// <remarks>
    /// "Stay" events are fired after any "End" and "Begin" events have been fired.
    /// </remarks>
    public Action OnPrimaryHoverStay;

    /// <summary>
    /// Called whenever an interaction controller (a Leap hand or supported VR controller) begins primarily
    /// hovering over this object. Only one interaction object can be the primary hover of a given controller
    /// at a time.
    /// </summary>
    /// <remarks>
    /// If this event is to be fired on a given frame, it will be called before OnPrimaryHoverStay,
    /// and it will be called after OnPerControllerPrimaryHoverEnd.
    /// </remarks>
    public Action<InteractionController> OnPerControllerPrimaryHoverBegin;

    /// <summary>
    /// Called whenever an interaction controler (a Leap hand or supported VR controller) stops primarily
    /// hovering over this object. Only one interaction object can be the primary hover of a given controller
    /// at a time.
    /// </summary>
    /// <remarks>
    /// If this event is to be fired on a given frame, it will be called before OnPerControllerPrimaryHoverBegin
    /// and OnPrimaryHoverStay.
    /// </remarks>
    public Action<InteractionController> OnPerControllerPrimaryHoverEnd;

    #endregion

    #endregion

    #region Grasping API

    /// <summary> Gets whether this object is grasped by any interaction controller. </summary>
    public bool isGrasped { get { return _graspingControllers.Count > 0; } }

    /// <summary>
    /// Gets the controller currently grasping this object. Warning: If allowMultigrasp is enabled on
    /// this object, it might have multiple grasping controllers, in which case this will only return one
    /// of the controllers grasping this object, and there is no guarantee on which controller is returned!
    /// If no controllers (Leap hands or supported VR controllers) are currently grasping this object,
    /// returns null.
    /// </summary>
    public InteractionController graspingController { get { return _graspingControllers.Query().FirstOrDefault(); } }

    /// <summary>
    /// Gets the set of all interaction controllers currently grasping this object. Interaction
    /// controllers include Leap hands via InteractionHand and supported VR controllers.
    /// </summary>
    public ReadonlyHashSet<InteractionController> graspingControllers { get { return _graspingControllers; } }

    private HashSet<InteractionHand> _graspingHandsBuffer = new HashSet<InteractionHand>();
    /// <summary>
    /// Gets a set of all Leap hands currently grasping this object.
    /// </summary>
    public ReadonlyHashSet<InteractionHand> graspingHands {
      get {
        _graspingHandsBuffer.Clear();
        _graspingControllers.Query().OfType<InteractionHand>().FillHashSet(_graspingHandsBuffer);
        return _graspingHandsBuffer;
      }
    }

    /// <summary>
    /// Gets whether the object is currently suspended. An object is "suspended" if it
    /// is currently grasped by an untracked controller. For more details, refer to
    /// OnSuspensionBegin.
    /// </summary>
    public bool isSuspended { get { return _suspendingController != null; } }

    /// <summary>
    /// Nonkinematic grasping motion applies clamped velocities to Interaction Behaviours
    /// when they are grasped to move them to their target position and rotation in the
    /// grasping hand. If a controller applies its SwapGrasp method to an interaction
    /// object that didn't reach its target pose due to velocity clamping, the
    /// swapped-out object will inherit the offset as a new target pose relative to the
    /// hand.
    /// 
    /// To prevent slippage in this scenario, we always track the latest scheduled grasp
    /// pose for interaction objects here, and use it whenever possible in the SwapGrasp
    /// method.
    /// </summary>
    public Pose? latestScheduledGraspPose = null;

    #region Grasp Events

    /// <summary>
    /// Called directly after this grasped object's Rigidbody has had its position and rotation set
    /// by its currently grasping controller(s). Subscribe to this callback if you'd like to override
    /// the default behaviour for grasping objects, for example, to constrain the object's position or rotation.
    /// 
    /// Use InteractionBehaviour.rigidbody.position and InteractionBehaviour.rigidbody.rotation to set the
    /// object's position and rotation. Merely setting the object's Transform's position and rotation is not
    /// recommended unless you understand the difference.
    /// </summary>
    /// <remarks>
    /// This method is called after any OnGraspBegin or OnGraspEnd callbacks, but before OnGraspStay. It is
    /// also valid to move the Interaction object (via its Rigidbody) in OnGraspStay, although OnGraspStay does
    /// not provide pre- and post-solve data in its callback signature.
    /// </remarks>
    public GraspedMovementEvent OnGraspedMovement = (preSolvedPos, preSolvedRot,
                                                     solvedPos,    solvedRot,
                                                     graspingControllers) => { };

    /// <summary>
    /// Called when the object becomes grasped, if it was not already held by any interaction controllers on the
    /// previous frame.
    /// </summary>
    /// <remarks>
    /// If this event is fired on a given frame, it will occur after OnGraspEnd and before OnGraspStay.
    /// </remarks>
    public Action OnGraspBegin;

    /// <summary>
    /// Called when the object is no longer grasped by any interaction controllers.
    /// </summary>
    /// <remarks>
    /// If this event is fired on a given frame, it will occur before OnGraspBegin and OnGraspStay.
    /// </remarks>
    public Action OnGraspEnd;

    /// <summary>
    /// Called every fixed (physics) frame during which this object is grasped by one or more hands.
    /// 
    /// Unless allowMultigrasp is set to true, only one hand will ever be grasping an object at any given
    /// time.
    /// </summary>
    /// <remarks>
    /// If this event is fired on a given frame, it will be fired after all other grasping callbacks, including
    /// OnGraspedMovement.
    /// </remarks>
    public Action OnGraspStay;

    /// <summary>
    /// Called whenever an interaction controller grasps this object.
    /// 
    /// Unless allowMultigrasp is set to true, only one controller will ever be grasping an object at any given
    /// time.
    /// </summary>
    /// <remarks>
    /// If this event is fired on a given frame, it will be called after OnPreControllerGraspEnd and before
    /// OnGraspStay.
    /// </remarks>
    public Action<InteractionController> OnPerControllerGraspBegin;

    /// <summary>
    /// Called whenever an interaction controller stops grasping this object.
    /// 
    /// Unless allowMultigrasp is set to true, only one controller will ever be grasping an object at any given
    /// time. If a new controller grasps an object while allowMultigrasp is disabled, the object will first
    /// receive the end grasp event before receiving the begin grasp event for the newly grasping controller.
    /// </summary>
    /// <remarks>
    /// If this event is fired on a given frame, it will be before all other grasping callbacks.
    /// </remarks>
    public Action<InteractionController> OnPerControllerGraspEnd;

    /// <summary>
    /// Called when the interaction controller that is grasping this interaction object loses tracking. This can
    /// occur if the controller is occluded from the sensor that is tracking it, e.g. by as the user's body or
    /// an object in the real world.
    /// 
    /// An object is "suspended" if it is currently grasped by an untracked controller.
    /// 
    /// By default, suspended objects will hang in the air until the interaction controller grasping them
    /// resumes tracking. Subscribe to this callback and OnResume to implement, e.g., the object disappearing
    /// and re-appearing.
    /// </summary>
    public Action<InteractionController> OnSuspensionBegin;

    /// <summary>
    /// Called when an object ceases being suspended. An object is suspended if it is currently grasped by
    /// an untracked controller.
    /// 
    /// Grasping a suspended object with a different controller will cease suspension of the object, and will
    /// invoke OnSuspensionEnd, although the input to OnSuspensionEnd will be the newly grasping controller, not
    /// the controller that suspended the object. OnGraspEnd will also be called for the interaction controller
    /// that was formerly causing suspension.
    /// </summary>
    public Action<InteractionController> OnSuspensionEnd;

    #endregion

    /// <summary>
    /// Releases this object from the interaction controller currently grasping it, if it
    /// is grasped, and returns true. If the object was not grasped, this method returns 
    /// false. Directly after calling this method, the object is guaranteed not to be
    /// held. However, a grasp may retrigger on the next frame, if the Interaction
    /// Controller determines that the released object should be grasped. The safest way
    /// to ensure an object is released and ungraspable is to use the interaction
    /// object's ignoreGrasp property.
    /// </summary>
    public bool ReleaseFromGrasp() {
      if (isGrasped) {
        InteractionController.ReleaseGrasps(this, graspingControllers);
        return true;
      }

      return false;
    }

    /// <summary>
    /// Returns (approximately) where the argument hand is grasping this object.
    /// If the interaction controller is not currently grasping this object, returns
    /// Vector3.zero, and logs an error to the Unity console.
    /// </summary>
    public Vector3 GetGraspPoint(InteractionController intController) {
      if (intController.graspedObject == this as IInteractionBehaviour) {
        return intController.GetGraspPoint();
      }
      else {
        Debug.LogError("Cannot get this object's grasp point: It is not currently grasped "
                     + "by the provided interaction controller.", intController);
        return Vector3.zero;
      }
    }

    #endregion

    #region Contact API

    /// <summary>
    /// Gets a set of all InteractionControllers currently contacting this interaction
    /// object.
    /// </summary>
    public ReadonlyHashSet<InteractionController> contactingControllers {
      get { return _contactingControllers; }
    }

    /// <summary>
    /// Called when this object begins colliding with any interaction controllers, if the
    /// object was not colliding with any interaction controllers last frame.
    /// </summary>
    public Action OnContactBegin;

    /// <summary>
    /// Called when the object ceases colliding with any interaction controllers, if the     
    /// object was colliding with interaction controllers last frame.
    /// </summary>
    public Action OnContactEnd;

    /// <summary>
    /// Called every frame during which one or more interaction controllers is colliding
    /// with this object.
    /// </summary>
    public Action OnContactStay;

    /// <summary>
    /// Called whenever an interaction controller begins colliding with this object.
    /// </summary>
    public Action<InteractionController> OnPerControllerContactBegin;

    /// <summary>
    /// Called whenever an interaction controller stops colliding with this object.
    /// </summary>
    public Action<InteractionController> OnPerControllerContactEnd;

    #endregion

    #region Forces API

    /// <summary>
    /// Adds a linear acceleration to the center of mass of this object. 
    /// Use this instead of Rigidbody.AddForce() to accelerate an Interaction object.
    /// </summary>
    /// <remarks>
    /// Rigidbody.AddForce() will work in most scenarios, but will produce unexpected
    /// behavior when interaction controllers are embedded inside an object due to soft
    /// contact. Calling this method instead solves that problem.
    /// </remarks>
    public void AddLinearAcceleration(Vector3 acceleration) {
      _appliedForces = true;
      _accumulatedLinearAcceleration += acceleration;
    }

    /// <summary>
    /// Adds an angular acceleration to the center of mass of this object. 
    /// Use this instead of Rigidbody.AddTorque() to add angular acceleration 
    /// to an Interaction object.
    /// </summary>
    /// <remarks>
    /// Rigidbody.AddTorque() will work in most scenarios, but will produce unexpected
    /// behavior when interaction controllers are embedded inside an object due to soft
    /// contact. Calling this method instead solves that problem.
    /// </remarks>
    public void AddAngularAcceleration(Vector3 acceleration) {
      _appliedForces = true;
      _accumulatedAngularAcceleration += acceleration;
    }

    #endregion

    #region General API

    /// <summary> Use this if you want to modify the isKinematic state of an
    /// interaction object while it is grasped; otherwise the object's grasp
    /// settings may return the Rigidbody to the kinematic state of the object
    /// from right before it was grasped. </summary>
    public void SetKinematicWithoutGrasp(bool isKinematic) {
      if (this.isGrasped) {
        _wasKinematicBeforeGrasp = isKinematic;
      }
      else {
        _rigidbody.isKinematic = isKinematic;
      }
    }

    /// <summary> Use this to retrieve the isKinematic state of the interactino
    /// object ignoring any temporary modification to isKinematic that may be
    /// due to the object being grasped. </summary>
    public bool GetKinematicWithoutGrasp() {
      if (this.isGrasped) {
        return _wasKinematicBeforeGrasp;
      }
      else {
        return _rigidbody.isKinematic;
      }
    }

    #endregion

    #endregion

    #region Inspector

    [Tooltip("The Interaction Manager responsible for this interaction object.")]
    [SerializeField]
    private InteractionManager _manager;
    public InteractionManager manager {
      get { return _manager; }
      set {
        if (Application.isPlaying) {
          if (_manager != null && _manager.IsBehaviourRegistered(this)) {
            _manager.UnregisterInteractionBehaviour(this);
          }
        }

        _manager = value;

        if (Application.isPlaying) {
          if (_manager != null && !manager.IsBehaviourRegistered(this)) {
            _manager.RegisterInteractionBehaviour(this);
          }
        }
      }
    }

    private Rigidbody _rigidbody;
    #if UNITY_EDITOR
    new 
    #endif
    /// <summary> The Rigidbody associated with this interaction object. </summary>
    public Rigidbody rigidbody {
      get { return _rigidbody; }
      protected set { _rigidbody = value; }
    }

    public ISpaceComponent space { get; protected set; }

    [Header("Interaction Overrides")]

    [Tooltip("This object will not receive callbacks from left controllers, right "
           + "controllers, or either hand if this mode is set to anything other than "
           + "None.")]
    [SerializeField]
    private IgnoreHoverMode _ignoreHoverMode = IgnoreHoverMode.None;
    public IgnoreHoverMode ignoreHoverMode {
      get { return _ignoreHoverMode; }
      set {
        _ignoreHoverMode = value;

        if (_ignoreHoverMode != IgnoreHoverMode.None) {
          ClearHoverTracking(onlyInvalidControllers: true);
        }
      }
    }
    [SerializeField, HideInInspector]
    private bool _isIgnoringAllHoverState = false;

    [Tooltip("Interaction controllers will not be able to mark this object as their "
           + "primary hover if this property is checked. Primary hover requires hovering "
           + "enabled to function, but it can be disabled independently of hovering.")]
    [SerializeField]
    [DisableIf("_isIgnoringAllHoverState", isEqualTo: true)]
    private bool _ignorePrimaryHover = false;
    public bool ignorePrimaryHover {
      get { return _ignorePrimaryHover; }
      set {
        _ignorePrimaryHover = value;

        if (_ignorePrimaryHover) {
          ClearPrimaryHoverTracking();
        }
      }
    }

    [Tooltip("Interaction controllers will not be able to touch this object if this "
           + "property is checked.")]
    [SerializeField]
    private bool _ignoreContact = false;
    public bool ignoreContact {
      get { return _ignoreContact; }
      set {
        _ignoreContact = value;

        if (_ignoreContact) {
          ClearContactTracking();
        }
      }
    }

    [Tooltip("Interaction controllers will not be able to grasp this object if this "
           + "property is checked.")]
    [SerializeField]
    private bool _ignoreGrasping = false;
    public bool ignoreGrasping {
      get { return _ignoreGrasping; }
      set {
        _ignoreGrasping = value;

        if (_ignoreGrasping && isGrasped) {
          graspingController.ReleaseGrasp();
        }
      }
    }

    [Header("Contact Settings")]

    [Tooltip("Determines how much force an interaction controller should apply to this "
           + "object. For interface-style objects like buttons and sliders, choose UI. "
           + "This will make the objects to feel lighter and more reactive to gentle "
           + "touches; for normal physical objects, you'll almost always want Object.")]
    [SerializeField]
    private ContactForceMode _contactForceMode = ContactForceMode.Object;
    public ContactForceMode contactForceMode {
      get { return _contactForceMode; }
      set { _contactForceMode = value; }
    }

    [Header("Grasp Settings")]

    [Tooltip("Can this object be grasped simultaneously with two or more interaction "
           + "controllers?")]
    [SerializeField]
    private bool _allowMultiGrasp = false;
    public bool allowMultiGrasp {
      get { return _allowMultiGrasp; }
      set { _allowMultiGrasp = value; }
    }

    [Tooltip("Should interaction controllers move this object when it is grasped? "
           + "Without this property checked, objects will still receive grasp callbacks, "
           + "but you will need to move them manually via script.")]
    [SerializeField]
    [OnEditorChange("moveObjectWhenGrasped")]
    private bool _moveObjectWhenGrasped = true;
    public bool moveObjectWhenGrasped {
      get { return _moveObjectWhenGrasped; }
      set {
        if (_moveObjectWhenGrasped != value && value == false) {
          if (graspedPoseHandler != null) {
            graspedPoseHandler.ClearControllers();
          }
        }
        _moveObjectWhenGrasped = value;
      }
    }

    public enum GraspedMovementType {
      Inherit,
      Kinematic,
      Nonkinematic
    }
    [Tooltip("When the object is held by an interaction controller, how should it move to "
           + "its new position? Nonkinematic bodies will collide with other Rigidbodies, "
           + "so they might not reach the target position. Kinematic rigidbodies will "
           + "always move to the target position, ignoring collisions. Inherit will "
           + "simply use the isKinematic state of the Rigidbody from before it was "
           + "grasped.")]
    [DisableIf("_moveObjectWhenGrasped", isEqualTo: false)]
    public GraspedMovementType graspedMovementType;

    [Header("Layer Overrides")]

    [SerializeField]
    [OnEditorChange("overrideInteractionLayer")]
    [Tooltip("If set to true, this interaction object will override the Interaction "
           + "Manager's layer setting for its default layer. The interaction layer is "
           + "used for an object when it is not grasped and not ignoring contact.")]
    private bool _overrideInteractionLayer = false;
    public bool overrideInteractionLayer {
      get {
        return _overrideInteractionLayer;
      }
      set {
        _overrideInteractionLayer = value;
      }
    }

    [Tooltip("Sets the override layer to use for this object when it is not grasped and "
           + "not ignoring contact.")]
    [SerializeField]
    private SingleLayer _interactionLayer;
    public SingleLayer interactionLayer {
      get { return _interactionLayer; }
      protected set { _interactionLayer = value; }
    }

    [SerializeField]
    [OnEditorChange("overrideNoContactLayer")]
    [Tooltip("If set to true, this interaction object will override the Interaction "
           + "Manager's layer setting for its default no-contact layer. The no-contact "
           + "layer should not collide with the contact bone layer; it is used when the "
           + "interaction object is grasped or when it is ignoring contact.")]
    private bool _overrideNoContactLayer = false;
    public bool overrideNoContactLayer {
      get {
        return _overrideNoContactLayer;
      }
      set {
        _overrideNoContactLayer = value;
      }
    }

    [Tooltip("Overrides the layer this interaction object should be on when it is grasped "
           + "or ignoring contact. This layer should not collide with the contact bone "
           + "layer -- the layer interaction controllers' colliders are on.")]
    [SerializeField]
    private SingleLayer _noContactLayer;
    public SingleLayer noContactLayer {
      get { return _noContactLayer; }
      protected set { _noContactLayer = value; }
    }

    #endregion

    #region Unity Callbacks

    protected virtual void OnValidate() {
      rigidbody = GetComponent<Rigidbody>();

      _isIgnoringAllHoverState = ignoreHoverMode == IgnoreHoverMode.Both;
      if (_isIgnoringAllHoverState) _ignorePrimaryHover = true;
    }

    protected virtual void Awake() {
      InitUnityEvents();

      rigidbody = GetComponent<Rigidbody>();
      rigidbody.maxAngularVelocity = MAX_ANGULAR_VELOCITY;
    }

    protected virtual void OnEnable() {
      if (manager == null) {
        manager = InteractionManager.instance;

        if (manager == null) {
          Debug.LogError("Interaction Behaviours require an Interaction Manager. Please "
                       + "ensure you have an InteractionManager in your scene.");
          this.enabled = false;
        }
      }

      if (manager != null && !manager.IsBehaviourRegistered(this)) {
        manager.RegisterInteractionBehaviour(this);
      }

      // Make sure we have a list of all of this object's colliders.
      RefreshInteractionColliders();

      // Refresh curved space. Currently a maximum of one (1) LeapSpace is supported per
      // InteractionBehaviour.
      foreach (var collider in _interactionColliders) {
        var leapSpace = collider.transform.GetComponentInParent<ISpaceComponent>();
        if (leapSpace != null) {
          space = leapSpace;
          break;
        }
      }

      // Ensure physics layers are set up properly.
      initLayers();
    }

    protected virtual void OnDisable() {
      // Remove this object's layer tracking from the manager.
      finalizeLayers();

      if (manager != null && manager.IsBehaviourRegistered(this)) {
        manager.UnregisterInteractionBehaviour(this);
      }
    }

    protected virtual void Start() {
      // Check any Joint attachments to automatically be able to choose Kabsch pivot
      // setting (grasping).
      RefreshPositionLockedState();
    }

    #endregion

    /// <summary>
    /// The InteractionManager manually calls method this after all
    /// InteractionControllerBase objects are updated via the InteractionManager's
    /// FixedUpdate().
    /// </summary>
    public void FixedUpdateObject() {
      fixedUpdateLayers();

      if (_appliedForces) { FixedUpdateForces(); }
    }

    #region Hovering

    private HashSet<InteractionController> _hoveringControllers = new HashSet<InteractionController>();

    private InteractionController _closestHoveringController = null;
    private float _closestHoveringControllerDistance = float.PositiveInfinity;
    private InteractionHand _closestHoveringHand = null;

    /// <summary>
    /// Returns a comparative distance to this interaction object. Calculated by finding
    /// the smallest distance to each of the object's colliders.
    /// 
    /// Any MeshColliders, however, will not have their distances calculated precisely;
    /// the squared distance to their bounding box is calculated instead. It is possible
    /// to use a custom set of colliders against which to test primary hover calculations:
    /// see primaryHoverColliders.
    /// </summary>
    public virtual float GetHoverDistance(Vector3 worldPosition) {
      float closestComparativeColliderDistance = float.PositiveInfinity;
      bool hasColliders = false;
      float testDistance = float.PositiveInfinity;

      if (rigidbody == null) {
        // The Interaction Object is probably being destroyed, or is otherwise in an
        // invalid state.
        return float.PositiveInfinity;
      }

      foreach (var collider in _interactionColliders) {
        if (!hasColliders) hasColliders = true;

        if (collider is MeshCollider) {
          // Native, faster ClosestPoint, but no support for off-center colliders; use to
          // support MeshColliders.
          testDistance = (Physics.ClosestPoint(worldPosition,
                                               collider,
                                               collider.attachedRigidbody.position,
                                               collider.attachedRigidbody.rotation)
                          - worldPosition).magnitude;
        }
        // Custom, slower ClosestPoint
        else {
          // Note: Should be using rigidbody position instead of transform; this will
          // cause problems when colliders are moving very fast (one-frame delay).
          testDistance = (collider.transform.TransformPoint(
                            collider.ClosestPointOnSurface(
                              collider.transform.InverseTransformPoint(worldPosition)))
                          - worldPosition).magnitude;
        }

        if (testDistance < closestComparativeColliderDistance) {
          closestComparativeColliderDistance = testDistance;
        }
      }

      if (!hasColliders) {
        return (this.rigidbody.position - worldPosition).magnitude;
      }
      else {
        return closestComparativeColliderDistance;
      }
    }

    public void BeginHover(List<InteractionController> controllers) {
      foreach (var controller in controllers) {
        _hoveringControllers.Add(controller);
      }

      refreshClosestHoveringController();

      foreach (var controller in controllers) {
        OnPerControllerHoverBegin(controller);
      }

      if (_hoveringControllers.Count == controllers.Count) {
        OnHoverBegin();
      }
    }

    public void EndHover(List<InteractionController> controllers) {
      foreach (var controller in controllers) {
        _hoveringControllers.Remove(controller);
      }

      refreshClosestHoveringController();

      foreach (var controller in controllers) {
        OnPerControllerHoverEnd(controller);
      }

      if (_hoveringControllers.Count == 0) {
        OnHoverEnd();
      }
    }

    public void StayHovered(List<InteractionController> controllers) {
      refreshClosestHoveringController();
      OnHoverStay();
    }

    private void refreshClosestHoveringController() {
      float closestControllerDistance = float.PositiveInfinity;
      _closestHoveringController = getClosestController(_hoveringControllers,
                                                        out closestControllerDistance);
      _closestHoveringControllerDistance = closestControllerDistance;

      float closestHandDistance = float.PositiveInfinity;
      _closestHoveringHand = getClosestController(_hoveringControllers,
                                                  out closestHandDistance,
                                                  controller => controller.intHand != null)
                               as InteractionHand;
      // closestHandDistance unused for now.
    }

    private InteractionController getClosestController(HashSet<InteractionController> controllers,
                                                       out float closestDistance,
                                                       Func<InteractionController, bool> filter = null) {
      InteractionController closestHoveringController = null;
      float closestHoveringControllerDist = float.PositiveInfinity;
      foreach (var controller in controllers) {
        if (filter != null && filter(controller) == false) continue;

        float distance = GetHoverDistance(controller.hoverPoint);
        if (closestHoveringHand == null
            || distance < closestHoveringControllerDist) {
          closestHoveringController = controller;
          closestHoveringControllerDist = distance;
        }
      }

      closestDistance = closestHoveringControllerDist;
      return closestHoveringController;
    }

    /// <summary>
    /// Clears hover tracking state for this object on all of the currently-hovering
    /// controllers. New hover state will begin anew on the next fixed frame if the
    /// appropriate conditions for hover are still fulfilled.
    /// 
    /// Optionally, only clear hover tracking state for controllers that should be
    /// ignoring hover for this interaction object due to its ignoreHoverMode.
    /// </summary>
    public void ClearHoverTracking(bool onlyInvalidControllers = false) {
      var tempControllers = Pool<HashSet<InteractionController>>.Spawn();
      try {
        foreach (var controller in hoveringControllers) {
          if (onlyInvalidControllers && this.ShouldIgnoreHover(controller)) {
            tempControllers.Add(controller);
          }
        }

        foreach (var controller in tempControllers) {
          controller.ClearHoverTrackingForObject(this);
        }
      }
      finally {
        tempControllers.Clear();
        Pool<HashSet<InteractionController>>.Recycle(tempControllers);
      }
    }

    private HashSet<InteractionController> _primaryHoveringControllers = new HashSet<InteractionController>();
    private InteractionController _closestPrimaryHoveringController = null;
    private InteractionHand _closestPrimaryHoveringHand = null;

    public void BeginPrimaryHover(List<InteractionController> controllers) {
      foreach (var controller in controllers) {
        _primaryHoveringControllers.Add(controller);
      }

      refreshClosestPrimaryHoveringController();

      foreach (var controller in controllers) {
        OnPerControllerPrimaryHoverBegin(controller);
      }

      if (_primaryHoveringControllers.Count == controllers.Count) {
        OnPrimaryHoverBegin();
      }
    }

    public void EndPrimaryHover(List<InteractionController> controllers) {
      foreach (var controller in controllers) {
        _primaryHoveringControllers.Remove(controller);
      }

      refreshClosestPrimaryHoveringController();

      foreach (var controller in controllers) {
        OnPerControllerPrimaryHoverEnd(controller);
      }

      if (_primaryHoveringControllers.Count == 0) {
        OnPrimaryHoverEnd();
      }
    }

    public void StayPrimaryHovered(List<InteractionController> controllers) {
      refreshClosestPrimaryHoveringController();
      OnPrimaryHoverStay();
    }

    private void refreshClosestPrimaryHoveringController() {
      _closestPrimaryHoveringController = getClosestPrimaryHoveringController();
      _closestPrimaryHoveringHand = getClosestPrimaryHoveringController((controller) => controller.intHand != null) as InteractionHand;
    }

    private InteractionController getClosestPrimaryHoveringController(Func<InteractionController, bool> filter = null) {
      InteractionController closestController = null;
      float closestDist = float.PositiveInfinity;
      foreach (var controller in _primaryHoveringControllers) {
        if (filter != null && filter(controller) == false) continue;

        if (closestController == null || controller.primaryHoverDistance < closestDist) {
          closestController = controller;
          closestDist = controller.primaryHoverDistance;
        }
      }
      return closestController;
    }
    /// <summary>
    /// Clears primary hover tracking state for this object on all of the currently-
    /// primary-hovering controllers. New priamry hover state will begin anew on the next
    /// fixed frame if the appropriate conditions for primary hover are still fulfilled.
    /// </summary>
    public void ClearPrimaryHoverTracking() {
      var tempControllers = Pool<HashSet<InteractionController>>.Spawn();
      try {
        foreach (var controller in primaryHoveringControllers) {
          tempControllers.Add(controller);
        }

        foreach (var controller in tempControllers) {
          controller.ClearPrimaryHoverTracking();
        }
      }
      finally {
        tempControllers.Clear();
        Pool<HashSet<InteractionController>>.Recycle(tempControllers);
      }
    }

    /// <summary>
    /// Gets the List of Colliders used for hover distance checking for this Interaction
    /// object. Hover distancing checking will affect which object is chosen for an
    /// interaction controller's primary hover, as well as for determining this object's
    /// closest hovering controller.
    /// 
    /// RefreshInteractionColliders() will automatically populate the colliders List with
    /// the this rigidbody's colliders, but is only called once on Start(). If you change
    /// the colliders for this object at runtime, you should call RefreshInteractionColliders()
    /// to keep the _hoverColliders list up-to-date.
    /// </summary>
    /// <remarks>
    /// If you're feeling brave, you can manually modify this list yourself.
    /// 
    /// Hover candidacy is determined by a hand-centric PhysX sphere-check against the
    /// Interaction object's rigidbody's attached colliders. This behavior cannot be
    /// changed, even if you modify the contents of primaryHoverColliders.
    /// 
    /// However, primary hover is determined by performing distance checks against the
    /// colliders in the primaryHoverColliders list, so it IS possible to use different
    /// collider(s) for primary hover checks than are used for hover candidacy, by
    /// modifying the collider contents of this list. This will also affect which hand is
    /// chosen by this object as its closestHoveringHand.
    /// </remarks>
    public List<Collider> primaryHoverColliders {
      get { return _interactionColliders; }
    }

    #endregion

    #region Contact

    private HashSet<InteractionController> _contactingControllers = new HashSet<InteractionController>();

    public void BeginContact(List<InteractionController> controllers) {
      foreach (var controller in controllers) {
        _contactingControllers.Add(controller);

        OnPerControllerContactBegin(controller);
      }

      if (_contactingControllers.Count == controllers.Count) {
        OnContactBegin();
      }
    }

    public void EndContact(List<InteractionController> controllers) {
      foreach (var controller in controllers) {
        _contactingControllers.Remove(controller);

        OnPerControllerContactEnd(controller);
      }

      if (_contactingControllers.Count == 0) {
        OnContactEnd();
      }
    }

    public void StayContacted(List<InteractionController> controllers) {
      OnContactStay();
    }

    /// <summary>
    /// Clears contact tracking for this object on any currently-contacting controllers.
    /// If the object is still contacting controllers and they are appropriately enabled,
    /// contact will begin anew on the next fixed frame.
    /// </summary>
    public void ClearContactTracking() {
      var tempControllers = Pool<HashSet<InteractionController>>.Spawn();
      try {
        foreach (var controller in contactingControllers) {
          tempControllers.Add(controller);
        }

        foreach (var controller in tempControllers) {
          controller.ClearContactTrackingForObject(this);
        }
      }
      finally {
        tempControllers.Clear();
        Pool<HashSet<InteractionController>>.Recycle(tempControllers);
      }
    }

    #endregion

    #region Grasping

    private HashSet<InteractionController> _graspingControllers = new HashSet<InteractionController>();
    
    private bool _wasKinematicBeforeGrasp;
    private bool _justGrasped = false;

    private float _dragBeforeGrasp = 0F;
    private float _angularDragBeforeGrasp = 0.05F;

    private IGraspedPoseHandler _graspedPoseHandler;
    /// <summary> Gets or sets the grasped pose handler for this Interaction object. </summary>
    public IGraspedPoseHandler graspedPoseHandler {
      get {
        if (_graspedPoseHandler == null) {
          _graspedPoseHandler = new KabschGraspedPose(this);
        }
        return _graspedPoseHandler;
      }
      set {
        _graspedPoseHandler = value;
      }
    }

    private KinematicGraspedMovement _lazyKinematicGraspedMovement;
    private KinematicGraspedMovement _kinematicGraspedMovement {
      get {
        if (_lazyKinematicGraspedMovement == null) {
          _lazyKinematicGraspedMovement = new KinematicGraspedMovement();
        }
        return _lazyKinematicGraspedMovement;
      }
    }

    private NonKinematicGraspedMovement _lazyNonKinematicGraspedMovement;
    private NonKinematicGraspedMovement _nonKinematicGraspedMovement {
      get {
        if (_lazyNonKinematicGraspedMovement == null) {
          _lazyNonKinematicGraspedMovement = new NonKinematicGraspedMovement();
        }
        return _lazyNonKinematicGraspedMovement;
      }
    }

    private IThrowHandler _throwHandler;
    /// <summary> Gets or sets the throw handler for this Interaction object. </summary>
    public IThrowHandler throwHandler {
      get {
        if (_throwHandler == null) {
          _throwHandler = new SlidingWindowThrow();
        }
        return _throwHandler;
      }
      set {
        _throwHandler = value;
      }
    }

    public void BeginGrasp(List<InteractionController> controllers) {
      _justGrasped = true;

      // End suspension by ending the grasp on the suspending hand,
      // calling EndGrasp immediately.
      if (isSuspended) {
        _suspendingController.ReleaseGrasp();
      }

      // If multi-grasp is not allowed, release the old grasp.
      if (!allowMultiGrasp && isGrasped) {
        _graspingControllers.Query().First().ReleaseGrasp();
      }

      // Add each newly grasping hand to internal reference and pose solver.
      foreach (var controller in controllers) {
        _graspingControllers.Add(controller);

        if (moveObjectWhenGrasped) {
          graspedPoseHandler.AddController(controller);
        }

        // Fire interaction callback.
        OnPerControllerGraspBegin(controller);
      }

      // If object wasn't grasped before, store rigidbody settings and
      // fire object interaction callback.
      if (_graspingControllers.Count == controllers.Count) {

        // Remember drag settings pre-grasp, to be restored on release.
        _dragBeforeGrasp = rigidbody.drag;
        _angularDragBeforeGrasp = rigidbody.angularDrag;

        // Remember kinematic state.
        _wasKinematicBeforeGrasp = rigidbody.isKinematic;
        switch (graspedMovementType) {
          case GraspedMovementType.Inherit: break; // no change
          case GraspedMovementType.Kinematic:
            rigidbody.isKinematic = true; break;
          case GraspedMovementType.Nonkinematic:
            rigidbody.isKinematic = false; break;
        }

        // Set rigidbody drag/angular drag to zero.
        rigidbody.drag = 0F;
        rigidbody.angularDrag = 0F;

        OnGraspBegin();
      }
    }

    public void EndGrasp(List<InteractionController> controllers) {
      if (_graspingControllers.Count == controllers.Count && isSuspended) {
        // No grasped hands: Should not be suspended any more;
        // having been suspended also means we were only grasped by one hand
        EndSuspension(controllers[0]);
      }

      foreach (var controller in controllers) {
        _graspingControllers.Remove(controller);

        // Fire interaction callback.
        OnPerControllerGraspEnd(controller);

        if (moveObjectWhenGrasped) {
          // Remove each hand from the pose solver.
          graspedPoseHandler.RemoveController(controller);
        }
      }

      // If the object is no longer grasped by any hands, restore state and
      // activate throw handler.
      if (_graspingControllers.Count == 0) {
        // Restore drag settings from prior to the grasp.
        rigidbody.drag = _dragBeforeGrasp;
        rigidbody.angularDrag = _angularDragBeforeGrasp;

        // Revert kinematic state.
        rigidbody.isKinematic = _wasKinematicBeforeGrasp;

        if (controllers.Count == 1) {
          throwHandler.OnThrow(this, controllers.Query().First());
        }

        OnGraspEnd();

        if (_justGrasped) _justGrasped = false;
      }
    }

    public void StayGrasped(List<InteractionController> controllers) {
      if (moveObjectWhenGrasped) {
        Vector3    origPosition = rigidbody.position;
        Quaternion origRotation = rigidbody.rotation;
        Vector3    newPosition;
        Quaternion newRotation;

        graspedPoseHandler.GetGraspedPosition(out newPosition, out newRotation);

        fixedUpdateGraspedMovement(new Pose(origPosition, origRotation),
                                   new Pose(newPosition, newRotation),
                                   controllers);

        throwHandler.OnHold(this, controllers);
      }

      OnGraspStay();

      _justGrasped = false;
    }

    protected virtual void fixedUpdateGraspedMovement(Pose origPose, Pose newPose,
                                              List<InteractionController> controllers) {
      IGraspedMovementHandler graspedMovementHandler
          = rigidbody.isKinematic ?
              (IGraspedMovementHandler)_kinematicGraspedMovement
            : (IGraspedMovementHandler)_nonKinematicGraspedMovement;
      graspedMovementHandler.MoveTo(newPose.position, newPose.rotation,
                                    this, _justGrasped);

      OnGraspedMovement(origPose.position, origPose.rotation,
                        newPose.position, newPose.rotation,
                        controllers);
    }

    protected InteractionController _suspendingController = null;

    public void BeginSuspension(InteractionController controller) {
      _suspendingController = controller;

      OnSuspensionBegin(controller);
    }

    public void EndSuspension(InteractionController controller) {
      _suspendingController = null;

      OnSuspensionEnd(controller);
    }

    #endregion

    #region Forces

    private bool _appliedForces = false;
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

        _appliedForces = false;
      }
    }

    #endregion

    #region Colliders

    protected List<Collider> _interactionColliders = new List<Collider>();

    /// <summary>
    /// Recursively searches the hierarchy of this Interaction object to
    /// find all of the Colliders that are attached to its Rigidbody. These will
    /// be the colliders used to calculate distance from the controller to determine
    /// which object will become the primary hover.
    /// 
    /// Call this method manually if you change an Interaction object's colliders
    /// after its Start() method has been called! (Called automatically in OnEnable.)
    /// </summary>
    public void RefreshInteractionColliders() {
      Utils.FindColliders<Collider>(this.gameObject, _interactionColliders,
                                    includeInactiveObjects: false);

      _interactionColliders.RemoveAll(
        c => c.GetComponent<IgnoreColliderForInteraction>() != null);

      // Since the interaction colliders might have changed, or appeared for the first
      // time, set their layers appropriately.
      refreshInteractionColliderLayers();
    }

    #endregion

    #region Interaction Layers

    private int _lastInteractionLayer = -1;
    private int _lastNoContactLayer = -1;

    private void initLayers() {
      refreshInteractionLayer();
      refreshNoContactLayer();

      (manager as IInternalInteractionManager).NotifyIntObjAddedInteractionLayer(this, interactionLayer, false);
      (manager as IInternalInteractionManager).NotifyIntObjAddedNoContactLayer(this, noContactLayer, false);
      (manager as IInternalInteractionManager).RefreshLayersNow();

      _lastInteractionLayer = interactionLayer;
      _lastNoContactLayer = noContactLayer;
    }

    private void refreshInteractionLayer() {
      interactionLayer = overrideInteractionLayer ? this.interactionLayer
                                                  : manager.interactionLayer;
    }

    private void refreshNoContactLayer() {
      noContactLayer = overrideNoContactLayer ? this.noContactLayer
                                              : manager.interactionNoContactLayer;
    }

    private void fixedUpdateLayers() {
      using (new ProfilerSample("Interaction Behaviour: fixedUpdateLayers")) {
        int layer;
        refreshInteractionLayer();
        refreshNoContactLayer();

        // Update the object's layer based on interaction state.
        if (ignoreContact) {
          layer = noContactLayer;
        }
        else {
          if (isGrasped) {
            layer = noContactLayer;
          }
          else {
            layer = interactionLayer;
          }
        }
        if (this.gameObject.layer != layer) {
          this.gameObject.layer = layer;

          refreshInteractionColliderLayers();
        }

        // Update the manager if necessary.

        if (interactionLayer != _lastInteractionLayer) {
          (manager as IInternalInteractionManager).NotifyIntObjHasNewInteractionLayer(this, oldInteractionLayer: _lastInteractionLayer,
                                                                                            newInteractionLayer: interactionLayer);
          _lastInteractionLayer = interactionLayer;
        }

        if (noContactLayer != _lastNoContactLayer) {
          (manager as IInternalInteractionManager).NotifyIntObjHasNewNoContactLayer(this, oldNoContactLayer: _lastNoContactLayer,
                                                                                          newNoContactLayer: noContactLayer);
          _lastNoContactLayer = noContactLayer;
        }
      }
    }

    private void finalizeLayers() {
      (manager as IInternalInteractionManager).NotifyIntObjRemovedInteractionLayer(this, interactionLayer, false);
      (manager as IInternalInteractionManager).NotifyIntObjRemovedNoContactLayer(this, noContactLayer, false);
      (manager as IInternalInteractionManager).RefreshLayersNow();
    }

    /// <summary>
    /// Sets the layer state of the _interactionColliders to match the root interaction
    /// object if their layer differs from it.
    /// 
    /// This method does NOT modify the interaction object's own layer (unless the 
    /// interaction object has a collider on itself; which would result in a no-op).
    /// 
    /// This needs to be called if the layer of the interaction object changes or if the
    /// object gains new colliders.
    /// </summary>
    private void refreshInteractionColliderLayers() {
      for (int i = 0; i < _interactionColliders.Count; i++) {
        if (_interactionColliders[i].gameObject.layer != this.gameObject.layer) {
          _interactionColliders[i].gameObject.layer = this.gameObject.layer;
        }
      }
    }

    #endregion

    #region Locked Position (Joint) Checking

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
          if (joint.connectedBody == null || joint.connectedBody.isKinematic) {
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
    }

    #endregion

    #region Unity Events

    [SerializeField]
    private EnumEventTable _eventTable;

    public enum EventType {
      HoverBegin = 100,
      HoverEnd = 101,
      HoverStay = 102,
      PerControllerHoverBegin = 110,
      PerControllerHoverEnd = 111,

      PrimaryHoverBegin = 120,
      PrimaryHoverEnd = 121,
      PrimaryHoverStay = 122,
      PerControllerPrimaryHoverBegin = 130,
      PerControllerPrimaryHoverEnd = 132,

      GraspBegin = 140,
      GraspEnd = 141,
      GraspStay = 142,
      PerControllerGraspBegin = 150,
      PerControllerGraspEnd = 152,

      SuspensionBegin = 160,
      SuspensionEnd = 161,

      ContactBegin = 170,
      ContactEnd = 171,
      ContactStay = 172,
      PerControllerContactBegin = 180,
      PerControllerContactEnd = 181
    }

    private void InitUnityEvents() {
      // If the interaction component is added at runtime, _eventTable won't have been
      // constructed yet.
      if (_eventTable == null) _eventTable = new EnumEventTable();

      setupCallback(ref OnHoverBegin,                     EventType.HoverBegin);
      setupCallback(ref OnHoverEnd,                       EventType.HoverEnd);
      setupCallback(ref OnHoverStay,                      EventType.HoverStay);
      setupCallback(ref OnPerControllerHoverBegin,        EventType.PerControllerHoverBegin);
      setupCallback(ref OnPerControllerHoverEnd,          EventType.PerControllerHoverEnd);

      setupCallback(ref OnPrimaryHoverBegin,              EventType.PrimaryHoverBegin);
      setupCallback(ref OnPrimaryHoverEnd,                EventType.PrimaryHoverEnd);
      setupCallback(ref OnPrimaryHoverStay,               EventType.PrimaryHoverStay);
      setupCallback(ref OnPerControllerPrimaryHoverBegin, EventType.PerControllerPrimaryHoverBegin);
      setupCallback(ref OnPerControllerPrimaryHoverEnd,   EventType.PerControllerPrimaryHoverEnd);

      setupCallback(ref OnGraspBegin,                     EventType.GraspBegin);
      setupCallback(ref OnGraspEnd,                       EventType.GraspEnd);
      setupCallback(ref OnGraspStay,                      EventType.GraspStay);
      setupCallback(ref OnPerControllerGraspBegin,        EventType.PerControllerGraspBegin);
      setupCallback(ref OnPerControllerGraspEnd,          EventType.PerControllerGraspEnd);

      setupCallback(ref OnSuspensionBegin,                EventType.SuspensionBegin);
      setupCallback(ref OnSuspensionEnd,                  EventType.SuspensionEnd);

      setupCallback(ref OnContactBegin,                   EventType.ContactBegin);
      setupCallback(ref OnContactEnd,                     EventType.ContactEnd);
      setupCallback(ref OnContactStay,                    EventType.ContactStay);
      setupCallback(ref OnPerControllerContactBegin,      EventType.PerControllerContactBegin);
      setupCallback(ref OnPerControllerContactEnd,        EventType.PerControllerContactEnd);
    }

    private void setupCallback(ref Action action, EventType type) {
      if (_eventTable.HasUnityEvent((int)type)) {
        action += () => _eventTable.Invoke((int)type);
      }
      else {
        action += () => { };
      }
    }

    private void setupCallback<T>(ref Action<T> action, EventType type) {
      if (_eventTable.HasUnityEvent((int)type)) {
        action += (h) => _eventTable.Invoke((int)type);
      }
      else {
        action += (h) => { };
      }
    }

    #endregion

  }

}
