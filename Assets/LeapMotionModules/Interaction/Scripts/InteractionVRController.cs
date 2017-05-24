using Leap.Unity.Attributes;
using Leap.Unity.Interaction.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

namespace Leap.Unity.Interaction {

  [DisallowMultipleComponent]
  public class InteractionVRController : InteractionController {

    [Header("Controller Configuration")]

    [Tooltip("Read-only. InteractionVRControllers use Unity's built-in VRNode tracking "
           + "API to receive tracking data for VR controllers by default. If you add a "
           + "custom script to set this controller's trackingProvider to something "
           + "other than the DefaultVRNodeTrackingProvider, the change will be reflected "
           + "here. (Hint: Use the ExecuteInEditMode attribute.)")]
    [Disable, SerializeField]
    #pragma warning disable 0414
    private string _trackingProviderType = "DefaultVRNodeTrackingProvider";
    #pragma warning restore 0414
    public bool isUsingCustomTracking {
      get { return !(trackingProvider is DefaultVRNodeTrackingProvider); }
    }

    [Tooltip("Which hand will hold this controller? This property cannot be changed "
           + "at runtime.")]
    [SerializeField, EditTimeOnly]
    private Chirality _chirality;
    public Chirality chirality { get { return _chirality; } }

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

    private bool _hasTrackedPositionLastFrame = false;
    private Vector3 _trackedPositionLastFrame = Vector3.zero;

    private IVRControllerTrackingProvider _backingTrackingProvider = null;
    public IVRControllerTrackingProvider trackingProvider {
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

    private IVRControllerTrackingProvider _backingDefaultTrackingProvider;
    private IVRControllerTrackingProvider _defaultTrackingProvider {
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
      var defaultProvider = gameObject.GetComponent<DefaultVRNodeTrackingProvider>();
      if (defaultProvider == null) {
        defaultProvider = gameObject.AddComponent<DefaultVRNodeTrackingProvider>();
      }
      defaultProvider.vrNode = this.vrNode;

      _defaultTrackingProvider = defaultProvider;
    }

    protected virtual void OnValidate() {
      _trackingProviderType = trackingProvider.GetType().ToString();
    }

    protected override void Start() {
      base.Start();

      trackingProvider.OnTrackingDataUpdate += refreshControllerTrackingData;
    }

    protected virtual void Reset() {

    }

    private void refreshControllerTrackingData(Vector3 position, Quaternion rotation) {
      if (_hasTrackedPositionLastFrame) {
        _trackedPositionLastFrame = this.transform.position;
      }

      this.transform.position = position;
      this.transform.rotation = rotation;
      refreshContactBoneTargets();

      if (!_hasTrackedPositionLastFrame) {
        _hasTrackedPositionLastFrame = true;
        _trackedPositionLastFrame = this.transform.position;
      }
    }

    #region General InteractionController Implementation

    /// <summary>
    /// Gets whether or not the underlying controller is currently tracked.
    /// </summary>
    public override bool isTracked {
      get {
        return trackingProvider != null && trackingProvider.isTracked;
      }
    }

    /// <summary>
    /// Gets the VRNode associated with this VR controller. Note: If the tracking mode 
    /// for this controller is specified as ControllerTrackingMode.Custom, this value
    /// may be ignored.
    /// </summary>
    public VRNode vrNode {
      get { return chirality == Chirality.Left ? VRNode.LeftHand : VRNode.RightHand; }
    }

    /// <summary>
    /// Gets whether the controller is a left-hand controller.
    /// </summary>
    public override bool isLeft {
      get { return chirality == Chirality.Left; }
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
      get { return ControllerType.VRController; }
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

    // TODO: Implement me to support curved spaces
    protected override void unwarpColliders(Transform primaryHoverPoint, Space.ISpaceComponent warpedSpaceElement) {
      throw new System.NotImplementedException();
    }

    #endregion

    #region Contact Implementation

    private Vector3[] _contactBoneLocalPositions;
    private Quaternion[] _contactBoneLocalRotations;

    private Vector3[] _contactBoneTargetPositions;
    private Quaternion[] _contactBoneTargetRotations;

    private ContactBone[] _contactBones;
    protected override ContactBone[] contactBones {
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

    private void refreshContactBoneTargets() {
      if (_wasContactInitialized) {
        for (int i = 0; i < _contactBones.Length; i++) {
          _contactBoneTargetPositions[i]
            = this.transform.TransformPoint(_contactBoneLocalPositions[i]);
          _contactBoneTargetRotations[i]
            = this.transform.TransformRotation(_contactBoneLocalRotations[i]);
        }
      }
    }

    private List<ContactBone> _contactBoneBuffer = new List<ContactBone>();
    private List<Collider> _colliderBuffer = new List<Collider>();
    private void initContactBones() {
      _colliderBuffer.Clear();
      _contactBoneBuffer.Clear();

      // Scan for existing colliders and construct contact bones out of them.
      Utils.FindColliders<Collider>(this.gameObject, ref _colliderBuffer);

      foreach (var collider in _colliderBuffer) {
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
        contactBone.body = body;
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
    public Func<float> graspingAxisOverride = null;

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

    private void fixedUpdateGraspButtonState() {
      _graspButtonDown = false;
      _graspButtonUp = false;

      bool graspButton = _graspButtonLastFrame;

      if (!_graspButtonLastFrame) {
        if (graspingAxisOverride == null) {
          graspButton = Input.GetAxis(graspButtonAxis) > graspDepressedValue;
        }
        else {
          graspButton = graspingAxisOverride() > graspDepressedValue;
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

        if (graspingAxisOverride == null) {
          graspButton = Input.GetAxis(graspButtonAxis) > graspReleasedValue;
        }
        else {
          graspButton = graspingAxisOverride() > graspReleasedValue;
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
      float graspAmount;
      if (graspingAxisOverride != null) graspAmount = graspingAxisOverride();
      else graspAmount = Input.GetAxis(graspButtonAxis);

      drawer.color = Color.Lerp(Color.blue, Color.white, graspAmount);
      drawer.DrawWireSphere(GetGraspPoint(), maxGraspDistance);

      // Nearest graspable object
      if (_closestGraspableObject != null) {
        drawer.color = Color.Lerp(Color.red, Color.white, Mathf.Sin(Time.time * 2 * Mathf.PI));
        drawer.DrawWireSphere(_closestGraspableObject.rigidbody.position, maxGraspDistance * 0.5F);
      }
    }

    #endregion

  }

}
