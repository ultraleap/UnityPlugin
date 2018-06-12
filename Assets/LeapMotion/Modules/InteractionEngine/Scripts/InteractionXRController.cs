/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Attributes;

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
#else
using UnityEngine.VR;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Query;
using Leap.Unity.Space;
using UnityEngine.Serialization;

namespace Leap.Unity.Interaction {

  [DisallowMultipleComponent]
  public class InteractionXRController : InteractionController {

    #region Inspector

    [Header("Controller Configuration")]

    [Tooltip("Read-only. InteractionXRControllers use Unity's built-in XRNode tracking "
           + "API to receive tracking data for XR controllers by default. If you add a "
           + "custom script to set this controller's trackingProvider to something "
           + "other than the DefaultXRNodeTrackingProvider, the change will be reflected "
           + "here. (Hint: Use the ExecuteInEditMode attribute.)")]
    [Disable, SerializeField]
    #pragma warning disable 0414
    private string _trackingProviderType = "DefaultXRNodeTrackingProvider";
#pragma warning restore 0414
    public bool isUsingCustomTracking {
      get { return !(trackingProvider is DefaultXRNodeTrackingProvider); }
    }

    [Tooltip("If this string is not empty and does not match a controller in "
           + "Input.GetJoystickNames(), then this game object will disable itself.")]
    [SerializeField, EditTimeOnly]
    [FormerlySerializedAs("_deviceString")]
    private string _deviceJoystickTokens = "oculus touch right"; // or, e.g., "openvr controller right"
    public string deviceJoystickTokens { get { return _deviceJoystickTokens; } }

    [Tooltip("Which hand will hold this controller? This property cannot be changed "
           + "at runtime.")]
    [SerializeField, EditTimeOnly]
    private Chirality _chirality;
    public Chirality chirality { get { return _chirality; } }

    [Tooltip("Whether to continuously poll attached joystick data for a joystick that "
           + "matches the device joystick tokens, using Input.GetJoystickNames(). This "
           + "call allocates garbage, so be wary of setting a low polling interval.")]
    [SerializeField, OnEditorChange("pollConnection")]
    private bool _pollConnection = true;
    /// <summary>
    /// Whether to continuously poll attached joystick data for a joystick that
    /// matches the device joystick tokens, using Input.GetJoystickNames(). This
    /// call allocates garbage, so be wary of setting a low polling interval.
    /// 
    /// The connection is polled only until a joystick is detected to minimize allocation.
    /// Once a joystick has been detected (isJoystickDetected), you must manually call
    /// RefreshControllerConnection() to check if the joystick is no longer detected.
    /// </summary>
    public bool pollConnection {
      get { return _pollConnection; }
      set { _pollConnection = value; }
    }
    [Tooltip("The period in seconds at which to check if the controller is connected "
           + "by calling Input.GetJoystickNames(). This call allocates garbage, so be "
           + "wary of setting a low polling interval.")]
    [MinValue(0f)]
    [DisableIf("_pollConnection", isEqualTo: false)]
    public float pollConnectionInterval = 2f;

    [Header("Hover Configuration")]

    [Tooltip("This is the point used to determine the distance to objects for the "
           + "purposes of their 'hovered' state. Generally, it should be somewhere "
           + "between the tip of the controller and the controller's center of mass.")]
    [SerializeField]
    private Transform _hoverPoint;

    [Tooltip("These points refine the hover point when determining distances to "
           + "interaction objects for evaluating which object should be the primary hover "
           + "of this interaction controller. An object's proximity to one of these "
           + "points is interpreted as the user's intention to interact specifically "
           + "with that object, and is important when building less accident-prone user "
           + "interfaces. For example, hands place their primary hover points on the "
           + "thumb, index finger, and middle finger by default. Controllers generally "
           + "should have a primary hover point at any tip of the controller you expect "
           + "users might use to hit a button. Warning: Each point costs distance checks "
           + "against nearby objects, so making this list large is costly!")]
    [SerializeField]
    public new List<Transform> primaryHoverPoints;

    [Header("Grasping Configuration")]

    [Tooltip("The point around which to check objects eligible for being grasped. Only "
          + "objects with an InteractionBehaviour component with ignoreGrasping disabled "
          + "are eligible for grasping. Upon attempting to grasp with a controller, the "
          + "object closest to the grasp point is chosen for grasping.")]
    public Transform graspPoint;

    public float maxGraspDistance = 0.06F;

