/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using InteractionEngineUtility;
using Leap.Unity.Interaction.Internal;
using Leap.Unity.RuntimeGizmos;
using Leap.Unity.Space;
using Leap.Unity.Query;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;

namespace Leap.Unity.Interaction {

  public enum HandDataMode { PlayerLeft, PlayerRight, Custom }

  [DisallowMultipleComponent]
  public class InteractionHand : InteractionController {

    #region Inspector

    [SerializeField]
    private LeapProvider _leapProvider;

    [Header("Hand Configuration")]

    [Tooltip("Should the data for the underlying Leap hand come from the player's left "
           + "hand or their right hand? Alternatively, you can set this mode to Custom "
           + "to specify accessor functions manually via script (recommended for advanced "
           + "users only).")]
    [SerializeField, EditTimeOnly]
    private HandDataMode _handDataMode;
    public HandDataMode handDataMode {
      get { return _handDataMode; }
      set {
        // TODO: Do validation if this is modified!
        _handDataMode = value;
      }
    }

    /// <summary>
    /// Set slots to true to consider the corresponding finger's fingertip for primary
    /// hover checks. 0 is the thumb, 1 is the index finger, etc. Generally speaking,
    /// enable the fingertips you'd like users to be able to use to choose and push a
    /// button, but keep in mind you pay distance check costs for each fingertip enabled!
    /// </summary>
    public bool[] enabledPrimaryHoverFingertips = new bool[5] { true, true, true, false, false };

    #endregion

    #region Hand Data
    /// <summary>
    /// If the hand data mode for this InteractionHand is set to Custom, you must also
    /// manually specify the provider from which to retrieve Leap frames containing
    /// hand data.
    /// </summary>
    public LeapProvider leapProvider {
      get { return _leapProvider; }
      set {
        if (_leapProvider != null && Application.isPlaying) {
          _leapProvider.OnFixedFrame -= onProviderFixedFrame;
        }

        _leapProvider = value;

        if (_leapProvider != null && Application.isPlaying) {
          _leapProvider.OnFixedFrame += onProviderFixedFrame;
        }
      }
    }

    private Func<Leap.Frame, Leap.Hand> _handAccessorFunc;
    /// <summary>
    /// If the hand data mode for this InteractionHand is set to Custom, you must
    /// manually specify how this InteractionHand should retrieve a specific Hand data
    /// object from a Leap frame.
    /// </summary>
    public Func<Leap.Frame, Leap.Hand> handAccessorFunc {
      get { return _handAccessorFunc; }
      set { _handAccessorFunc = value; }
    }

    /// <summary>
    /// A copy of the latest tracked hand data; never null, never warped.
    /// </summary>
    private Hand _handData = new Hand();

    /// <summary>
    /// An unwarped copy of _handData (if unwarping is necessary; otherwise
    /// identical). Only relevant when using the Leap Graphical Renderer to create
    /// curved user interfaces.
    /// </summary>
    private Hand _unwarpedHandData = new Hand();

    /// <summary>
    /// Will be null when not tracked, otherwise contains the same data as _handData.
    /// </summary>
    private Hand _hand;



    public Transform headTransform;
    #endregion

    #region Unity Events

    protected override void Reset() {
      base.Reset();

      enabledPrimaryHoverFingertips = new bool[] { true, true, true, false, false };
    }

