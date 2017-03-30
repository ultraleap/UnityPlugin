using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.RuntimeGizmos;
using Leap.Unity.Space;
using InteractionEngineUtility;

namespace Leap.Unity.UI.Interaction {

  public class InteractionHand {

    public InteractionManager interactionManager;

    private Func<Hand> _handAccessor;

    private Hand _handData; // copy of latest tracked hand data, never null
    private Hand _hand;     // null when not tracked, otherwise _handData

    public InteractionHand(InteractionManager interactionManager,
                           Func<Hand> handAccessor) {
      this.interactionManager = interactionManager;
      _handAccessor = handAccessor;
      _handData = new Hand();
      RefreshHandState();
    }

    private void RefreshHandState() {
      var hand = _handAccessor();
      if (hand != null) { _hand = _handData.CopyFrom(hand); }
      else { _hand = null; }
    }

    public void FixedUpdateHand(bool doHovering, bool doContact, bool doGrasping) {
      RefreshHandState();

      if (doHovering) FixedUpdateHovering();
      if (doContact || doGrasping) FixedUpdateTouch();
      if (doContact) FixedUpdateContact();
      if (doGrasping) FixedUpdateGrasping();
    }

    public Hand GetLeapHand() {
      return _hand;
    }

    public Hand GetLastTrackedLeapHand() {
      return _handData;
    }

    #region Hovering

    private ActivityManager _hoverActivityManager;
    public ActivityManager hoverActivityManager {
      get {
        if (_hoverActivityManager == null) { _hoverActivityManager = new ActivityManager(interactionManager); }
        return _hoverActivityManager;
      }
    }

    private HoverCheckResults hoverResults = new HoverCheckResults();
    private void FixedUpdateHovering() {
      if (_contactBehaviours.Count == 0) {
        hoverActivityManager.activationRadius = interactionManager.WorldHoverActivationRadius;
        _hoverActivityManager.FixedUpdateHand((_hand != null) ? _hand.PalmPosition.ToVector3() : Vector3.zero, LeapSpace.allEnabled);
        hoverResults = CheckHoverForHand(_hand, _hoverActivityManager.ActiveBehaviours);
        ProcessHoverCheckResults(hoverResults);
        ProcessPrimaryHoverCheckResults(hoverResults);
      }

      ISpaceComponent space;
      if (_hand != null && hoverResults.primaryHovered != null && (space = hoverResults.primaryHovered.GetComponent<ISpaceComponent>()) != null) {
        //Transform bulk hand to the closest element's warped space
        coarseInverseTransformHand(_hand, space);
      }
    }

    private struct HoverCheckResults {
      public HashSet<InteractionBehaviourBase> hovered;
      public InteractionBehaviourBase[] perFingerHovered;
      public InteractionBehaviourBase primaryHovered;
      public float primaryHoveredDistance;
      public Hand checkedHand;
    }

    private HashSet<InteractionBehaviourBase> _hoveredLastFrame = new HashSet<InteractionBehaviourBase>();

    private HashSet<InteractionBehaviourBase> _hoverableCache = new HashSet<InteractionBehaviourBase>();
    public InteractionBehaviourBase[] tempPerFingerHovered = new InteractionBehaviourBase[3];

    private HoverCheckResults CheckHoverForHand(Hand hand, HashSet<InteractionBehaviourBase> hoverCandidates) {
      _hoverableCache.Clear();
      Array.Clear(tempPerFingerHovered, 0, tempPerFingerHovered.Length);

      HoverCheckResults results = new HoverCheckResults() {
        hovered = _hoverableCache,
        perFingerHovered = tempPerFingerHovered,
        primaryHovered = null,
        primaryHoveredDistance = float.PositiveInfinity,
        checkedHand = hand
      };

      //Loop through all the fingers (that we care about)
      if (hand != null) {
        float leastHandDistance = float.PositiveInfinity;
        for (int i = 0; i < 3; i++) {
          if (!hand.Fingers[i].IsExtended) { continue; }
          float leastFingerDistance = float.PositiveInfinity;
          Vector3 fingerTip = hand.Fingers[i].TipPosition.ToVector3();

          //Loop through all the candidates
          foreach (InteractionBehaviourBase elem in hoverCandidates) {
            if (elem.ignoreHover) continue;
            ISpaceComponent element = elem.GetComponent<ISpaceComponent>();
            if (element != null) {
              CheckHoverForElement(fingerTip, elem, element, i, leastFingerDistance, leastHandDistance, ref results);
            } else {
              CheckHoverForBehavior(fingerTip, elem, ref results);
            }
          }
        }
      }

      return results;
    }