    [Tooltip("This string should match an Axis specified in Edit->Project Settings->"
           + "Input. This is the button to use to listen for grasping.")]
    public string graspButtonAxis;

    [Tooltip("The duration of time in seconds beyond initially pressing the grasp button "
           + "that the user can move the grasp point within range of a graspable "
           + "interaction object and still trigger a grasp. With a value of zero, objects "
           + "can only be grasped if they are already within the grasp distance of the "
           + "grasp point.")]
    public float graspTimingSlop = 0.10F;

    [Header("Enable/Disable GameObjects with Tracking State")]
    
    [Tooltip("These objects will be made active only while the controller is tracked. "
           + "For more fine-tuned behavior, we recommend implementing your own logic. "
           + "controller.isJoystickDetected and controller.isTracked are useful for this.")]
    [SerializeField]
    private List<GameObject> _enableObjectsOnlyWhenTracked;
    /// <summary>
    /// These objects will be made active only while the controller is tracked. For more
    /// fine-tuned behavior, we recommend implementing your own logic.
    /// 
    /// controller.isJoystickDetected and controller.isTracked are useful for this.
    /// </summary>
    public List<GameObject> enableObjectsOnlyWhenTracked {
      get {
        if (_enableObjectsOnlyWhenTracked == null) {
          _enableObjectsOnlyWhenTracked = new List<GameObject>();
        }
        return _enableObjectsOnlyWhenTracked;
      }
    }

    #endregion // Inspector

    #region Unity Events

    protected override void Reset() {
      base.Reset();

      hoverEnabled = true;
      contactEnabled = true;
      graspingEnabled = true;

      trackingProvider = _defaultTrackingProvider;
      _hoverPoint = null;
      primaryHoverPoints.Clear();
      graspPoint = null;

      maxGraspDistance = 0.06F;
      graspTimingSlop = 0.1F;
    }

    protected virtual void OnValidate() {
      _trackingProviderType = trackingProvider.GetType().ToString();
    }

    protected override void Start() {
      base.Start();

      trackingProvider.OnTrackingDataUpdate += refreshControllerTrackingData;
    }

    protected override void fixedUpdateController() {
      fixedUpdatePollConnection();

      if (isTracked) {
        foreach (var gameObject in enableObjectsOnlyWhenTracked) {
          gameObject.SetActive(true);
        }
      }
      else {
        foreach (var gameObject in enableObjectsOnlyWhenTracked) {
          gameObject.SetActive(false);
        }
      }

      refreshContactBoneTargets();
    }

    #endregion

    #region Controller Connection Polling

    private bool _isJoystickDetected = false;
    /// <summary>
    /// Whether the device joystick tokens matched an entry in Input.GetJoystickNames().
    /// If pollConnection is set to true, this status is refreshed periodically based on
    /// the pollConnectionInterval, but only while the joystick tokens have not been
    /// detected from Input.GetJoystickNames(). Call RefreshControllerConnection() to
    /// detect if the controller has been disconnected.
    /// 
    /// Joystick detection is skipped if deviceJoystickTokens is null or empty, causing
    /// this check to always return true.
    /// </summary>
    public bool isJoystickDetected {
      get { return string.IsNullOrEmpty(deviceJoystickTokens) || _isJoystickDetected; }
    }

    private float _pollTimer = 0f;

    private void fixedUpdatePollConnection() {
      if (_pollConnection && !_isJoystickDetected) {
        _pollTimer += Time.fixedDeltaTime;
      }

      if (_pollConnection && _pollTimer >= pollConnectionInterval) {
        _pollTimer = 0f;

        RefreshControllerConnection();
      }
    }

    public void RefreshControllerConnection() {
      _isJoystickDetected = true; // deviceJoystickTokens must be parsible to falsify.

      if (deviceJoystickTokens != null && deviceJoystickTokens.Length > 0) {
        string[] joysticksConnected = Input.GetJoystickNames().Query()
          .Select(s => s.ToLower()).ToArray();
        string[] controllerSupportTokens = deviceJoystickTokens.ToLower()
          .Split(" ".ToCharArray());
        bool matchesController = joysticksConnected.Query()
                                 .Any(joystick => controllerSupportTokens.Query()
                                                 .All(token => joystick.Contains(token)));

        _isJoystickDetected = matchesController;
      }
    }

    #endregion

    #region Controller Tracking