    protected override void Start() {
      base.Start();

      // Check manual configuration if data mode is custom.
      if (handDataMode == HandDataMode.Custom) {
        if (leapProvider == null) {
          Debug.LogError("handDataMode is set to Custom, but no provider is set! "
                       + "Please add a custom script that will configure the correct "
                       + "LeapProvider for this InteractionHand before its Start() is "
                       + "called, or set the handDataMode to a value other than Custom.",
                       this);
          return;
        }
        else if (handAccessorFunc == null) {
          Debug.LogError("handDataMode is set to Custom, but no handAccessorFunc has "
                       + "been set! Please add a custom script that will configure the "
                       + "hand accessor function that will convert Leap frames into "
                       + "Leap hand data for this InteractionHand before its Start() "
                       + "is called, or set the handDataMode to a value other than "
                       + "Custom.", this);
          return;
        }
      }
      else { // Otherwise, configure automatically.
        if (leapProvider == null) {
          leapProvider = Hands.Provider;

          if (leapProvider == null) {
            Debug.LogError("No LeapServiceProvider was found in your scene! Please "
                         + "make sure you have a LeapServiceProvider if you intend to "
                         + "use Leap hands in your scene.", this);
            return;
          }
        }

        if (handAccessorFunc == null) {
          if (handDataMode == HandDataMode.PlayerLeft) {
            handAccessorFunc = (frame) => frame.Hands.Query()
                                                     .FirstOrDefault(hand => hand.IsLeft);
          }
          else {
            handAccessorFunc = (frame) => frame.Hands.Query()
                                                     .FirstOrDefault(hand => hand.IsRight);
          }
        }
      }

      leapProvider.OnFixedFrame -= onProviderFixedFrame; // avoid double-subscribe
      leapProvider.OnFixedFrame += onProviderFixedFrame;

      // Set up hover point Transform for the palm.
      Transform palmTransform = new GameObject("Palm Transform").transform;
      palmTransform.parent = this.transform;
      _backingHoverPointTransform = palmTransform;

      // Set up primary hover point Transforms for the fingertips. We'll only use
      // some of them, depending on user settings.
      for (int i = 0; i < 5; i++) {
        Transform fingertipTransform = new GameObject("Fingertip Transform").transform;
        fingertipTransform.parent = this.transform;
        _backingFingertipTransforms.Add(fingertipTransform);
        _fingertipTransforms.Add(null);
      }
    }

    private void OnDestroy() {
      if (_leapProvider != null) {
        _leapProvider.OnFixedFrame -= onProviderFixedFrame;
      }
    }

    private void onProviderFixedFrame(Leap.Frame frame) {
      _hand = handAccessorFunc(frame);

      if (_hand != null) {
        _handData.CopyFrom(_hand);
        _unwarpedHandData.CopyFrom(_handData);

        refreshPointDataFromHand();
        _lastCustomHandWasLeft = _unwarpedHandData.IsLeft;
      }

    }

    #endregion

    #region General InteractionController Implementation

    /// <summary>
    /// Gets whether the underlying Leap hand is currently tracked.
    /// </summary>
    public override bool isTracked { get { return _hand != null; } }

    /// <summary>
    /// Gets whether the underlying Leap hand is currently being moved in worldspace.
    /// </summary>
    public override bool isBeingMoved { get { return isTracked; } }

    /// <summary>
    /// Gets the last tracked state of the Leap hand.
    /// 
    /// Note for those using the Leap Graphical Renderer: If the hand required warping
    /// due to the nearby presence of an object in warped (curved) space, this will
    /// return the hand as warped from that object's curved space into the rectilinear
    /// space containing its colliders. This is only relevant if you are using the Leap
    /// Graphical Renderer to render curved, interactive objects.
    /// </summary>
    public Hand leapHand { get { return _unwarpedHandData; } }

    private bool _lastCustomHandWasLeft = false;
    /// <summary>
    /// Gets whether the underlying tracked Leap hand is a left hand.
    /// </summary>
    public override bool isLeft {
      get {
        switch (handDataMode) {
          case HandDataMode.PlayerLeft:
            return true;
          case HandDataMode.PlayerRight:
            return false;
          case HandDataMode.Custom: default:
            return _lastCustomHandWasLeft;
        }
      }
    }

    /// <summary>
    /// Gets the last-tracked position of the underlying Leap hand.
    /// </summary>
    public override Vector3 position {
      get { return _handData.PalmPosition.ToVector3(); }
    }

    /// <summary>
    /// Gets the last-tracked rotation of the underlying Leap hand.
    /// </summary>
    public override Quaternion rotation {
      get { return _handData.Rotation.ToQuaternion(); }
    }

