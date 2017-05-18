/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using InteractionEngineUtility;
using Leap.Unity.RuntimeGizmos;
using Leap.Unity.Space;
using Leap.Unity.Interaction.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  public class InteractionHand : InteractionControllerBase {

    /// <summary>
    /// Gets whether the underlying Leap hand is currently tracked.
    /// </summary>
    public override bool isTracked { get { return _hand != null; } }

    /// <summary>
    /// Gets the last tracked state of the Leap hand.
    /// </summary>
    /// <remarks>
    /// If the hand required warping due to the nearby presence of an object
    /// in warped (curved) space, this will return the hand as warped from that
    /// object's curved space into rectilinear space. This is only relevant if
    /// you are using the Leap Graphical Renderer to render curved interfaces.
    /// </remarks>
    public Hand leapHand { get { return _unwarpedHandData; } }

    /// <summary>
    /// Gets whether the underlying tracked Leap hand is a left hand.
    /// </summary>
    public override bool isLeft {
      get { return isTracked ? false : leapHand.IsLeft; }
    }

    /// <summary>
    /// Gets the velocity of the underlying tracked Leap hand.
    /// </summary>
    public override Vector3 velocity {
      get { return isTracked ? Vector3.zero : leapHand.PalmVelocity.ToVector3(); }
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

    protected override void getColliderBoneTargetPositionRotation(int contactBoneIndex,
                                                                  out Vector3 targetPosition,
                                                                  out Quaternion targetRotation) {
      _handContactBoneMapFunctions[contactBoneIndex](_unwarpedHandData,
                                                     out targetPosition,
                                                     out targetRotation);
    }

    public void REFRESHHANDDATA() {
      // TODO: Fixme!
      // Subscribe hands to the fixed-frame dispatch of the ServiceProvider.
      // Hands will likely need methods to convert frames into hands.

      // It will need to contain this:

      _unwarpedHandData.CopyFrom(_handData);

      return false; // fix this method D:<
      // Interaction Manager needs to handle it, or the object itself but via a call
    }

    #region Hovering Controller Implementation

    protected override Vector3 hoverPoint {
      get {
        return leapHand.PalmPosition.ToVector3();
      }
    }

    private List<Transform> _fingertipTransforms = new List<Transform>();
    protected override List<Transform> _primaryHoverPoints {
      get {
        // TODO: return the thing
      }
    }

    protected override void unwarpColliders(Transform primaryHoverPoint, ISpaceComponent warpedSpaceElement) {

      Hand handToWarp = _unwarpedHandData;

      if (warpedSpaceElement.anchor != null && warpedSpaceElement.anchor.space != null) {

        // Extension method calculates "unwarped" pose, both in world space.
        Vector3 unwarpedPosition;
        Quaternion unwarpedRotation;
        warpedSpaceElement.anchor.transformer.WorldSpaceUnwarp(primaryHoverPoint.position, 
                                                               primaryHoverPoint.rotation,
                                                               out unwarpedPosition,
                                                               out unwarpedRotation);
        
        // First shift the hand to be centered on the fingertip position so that rotations
        // applied to the hand pivot around the fingertip, then apply the rest of the
        // transformation.
        handToWarp.Transform(-primaryHoverPoint.position, Quaternion.identity);
        handToWarp.Transform(unwarpedPosition, unwarpedRotation
                                               * Quaternion.Inverse(primaryHoverPoint.rotation));
      }
    }

    #endregion

    #region Contact Controller Implementation

    private const int NUM_FINGERS = 5;
    private const int BONES_PER_FINGER = 3;

    private ContactBone[] _contactBones;
    protected override ContactBone[] contactBones {
      get { return _contactBones; }
    }

    private GameObject _contactBoneParent;
    protected override GameObject contactBoneParent {
      get { return _contactBoneParent; }
    }

    private delegate void BoneMapFunc(Leap.Hand hand, out Vector3 targetPosition,
                                                      out Quaternion targetRotation);
    private BoneMapFunc[] _handContactBoneMapFunctions;

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
      contactBoneParent.transform.parent = manager.transform;
    }

    private void initContactBones() {
      _contactBones = new ContactBone[NUM_FINGERS * BONES_PER_FINGER + 1];

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
      contactBone.interactionHand = this;
      _contactBones[boneArrayIndex] = contactBone;

      Transform capsuleTransform = contactBoneObj.transform;
      capsuleTransform.SetParent(_contactBoneParent.transform, false);

      Rigidbody body = contactBoneObj.GetComponent<Rigidbody>();
      body.freezeRotation = true;
      contactBone.body = body;
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
            joint.connectedBody = _contactBones[boneArrayIndex - 1].body;
            joint.anchor = Vector3.back * bone.Length / 2f;
            joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
            _contactBones[boneArrayIndex].joint = joint;
          }
          else {
            joint.connectedBody = _contactBones[NUM_FINGERS * BONES_PER_FINGER].body;
            joint.anchor = Vector3.back * bone.Length / 2f;
            joint.connectedAnchor = _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform.InverseTransformPoint(bone.PrevJoint.ToVector3());
            _contactBones[boneArrayIndex].metacarpalJoint = joint;
          }
        }
      }
    }

    /// <summary> Reconnects and resets all the joints in the hand. </summary>
    private void resetContactBoneJoints() {
      _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform.position = _unwarpedHandData.PalmPosition.ToVector3();
      _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform.rotation = _unwarpedHandData.Rotation.ToQuaternion();
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _unwarpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

          if (jointIndex != 0 && _contactBones[boneArrayIndex].joint != null) {
            Bone prevBone = _unwarpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            _contactBones[boneArrayIndex].joint.connectedBody = _contactBones[boneArrayIndex - 1].body;
            _contactBones[boneArrayIndex].joint.anchor = Vector3.back * bone.Length / 2f;
            _contactBones[boneArrayIndex].joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
          }
          else if (_contactBones[boneArrayIndex].metacarpalJoint != null) {
            _contactBones[boneArrayIndex].metacarpalJoint.connectedBody = _contactBones[NUM_FINGERS * BONES_PER_FINGER].body;
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
        inHand.SetTransform(_contactBones[NUM_FINGERS * BONES_PER_FINGER].body.position, _contactBones[NUM_FINGERS * BONES_PER_FINGER].body.rotation);

        for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
          for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
            Bone bone = inHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1);
            int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;
            Vector displacement = _contactBones[boneArrayIndex].body.position.ToVector() - bone.Center;
            bone.Center += displacement;
            bone.PrevJoint += displacement;
            bone.NextJoint += displacement;
            bone.Rotation = _contactBones[boneArrayIndex].body.rotation.ToLeapQuaternion();
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

    /// <summary>
    /// Returns approximately where the controller is grasping the currently grasped
    /// InteractionBehaviour.
    /// This method will print an error if the controller is not currently grasping an object.
    /// </summary>
    public override Vector3 GetGraspPoint() {
      throw new NotImplementedException();
    }

    protected override void fixedUpdateGraspingState() {
      grabClassifier.FixedUpdateClassifierHandState();
    }

    protected override void onGraspedObjectForciblyReleased(IInteractionBehaviour objectToBeReleased) {
      grabClassifier.NotifyGraspReleased(objectToBeReleased);
    }

    protected override bool checkShouldRelease(out IInteractionBehaviour objectToRelease) {
      return grabClassifier.FixedUpdateClassifierRelease(out objectToRelease);
    }

    protected override bool checkShouldGrasp(out IInteractionBehaviour objectToGrasp) {
      return grabClassifier.FixedUpdateClassifierGrasp(out objectToGrasp);
    }

    #endregion

  }

}