    private bool _hasTrackedPositionLastFrame = false;
    private Vector3 _trackedPositionLastFrame = Vector3.zero;
    private Quaternion _trackedRotationLastFrame = Quaternion.identity;

    private IXRControllerTrackingProvider _backingTrackingProvider = null;
    public IXRControllerTrackingProvider trackingProvider {
      get {
        if (_backingDefaultTrackingProvider == null) {
          _backingDefaultTrackingProvider = _defaultTrackingProvider;
        }

        return _backingDefaultTrackingProvider;
      }
      set {
        if (_backingTrackingProvider != null) {
          _backingTrackingProvider.OnTrackingDataUpdate -= refreshControllerTrackingData;
        }

        _backingTrackingProvider = value;

        if (_backingTrackingProvider != null) {
          _backingTrackingProvider.OnTrackingDataUpdate += refreshControllerTrackingData;
        }
      }
    }

    private IXRControllerTrackingProvider _backingDefaultTrackingProvider;
    private IXRControllerTrackingProvider _defaultTrackingProvider {
      get {
        if (_backingDefaultTrackingProvider == null) {
          refreshDefaultTrackingProvider();
        }

        return _backingDefaultTrackingProvider;
      }
      set {
        _backingDefaultTrackingProvider = value;
      }
    }

    private void refreshDefaultTrackingProvider() {
      var defaultProvider = gameObject.GetComponent<DefaultXRNodeTrackingProvider>();
      if (defaultProvider == null) {
        defaultProvider = gameObject.AddComponent<DefaultXRNodeTrackingProvider>();
      }
      defaultProvider.xrNode = this.xrNode;

      _defaultTrackingProvider = defaultProvider;
    }

    private void refreshControllerTrackingData(Vector3 position, Quaternion rotation) {
      refreshIsBeingMoved(position, rotation);

      if (_hasTrackedPositionLastFrame) {
        _trackedPositionLastFrame = this.transform.position;
        _trackedRotationLastFrame = this.transform.rotation;
      }

      this.transform.position = position;
      this.transform.rotation = rotation;
      refreshContactBoneTargets();

      if (!_hasTrackedPositionLastFrame) {
        _hasTrackedPositionLastFrame = true;
        _trackedPositionLastFrame = this.transform.position;
        _trackedRotationLastFrame = this.transform.rotation;
      }
    }

    #endregion

    #region Movement Detection

    private const float RIG_LOCAL_MOVEMENT_SPEED_THRESHOLD = 00.07F;
    private const float RIG_LOCAL_MOVEMENT_SPEED_THRESHOLD_SQR
      = RIG_LOCAL_MOVEMENT_SPEED_THRESHOLD * RIG_LOCAL_MOVEMENT_SPEED_THRESHOLD;
    private const float RIG_LOCAL_ROTATION_SPEED_THRESHOLD = 10.00F;
    private const float BEING_MOVED_TIMEOUT = 0.5F;

    private float _lastTimeMoved = 0F;
    private bool  _isBeingMoved = false;
    private void refreshIsBeingMoved(Vector3 position, Quaternion rotation) {
      var isMoving = false;
      var baseTransform = this.manager.transform;

      // Check translation speed, relative to the Interaction Manager.
      var baseLocalPos = baseTransform.InverseTransformPoint(position);
      var baseLocalPosLastFrame =baseTransform.InverseTransformPoint(
        _trackedPositionLastFrame);
      var baseLocalSqrSpeed = ((baseLocalPos - baseLocalPosLastFrame)
        / Time.fixedDeltaTime).sqrMagnitude;
      if (baseLocalSqrSpeed > RIG_LOCAL_MOVEMENT_SPEED_THRESHOLD_SQR) {
        isMoving = true;
      }

      // Check rotation speed, relative to the Interaction Manager.
      var baseLocalRot = baseTransform.InverseTransformRotation(rotation);
      var baseLocalRotLastFrame = baseTransform.InverseTransformRotation(
        _trackedRotationLastFrame);
      var baseLocalAngularSpeed = Quaternion.Angle(baseLocalRot, baseLocalRotLastFrame)
        / Time.fixedDeltaTime;
      if (baseLocalAngularSpeed > RIG_LOCAL_ROTATION_SPEED_THRESHOLD) {
        isMoving = true;
      }

      if (isMoving) {
        _lastTimeMoved = Time.fixedTime;
      }

      // "isMoving" lasts for a bit after the controller stops moving, to avoid
      // rapid oscillation of the value.
      var timeSinceLastMoving = Time.fixedTime - _lastTimeMoved;
      _isBeingMoved = trackingProvider != null && trackingProvider.isTracked
        && timeSinceLastMoving < BEING_MOVED_TIMEOUT;
    }