    public Vector3 transformPoint(Vector3 worldPoint, ISpaceComponent element) {
      Vector3 localPos = element.anchor.space.transform.InverseTransformPoint(worldPoint);
      return element.anchor.space.transform.TransformPoint(element.anchor.transformer.InverseTransformPoint(localPos));
    }

    public void coarseInverseTransformHand(Hand inHand, ISpaceComponent element) {
      Vector3 localPalmPos = element.anchor.space.transform.InverseTransformPoint(inHand.PalmPosition.ToVector3());
      Quaternion localPalmRot = element.anchor.space.transform.InverseTransformRotation(inHand.Rotation.ToQuaternion());

      inHand.SetTransform(element.anchor.space.transform.TransformPoint(element.anchor.transformer.InverseTransformPoint(localPalmPos)),
                          element.anchor.space.transform.TransformRotation(element.anchor.transformer.InverseTransformRotation(localPalmPos, localPalmRot)));
    }

    private void CheckHoverForElement(Vector3 position, InteractionBehaviourBase behaviour, ISpaceComponent element, int whichFinger, float leastFingerDistance, float leastHandDistance, ref HoverCheckResults curResults) {
      //NEED BETTER DISTANCE FUNCTION
      float dist = Vector3.SqrMagnitude(((Component)element).transform.position - transformPoint(position, element));

      if (dist < leastFingerDistance) {
        curResults.perFingerHovered[whichFinger] = behaviour;
        leastFingerDistance = dist;
        if (leastFingerDistance < curResults.primaryHoveredDistance) {
          curResults.primaryHoveredDistance = leastFingerDistance;
          curResults.primaryHovered = curResults.perFingerHovered[whichFinger];
        }
      }
    }

    private void CheckHoverForBehavior(Vector3 position, InteractionBehaviourBase hoverable, ref HoverCheckResults curResults) {
      float distance = hoverable.GetDistance(position);
      if (distance > 0F) {
        curResults.hovered.Add(hoverable);
      }
      if (distance < MAX_PRIMARY_HOVER_DISTANCE && distance < curResults.primaryHoveredDistance) {
        curResults.primaryHovered = hoverable;
        curResults.primaryHoveredDistance = distance;
      }
    }

