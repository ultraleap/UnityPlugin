using Leap.Unity.RuntimeGizmos;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Interaction {

  public class InteractionHand {

    private InteractionManager _interactionManager;
    private Func<Hand> _handAccessor;
    private Hand _hand;
    private bool _isLeft;

    public InteractionHand(InteractionManager interactionManager,
                           Func<Hand> handAccessor,
                           Chirality handedness,
                           float hoverActivationRadius,
                           float touchActivationRadius) {
      _interactionManager = interactionManager;
      _handAccessor = handAccessor;
      _isLeft = handedness == Chirality.Left;

      InitHovering(hoverActivationRadius);
      InitTouch(touchActivationRadius);
      InitContact();
      InitGrasping();
    }

    public Hand GetLeapHand() {
      return _hand;
    }

    public void FixedUpdateHand(bool doHovering, bool doContact, bool doGrasping) {
      _hand = _handAccessor();

      if (doHovering) FixedUpdateHovering();
      if (doContact || doGrasping) FixedUpdateTouch();
      if (doContact) FixedUpdateContact();
      if (doGrasping) FixedUpdateGrasping();
    }

    #region Hovering

    private void InitHovering(float hoverActivationRadius) {
      // Standard Hover
      _hoverActivityManager = new ActivityManager(_interactionManager, hoverActivationRadius);
    }

    private void FixedUpdateHovering() {
      _hoverActivityManager.FixedUpdateHand(_hand);
      CalculateIntentionPosition();
      HoverCheckResults hoverResults = CheckHoverForHand(_hand, _hoverActivityManager.ActiveBehaviours);
      ProcessHoverCheckResults(hoverResults);
      ProcessPrimaryHoverCheckResults(hoverResults);
    }

    /// <summary>
    /// Encapsulates tracking Hand <-> InteractionBehaviour hover candidates
    /// (broad-phase) via PhysX sphere queries.
    /// </summary>
    private ActivityManager _hoverActivityManager;
    public float HoverActivationRadius {
      get {
        return _hoverActivityManager.activationRadius;
      }
      set {
        _hoverActivityManager.activationRadius = value;
      }
    }

    private struct HoverCheckResults {
      public HashSet<InteractionBehaviourBase> hovered;
      public InteractionBehaviourBase primaryHovered;
      public float primaryHoveredDistance;
      public Hand checkedHand;
    }

    private HashSet<InteractionBehaviourBase> _hoveredLastFrame = new HashSet<InteractionBehaviourBase>();

    private HashSet<InteractionBehaviourBase> _hoverableCache = new HashSet<InteractionBehaviourBase>();
    private HoverCheckResults CheckHoverForHand(Hand hand, HashSet<InteractionBehaviourBase> hoverCandidates) {
      _hoverableCache.Clear();

      HoverCheckResults results = new HoverCheckResults() {
        hovered = _hoverableCache,
        primaryHovered = null,
        primaryHoveredDistance = float.PositiveInfinity,
        checkedHand = hand
      };

      foreach (var interactionObj in hoverCandidates) {
        results = CheckHoverForElement(hand, interactionObj, results);
      }

      return results;
    }

    private HoverCheckResults CheckHoverForElement(Hand hand, InteractionBehaviourBase hoverable, HoverCheckResults curResults) {
      float distance = hoverable.GetDistance(_intentionPosition);
      if (distance > 0F) {
        curResults.hovered.Add(hoverable);
      }
      if (distance < MAX_PRIMARY_HOVER_DISTANCE && distance < curResults.primaryHoveredDistance) {
        curResults.primaryHovered = hoverable;
        curResults.primaryHoveredDistance = distance;
      }
      return curResults;
    }

    private List<InteractionBehaviourBase> _removalCache = new List<InteractionBehaviourBase>();
    private void ProcessHoverCheckResults(HoverCheckResults hoverResults) {
      var trackedBehaviours = _hoverActivityManager.ActiveBehaviours;
      foreach (var hoverable in trackedBehaviours) {
        bool inLastFrame = false, inCurFrame = false;
        if (hoverResults.hovered.Contains(hoverable)) {
          inCurFrame = true;
        }
        if (_hoveredLastFrame.Contains(hoverable)) {
          inLastFrame = true;
        }

        if (inCurFrame && !inLastFrame) {
          hoverable.HoverBegin(hoverResults.checkedHand);
          _hoveredLastFrame.Add(hoverable);
        }
        if (inCurFrame && inLastFrame) {
          hoverable.HoverStay(hoverResults.checkedHand);
        }
        if (!inCurFrame && inLastFrame) {
          hoverable.HoverEnd(hoverResults.checkedHand);
          _hoveredLastFrame.Remove(hoverable);
        }
      }

      foreach (var hoverable in _hoveredLastFrame) {
        if (!trackedBehaviours.Contains(hoverable)) {
          hoverable.HoverEnd(hoverResults.checkedHand);
          _removalCache.Add(hoverable);
        }
      }
      foreach (var hoverable in _removalCache) {
        _hoveredLastFrame.Remove(hoverable);
      }
      _removalCache.Clear();
    }

    private void ProcessPrimaryHoverCheckResults(HoverCheckResults hoverResults) {
      if (hoverResults.primaryHovered == _primaryHoveredLastFrame) {
        if (hoverResults.primaryHovered != null) hoverResults.primaryHovered.PrimaryHoverStay(hoverResults.checkedHand);
      }
      else {
        if (_primaryHoveredLastFrame != null) {
          _primaryHoveredLastFrame.PrimaryHoverEnd(hoverResults.checkedHand);
        }
        _primaryHoveredLastFrame = hoverResults.primaryHovered;
        if (_primaryHoveredLastFrame != null) _primaryHoveredLastFrame.PrimaryHoverBegin(hoverResults.checkedHand);
      }
    }

    private const float MAX_PRIMARY_HOVER_DISTANCE = 0.1F;
    private Vector3 _intentionPosition;
    private InteractionBehaviourBase _primaryHoveredLastFrame = null;

    private void CalculateIntentionPosition() {
      if (_hand == null) return;

      // Weighted average of medial finger bone positions and directions for base (distal) intention ray
      float usageSum = 0F;
      Leap.Vector averagePosition = Leap.Vector.Zero;
      Leap.Vector averageDirection = Leap.Vector.Zero;
      for (int i = 1; i < 5; i++) {
        Leap.Vector distalFingerBoneTip = _hand.Fingers[i].bones[3].NextJoint;
        Leap.Vector medialFingerBoneDirection = _hand.Fingers[i].bones[2].Direction;
        Leap.Vector distalDirection = _hand.Basis.zBasis;
        var fingerUsage = ((medialFingerBoneDirection.x * distalDirection.x)
                         + (medialFingerBoneDirection.y * distalDirection.y)
                         + (medialFingerBoneDirection.z * distalDirection.z)).Map(-0.6F, 0.9F, 0, 1);
        usageSum += fingerUsage;
        averagePosition = new Leap.Vector(averagePosition.x + distalFingerBoneTip.x * fingerUsage,
                                          averagePosition.y + distalFingerBoneTip.y * fingerUsage,
                                          averagePosition.z + distalFingerBoneTip.z * fingerUsage);
        averageDirection = new Leap.Vector(averageDirection.x + medialFingerBoneDirection.x * fingerUsage,
                                           averageDirection.y + medialFingerBoneDirection.y * fingerUsage,
                                           averageDirection.z + medialFingerBoneDirection.z * fingerUsage);
      }

      // Distal Intention Ray: Add punch vector for when finger usage is low (closed fist).
      Vector3 distalAxis = _hand.DistalAxis();
      float punchWeight = usageSum.Map(0F, 1F, 1F, 0F);
      averagePosition = new Leap.Vector(averagePosition.x + _hand.Fingers[2].bones[1].PrevJoint.x * punchWeight,
                                         averagePosition.y + _hand.Fingers[2].bones[1].PrevJoint.y * punchWeight,
                                         averagePosition.z + _hand.Fingers[2].bones[1].PrevJoint.z * punchWeight);
      averageDirection = new Leap.Vector(averageDirection.x + distalAxis.x * punchWeight,
                                         averageDirection.y + distalAxis.y * punchWeight,
                                         averageDirection.z + distalAxis.z * punchWeight);
      usageSum += punchWeight;

      // Finalize weighted average for Distal Intention Ray
      _intentionPosition = new Vector3(averagePosition.x / usageSum,
                                        averagePosition.y / usageSum,
                                        averagePosition.z / usageSum);
    }

    #endregion

    #region Touch (common logic for Contact and Grasping)

    /// <summary>
    /// Encapsulates tracking Hand <-> InteractionBehaviour contact and grasping candidates
    /// (broad-phase) via PhysX sphere queries.
    /// </summary>
    private ActivityManager _touchActivityManager;
    public float TouchActivationRadius {
      get {
        return _touchActivityManager.activationRadius;
      }
      set {
        _touchActivityManager.activationRadius = value;
      }
    }

    private void InitTouch(float touchActivationRadius) {
      _touchActivityManager = new ActivityManager(_interactionManager, touchActivationRadius);
    }

    private void FixedUpdateTouch() {
      _touchActivityManager.FixedUpdateHand(_hand);
    }

    #endregion

    #region Contact

    private const int NUM_FINGERS = 5;
    private const int BONES_PER_FINGER = 3;
    public const float PER_BONE_MASS = 1.0F;
    private const float DEAD_ZONE_FRACTION = 0.1f; // TODO: What on earth IS this. Give it a more explicit name.
    private const float DISLOCATION_FRACTION = 5.0f; // TODO: not as bad but maybe rename this too
    private const float FINGERTIP_RADIUS = 0.02F; // TODO: replace with bone fingertip radius

    private ContactBone[] _contactBones;
    private GameObject _contactBoneParent;

    private PhysicMaterial _material;
    protected PhysicMaterial ContactMaterial {
      get { if (_material == null) { InitContactMaterial(); } return _material; }
    }
    /// <summary> ContactBones must have PhysicMaterials with a
    /// Bounciness of zero and Bounce Combine set to Minimum. </summary>
    private void InitContactMaterial() {
      _material = new PhysicMaterial();
      _material.hideFlags = HideFlags.HideAndDontSave;
      _material.bounceCombine = PhysicMaterialCombine.Minimum;
      _material.bounciness = 0F;
      // TODO: Ensure there aren't other optimal PhysicMaterial defaults to set here.
    }

    // Contact callback support
    private HashSet<InteractionBehaviourBase> _contactBehaviours = new HashSet<InteractionBehaviourBase>();
    private HashSet<InteractionBehaviourBase> _contactBehaviourLastFrame = new HashSet<InteractionBehaviourBase>();
    private List<InteractionBehaviourBase> _contactBehaviourRemovalCache = new List<InteractionBehaviourBase>();

    private bool _contactInitialized = false;

    private void InitContact() {
      if (_hand == null) return;
      InitContactBoneContainer();
      InitContactBones();
      _contactInitialized = true;
    }

    private void FixedUpdateContact() {
      if (!_contactInitialized) {
        InitContact();
      }
      if (!_contactInitialized) {
        return;
      }

      FixedUpdateContactBones();
      FixedUpdateContactCallbacks();
    }

    private void InitContactBoneContainer() {
      _contactBoneParent = new GameObject((_isLeft ? "Left" : "Right") + " Interaction Hand Contact Bones");
      _contactBoneParent.transform.parent = _interactionManager.transform;
    }

    private void InitContactBones() {
      _contactBones = new ContactBone[NUM_FINGERS * BONES_PER_FINGER + 1];

      // Finger bones
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

          GameObject contactBoneObj = new GameObject("Contact Fingerbone", typeof(CapsuleCollider), typeof(Rigidbody), typeof(ContactBone));

          contactBoneObj.transform.position = bone.Center.ToVector3();
          contactBoneObj.transform.rotation = bone.Rotation.ToQuaternion();
          CapsuleCollider capsule = contactBoneObj.GetComponent<CapsuleCollider>();
          capsule.direction = 2;
          capsule.radius = bone.Width * 0.5f;
          capsule.height = bone.Length + bone.Width;
          capsule.material = ContactMaterial;

          ContactBone contactBone = InitContactBone(bone, contactBoneObj, boneArrayIndex, capsule);

          contactBone.lastTarget = bone.Center.ToVector3();
        }
      }

      // Palm bone
      {
        // Palm is attached to the third metacarpal and derived from it.
        Bone bone = _hand.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL);
        int boneArrayIndex = NUM_FINGERS * BONES_PER_FINGER;
        GameObject contactBoneObj = new GameObject("Contact Palm Bone", typeof(BoxCollider), typeof(Rigidbody), typeof(ContactBone));

        contactBoneObj.transform.position = _hand.PalmPosition.ToVector3();
        contactBoneObj.transform.rotation = _hand.Rotation.ToQuaternion();
        BoxCollider box = contactBoneObj.GetComponent<BoxCollider>();
        box.center = new Vector3(_hand.IsLeft ? -0.005f : 0.005f, bone.Width * -0.3f, -0.01f);
        box.size = new Vector3(bone.Length, bone.Width, bone.Length);
        box.material = _material;

        InitContactBone(null, contactBoneObj, boneArrayIndex, box);
      }

      // Constrain the bones to each other to prevent separation
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

          FixedJoint joint = _contactBones[boneArrayIndex].gameObject.AddComponent<FixedJoint>();
          joint.autoConfigureConnectedAnchor = false;
          if (jointIndex != 0) {
            Bone prevBone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            joint.connectedBody = _contactBones[boneArrayIndex - 1].body;
            joint.anchor = Vector3.back * bone.Length / 2f;
            joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
          }
          else {
            joint.connectedBody = _contactBones[NUM_FINGERS * BONES_PER_FINGER].body;
            joint.anchor = Vector3.back * bone.Length / 2f;
            joint.connectedAnchor = _contactBones[NUM_FINGERS * BONES_PER_FINGER].transform.InverseTransformPoint(bone.PrevJoint.ToVector3());
          }
        }
      }

    }

    private ContactBone InitContactBone(Bone bone, GameObject contactBoneObj, int boneArrayIndex, Collider boneCollider) {
      contactBoneObj.layer = _contactBoneParent.layer;
      // TODO: _contactBoneParent will need its layer set appropriately once interaction layers are implemented.
      contactBoneObj.transform.localScale = Vector3.one;

      ContactBone contactBone = contactBoneObj.GetComponent<ContactBone>();
      contactBone.collider = boneCollider;
      contactBone.StartTriggering();
      contactBone.interactionHand = this;
      _contactBones[boneArrayIndex] = contactBone;

      Transform capsuleTransform = contactBoneObj.transform;
      capsuleTransform.SetParent(_contactBoneParent.transform, false);

      Rigidbody body = contactBoneObj.GetComponent<Rigidbody>();
      body.freezeRotation = true;
      contactBone.body = body;
      body.useGravity = false;
      body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // TODO: Allow different collision detection modes as an optimization.
      if (boneCollider is BoxCollider) {
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
      }

      body.mass = PER_BONE_MASS;
      body.position = bone != null ? bone.Center.ToVector3() : _hand.PalmPosition.ToVector3();
      body.rotation = bone != null ? bone.Rotation.ToQuaternion() : _hand.Rotation.ToQuaternion();
      contactBone.lastTarget = bone != null ? bone.Center.ToVector3() : _hand.PalmPosition.ToVector3();

      return contactBone;
    }

    private void FixedUpdateContactBones() {
      if (_hand == null) {
        _contactBoneParent.gameObject.SetActive(false);
        return;
      }
      else {
        if (!_contactBoneParent.gameObject.activeSelf) {
          _contactBoneParent.gameObject.SetActive(true);
        }
      }

      float deadzone = DEAD_ZONE_FRACTION * _hand.Fingers[1].Bone((Bone.BoneType)1).Width;
      
      // Update finger contact bones
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1);
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;
          UpdateContactBone(bone, boneArrayIndex, deadzone);
        }
      }

      // Update palm contact bone
      {
        Bone bone = _hand.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL);
        int boneArrayIndex = NUM_FINGERS * BONES_PER_FINGER;
        UpdateContactBone(bone, boneArrayIndex, deadzone);
      }
    }

    private void UpdateContactBone(Bone bone, int boneArrayIndex, float deadzone) {
      ContactBone contactBone = _contactBones[boneArrayIndex];
      Rigidbody body = contactBone.body;

      // This hack works best when we set a fixed rotation for bones.  Otherwise
      // most friction is lost as the bones roll on contact.
      body.MoveRotation(bone.Rotation.ToQuaternion());

      if (contactBone.UpdateTriggering() == false) {
        // Calculate how far off the mark the brushes are.
        float targetingError = Vector3.Distance(contactBone.lastTarget, body.position) / bone.Width;
        float massScale = Mathf.Clamp(1.0f - (targetingError * 2.0f), 0.1f, 1.0f) * Mathf.Clamp(_hand.PalmVelocity.Magnitude * 10f, 1f, 10f);
        body.mass = PER_BONE_MASS * massScale;

        Debug.DrawLine(contactBone.lastTarget, body.position);
        if (targetingError >= DISLOCATION_FRACTION && _hand.PalmVelocity.Magnitude < 1.5f && boneArrayIndex != BONES_PER_FINGER * NUM_FINGERS) {
          contactBone.StartTriggering();
        }
      }

      // Add a deadzone to avoid vibration.
      Vector3 delta = bone.Center.ToVector3() - body.position;
      float deltaLen = delta.magnitude;
      if (deltaLen <= deadzone) {
        body.velocity = Vector3.zero;
        contactBone.lastTarget = body.position;
      }
      else {
        delta *= (deltaLen - deadzone) / deltaLen;
        contactBone.lastTarget = body.position + delta;
        delta /= Time.fixedDeltaTime;
        body.velocity = (delta / delta.magnitude) * Mathf.Clamp(delta.magnitude, 0f, 3f);
      }
    }

    private void FixedUpdateContactCallbacks() {
      foreach (var interactionObj in _contactBehaviourLastFrame) {
        if (!_contactBehaviours.Contains(interactionObj)) {
          interactionObj.ContactEnd(_hand);
          _contactBehaviourRemovalCache.Add(interactionObj);
        }
      }
      foreach (var interactionObj in _contactBehaviourRemovalCache) {
        _contactBehaviourLastFrame.Remove(interactionObj);
      }
      _contactBehaviourRemovalCache.Clear();
      foreach (var interactionObj in _contactBehaviours) {
        if (_contactBehaviourLastFrame.Contains(interactionObj)) {
          interactionObj.ContactStay(_hand);
        }
        else {
          interactionObj.ContactBegin(_hand);
          _contactBehaviourLastFrame.Add(interactionObj);
        }
      }
    }

    #endregion

    #region Grasping

    private HeuristicGrabClassifier _grabClassifier;
    private InteractionBehaviourBase _graspedObject;

    private void InitGrasping() {
      _grabClassifier = new HeuristicGrabClassifier(this);
    }

    private void FixedUpdateGrasping() {
      _grabClassifier.FixedUpdate();

      if (_graspedObject != null) {
        _graspedObject.GraspHold(_hand);
      }
    }

    public void Grasp(InteractionBehaviourBase interactionObj) {
      interactionObj.GraspBegin(_hand);
      _graspedObject = interactionObj;
    }

    public void ReleaseGrasp() {
      if (_graspedObject == null) return; // Nothing to release.

      _grabClassifier.NotifyGraspReleased(_graspedObject);
      _graspedObject.GraspEnd(_hand);
      _graspedObject = null;
    }

    public bool ReleaseObject(InteractionBehaviourBase interactionObj) {
      if (interactionObj == _graspedObject) {
        ReleaseGrasp();
        return true;
      }
      else {
        return false;
      }
    }

    public InteractionBehaviourBase GetGraspedObject() {
      return _graspedObject;
    }

    public HashSet<InteractionBehaviourBase> GetGraspCandidates() {
      return _touchActivityManager.ActiveBehaviours;
    }

    public bool IsGrasping(InteractionBehaviourBase interactionObj) {
      return _graspedObject == interactionObj;
    }

    #endregion

    #region Gizmos

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      // Draw runtime gizmos here
    }

    #endregion

  }

}