    #endregion

    #region General InteractionController Implementation

    /// <summary>
    /// Gets whether or not the underlying controller is currently tracked and any
    /// joystick token filtering has confirmed that this controller has been detected as
    /// a connected joystick.
    /// </summary>
    public override bool isTracked {
      get {
        return isJoystickDetected && trackingProvider != null && trackingProvider.isTracked;
      }
    }

    /// <summary>
    /// Gets whether or not the underlying controller is currently being moved in world
    /// space, but relative to the Interaction Manager's transform. The Interaction
    /// Manager is usually a sibling of the main camera beneath the camera rig transform,
    /// so that if your application is only translating the player rig in space, this
    /// method won't incorrectly return true.
    /// </summary>
    public override bool isBeingMoved {
      get {
        return _isBeingMoved;
      }
    }

    /// <summary>
    /// Gets the XRNode associated with this XR controller. Note: If the tracking mode 
    /// for this controller is specified as ControllerTrackingMode.Custom, this value
    /// may be ignored.
    /// </summary>
    /// 
    #if UNITY_2017_2_OR_NEWER
    public XRNode xrNode {
      get { return chirality == Chirality.Left ? XRNode.LeftHand : XRNode.RightHand; }
    }
    #else
    public VRNode xrNode {
      get { return chirality == Chirality.Left ? VRNode.LeftHand : VRNode.RightHand; }
    }
    #endif

    /// <summary>
    /// Gets whether the controller is a left-hand controller.
    /// </summary>
    public override bool isLeft {
      get { return chirality == Chirality.Left; }
    }

    /// <summary>
    /// Gets the last-tracked position of the controller.
    /// </summary>
    public override Vector3 position {
      get {
        return this.transform.position;
      }
    }

    /// <summary>
    /// Gets the last-tracked rotation of the controller.
    /// </summary>
    public override Quaternion rotation {
      get {
        return this.transform.rotation;
      }
    }

    /// <summary>
    /// Gets the current velocity of the controller.
    /// </summary>
    public override Vector3 velocity {
      get {
        if (_hasTrackedPositionLastFrame) {
          return (this.transform.position - _trackedPositionLastFrame) / Time.fixedDeltaTime;
        }
        else {
          return Vector3.zero;
        }
      }
    }

    /// <summary>
    /// Gets the type of controller this is. For InteractionVRController, the type is
    /// always ControllerType.VRController.
    /// </summary>
    public override ControllerType controllerType {
      get { return ControllerType.XRController; }
    }

    /// <summary>
    /// This implementation of InteractionControllerBase does not represent a Leap hand,
    /// so it need not return an InteractionHand object.
    /// </summary>
    public override InteractionHand intHand {
      get { return null; }
    }

    /// <summary>
    /// InteractionVRController doesn't need to do anything when an object is
    /// unregistered.
    /// </summary>
    protected override void onObjectUnregistered(IInteractionBehaviour intObj) { }

    #endregion

    #region Hover Implementation

    /// <summary>
    /// Gets the center point used for hover distance checking.
    /// </summary>
    public override Vector3 hoverPoint {
      get { return _hoverPoint == null ? Vector3.zero : _hoverPoint.position; }
    }

    /// <summary>
    /// Gets the list of points to be used to perform higher-fidelity "primary hover"
    /// checks. Only one interaction object may be the primary hover of an interaction
    /// controller (Leap hand or otherwise) at a time. Interface objects such as buttons
    /// can only be pressed when they are primarily hovered by an interaction controller,
    /// so it's best to return points on whatever you expect to be able to use to push
    /// buttons with the controller.
    /// </summary>
    protected override List<Transform> _primaryHoverPoints {
      get { return primaryHoverPoints; }
    }

    private Vector3    _pivotingPositionOffset  = Vector3.zero;
    private Vector3    _unwarpingPositionOffset = Vector3.zero;
    private Quaternion _unwarpingRotationOffset = Quaternion.identity;