    /// <summary>
    /// Gets the velocity of the underlying tracked Leap hand.
    /// </summary>
    public override Vector3 velocity {
      get { return isTracked ? leapHand.PalmVelocity.ToVector3() : Vector3.zero; }
    }

    /// <summary>
    /// Gets the controller type of this InteractionControllerBase. InteractionHands
    /// are Interaction Engine controllers implemented over Leap hands.
    /// </summary>
    public override ControllerType controllerType {
      get { return ControllerType.Hand; }
    }

    /// <summary>
    /// Returns this InteractionHand object. This property will be null if the
    /// InteractionControllerBase is not ControllerType.Hand.
    /// </summary>
    public override InteractionHand intHand {
      get { return this; }
    }

    protected override void onObjectUnregistered(IInteractionBehaviour intObj) {
      grabClassifier.UnregisterInteractionBehaviour(intObj);
    }

    protected override void fixedUpdateController() {
      // Transform the hand ahead if the manager is in a moving reference frame.
      if (manager.hasMovingFrameOfReference) {
        Vector3 transformAheadPosition;
        Quaternion transformAheadRotation;
        manager.TransformAheadByFixedUpdate(_unwarpedHandData.PalmPosition.ToVector3(),
                                            _unwarpedHandData.Rotation.ToQuaternion(),
                                            out transformAheadPosition,
                                            out transformAheadRotation);
        _unwarpedHandData.SetTransform(transformAheadPosition, transformAheadRotation);
      }
    }

    #endregion

    #region Hovering Controller Implementation

    private Transform _backingHoverPointTransform = null;
    public override Vector3 hoverPoint {
      get {
        if (_backingHoverPointTransform == null) {
          return leapHand.PalmPosition.ToVector3();
        }
        else {
          return _backingHoverPointTransform.position;
        }
      }
    }

    private List<Transform> _backingFingertipTransforms = new List<Transform>();
    private List<Transform> _fingertipTransforms = new List<Transform>();
    protected override List<Transform> _primaryHoverPoints {
      get {
        return _fingertipTransforms;
      }
    }

    private void refreshPointDataFromHand() {
      refreshHoverPoint();
      refreshPrimaryHoverPoints();
      refreshGraspManipulatorPoints();
    }

    private void refreshHoverPoint() {
      _backingHoverPointTransform.position = leapHand.PalmPosition.ToVector3();
      _backingHoverPointTransform.rotation = leapHand.Rotation.ToQuaternion();
    }

    private void refreshPrimaryHoverPoints() {
      for (int i = 0; i < enabledPrimaryHoverFingertips.Length; i++) {
        if (enabledPrimaryHoverFingertips[i]) {
          _fingertipTransforms[i] = _backingFingertipTransforms[i];

          Finger finger = leapHand.Fingers[i];
          _fingertipTransforms[i].position = finger.TipPosition.ToVector3();
          _fingertipTransforms[i].rotation = finger.bones[3].Rotation.ToQuaternion();
        }
        else {
          _fingertipTransforms[i] = null;
        }
      }
    }

    protected override void unwarpColliders(Transform primaryHoverPoint,
                                            ISpaceComponent warpedSpaceElement) {
      // Extension method calculates "unwarped" pose in world space.
      Vector3    unwarpedPosition;
      Quaternion unwarpedRotation;
      warpedSpaceElement.anchor.transformer.WorldSpaceUnwarp(primaryHoverPoint.position, 
                                                             primaryHoverPoint.rotation,
                                                             out unwarpedPosition,
                                                             out unwarpedRotation);
        
      // First shift the hand to be centered on the fingertip position so that rotations
      // applied to the hand will pivot around the fingertip, then apply the rest of the
      // transformation.
      _unwarpedHandData.Transform(-primaryHoverPoint.position, Quaternion.identity);
      _unwarpedHandData.Transform(unwarpedPosition, unwarpedRotation
                                                    * Quaternion.Inverse(primaryHoverPoint.rotation));

      // Hand data was modified, so refresh point data.
      refreshPointDataFromHand();
    }

