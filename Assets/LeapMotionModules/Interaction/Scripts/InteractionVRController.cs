using Leap.Unity.Interaction.Internal;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

namespace Leap.Unity.Interaction {

  public class InteractionVRController : InteractionController {

    public Chirality chirality;

    [SerializeField]
    private Transform _hoverPoint;

    public new List<Transform> primaryHoverPoints;

    public string graspButtonAxis;

    public Transform graspPoint;

    public float maxGraspDistance = 0.06F;

    private Vector3 _trackedPosition = Vector3.zero;

    private bool _hasTrackedPositionLastFrame = false;
    private Vector3 _trackedPositionLastFrame = Vector3.zero;

    protected virtual void Update() {
      _trackedPosition = InputTracking.GetLocalPosition(vrNode);
      this.transform.position = _trackedPosition;
      this.transform.rotation = InputTracking.GetLocalRotation(vrNode);

      refreshContactBoneTargets();
    }

    #region General InteractionController Implementation

    /// <summary>
    /// Gets whether or not the underlying controller is currently tracked.
    /// </summary>
    public override bool isTracked {
      get {
        // Unfortunately, the only alternative to checking the controller's position for
        // whether or not it is tracked is to request the _allocated string array_ of
        // all currently-connected joysticks, which would allocate garbage every frame,
        // so it's unusable.
        return _trackedPosition != Vector3.zero;
      }
    }

    /// <summary>
    /// Gets the VRNode associated with this VR controller.
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
          return (_trackedPosition - _trackedPositionLastFrame) / Time.fixedDeltaTime;
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

      _contactBoneParent = new GameObject("VR Controller Contact Bones");
      _contactBoneParent.transform.parent = this.transform;

      foreach (var contactBone in _contactBones) {
        contactBone.transform.parent = _contactBoneParent.transform;
      }

      return true;
    }

    private void refreshContactBoneTargets() {
      for (int i = 0; i < _contactBones.Length; i++) {
        _contactBoneTargetPositions[i]
          = this.transform.TransformPoint(_contactBoneLocalPositions[i]);
        _contactBoneTargetRotations[i]
          = this.transform.TransformRotation(_contactBoneLocalRotations[i]);
      }
    }

    private List<ContactBone> _contactBoneBuffer = new List<ContactBone>();
    private List<CapsuleCollider> _colliderBuffer = new List<CapsuleCollider>();
    private void initContactBones() {
      _colliderBuffer.Clear();
      _contactBoneBuffer.Clear();

      // Scan for existing colliders and construct contact bones out of them.
      Utils.FindColliders<CapsuleCollider>(this.gameObject, ref _colliderBuffer);

      foreach (var capsule in _colliderBuffer) {
        ContactBone contactBone = capsule.gameObject.AddComponent<ContactBone>();
        Rigidbody body = capsule.gameObject.GetComponent<Rigidbody>();
        if (body == null) {
          body = capsule.gameObject.AddComponent<Rigidbody>();
        }

        body.useGravity = false;
        contactBone.interactionController = this;
        contactBone.body = body;
        contactBone.collider = capsule;

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

    private IInteractionBehaviour _closestGraspableObject = null;

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

    public override Vector3 GetGraspPoint() {
      return graspPoint.transform.position;
    }

    protected override void fixedUpdateGraspingState() {
      refreshClosestGraspableObject();

      updateGraspButtonState();
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

    private bool _graspButtonLastFrame = false;
    private bool _graspButtonDown = false;
    private bool _graspButtonUp = false;

    private void updateGraspButtonState() {
      _graspButtonDown = false;
      _graspButtonUp = false;

      bool graspButton = _graspButtonLastFrame;

      if (!_graspButtonLastFrame) {
        graspButton = Input.GetAxis(graspButtonAxis) > 0.8F;

        if (graspButton) {
          _graspButtonDown = true;
        }
      }
      else {
        graspButton = Input.GetAxis(graspButtonAxis) > 0.7F;

        if (!graspButton) {
          _graspButtonUp = true;
        }
      }

      _graspButtonLastFrame = graspButton;
    }

    protected override bool checkShouldGrasp(out IInteractionBehaviour objectToGrasp) {
      bool shouldGrasp = _graspButtonDown && _closestGraspableObject != null;

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
      drawer.color = Color.Lerp(Color.blue, Color.white, Input.GetAxis(graspButtonAxis));
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