    protected override void unwarpColliders(Transform primaryHoverPoint, ISpaceComponent warpedSpaceElement) {
      // Extension method calculates "unwarped" pose in world space.
      Vector3    unwarpedPosition;
      Quaternion unwarpedRotation;
      warpedSpaceElement.anchor.transformer.WorldSpaceUnwarp(primaryHoverPoint.position, 
                                                             primaryHoverPoint.rotation,
                                                             out unwarpedPosition,
                                                             out unwarpedRotation);

      // Shift the controller to have its origin on the primary hover point so that
      // rotations applied to the hand cause it to pivot around that point, then apply
      // the position and rotation transformation.
      _pivotingPositionOffset  = -primaryHoverPoint.position;
      _unwarpingPositionOffset = unwarpedPosition;
      _unwarpingRotationOffset = unwarpedRotation * Quaternion.Inverse(primaryHoverPoint.rotation);

      refreshContactBoneTargets(useUnwarpingData: true);
    }

    #endregion

    #region Contact Implementation

    private Vector3[] _contactBoneLocalPositions;
    private Quaternion[] _contactBoneLocalRotations;

    private Vector3[] _contactBoneTargetPositions;
    private Quaternion[] _contactBoneTargetRotations;

    private ContactBone[] _contactBones;
    public override ContactBone[] contactBones {
      get { return _contactBones; }
    }

    private GameObject _contactBoneParent;
    protected override GameObject contactBoneParent {
      get { return _contactBoneParent; }
    }

    protected override bool initContact() {
      initContactBones();

      if (_contactBoneParent == null) {
        _contactBoneParent = new GameObject("VR Controller Contact Bones "
                                          + (isLeft ? "(Left)" : "(Right"));
      }

      foreach (var contactBone in _contactBones) {
        contactBone.transform.parent = _contactBoneParent.transform;
      }

      return true;
    }

    private void refreshContactBoneTargets(bool useUnwarpingData = false) {
      if (_wasContactInitialized) {

        // Move the controller transform temporarily into its "unwarped space" pose
        // (only if we are using the controller in a curved space)
        if (useUnwarpingData) {
          moveControllerTransform(_pivotingPositionOffset, Quaternion.identity);
          moveControllerTransform(_unwarpingPositionOffset, _unwarpingRotationOffset);
        }

        for (int i = 0; i < _contactBones.Length; i++) {
          _contactBoneTargetPositions[i]
            = this.transform.TransformPoint(_contactBoneLocalPositions[i]);
          _contactBoneTargetRotations[i]
            = this.transform.TransformRotation(_contactBoneLocalRotations[i]);
        }

        // Move the controller transform back to its original pose.
        if (useUnwarpingData) {
          moveControllerTransform(-_unwarpingPositionOffset, Quaternion.Inverse(_unwarpingRotationOffset));
          moveControllerTransform(-_pivotingPositionOffset, Quaternion.identity);
        }
      }
    }

    private void moveControllerTransform(Vector3 deltaPosition, Quaternion deltaRotation) {
      this.transform.rotation = deltaRotation * this.transform.rotation;
      this.transform.position = deltaPosition + this.transform.position;
    }

    private List<ContactBone> _contactBoneBuffer = new List<ContactBone>();
    private List<Collider> _colliderBuffer = new List<Collider>();
    private void initContactBones() {
      _colliderBuffer.Clear();
      _contactBoneBuffer.Clear();

      // Scan for existing colliders and construct contact bones out of them.
      Utils.FindColliders<Collider>(this.gameObject, _colliderBuffer,
        includeInactiveObjects: true);

      foreach (var collider in _colliderBuffer) {
        if (collider.isTrigger) continue; // Contact Bones are for "contacting" colliders.

        ContactBone contactBone = collider.gameObject.AddComponent<ContactBone>();
        Rigidbody body = collider.gameObject.GetComponent<Rigidbody>();
        if (body == null) {
          body = collider.gameObject.AddComponent<Rigidbody>();
        }

        body.freezeRotation = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.mass = 1F;

        contactBone.interactionController = this;
        contactBone.rigidbody = body;
        contactBone.collider = collider;

        _contactBoneBuffer.Add(contactBone);
      }
      
      int numBones = _colliderBuffer.Count;
      _contactBones = new ContactBone[numBones];
      _contactBoneLocalPositions = new Vector3[numBones];
      _contactBoneLocalRotations = new Quaternion[numBones];
      _contactBoneTargetPositions = new Vector3[numBones];
      _contactBoneTargetRotations = new Quaternion[numBones];
      for (int i = 0; i < numBones; i++) {
        _contactBones[i] = _contactBoneBuffer[i];

        _contactBoneLocalPositions[i]
          = _contactBoneTargetPositions[i]
          = this.transform.InverseTransformPoint(_contactBones[i].transform.position);
        _contactBoneLocalRotations[i]
          = _contactBoneTargetRotations[i]
          = this.transform.InverseTransformRotation(_contactBones[i].transform.rotation);
      }
    }