    #endregion

    #region Contact Controller Implementation

    private const int NUM_FINGERS = 5;
    private const int BONES_PER_FINGER = 3;

    private ContactBone[] _contactBones;
    public override ContactBone[] contactBones {
      get { return _contactBones; }
    }

    private GameObject _contactBoneParent;
    protected override GameObject contactBoneParent {
      get { return _contactBoneParent; }
    }

    private delegate void BoneMapFunc(Leap.Hand hand, out Vector3 targetPosition,
                                                      out Quaternion targetRotation);
    private BoneMapFunc[] _handContactBoneMapFunctions;

    protected override void getColliderBoneTargetPositionRotation(int contactBoneIndex,
                                                                  out Vector3 targetPosition,
                                                                  out Quaternion targetRotation) {
      using (new ProfilerSample("InteractionHand: getColliderBoneTargetPositionRotation")) {
        _handContactBoneMapFunctions[contactBoneIndex](_unwarpedHandData,
                                                       out targetPosition,
                                                       out targetRotation);
      }
    }

    protected override bool initContact() {
      if (!isTracked) return false;

      initContactBoneContainer();
      initContactBones();

      return true;
    }

    protected override void onPreEnableSoftContact() {
      resetContactBoneJoints();
    }

    protected override void onPostDisableSoftContact() {
      if (isTracked) resetContactBoneJoints();
    }

    #region Contact Bone Management

    private void initContactBoneContainer() {
      string name = (_unwarpedHandData.IsLeft ? "Left" : "Right") + " Interaction Hand Contact Bones";
      _contactBoneParent = new GameObject(name);
    }

    private void initContactBones() {
      _contactBones = new ContactBone[NUM_FINGERS * BONES_PER_FINGER + 1];
      _handContactBoneMapFunctions = new BoneMapFunc[NUM_FINGERS * BONES_PER_FINGER + 1];

      // Finger bones
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          GameObject contactBoneObj = new GameObject("Contact Fingerbone", typeof(CapsuleCollider), typeof(Rigidbody), typeof(ContactBone));
          contactBoneObj.layer = manager.contactBoneLayer;
          
          Bone bone = _unwarpedHandData.Fingers[fingerIndex]
                                       .Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;
          contactBoneObj.transform.position = bone.Center.ToVector3();
          contactBoneObj.transform.rotation = bone.Rotation.ToQuaternion();

          // Remember the method we used to calculate this bone position from
          // a Leap Hand for later.
          int fingerIndexCopy = fingerIndex;
          int jointIndexCopy = jointIndex;
          _handContactBoneMapFunctions[boneArrayIndex] = (Leap.Hand hand,
                                                          out Vector3 targetPosition,
                                                          out Quaternion targetRotation) => {
            Bone theBone = hand.Fingers[fingerIndexCopy].Bone((Bone.BoneType)(jointIndexCopy + 1));
            targetPosition = theBone.Center.ToVector3();
            targetRotation = theBone.Rotation.ToQuaternion();
          };

          CapsuleCollider capsule = contactBoneObj.GetComponent<CapsuleCollider>();
          capsule.direction = 2;
          capsule.radius = bone.Width * 0.5f;
          capsule.height = bone.Length + bone.Width;
          capsule.material = defaultContactBoneMaterial;

          ContactBone contactBone = initContactBone(bone, contactBoneObj, boneArrayIndex, capsule);

          contactBone.lastTargetPosition = bone.Center.ToVector3();
        }
      }

      // Palm bone
      {
        // Palm is attached to the third metacarpal and derived from it.
        GameObject contactBoneObj = new GameObject("Contact Palm Bone", typeof(BoxCollider), typeof(Rigidbody), typeof(ContactBone));

        Bone bone = _unwarpedHandData.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL);
        int boneArrayIndex = NUM_FINGERS * BONES_PER_FINGER;
        contactBoneObj.transform.position = _unwarpedHandData.PalmPosition.ToVector3();
        contactBoneObj.transform.rotation = _unwarpedHandData.Rotation.ToQuaternion();