    private List<InteractionBehaviourBase> _hoverRemovalCache = new List<InteractionBehaviourBase>();
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
          _hoverRemovalCache.Add(hoverable);
        }
      }
      foreach (var hoverable in _hoverRemovalCache) {
        _hoveredLastFrame.Remove(hoverable);
      }
      _hoverRemovalCache.Clear();
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
    private InteractionBehaviourBase _primaryHoveredLastFrame = null;

    #endregion

    #region Touch (common logic for Contact and Grasping)

    private ActivityManager __touchActivityManager;
    private ActivityManager _touchActivityManager {
      get {
        if (__touchActivityManager == null) { __touchActivityManager = new ActivityManager(interactionManager); }
        return __touchActivityManager;
      }
    }

    private void FixedUpdateTouch() {
      _touchActivityManager.activationRadius = interactionManager.WorldTouchActivationRadius;
      _touchActivityManager.FixedUpdateHand((_hand != null) ? _hand.PalmPosition.ToVector3() : Vector3.zero);
    }

    #endregion

    #region Contact

    #region Brush Bones & Soft Contact

    private const int NUM_FINGERS = 5;
    private const int BONES_PER_FINGER = 3;
    public  const float PER_BONE_MASS_MULTIPLIER = 0.2F;
    private const float DEAD_ZONE_FRACTION = 0.1F;
    private const float DISLOCATION_FRACTION = 3.0F;

    private BrushBone[] _brushBones;
    private GameObject _brushBoneParent;

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

    private bool _contactInitialized = false;

    private void InitContact() {
      if (_hand == null) return;
      InitBrushBoneContainer();
      InitBrushBones();
      _contactInitialized = true;
    }

    private void FixedUpdateContact() {
      if (!_contactInitialized) {
        InitContact();
      }
      if (!_contactInitialized) {
        return;
      }

      FixedUpdateBrushBones();
      FixedUpdateSoftContact();
      FixedUpdateContactCallbacks();
    }

    private void InitBrushBoneContainer() {
      _brushBoneParent = new GameObject((_hand.IsLeft ? "Left" : "Right") + " Interaction Hand Contact Bones");
      _brushBoneParent.transform.parent = interactionManager.transform;
      _brushBoneParent.layer = interactionManager.ContactBoneLayer;
    }

    private void InitBrushBones() {
      _brushBones = new BrushBone[NUM_FINGERS * BONES_PER_FINGER + 1];

      // Finger bones
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

          GameObject contactBoneObj = new GameObject("Contact Fingerbone", typeof(CapsuleCollider), typeof(Rigidbody), typeof(BrushBone));
          contactBoneObj.layer = interactionManager.ContactBoneLayer;

          contactBoneObj.transform.position = bone.Center.ToVector3();
          contactBoneObj.transform.rotation = bone.Rotation.ToQuaternion();
          CapsuleCollider capsule = contactBoneObj.GetComponent<CapsuleCollider>();
          capsule.direction = 2;
          capsule.radius = bone.Width * 0.5f;
          capsule.height = bone.Length + bone.Width;
          capsule.material = ContactMaterial;

          BrushBone contactBone = InitBrushBone(bone, contactBoneObj, boneArrayIndex, capsule);

          contactBone.lastTarget = bone.Center.ToVector3();
        }
      }

      // Palm bone
      {
        // Palm is attached to the third metacarpal and derived from it.
        Bone bone = _hand.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL);
        int boneArrayIndex = NUM_FINGERS * BONES_PER_FINGER;
        GameObject contactBoneObj = new GameObject("Contact Palm Bone", typeof(BoxCollider), typeof(Rigidbody), typeof(BrushBone));

        contactBoneObj.transform.position = _hand.PalmPosition.ToVector3();
        contactBoneObj.transform.rotation = _hand.Rotation.ToQuaternion();
        BoxCollider box = contactBoneObj.GetComponent<BoxCollider>();
        box.center = new Vector3(_hand.IsLeft ? -0.005f : 0.005f, bone.Width * -0.3f, -0.01f);
        box.size = new Vector3(bone.Length, bone.Width, bone.Length);
        box.material = _material;

        InitBrushBone(null, contactBoneObj, boneArrayIndex, box);
      }

      // Constrain the bones to each other to prevent separation
      AddBrushBoneJoints();

    }

    private BrushBone InitBrushBone(Bone bone, GameObject contactBoneObj, int boneArrayIndex, Collider boneCollider) {
      contactBoneObj.layer = _brushBoneParent.layer;
      // TODO: _contactBoneParent will need its layer set appropriately once interaction layers are implemented.
      contactBoneObj.transform.localScale = Vector3.one;

      BrushBone contactBone = contactBoneObj.GetComponent<BrushBone>();
      contactBone.collider = boneCollider;
      contactBone.interactionHand = this;
      _brushBones[boneArrayIndex] = contactBone;

      Transform capsuleTransform = contactBoneObj.transform;
      capsuleTransform.SetParent(_brushBoneParent.transform, false);

      Rigidbody body = contactBoneObj.GetComponent<Rigidbody>();
      body.freezeRotation = true;
      contactBone.body = body;
      body.useGravity = false;
      body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // TODO: Allow different collision detection modes as an optimization.
      if (boneCollider is BoxCollider) {
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
      }

      body.mass = PER_BONE_MASS_MULTIPLIER;
      body.position = bone != null ? bone.Center.ToVector3() : _hand.PalmPosition.ToVector3();
      body.rotation = bone != null ? bone.Rotation.ToQuaternion() : _hand.Rotation.ToQuaternion();
      contactBone.lastTarget = bone != null ? bone.Center.ToVector3() : _hand.PalmPosition.ToVector3();

      return contactBone;
    }

    private void FixedUpdateBrushBones() {
      if (_hand == null) {
        _brushBoneParent.gameObject.SetActive(false);
        return;
      }
      else {
        if (!_brushBoneParent.gameObject.activeSelf) {
          _brushBoneParent.gameObject.SetActive(true);
        }
      }

      float deadzone = DEAD_ZONE_FRACTION * _hand.Fingers[1].Bone((Bone.BoneType)1).Width;
      
      // Update finger contact bones
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1);
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;
          FixedUpdateBrushBone(bone, boneArrayIndex, deadzone);
        }
      }

      // Update palm contact bone
      {
        Bone bone = _hand.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL);
        int boneArrayIndex = NUM_FINGERS * BONES_PER_FINGER;
        FixedUpdateBrushBone(bone, boneArrayIndex, deadzone);
      }
    }

    private void FixedUpdateBrushBone(Bone bone, int boneArrayIndex, float deadzone) {
      BrushBone brushBone = _brushBones[boneArrayIndex];
      Rigidbody body = brushBone.body;

      // This hack works best when we set a fixed rotation for bones.  Otherwise
      // most friction is lost as the bones roll on contact.
      body.MoveRotation(bone.Rotation.ToQuaternion());

      // Calculate how far off the mark the brushes are.
      float targetingError = Vector3.Distance(brushBone.lastTarget, body.position) / bone.Width;
      float massScale = Mathf.Clamp(1.0f - (targetingError * 2.0f), 0.1f, 1.0f)
                        * Mathf.Clamp(_hand.PalmVelocity.Magnitude * 10f, 1f, 10f);
      body.mass = PER_BONE_MASS_MULTIPLIER * massScale * brushBone._lastObjectTouchedMass;

      //If these conditions are met, stop using brush hands to contact objects and switch to "Soft Contact"
      if (!_softContactEnabled && targetingError >= DISLOCATION_FRACTION
          && _hand.PalmVelocity.Magnitude < 1.5f && boneArrayIndex != NUM_FINGERS * BONES_PER_FINGER) {
        EnableSoftContact();
        return;
      }

      // Add a deadzone to avoid vibration.
      Vector3 delta = bone.Center.ToVector3() - body.position;
      float deltaLen = delta.magnitude;
      if (deltaLen <= deadzone) {
        body.velocity = Vector3.zero;
        brushBone.lastTarget = body.position;
      }
      else {
        delta *= (deltaLen - deadzone) / deltaLen;
        brushBone.lastTarget = body.position + delta;
        delta /= Time.fixedDeltaTime;
        body.velocity = (delta / delta.magnitude) * Mathf.Clamp(delta.magnitude, 0f, 100f);
      }
    }

    private bool _softContactEnabled = false;
    private bool _disableSoftContactEnqueued = false;
    private IEnumerator _delayedDisableSoftContactCoroutine;
    private Collider[] _tempColliderArray = new Collider[2];
    private Vector3[] _previousBoneCenters = new Vector3[20];
    private float _softContactBoneRadius = 0.015f;

    // Vars removed because Interaction Managers are currently non-optional.
    //private List<PhysicsUtility.SoftContact> softContacts = new List<PhysicsUtility.SoftContact>(40);
    //private Dictionary<Rigidbody, PhysicsUtility.Velocities> originalVelocities = new Dictionary<Rigidbody, PhysicsUtility.Velocities>();

    //private List<int> _softContactIdxRemovalBuffer = new List<int>();
    private void FixedUpdateSoftContact() {
      if (_hand == null) return;

      if (_softContactEnabled) {

        // Generate contacts.
        bool softlyContacting = false;
        for (int fingerIndex = 0; fingerIndex < 5; fingerIndex++) {
          for (int jointIndex = 0; jointIndex < 4; jointIndex++) {
            Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            int boneArrayIndex = fingerIndex * 4 + jointIndex;
            Vector3 boneCenter = bone.Center.ToVector3();

            // Generate and fill softContacts with SoftContacts that are intersecting a sphere
            // at boneCenter, with radius softContactBoneRadius.
            bool sphereIntersecting;
            sphereIntersecting = PhysicsUtility.generateSphereContacts(boneCenter, _softContactBoneRadius,
                                                                       (boneCenter - _previousBoneCenters[boneArrayIndex])
                                                                         / Time.fixedDeltaTime,
                                                                       1 << interactionManager.interactionLayer,
                                                                       ref interactionManager._softContacts,
                                                                       ref interactionManager._softContactOriginalVelocities,
                                                                       ref _tempColliderArray);

            // Support allowing individual Interaction Behaviours to choose whether they are influenced by soft contact at trigger colliders
            // NOPE nevermind
            //_softContactIdxRemovalBuffer.Clear();
            //for (int i = 0; i < interactionManager._softContacts.Count; i++) {
            //  var softContact = interactionManager._softContacts[i];
            //  InteractionBehaviourBase intObj;
            //  if (interactionManager.rigidbodyRegistry.TryGetValue(softContact.body, out intObj)) {
            //    if (softContact.touchingTrigger && !intObj.allowContactOnTriggers) {
            //      _softContactIdxRemovalBuffer.Add(i);
            //    }
            //  }
            //}
            //// TODO: Consider making the soft contact list a HashSet instead to support O(1) removal
            //for (int i = _softContactIdxRemovalBuffer.Count - 1; i >= 0; i++) {
            //  interactionManager._softContacts.RemoveAt(_softContactIdxRemovalBuffer[i]);
            //}

            softlyContacting = sphereIntersecting ? true : softlyContacting;
          }
        }


        if (softlyContacting) {
          _disableSoftContactEnqueued = false;
        }
        else {
          // If there are no detected Contacts, exit soft contact mode.
          DisableSoftContact();
        }
      }

      // Update the last positions of the bones with this frame.
      for (int fingerIndex = 0; fingerIndex < 5; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < 4; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
          int boneArrayIndex = fingerIndex * 4 + jointIndex;
          _previousBoneCenters[boneArrayIndex] = bone.Center.ToVector3();
        }
      }
    }

    private void AddBrushBoneJoints() {
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

          FixedJoint joint = _brushBones[boneArrayIndex].gameObject.AddComponent<FixedJoint>();
          joint.autoConfigureConnectedAnchor = false;
          if (jointIndex != 0) {
            Bone prevBone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            joint.connectedBody = _brushBones[boneArrayIndex - 1].body;
            joint.anchor = Vector3.back * bone.Length / 2f;
            joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
            _brushBones[boneArrayIndex].joint = joint;
          }
          else {
            joint.connectedBody = _brushBones[NUM_FINGERS * BONES_PER_FINGER].body;
            joint.anchor = Vector3.back * bone.Length / 2f;
            joint.connectedAnchor = _brushBones[NUM_FINGERS * BONES_PER_FINGER].transform.InverseTransformPoint(bone.PrevJoint.ToVector3());
            _brushBones[boneArrayIndex].metacarpalJoint = joint;
          }
        }
      }
    }

    /// <summary> Reconnects and resets all the joints in the hand. </summary>
    private void ResetBrushBoneJoints() {
      _brushBones[NUM_FINGERS * BONES_PER_FINGER].transform.position = _hand.PalmPosition.ToVector3();
      _brushBones[NUM_FINGERS * BONES_PER_FINGER].transform.rotation = _hand.Rotation.ToQuaternion();
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

          if (jointIndex != 0 && _brushBones[boneArrayIndex].joint != null) {
            Bone prevBone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            _brushBones[boneArrayIndex].joint.connectedBody = _brushBones[boneArrayIndex - 1].body;
            _brushBones[boneArrayIndex].joint.anchor = Vector3.back * bone.Length / 2f;
            _brushBones[boneArrayIndex].joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
          }
          else if (_brushBones[boneArrayIndex].metacarpalJoint != null) {
            _brushBones[boneArrayIndex].metacarpalJoint.connectedBody = _brushBones[NUM_FINGERS * BONES_PER_FINGER].body;
            _brushBones[boneArrayIndex].metacarpalJoint.anchor = Vector3.back * bone.Length / 2f;
            _brushBones[boneArrayIndex].metacarpalJoint.connectedAnchor = _brushBones[NUM_FINGERS * BONES_PER_FINGER].transform
                                                                            .InverseTransformPoint(bone.PrevJoint.ToVector3());
          }
        }
      }
    }

    public void EnableSoftContact() {
      if (_hand == null) return;
      _disableSoftContactEnqueued = false;
      if (!_softContactEnabled) {
        _softContactEnabled = true;
        ResetBrushBoneJoints();
        if (_delayedDisableSoftContactCoroutine != null) {
          interactionManager.StopCoroutine(_delayedDisableSoftContactCoroutine);
        }
        for (int i = _brushBones.Length; i-- != 0; ) {
          _brushBones[i].collider.isTrigger = true;
        }

        // Update the last positions of the bones with this frame.
        // This prevents spurious velocities from freshly initialized hands.
        for (int fingerIndex = 0; fingerIndex < 5; fingerIndex++) {
          for (int jointIndex = 0; jointIndex < 4; jointIndex++) {
            Bone bone = _hand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            int boneArrayIndex = fingerIndex * 4 + jointIndex;
            _previousBoneCenters[boneArrayIndex] = bone.Center.ToVector3();
          }
        }
      }
    }

    public void DisableSoftContact() {
      if (!_disableSoftContactEnqueued) {
        _delayedDisableSoftContactCoroutine = DelayedDisableSoftContact();
        interactionManager.StartCoroutine(_delayedDisableSoftContactCoroutine); 
        _disableSoftContactEnqueued = true;
      }
    }

    private IEnumerator DelayedDisableSoftContact() {
      if (_disableSoftContactEnqueued) { yield break; }
      yield return new WaitForSecondsRealtime(0.3f);
      if (_disableSoftContactEnqueued) {
        _softContactEnabled = false;
        for (int i = _brushBones.Length; i-- != 0;) {
          _brushBones[i].collider.isTrigger = false;
        }
        if (_hand != null) ResetBrushBoneJoints();
      }
    }

    /// <summary> Constructs a Hand object from this InteractionHand. Optional utility/debug function.
    /// Can be used to display a graphical hand that matches the physical one. </summary>
    public void FillBones(Hand inHand) {
      if (_softContactEnabled) { return; }
      if (Application.isPlaying && _brushBones.Length == NUM_FINGERS * BONES_PER_FINGER + 1) {
        Vector elbowPos = inHand.Arm.ElbowPosition;
        inHand.SetTransform(_brushBones[NUM_FINGERS * BONES_PER_FINGER].body.position, _brushBones[NUM_FINGERS * BONES_PER_FINGER].body.rotation);

        for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
          for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
            Bone bone = inHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1);
            int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;
            Vector displacement = _brushBones[boneArrayIndex].body.position.ToVector() - bone.Center;
            bone.Center += displacement;
            bone.PrevJoint += displacement;
            bone.NextJoint += displacement;
            bone.Rotation = _brushBones[boneArrayIndex].body.rotation.ToLeapQuaternion();
          }
        }

        inHand.Arm.PrevJoint = elbowPos; inHand.Arm.Direction = (inHand.Arm.PrevJoint - inHand.Arm.NextJoint).Normalized; inHand.Arm.Center = (inHand.Arm.PrevJoint + inHand.Arm.NextJoint) / 2f;
      }
    }

    #endregion

    #region Contact Callbacks

    private Dictionary<InteractionBehaviourBase, int> _contactBehaviours = new Dictionary<InteractionBehaviourBase, int>();
    private HashSet<InteractionBehaviourBase> _contactBehavioursLastFrame = new HashSet<InteractionBehaviourBase>();
    private List<InteractionBehaviourBase>    _contactBehaviourRemovalCache = new List<InteractionBehaviourBase>();

    internal void ContactBoneCollisionEnter(BrushBone contactBone, InteractionBehaviourBase interactionObj, bool wasTrigger) {
      int count;
      if (_contactBehaviours.TryGetValue(interactionObj, out count)) {
        _contactBehaviours[interactionObj] = count + 1;
      }
      else {
        _contactBehaviours[interactionObj] = 1;
      }
    }

    internal void ContactBoneCollisionExit(BrushBone contactBone, InteractionBehaviourBase interactionObj, bool wasTrigger) {
      int count = _contactBehaviours[interactionObj];
      if (count == 1) {
        _contactBehaviours.Remove(interactionObj);
      }
      else {
        _contactBehaviours[interactionObj] = count - 1;
      }
    }

    // TODO: Is there a need for separate methods for Triggers?
    //internal void ContactBoneTriggerEnter(ContactBone contactBone, InteractionBehaviourBase interactionObjr) {
    //}

    //internal void ContactBoneTriggerExit(ContactBone contactBone, InteractionBehaviourBase interactionObj) {
    //}

    private void FixedUpdateContactCallbacks() {
      foreach (var interactionObj in _contactBehavioursLastFrame) {
        if (!_contactBehaviours.ContainsKey(interactionObj)
         || !_brushBoneParent.gameObject.activeSelf) {
          interactionObj.ContactEnd(_hand);
          _contactBehaviourRemovalCache.Add(interactionObj);
        }
      }
      foreach (var interactionObj in _contactBehaviourRemovalCache) {
        _contactBehavioursLastFrame.Remove(interactionObj);
      }
      _contactBehaviourRemovalCache.Clear();
      if (_brushBoneParent.gameObject.activeSelf) {
        foreach (var intObjCountPair in _contactBehaviours) {
          var interactionObj = intObjCountPair.Key;
          if (_contactBehavioursLastFrame.Contains(interactionObj)) {
            interactionObj.ContactStay(_hand);
          }
          else {
            interactionObj.ContactBegin(_hand);
            _contactBehavioursLastFrame.Add(interactionObj);
          }
        }
      }
    }

    #endregion

    #endregion

    #region Grasping

    private HeuristicGrabClassifier _grabClassifier;
    private HeuristicGrabClassifier grabClassifier {
      get {
        if (_grabClassifier == null) _grabClassifier = new HeuristicGrabClassifier(this);
        return _grabClassifier;
      }
    }

    private InteractionBehaviourBase _graspedObject;

    private void FixedUpdateGrasping() {
      // Suspension / Resume
      if (_graspedObject != null) {
        if (_hand == null && !_graspedObject.isSuspended) {
          _graspedObject.GraspSuspendObject();
        }
        else if (_hand != null && _graspedObject.isSuspended) {
          _graspedObject.GraspResumeObject();
        }
      }

      // Grab classifier update
      if (_graspedObject == null || !_graspedObject.isSuspended) {
        grabClassifier.FixedUpdateHeuristicClassifier(_hand);
      }

      // Call GraspHold if still grasping the object
      if (_graspedObject != null) {
        if (_graspedObject.ignoreGrasping) {
          ReleaseGrasp();
        }
        else {
          _graspedObject.GraspHold(_hand);
        }
      }
    }

    public void Grasp(InteractionBehaviourBase interactionObj) {
      interactionObj.GraspBegin(_hand);
      _graspedObject = interactionObj;
    }

    public void ReleaseGrasp() {
      if (_graspedObject == null) {
        Debug.LogWarning("ReleaseGrasp() was called, but there is no held object to release.");
        return; // Nothing to release.
      }

      // Enable Soft Contact if contact in general is enabled to prevent objects
      // popping right out of the hand.
      if (_contactInitialized && interactionManager.enableContact) {
        EnableSoftContact();
      }

      grabClassifier.NotifyGraspReleased(_graspedObject);
      _graspedObject.GraspEnd(this);
      _graspedObject = null;
    }

    public bool TryReleaseObject(InteractionBehaviourBase interactionObj) {
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

    private Vector3[] _graspingFingertipsCache = new Vector3[5];
    /// <summary> Returns approximately where the hand is grasping
    /// the currently grasped InteractionBehaviour.
    /// (Specifically, the centroid of the grasping fingers.)
    /// This method will print an error if the hand is not currently
    /// grasping an object. </summary>
    public Vector3 GetGraspPoint() {
      if (_graspedObject == null) {
        Debug.LogError("Cannot compute grasp point: This hand is not grasping an object.");
        return Vector3.zero;
      }
      int numGraspingFingertips;
      Vector3 sum = Vector3.zero; ;
      grabClassifier.GetGraspingFingertipPositions(_graspedObject, _graspingFingertipsCache, out numGraspingFingertips);
      if (numGraspingFingertips == 0) {
        Debug.LogError("Cannot compute grasp point: The hand has a grasped object, but this object is not classified as grasped by the classifier.");
      }
      for (int i = 0; i < numGraspingFingertips; i++) {
        sum += _graspingFingertipsCache[i];
      }
      sum /= numGraspingFingertips;
      return sum;
    }

    private HashSet<InteractionBehaviourBase> _graspCandidatesBuffer = new HashSet<InteractionBehaviourBase>();
    public HashSet<InteractionBehaviourBase> GetGraspCandidates() {
      _graspCandidatesBuffer.Clear();
      foreach (var intObj in _touchActivityManager.ActiveBehaviours) {
        if (!intObj.ignoreGrasping) {
          _graspCandidatesBuffer.Add(intObj);
        }
      }
      return _graspCandidatesBuffer;
    }

    public bool isGraspingObject { get { return _graspedObject != null; } }

    public bool IsGrasping(InteractionBehaviourBase interactionObj) {
      return _graspedObject == interactionObj;
    }

    #endregion

    #region Gizmos

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (_brushBoneParent != null) {
        drawer.DrawColliders(_brushBoneParent, true, true);
      }
    }

    #endregion

  }

}