    protected override void getColliderBoneTargetPositionRotation(int contactBoneIndex,
                                                                  out Vector3 targetPosition,
                                                                  out Quaternion targetRotation) {
      targetPosition = _contactBoneTargetPositions[contactBoneIndex];
      targetRotation = _contactBoneTargetRotations[contactBoneIndex];
    }

    #endregion

    #region Grasping Implementation

    /// <summary>
    /// By default, InteractionVRController uses Input.GetAxis(graspButtonAxis) to
    /// determine the "depression" state for the grasp button. By setting this value to
    /// something other than null, it is possible to modify this behavior to instead
    /// retrieve a grasping axis value based on arbitrary code.
    /// 
    /// A grasp is attempted when the grasp button axis value returned by this method
    /// becomes larger than the graspButtonDepressedValue, and a grasp is released when
    /// the grasp button axis value returned by this method becomes smaller than the
    /// graspButtonReleasedValue. Both of these values provide public setters.
    /// </summary>
    public Func<float> graspAxisOverride = null;

    private float _graspDepressedValue = 0.8F;
    /// <summary>
    /// The value between 0 and 1 past which the grasping axis value will cause an
    /// attempt to grasp a graspable interaction object near the grasp point.
    /// </summary>
    public float graspDepressedValue {
      get { return _graspDepressedValue; }
      set { _graspDepressedValue = value; }
    }

    private float _graspReleasedValue = 0.7F;
    /// <summary>
    /// If the grasping axis value passes the graspDepressedValue, it must then drop
    /// underneath this value in order to release the grasp attempt (potentially
    /// releasing a held object) and allow a new grasp attempt to occur.
    /// </summary>
    public float graspReleasedValue {
      get { return _graspReleasedValue; }
      set { _graspReleasedValue = value; }
    }

    private List<Vector3> _graspManipulatorPointsBuffer = new List<Vector3>();
    /// <summary>
    /// Gets a list returning this controller's hoverPoint. Because the
    /// InteractionVRController represents a rigid controller, any two points that
    /// rigidly move with the controller position and orientation will provide enough
    /// information.
    /// </summary>
    public override List<Vector3> graspManipulatorPoints {
      get {
        _graspManipulatorPointsBuffer.Clear();
        _graspManipulatorPointsBuffer.Add(hoverPoint);
        _graspManipulatorPointsBuffer.Add(hoverPoint + this.transform.rotation * Vector3.forward * 0.05F);
        _graspManipulatorPointsBuffer.Add(hoverPoint + this.transform.rotation * Vector3.right   * 0.05F);
        return _graspManipulatorPointsBuffer;
      }
    }

    private IInteractionBehaviour _closestGraspableObject = null;

    private bool _graspButtonLastFrame = false;
    private bool _graspButtonDown = false;
    private bool _graspButtonUp = false;
    private float _graspButtonDownSlopTimer = 0F;

    public override Vector3 GetGraspPoint() {
      return graspPoint.transform.position;
    }

    protected override void fixedUpdateGraspingState() {
      refreshClosestGraspableObject();

      fixedUpdateGraspButtonState();
    }

    private void refreshClosestGraspableObject() {
      _closestGraspableObject = null;

      float closestGraspableDistance = float.PositiveInfinity;
      foreach (var intObj in graspCandidates) {
        float testDist = intObj.GetHoverDistance(this.graspPoint.position);
        if (testDist < maxGraspDistance && testDist < closestGraspableDistance) {
          _closestGraspableObject = intObj;
          closestGraspableDistance = testDist;
        }
      }
    }