        // Remember the method we used to calculate the palm from a Leap Hand for later.
        _handContactBoneMapFunctions[boneArrayIndex] = (Leap.Hand hand,
                                                        out Vector3 targetPosition,
                                                        out Quaternion targetRotation) => {
          targetPosition = hand.PalmPosition.ToVector3();
          targetRotation = hand.Rotation.ToQuaternion();
        };

        BoxCollider box = contactBoneObj.GetComponent<BoxCollider>();
        box.center = new Vector3(_unwarpedHandData.IsLeft ? -0.005f : 0.005f, bone.Width * -0.3f, -0.01f);
        box.size = new Vector3(bone.Length, bone.Width, bone.Length);
        box.material = defaultContactBoneMaterial;

        initContactBone(null, contactBoneObj, boneArrayIndex, box);
      }

      // Constrain the bones to each other to prevent separation.
      addContactBoneJoints();
    }

    private ContactBone initContactBone(Leap.Bone bone, GameObject contactBoneObj, int boneArrayIndex, Collider boneCollider) {
      contactBoneObj.layer = _contactBoneParent.gameObject.layer;
      contactBoneObj.transform.localScale = Vector3.one;

      ContactBone contactBone = contactBoneObj.GetComponent<ContactBone>();
      contactBone.collider = boneCollider;
      contactBone.interactionController = this;
      contactBone.interactionHand = this;
      _contactBones[boneArrayIndex] = contactBone;

      Transform capsuleTransform = contactBoneObj.transform;
      capsuleTransform.SetParent(_contactBoneParent.transform, false);

      Rigidbody body = contactBoneObj.GetComponent<Rigidbody>();
      body.freezeRotation = true;
      contactBone.rigidbody = body;
      body.useGravity = false;
      body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // TODO: Allow different collision detection modes as an optimization.

      body.mass = 0.1f;
      body.position = bone != null ? bone.Center.ToVector3()
                                   : _unwarpedHandData.PalmPosition.ToVector3();
      body.rotation = bone != null ? bone.Rotation.ToQuaternion()
                                   : _unwarpedHandData.Rotation.ToQuaternion();
      contactBone.lastTargetPosition = bone != null ? bone.Center.ToVector3()
                                            : _unwarpedHandData.PalmPosition.ToVector3();

      return contactBone;
    }

    private void addContactBoneJoints() {
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _unwarpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

          FixedJoint joint = _contactBones[boneArrayIndex].gameObject.AddComponent<FixedJoint>();
          joint.autoConfigureConnectedAnchor = false;
          if (jointIndex != 0) {
            Bone prevBone = _unwarpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            joint.connectedBody = _contactBones[boneArrayIndex - 1].rigidbody;
            joint.anchor = Vector3.back * bone.Length / 2f;
            joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
            _contactBones[boneArrayIndex].joint = joint;
          }
          else {
            joint.connectedBody = _contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody;
            joint.anchor = Vector3.back * bone.Length / 2f;
            joint.connectedAnchor = _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform.InverseTransformPoint(bone.PrevJoint.ToVector3());
            _contactBones[boneArrayIndex].metacarpalJoint = joint;
          }
        }
      }
    }

    /// <summary> Reconnects and resets all the joints in the hand. </summary>
    private void resetContactBoneJoints() {
      // If contact bones array is null, there's nothing to reset. This can happen if
      // the controller is disabled before it had a chance to initialize contact.
      if (_contactBones == null) return;

      // If the palm contact bone is null, we can't reset bone joints.
      if (_contactBones[NUM_FINGERS * BONES_PER_FINGER] == null) return;

      _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform.position = _unwarpedHandData.PalmPosition.ToVector3();
      _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform.rotation = _unwarpedHandData.Rotation.ToQuaternion();
      _contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody.velocity = Vector3.zero;
      _contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody.angularVelocity = Vector3.zero;

      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _unwarpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

          _contactBones[boneArrayIndex].transform.position = bone.Center.ToVector3();
          _contactBones[boneArrayIndex].transform.rotation = bone.Rotation.ToQuaternion();
          _contactBones[boneArrayIndex].rigidbody.position = bone.Center.ToVector3();
          _contactBones[boneArrayIndex].rigidbody.rotation = bone.Rotation.ToQuaternion();
          _contactBones[boneArrayIndex].rigidbody.velocity = Vector3.zero;
          _contactBones[boneArrayIndex].rigidbody.angularVelocity = Vector3.zero;

          if (jointIndex != 0 && _contactBones[boneArrayIndex].joint != null) {
            Bone prevBone = _unwarpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            _contactBones[boneArrayIndex].joint.connectedBody = _contactBones[boneArrayIndex - 1].rigidbody;
            _contactBones[boneArrayIndex].joint.anchor = Vector3.back * bone.Length / 2f;
            _contactBones[boneArrayIndex].joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
          }
          else if (_contactBones[boneArrayIndex].metacarpalJoint != null) {
            _contactBones[boneArrayIndex].metacarpalJoint.connectedBody = _contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody;
            _contactBones[boneArrayIndex].metacarpalJoint.anchor = Vector3.back * bone.Length / 2f;
            _contactBones[boneArrayIndex].metacarpalJoint.connectedAnchor = _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform
                                                                            .InverseTransformPoint(bone.PrevJoint.ToVector3());
          }
        }
      }
    }

    /// <summary>
    /// A utility function that sets a Hand object's bones based on this InteractionHand.
    /// Can be used to display a graphical hand that matches the physical one.
    /// </summary>
    public void FillBones(Hand inHand) {
      if (softContactEnabled) { return; }
      if (Application.isPlaying && _contactBones.Length == NUM_FINGERS * BONES_PER_FINGER + 1) {
        Vector elbowPos = inHand.Arm.ElbowPosition;
        inHand.SetTransform(_contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody.position, _contactBones[NUM_FINGERS * BONES_PER_FINGER].rigidbody.rotation);

        for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
          for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
            Bone bone = inHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1);
            int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;
            Vector displacement = _contactBones[boneArrayIndex].rigidbody.position.ToVector() - bone.Center;
            bone.Center += displacement;
            bone.PrevJoint += displacement;
            bone.NextJoint += displacement;
            bone.Rotation = _contactBones[boneArrayIndex].rigidbody.rotation.ToLeapQuaternion();
          }
        }

        inHand.Arm.PrevJoint = elbowPos;
        inHand.Arm.Direction = (inHand.Arm.PrevJoint - inHand.Arm.NextJoint).Normalized;
        inHand.Arm.Center = (inHand.Arm.PrevJoint + inHand.Arm.NextJoint) * 0.5f;
      }
    }

    #endregion

    #endregion

    #region Grasp Controller Implementation

    private List<Vector3> _graspManipulatorPoints = new List<Vector3>();
    public override List<Vector3> graspManipulatorPoints {
      get {
        return _graspManipulatorPoints;
      }
    }

    private void refreshGraspManipulatorPoints() {
      int bufferIndex = 0;
      for (int i = 0; i < NUM_FINGERS; i++) {
        for (int boneIdx = 0; boneIdx < 2; boneIdx++) {
          // Update or add knuckle-joint and first-finger-bone positions as the grasp
          // manipulator points for this Hand.

          Vector3 point = leapHand.Fingers[i].bones[boneIdx].NextJoint.ToVector3();

          if (_graspManipulatorPoints.Count - 1 < bufferIndex) {
            _graspManipulatorPoints.Add(point);
          }
          else {
            _graspManipulatorPoints[bufferIndex] = point;
          }
          bufferIndex += 1;
        }
      }
    }

    private HeuristicGrabClassifier _grabClassifier;
    /// <summary>
    /// Handles logic determining whether a hand has grabbed or released an interaction object.
    /// </summary>
    public HeuristicGrabClassifier grabClassifier {
      get {
        if (_grabClassifier == null) _grabClassifier = new HeuristicGrabClassifier(this);
        return _grabClassifier;
      }
    }

    private Vector3[] _fingertipPositionsBuffer = new Vector3[5];
    /// <summary>
    /// Returns approximately where the controller is grasping the currently-grasped
    /// InteractionBehaviour. Specifically, returns the average position of all grasping
    /// fingertips of the InteractionHand.
    /// 
    /// This method will print an error if the hand is not currently grasping an object.
    /// </summary>
    public override Vector3 GetGraspPoint() {
      if (!isGraspingObject) {
        Debug.LogError("Tried to get grasp point of InteractionHand, but it is not "
                     + "currently grasping an object.", this);
        return leapHand.PalmPosition.ToVector3();
      }

      int numGraspingFingertips = 0;
      _grabClassifier.GetGraspingFingertipPositions(graspedObject, _fingertipPositionsBuffer, out numGraspingFingertips);
      if (numGraspingFingertips > 0) {
        Vector3 sum = Vector3.zero;
        for (int i = 0; i < numGraspingFingertips; i++) {
          sum += _fingertipPositionsBuffer[i];
        }
        return sum / numGraspingFingertips;
      }
      else {
        return leapHand.PalmPosition.ToVector3();
      }
    }

    /// <summary>
    /// Attempts to manually initiate a grasp on the argument interaction object. A grasp
    /// will only begin if a finger and thumb are both in contact with the interaction
    /// object. If this method successfully initiates a grasp, it will return true,
    /// otherwise it will return false.
    /// </summary>
    protected override bool checkShouldGraspAtemporal(IInteractionBehaviour intObj) {
      if (grabClassifier.TryGrasp(intObj, leapHand)) {
        var tempControllers = Pool<List<InteractionController>>.Spawn();
        try {
          tempControllers.Add(this);
          intObj.BeginGrasp(tempControllers);
          return true;
        }
        finally {
          tempControllers.Clear();
          Pool<List<InteractionController>>.Recycle(tempControllers);
        }
      }

      return false;
    }

    public override void SwapGrasp(IInteractionBehaviour replacement) {
      var original = graspedObject;

      base.SwapGrasp(replacement);

      grabClassifier.SwapClassifierState(original, replacement);
    }

    protected override void fixedUpdateGraspingState() {
      //Feed Camera Transform in for Projective Grabbing Hack (details inside)
      grabClassifier.FixedUpdateClassifierHandState(headTransform);
    }

    protected override void onGraspedObjectForciblyReleased(IInteractionBehaviour objectToBeReleased) {
      grabClassifier.NotifyGraspForciblyReleased(objectToBeReleased);
    }

    protected override bool checkShouldRelease(out IInteractionBehaviour objectToRelease) {
      return grabClassifier.FixedUpdateClassifierRelease(out objectToRelease);
    }

    protected override bool checkShouldGrasp(out IInteractionBehaviour objectToGrasp) {
      return grabClassifier.FixedUpdateClassifierGrasp(out objectToGrasp);
    }

    #endregion

    #region Gizmos

    #if UNITY_EDITOR

    private Leap.Hand _testHand = null;

    public override void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (Application.isPlaying) {
        base.OnDrawRuntimeGizmos(drawer);
      }
      else {
        var provider = leapProvider;
        if (provider == null) {
          provider = Hands.Provider;
        }

        if (_testHand == null && provider != null) {
          _testHand = provider.MakeTestHand(this.isLeft);
        }

        // Hover Point
        _unwarpedHandData = _testHand; // hoverPoint is driven by this backing variable
        drawHoverPoint(drawer, hoverPoint);

        // Primary Hover Points
        for (int i = 0; i < NUM_FINGERS; i++) {
          if (enabledPrimaryHoverFingertips[i]) {
            drawPrimaryHoverPoint(drawer, _testHand.Fingers[i].TipPosition.ToVector3());
          }
        }
      }
    }

    #endif

    #endregion

  }

}