    private void fixedUpdateGraspButtonState(bool ignoreTemporal = false) {
      _graspButtonDown = false;
      _graspButtonUp = false;

      bool graspButton = _graspButtonLastFrame;

      if (!_graspButtonLastFrame) {
        if (graspAxisOverride == null) {
          try {
            graspButton = Input.GetAxis(graspButtonAxis) > graspDepressedValue;
          } catch {
            Debug.LogError("INPUT AXIS NOT SET UP.  Go to your Input Manager and add a definition for " + graspButtonAxis + " on the " + (isLeft ? "9" : "10") + "th Joystick Axis.");
            graspButton = Input.GetKey(isLeft ? KeyCode.JoystickButton14: KeyCode.JoystickButton15);
          }
        }
        else {
          graspButton = graspAxisOverride() > graspDepressedValue;
        }

        if (graspButton) {
          // Grasp button was _just_ depressed this frame.
          _graspButtonDown = true;
          _graspButtonDownSlopTimer = graspTimingSlop;
        }
      }
      else {
        if (graspReleasedValue > graspDepressedValue) {
          Debug.LogWarning("The graspReleasedValue should be less than or equal to the "
                         + "graspDepressedValue!", this);
          graspReleasedValue = graspDepressedValue;
        }

        if (graspAxisOverride == null) {
          try {
            graspButton = Input.GetAxis(graspButtonAxis) > graspDepressedValue;
          } catch {
            Debug.LogError("INPUT AXIS NOT SET UP.  Go to your Input Manager and add a definition for " + graspButtonAxis + " on the " + (isLeft ? "9" : "10") + "th Joystick Axis.");
            graspButton = Input.GetKey(isLeft ? KeyCode.JoystickButton14 : KeyCode.JoystickButton15);
          }
        }
        else {
          graspButton = graspAxisOverride() > graspReleasedValue;
        }

        if (!graspButton) {
          // Grasp button was _just_ released this frame.
          _graspButtonUp = true;
          _graspButtonDownSlopTimer = 0F;
        }
      }

      if (_graspButtonDownSlopTimer > 0F) {
        _graspButtonDownSlopTimer -= Time.fixedDeltaTime;
      }
      
      _graspButtonLastFrame = graspButton;
    }

    protected override bool checkShouldGrasp(out IInteractionBehaviour objectToGrasp) {
      bool shouldGrasp = !isGraspingObject
                         && (_graspButtonDown || _graspButtonDownSlopTimer > 0F)
                         && _closestGraspableObject != null;

      objectToGrasp = null;
      if (shouldGrasp) { objectToGrasp = _closestGraspableObject; }

      return shouldGrasp;
    }

    /// <summary>
    /// If the provided object is within range of this VR controller's grasp point and
    /// the grasp button is currently held down, this method will manually initiate a
    /// grasp and return true. Otherwise, the method returns false.
    /// </summary>
    protected override bool checkShouldGraspAtemporal(IInteractionBehaviour intObj) {
      bool shouldGrasp = !isGraspingObject
                         && _graspButtonLastFrame
                         && intObj.GetHoverDistance(graspPoint.position) < maxGraspDistance;
      if (shouldGrasp) {
        var tempControllers = Pool<List<InteractionController>>.Spawn();
        try {
          intObj.BeginGrasp(tempControllers);
        }
        finally {
          tempControllers.Clear();
          Pool<List<InteractionController>>.Recycle(tempControllers);
        }
      }

      return shouldGrasp;
    }

    protected override bool checkShouldRelease(out IInteractionBehaviour objectToRelease) {
      bool shouldRelease = _graspButtonUp && isGraspingObject;

      objectToRelease = null;
      if (shouldRelease) { objectToRelease = graspedObject; }

      return shouldRelease;
    }

    #endregion

    #region Gizmos

    public override void OnDrawRuntimeGizmos(RuntimeGizmos.RuntimeGizmoDrawer drawer) {
      base.OnDrawRuntimeGizmos(drawer);

      // Grasp Point
      float graspAmount = 0F;
      if (graspAxisOverride != null) graspAmount = graspAxisOverride();
      else {
        try {
          graspAmount = Input.GetAxis(graspButtonAxis);
        }
        catch (ArgumentException) { }
      }

      drawer.color = Color.Lerp(GizmoColors.GraspPoint, Color.white, graspAmount);
      drawer.DrawWireSphere(GetGraspPoint(), maxGraspDistance);

      // Nearest graspable object
      if (_closestGraspableObject != null) {
        drawer.color = Color.Lerp(GizmoColors.Graspable, Color.white, Mathf.Sin(Time.time * 2 * Mathf.PI * 2F));
        drawer.DrawWireSphere(_closestGraspableObject.rigidbody.position, maxGraspDistance * 0.75F);
      }
    }

    #endregion

  }

}
