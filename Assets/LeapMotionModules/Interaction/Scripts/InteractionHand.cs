using InteractionEngineUtility;
using Leap.Unity.RuntimeGizmos;
using Leap.Unity.Space;
using Leap.Unity.Interaction.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// Specified on a per-object basis to allow Interaction objects
  /// to ignore hover for the left hand, right hand, or both hands.
  /// </summary>
  public enum IgnoreHoverMode { None, Left, Right, Both }

  public class InteractionHand : MonoBehaviour {

    #region Public API

    #region Hand State

    public bool isTracked { get { return _hand != null; } }

    public Hand GetLeapHand() {
      return _hand;
    }

    public Hand GetLastTrackedLeapHand() {
      return _warpedHandData;
    }

    #endregion

    #region Hovering

    /// <summary>
    /// Gets whether the InteractionHand is currently primarily hovering over any interaction object.
    /// </summary>
    public bool isPrimaryHovering { get { return hoverCheckResults.primaryHovered as InteractionBehaviour != null; } }

    /// <summary>
    /// Gets the InteractionBehaviour that is currently this InteractionHand's primary hovered object,
    /// if there is one.
    /// </summary>
    public InteractionBehaviour primaryHoveredObject { get { return hoverCheckResults.primaryHovered as InteractionBehaviour; } }

    /// <summary>
    /// Called when this InteractionHand begins primarily hovering over an InteractionBehaviour.
    /// If the hand has transitioned from one object to another, OnEndPrimaryHoveringObject will
    /// first be called on the old object, then OnBeginPrimaryHoveringObject will be called for
    /// the new object.
    /// </summary>
    public Action<InteractionBehaviour> OnBeginPrimaryHoveringObject = (intObj) => { };

    /// <summary>
    /// Called every (fixed) frame this InteractionHand is primarily hovering over an InteractionBehaviour.
    /// If the hand has transitioned from one object to another, OnEndPrimaryHoveringObject will
    /// first be called on the old object, then OnBeginPrimaryHoveringObject will be called for
    /// the new object.
    /// </summary>
    public Action<InteractionBehaviour> OnStayPrimaryHoveringObject = (intObj) => { };

    /// <summary>
    /// Called when this InteractionHand stops primarily hovering over an InteractionBehaviour.
    /// If the hand has transitioned from one object to another, OnEndPrimaryHoveringObject will
    /// first be called on the old object, then OnBeginPrimaryHoveringObject will be called for
    /// the new object.
    /// </summary>
    public Action<InteractionBehaviour> OnEndPrimaryHoveringObject = (intObj) => { };

    /// <summary>
    /// When set to true, locks the current primarily hovered object, even if the hand gets closer to
    /// a different object.
    /// </summary>
    public void SetInteractionHoverOverride(bool value) {
      _interactionHoverOverride = value;
    }

    #endregion

    #region Grasping

    /// <summary> Gets whether this hand is currently grasping an object. </summary>
    public bool isGraspingObject { get { return _graspedObject != null; } }

    /// <summary> Gets the object this hand is currently grasping, or null if there is no such object. </summary>
    public IInteractionBehaviour graspedObject { get { return _graspedObject; } }

    /// <summary> Gets the set of objects currently considered graspable. </summary>
    public HashSet<IInteractionBehaviour> graspCandidates { get { return graspActivityManager.ActiveObjects; } }

    private Vector3[] _graspingFingertipsCache = new Vector3[5];
    /// <summary>
    /// Returns approximately where the hand is grasping the currently grasped InteractionBehaviour.
    /// (Specifically, the centroid of the grasping fingers.) This method will print an error if the
    /// hand is not currently grasping an object. </summary>
    public Vector3 GetGraspPoint() {
      if (_graspedObject == null) {
        Debug.LogError("Cannot compute grasp point: This hand is not grasping an object.");
        return Vector3.zero;
      }
      using (new ProfilerSample("Compute Grasp Location")) {
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
    }

    private List<InteractionHand> _releasingHandListBuffer = new List<InteractionHand>();
    /// <summary>
    /// Releases the object this hand is holding and returns true if the hand was holding an object,
    /// or false if there was no object to release. The released object will dispatch OnGraspEnd()
    /// immediately. The hand is guaranteed not to be holding an object directly after this method
    /// is called.
    /// </summary>
    public bool ReleaseGrasp() {
      if (_graspedObject == null) {
        return false;
      }
      else {
        _releasingHandListBuffer.Clear();
        _releasingHandListBuffer.Add(this);

        grabClassifier.NotifyGraspReleased(_graspedObject);

        _graspedObject.EndGrasp(_releasingHandListBuffer);
        _graspedObject = null;

        return true;
      }
    }

    /// <summary>
    /// As ReleaseGrasp(), but also outputs the released object into releasedObject if the hand
    /// successfully released an object.
    /// </summary>
    public bool ReleaseGrasp(out IInteractionBehaviour releasedObject) {
      releasedObject = _graspedObject;

      if (ReleaseGrasp()) {
        // releasedObject will be non-null
        return true;
      }

      // releasedObject will be null
      return false;
    }

    /// <summary>
    /// Attempts to release this hand's object, but only if the argument object is the object currently
    /// grasped by this hand. If the hand was holding the argument object, returns true, otherwise returns false.
    /// </summary>
    public bool ReleaseObject(IInteractionBehaviour toRelease) {
      if (_graspedObject == toRelease) {
        ReleaseGrasp();
        return true;
      }
      else {
        return false;
      }
    }

    #endregion

    #endregion

    public InteractionManager interactionManager;
    public Func<Hand> handAccessor;

    private Hand _handData;       // copy of latest tracked hand data; never null, never warped
    private Hand _warpedHandData; // a warped copy of _handData (if warping is necessary, otherwise same as above)
    private Hand _hand;           // null when not tracked, otherwise _handData

    void Start() {
      if (interactionManager == null) interactionManager = InteractionManager.instance;
      if (handAccessor == null) handAccessor = () => { return null; };
      _handData = new Hand();
      _warpedHandData = new Hand();
      RefreshHandState();
    }

    private void RefreshHandState() {
      var hand = handAccessor();
      if (hand != null) { _hand = _handData.CopyFrom(hand); } else { _hand = null; }
    }

    private bool _didHoveringLastFrame, _didContactLastFrame, _didGraspingLastFrame;
    /// <summary>
    /// Called by the InteractionManager every fixed (physics) frame to populate the
    /// Interaction Hand with state from the Leap hand and perform bookkeeping operations.
    /// </summary>
    public void FixedUpdateHand(bool doHovering, bool doContact, bool doGrasping) {
      using (new ProfilerSample("Fixed Update InteractionHand", _brushBoneParent)) {
        RefreshHandState();
        if (doHovering || _didHoveringLastFrame) { FixedUpdateHovering(); }
        if (doContact  || _didContactLastFrame)  { FixedUpdateContact(doContact); }
        if (doGrasping || _didGraspingLastFrame) { FixedUpdateGrasping(); }

        // To support checking for interaction-ends e.g. if grasping is disabled, need to update an extra
        // frame beyond the frame in which the feature was disabled
        _didHoveringLastFrame = doHovering;
        _didContactLastFrame = doContact;
        _didGraspingLastFrame = doGrasping;
      }
    }

    #region Hovering

    private const float MAX_PRIMARY_HOVER_DISTANCE = 0.5F;
    private IInteractionBehaviour _primaryHoveredLastFrame = null;

    // Hover Activity Manager
    private ActivityManager<IInteractionBehaviour> _hoverActivityManager;
    public ActivityManager<IInteractionBehaviour> hoverActivityManager {
      get {
        if (_hoverActivityManager == null) {
          if (hoverActivityFilter == null) hoverActivityFilter = hoverFilterFunc;

          _hoverActivityManager = new ActivityManager<IInteractionBehaviour>(interactionManager.hoverActivationRadius,
                                                                             hoverActivityFilter);

          _hoverActivityManager.activationLayerMask = interactionManager.interactionLayer.layerMask
                                                    | interactionManager.interactionNoContactLayer.layerMask;
        }
        return _hoverActivityManager;
      }
    }

    private Func<Collider, IInteractionBehaviour> hoverActivityFilter;
    private IInteractionBehaviour hoverFilterFunc(Collider collider) {
      if (collider.attachedRigidbody != null) {
        Rigidbody body = collider.attachedRigidbody;
        IInteractionBehaviour intObj;
        if (body != null && interactionManager.rigidbodyRegistry.TryGetValue(body, out intObj)
            && !intObj.ShouldIgnore(this)) {
          return intObj;
        }
      }

      return null;
    }

    // Disables broadphase checks if an object is currently interacting with this hand.
    private bool _interactionHoverOverride = false;

    public struct HoverCheckResults {
      public HashSet<IInteractionBehaviour> hovered;
      public IInteractionBehaviour[] perFingerHovered;
      public IInteractionBehaviour primaryHovered;
      public int primaryHoveringFingerIdx;
      public float primaryHoveredDistance;
      public Hand checkedHand;
    }

    private HoverCheckResults _hoverResults = new HoverCheckResults();

    /// <summary>
    /// Returns the information used to calculate hover and primary hover callbacks from
    /// the latest fixed frame.
    /// </summary>
    public HoverCheckResults hoverCheckResults { get { return _hoverResults; } }

    private void FixedUpdateHovering() {
      using (new ProfilerSample("Fixed Update InteractionHand Hovering")) {
        if (_hand == null && _interactionHoverOverride) { _interactionHoverOverride = false; }

        if (!_interactionHoverOverride) {
          hoverActivityManager.activationRadius = interactionManager.WorldHoverActivationRadius;
          _hoverActivityManager.FixedUpdateQueryPosition((_hand != null) ? _hand.PalmPosition.ToVector3() : Vector3.zero, LeapSpace.allEnabled);
          using (new ProfilerSample("Check for Closest Elements")) { CheckHoverForHand(_hand, _hoverActivityManager.ActiveObjects); }
        }

        ProcessHoverCheckResults();
        ProcessPrimaryHoverCheckResults();

        ISpaceComponent space;
        if (_hand != null) {
          _warpedHandData.CopyFrom(_handData);
          if (_hoverResults.primaryHovered != null && (space = _hoverResults.primaryHovered.space) != null) {
            //Transform bulk hand to the closest element's warped space
            inverseTransformHand(_warpedHandData, space);
          }
        }
      }
    }
    private HashSet<IInteractionBehaviour> _hoveredLastFrame = new HashSet<IInteractionBehaviour>();
    private HashSet<IInteractionBehaviour> _hoverableCache = new HashSet<IInteractionBehaviour>();
    private IInteractionBehaviour[] _tempPerFingerHovered = new IInteractionBehaviour[3];

    private void CheckHoverForHand(Hand hand, HashSet<IInteractionBehaviour> hoverCandidates) {
      _hoverableCache.Clear();
      Array.Clear(_tempPerFingerHovered, 0, _tempPerFingerHovered.Length);

      _hoverResults.hovered = _hoverableCache;
      _hoverResults.perFingerHovered = _tempPerFingerHovered;
      _hoverResults.primaryHovered = null;
      _hoverResults.primaryHoveredDistance = float.PositiveInfinity;
      _hoverResults.primaryHoveringFingerIdx = -1;
      _hoverResults.checkedHand = hand;

      //Loop through all the fingers (that we care about)
      if (hand != null) {
        for (int i = 0; i < 3; i++) {
          if (!hand.Fingers[i].IsExtended && i != 1) { continue; }
          float leastFingerDistance = float.PositiveInfinity;
          Vector3 fingerTip = hand.Fingers[i].TipPosition.ToVector3();

          // Loop through all the candidates
          foreach (IInteractionBehaviour behaviour in hoverCandidates) {
            CheckHoverForBehaviour(fingerTip, behaviour, behaviour.space, i, ref leastFingerDistance, ref _hoverResults);
          }
        }
      }
    }

    public Vector3 transformPoint(Vector3 worldPoint, ISpaceComponent element) {
      if (element.anchor != null && element.anchor.space != null) {
        Vector3 localPos = element.anchor.space.transform.InverseTransformPoint(worldPoint);
        return element.anchor.space.transform.TransformPoint(element.anchor.transformer.InverseTransformPoint(localPos));
      } else {
        return worldPoint;
      }
    }

    private void inverseTransformHand(Hand inHand, ISpaceComponent element) {
      if (element.anchor != null && element.anchor.space != null) {
        Vector3 originalPosition = inHand.Fingers[hoverCheckResults.primaryHoveringFingerIdx].bones[3].NextJoint.ToVector3();
        Quaternion originalRotation = inHand.Fingers[hoverCheckResults.primaryHoveringFingerIdx].bones[3].Rotation.ToQuaternion();

        Vector3 localTipPos = element.anchor.space.transform.InverseTransformPoint(originalPosition);
        Quaternion localTipRot = element.anchor.space.transform.InverseTransformRotation(originalRotation);

        inHand.Transform(-originalPosition, Quaternion.identity);
        inHand.Transform(element.anchor.space.transform.TransformPoint(element.anchor.transformer.InverseTransformPoint(localTipPos)),
                         element.anchor.space.transform.TransformRotation(element.anchor.transformer.InverseTransformRotation(localTipPos, localTipRot)) * Quaternion.Inverse(originalRotation));
      }
    }

    private void CheckHoverForBehaviour(Vector3 position, IInteractionBehaviour behaviour, ISpaceComponent spaceComponent, int whichFinger, ref float leastFingerDistance, ref HoverCheckResults curResults) {

      float distance = float.PositiveInfinity;
      if (spaceComponent == null) {
        distance = behaviour.GetHoverDistance(position);
      } else {
        distance = behaviour.GetHoverDistance(transformPoint(position, spaceComponent));
      }

      curResults.hovered.Add(behaviour);

      if (distance < leastFingerDistance) {
        curResults.perFingerHovered[whichFinger] = behaviour;
        leastFingerDistance = distance;
        if (leastFingerDistance < curResults.primaryHoveredDistance) {
          curResults.primaryHoveredDistance = leastFingerDistance;
          curResults.primaryHovered = curResults.perFingerHovered[whichFinger];
          curResults.primaryHoveringFingerIdx = whichFinger;
        }
      }
    }

    private List<IInteractionBehaviour> _hoverRemovalCache = new List<IInteractionBehaviour>();
    private void ProcessHoverCheckResults() {
      _hoverBeganBuffer.Clear();
      _hoverEndedBuffer.Clear();

      var trackedBehaviours = _hoverActivityManager.ActiveObjects;
      foreach (var hoverable in trackedBehaviours) {
        bool inLastFrame = false, inCurFrame = false;
        if (_hoverResults.hovered.Contains(hoverable)) {
          inCurFrame = true;
        }
        if (_hoveredLastFrame.Contains(hoverable)) {
          inLastFrame = true;
        }

        if (inCurFrame && !inLastFrame) {
          _hoverBeganBuffer.Add(hoverable);
          _hoveredLastFrame.Add(hoverable);
        }
        if (!inCurFrame && inLastFrame) {
          _hoverEndedBuffer.Add(hoverable);
          _hoveredLastFrame.Remove(hoverable);
        }
      }

      foreach (var hoverable in _hoveredLastFrame) {
        if (!trackedBehaviours.Contains(hoverable)) {
          _hoverEndedBuffer.Add(hoverable);
          _hoverRemovalCache.Add(hoverable);
        }
      }
      foreach (var hoverable in _hoverRemovalCache) {
        _hoveredLastFrame.Remove(hoverable);
      }
      _hoverRemovalCache.Clear();
    }

    private HashSet<IInteractionBehaviour> _hoverEndedBuffer = new HashSet<IInteractionBehaviour>();
    private HashSet<IInteractionBehaviour> _hoverBeganBuffer = new HashSet<IInteractionBehaviour>();

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Outputs objects that stopped being hovered by this hand this frame into hoverEndedObjects and returns whether
    /// the output set is empty.
    /// </summary>
    public bool CheckHoverEnd(out HashSet<IInteractionBehaviour> hoverEndedObjects) {
      // Hover checks via the activity manager are robust to destroyed or made-invalid objects,
      // so no additional validity checking is required.
      hoverEndedObjects = _hoverEndedBuffer;
      return _hoverEndedBuffer.Count > 0;
    }

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Outputs objects that began being hovered by this hand this frame into hoverBeganObjects and returns whether
    /// the output set is empty.
    /// </summary>
    public bool CheckHoverBegin(out HashSet<IInteractionBehaviour> hoverBeganObjects) {
      hoverBeganObjects = _hoverBeganBuffer;
      return _hoverBeganBuffer.Count > 0;
    }

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Outputs objects that are currently hovered by this hand into hoveredObjects and returns whether the
    /// output set is empty.
    /// </summary>
    public bool CheckHoverStay(out HashSet<IInteractionBehaviour> hoveredObjects) {
      hoveredObjects = hoverActivityManager.ActiveObjects;
      return hoveredObjects.Count > 0;
    }

    // Primary Hover //

    private void ProcessPrimaryHoverCheckResults() {
      if (_hoverResults.primaryHovered != _primaryHoveredLastFrame) {
        if (_primaryHoveredLastFrame != null) _primaryHoverEndedObject = _primaryHoveredLastFrame;
        else _primaryHoverEndedObject = null;

        _primaryHoveredLastFrame = _hoverResults.primaryHovered;

        if (_primaryHoveredLastFrame != null) _primaryHoverBeganObject = _primaryHoveredLastFrame;
        else _primaryHoverBeganObject = null;
      } else {
        _primaryHoverEndedObject = null;
        _primaryHoverBeganObject = null;
      }
    }

    private IInteractionBehaviour _primaryHoverEndedObject = null;
    private IInteractionBehaviour _primaryHoverBeganObject = null;

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Returns whether an object stopped being primarily hovered by this hand this frame and, if so,
    /// outputs the object into primaryHoverEndedObject (it will be null otherwise).
    /// </summary>
    public bool CheckPrimaryHoverEnd(out IInteractionBehaviour primaryHoverEndedObject) {
      primaryHoverEndedObject = _primaryHoverEndedObject;
      bool primaryHoverEnded = primaryHoverEndedObject != null;

      if (primaryHoverEnded && _primaryHoverEndedObject is InteractionBehaviour) {
        OnEndPrimaryHoveringObject(_primaryHoverEndedObject as InteractionBehaviour);
      }

      return primaryHoverEnded;
    }

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Returns whether an object began being primarily hovered by this hand this frame and, if so,
    /// outputs the object into primaryHoverBeganObject (it will be null otherwise).
    /// </summary>
    public bool CheckPrimaryHoverBegin(out IInteractionBehaviour primaryHoverBeganObject) {
      primaryHoverBeganObject = _primaryHoverBeganObject;
      bool primaryHoverBegan = primaryHoverBeganObject != null;

      if (primaryHoverBegan && _primaryHoverBeganObject is InteractionBehaviour) {
        OnBeginPrimaryHoveringObject(_primaryHoverBeganObject as InteractionBehaviour);
      }

      return primaryHoverBegan;
    }

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Returns whether any object is the primary hover of this hand and, if so, outputs
    /// the object into primaryHoveredObject.
    /// </summary>
    public bool CheckPrimaryHoverStay(out IInteractionBehaviour primaryHoveredObject) {
      primaryHoveredObject = _hoverResults.primaryHovered;
      bool primaryHoverStayed = primaryHoveredObject != null;

      if (primaryHoverStayed && primaryHoveredObject is InteractionBehaviour) {
        OnStayPrimaryHoveringObject(primaryHoveredObject as InteractionBehaviour);
      }

      return primaryHoverStayed;
    }

    #endregion

    #region Contact

    #region Brush Bones & Soft Contact

    private const int NUM_FINGERS = 5;
    private const int BONES_PER_FINGER = 3;
    private const float DEAD_ZONE_FRACTION = 0.1F;
    private const float DISLOCATION_FRACTION = 3.0F;

    private ContactBone[] _brushBones;
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

    private bool InitContact() {
      if (_hand == null) return false;
      InitBrushBoneContainer();
      InitBrushBones();
      _contactInitialized = true;
      return _contactInitialized;
    }

    private void FixedUpdateContact(bool contactEnabled) {
      if (!_contactInitialized) {
        if (!InitContact()) {
          return;
        }
      }

      if (_hand == null && _contactBehaviours.Count > 0) {
        _contactBehaviours.Clear();
      }

      using (new ProfilerSample("Update BrushBones")) { FixedUpdateBrushBones(); }
      using (new ProfilerSample("Update SoftContacts")) { FixedUpdateSoftContact(); }
      using (new ProfilerSample("Update ContactCallbacks")) { FixedUpdateContactState(contactEnabled); }
    }

    private void InitBrushBoneContainer() {
      _brushBoneParent = new GameObject((_warpedHandData.IsLeft ? "Left" : "Right") + " Interaction Hand Contact Bones");
      _brushBoneParent.transform.parent = interactionManager.transform;
      _brushBoneParent.layer = interactionManager.ContactBoneLayer;
    }

    private void InitBrushBones() {
      _brushBones = new ContactBone[NUM_FINGERS * BONES_PER_FINGER + 1];

      // Finger bones
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _warpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

          GameObject contactBoneObj = new GameObject("Contact Fingerbone", typeof(CapsuleCollider), typeof(Rigidbody), typeof(ContactBone));
          contactBoneObj.layer = interactionManager.ContactBoneLayer;

          contactBoneObj.transform.position = bone.Center.ToVector3();
          contactBoneObj.transform.rotation = bone.Rotation.ToQuaternion();
          CapsuleCollider capsule = contactBoneObj.GetComponent<CapsuleCollider>();
          capsule.direction = 2;
          capsule.radius = bone.Width * 0.5f;
          capsule.height = bone.Length + bone.Width;
          capsule.material = ContactMaterial;

          ContactBone contactBone = InitBrushBone(bone, contactBoneObj, boneArrayIndex, capsule);

          contactBone.lastTarget = bone.Center.ToVector3();
        }
      }

      // Palm bone
      {
        // Palm is attached to the third metacarpal and derived from it.
        Bone bone = _warpedHandData.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL);
        int boneArrayIndex = NUM_FINGERS * BONES_PER_FINGER;
        GameObject contactBoneObj = new GameObject("Contact Palm Bone", typeof(BoxCollider), typeof(Rigidbody), typeof(ContactBone));

        contactBoneObj.transform.position = _warpedHandData.PalmPosition.ToVector3();
        contactBoneObj.transform.rotation = _warpedHandData.Rotation.ToQuaternion();
        BoxCollider box = contactBoneObj.GetComponent<BoxCollider>();
        box.center = new Vector3(_warpedHandData.IsLeft ? -0.005f : 0.005f, bone.Width * -0.3f, -0.01f);
        box.size = new Vector3(bone.Length, bone.Width, bone.Length);
        box.material = _material;

        InitBrushBone(null, contactBoneObj, boneArrayIndex, box);
      }

      // Constrain the bones to each other to prevent separation
      AddBrushBoneJoints();

    }

    private ContactBone InitBrushBone(Bone bone, GameObject contactBoneObj, int boneArrayIndex, Collider boneCollider) {
      contactBoneObj.layer = _brushBoneParent.layer;
      // TODO: _contactBoneParent will need its layer set appropriately once interaction layers are implemented.
      contactBoneObj.transform.localScale = Vector3.one;

      ContactBone contactBone = contactBoneObj.GetComponent<ContactBone>();
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

      body.mass = 0.1f;
      body.position = bone != null ? bone.Center.ToVector3() : _warpedHandData.PalmPosition.ToVector3();
      body.rotation = bone != null ? bone.Rotation.ToQuaternion() : _warpedHandData.Rotation.ToQuaternion();
      contactBone.lastTarget = bone != null ? bone.Center.ToVector3() : _warpedHandData.PalmPosition.ToVector3();

      return contactBone;
    }

    private void FixedUpdateBrushBones() {
      if (_hand == null) {
        _brushBoneParent.gameObject.SetActive(false);
        return;
      } else {
        if (!_brushBoneParent.gameObject.activeSelf) {
          _brushBoneParent.gameObject.SetActive(true);
        }
      }

      float deadzone = DEAD_ZONE_FRACTION * _warpedHandData.Fingers[1].Bone((Bone.BoneType)1).Width;

      // Update finger contact bones
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _warpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1);
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;
          FixedUpdateBrushBone(bone, boneArrayIndex, deadzone);
        }
      }

      // Update palm contact bone
      {
        Bone bone = _warpedHandData.Fingers[(int)Finger.FingerType.TYPE_MIDDLE].Bone(Bone.BoneType.TYPE_METACARPAL);
        int boneArrayIndex = NUM_FINGERS * BONES_PER_FINGER;
        FixedUpdateBrushBone(bone, boneArrayIndex, deadzone);
      }
    }

    private void FixedUpdateBrushBone(Bone bone, int boneArrayIndex, float deadzone) {
      ContactBone brushBone = _brushBones[boneArrayIndex];
      Rigidbody body = brushBone.body;

      // This hack works best when we set a fixed rotation for bones.  Otherwise
      // most friction is lost as the bones roll on contact.
      body.MoveRotation(bone.Rotation.ToQuaternion());

      // Calculate how far off the mark the brushes are.
      float targetingError = Vector3.Distance(brushBone.lastTarget, body.position) / bone.Width;
      float massScale = Mathf.Clamp(1.0f - (targetingError * 2.0f), 0.1f, 1.0f)
                      * Mathf.Clamp(_warpedHandData.PalmVelocity.Magnitude * 10f, 1f, 10f);
      body.mass = massScale * brushBone._lastObjectTouchedAdjustedMass;

      //If these conditions are met, stop using brush hands to contact objects and switch to "Soft Contact"
      if (!_softContactEnabled && targetingError >= DISLOCATION_FRACTION
        && _warpedHandData.PalmVelocity.Magnitude < 1.5f && boneArrayIndex != NUM_FINGERS * BONES_PER_FINGER) {
        EnableSoftContact();
        return;
      }

      // Add a deadzone to avoid vibration.
      Vector3 delta = bone.Center.ToVector3() - body.position;
      float deltaLen = delta.magnitude;
      if (deltaLen <= deadzone) {
        body.velocity = Vector3.zero;
        brushBone.lastTarget = body.position;
      } else {
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

    private bool _handWasNullLastFrame = true;

    // Vars removed because Interaction Managers are currently non-optional.
    //private List<PhysicsUtility.SoftContact> softContacts = new List<PhysicsUtility.SoftContact>(40);
    //private Dictionary<Rigidbody, PhysicsUtility.Velocities> originalVelocities = new Dictionary<Rigidbody, PhysicsUtility.Velocities>();

    //private List<int> _softContactIdxRemovalBuffer = new List<int>();

    private void FixedUpdateSoftContact() {
      if (_hand == null) {
        _handWasNullLastFrame = true;
        return;
      } else {
        // If the hand was just initialized, initialize with soft contact.
        if (_handWasNullLastFrame) {
          EnableSoftContact();
        }

        _handWasNullLastFrame = false;
      }

      if (_softContactEnabled) {

        // Generate contacts.
        bool softlyContacting = false;
        for (int fingerIndex = 0; fingerIndex < 5; fingerIndex++) {
          for (int jointIndex = 0; jointIndex < 4; jointIndex++) {
            Bone bone = _warpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            int boneArrayIndex = fingerIndex * 4 + jointIndex;
            Vector3 boneCenter = bone.Center.ToVector3();

            // Generate and fill softContacts with SoftContacts that are intersecting a sphere
            // at boneCenter, with radius softContactBoneRadius.
            bool sphereIntersecting;
            using (new ProfilerSample("Generate Soft Contacts")) {
              sphereIntersecting = PhysicsUtility.generateSphereContacts(boneCenter, _softContactBoneRadius,
                                                                       (boneCenter - _previousBoneCenters[boneArrayIndex])
                                                                         / Time.fixedDeltaTime,
                                                                       1 << interactionManager.interactionLayer,
                                                                       ref interactionManager._softContacts,
                                                                       ref interactionManager._softContactOriginalVelocities,
                                                                       ref _tempColliderArray);
            }

            // Support allowing individual Interaction Behaviours to choose whether they are influenced by soft contact at trigger colliders
            // NOPE nevermind
            //_softContactIdxRemovalBuffer.Clear();
            //for (int i = 0; i < interactionManager._softContacts.Count; i++) {
            //  var softContact = interactionManager._softContacts[i];
            //  IInteractionBehaviour intObj;
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
        } else {
          // If there are no detected Contacts, exit soft contact mode.
          DisableSoftContact();
        }
      }

      // Update the last positions of the bones with this frame.
      for (int fingerIndex = 0; fingerIndex < 5; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < 4; jointIndex++) {
          Bone bone = _warpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
          int boneArrayIndex = fingerIndex * 4 + jointIndex;
          _previousBoneCenters[boneArrayIndex] = bone.Center.ToVector3();
        }
      }
    }

    private void AddBrushBoneJoints() {
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _warpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

          FixedJoint joint = _brushBones[boneArrayIndex].gameObject.AddComponent<FixedJoint>();
          joint.autoConfigureConnectedAnchor = false;
          if (jointIndex != 0) {
            Bone prevBone = _warpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            joint.connectedBody = _brushBones[boneArrayIndex - 1].body;
            joint.anchor = Vector3.back * bone.Length / 2f;
            joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
            _brushBones[boneArrayIndex].joint = joint;
          } else {
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
      _brushBones[NUM_FINGERS * BONES_PER_FINGER].transform.position = _warpedHandData.PalmPosition.ToVector3();
      _brushBones[NUM_FINGERS * BONES_PER_FINGER].transform.rotation = _warpedHandData.Rotation.ToQuaternion();
      for (int fingerIndex = 0; fingerIndex < NUM_FINGERS; fingerIndex++) {
        for (int jointIndex = 0; jointIndex < BONES_PER_FINGER; jointIndex++) {
          Bone bone = _warpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex) + 1); // +1 to skip first bone.
          int boneArrayIndex = fingerIndex * BONES_PER_FINGER + jointIndex;

          if (jointIndex != 0 && _brushBones[boneArrayIndex].joint != null) {
            Bone prevBone = _warpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
            _brushBones[boneArrayIndex].joint.connectedBody = _brushBones[boneArrayIndex - 1].body;
            _brushBones[boneArrayIndex].joint.anchor = Vector3.back * bone.Length / 2f;
            _brushBones[boneArrayIndex].joint.connectedAnchor = Vector3.forward * prevBone.Length / 2f;
          } else if (_brushBones[boneArrayIndex].metacarpalJoint != null) {
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
      using (new ProfilerSample("Enable Soft Contact")) {
        _disableSoftContactEnqueued = false;
        if (!_softContactEnabled) {
          _softContactEnabled = true;
          ResetBrushBoneJoints();
          if (_delayedDisableSoftContactCoroutine != null) {
            interactionManager.StopCoroutine(_delayedDisableSoftContactCoroutine);
          }
          for (int i = _brushBones.Length; i-- != 0;) {
            _brushBones[i].collider.isTrigger = true;
          }

          // Update the last positions of the bones with this frame.
          // This prevents spurious velocities from freshly initialized hands.
          for (int fingerIndex = 0; fingerIndex < 5; fingerIndex++) {
            for (int jointIndex = 0; jointIndex < 4; jointIndex++) {
              Bone bone = _warpedHandData.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
              int boneArrayIndex = fingerIndex * 4 + jointIndex;
              _previousBoneCenters[boneArrayIndex] = bone.Center.ToVector3();
            }
          }
        }
      }
    }

    public void DisableSoftContact() {
      using (new ProfilerSample("Enqueue Disable Soft Contact")) {
        if (!_disableSoftContactEnqueued) {
          _delayedDisableSoftContactCoroutine = DelayedDisableSoftContact();
          interactionManager.StartCoroutine(_delayedDisableSoftContactCoroutine);
          _disableSoftContactEnqueued = true;
        }
      }
    }

    private IEnumerator DelayedDisableSoftContact() {
      if (_disableSoftContactEnqueued) { yield break; }
      yield return new WaitForSecondsRealtime(0.3f);
      if (_disableSoftContactEnqueued) {
        using (new ProfilerSample("Disable Soft Contact")) {
          _softContactEnabled = false;
          for (int i = _brushBones.Length; i-- != 0;) {
            _brushBones[i].collider.isTrigger = false;
          }
          if (_hand != null) ResetBrushBoneJoints();
        }
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

        inHand.Arm.PrevJoint = elbowPos;
        inHand.Arm.Direction = (inHand.Arm.PrevJoint - inHand.Arm.NextJoint).Normalized;
        inHand.Arm.Center = (inHand.Arm.PrevJoint + inHand.Arm.NextJoint) * 0.5f;
      }
    }

    #endregion

    #region Contact Callbacks

    private Dictionary<IInteractionBehaviour, int> _contactBehaviours = new Dictionary<IInteractionBehaviour, int>();
    private HashSet<IInteractionBehaviour> _contactBehavioursLastFrame = new HashSet<IInteractionBehaviour>();
    private List<IInteractionBehaviour> _contactBehaviourRemovalCache = new List<IInteractionBehaviour>();

    private HashSet<IInteractionBehaviour> _contactEndedBuffer = new HashSet<IInteractionBehaviour>();
    private HashSet<IInteractionBehaviour> _contactBeganBuffer = new HashSet<IInteractionBehaviour>();

    internal void ContactBoneCollisionEnter(ContactBone contactBone, IInteractionBehaviour interactionObj, bool wasTrigger) {
      int count;
      if (_contactBehaviours.TryGetValue(interactionObj, out count)) {
        _contactBehaviours[interactionObj] = count + 1;
      } else {
        _contactBehaviours[interactionObj] = 1;
      }
    }

    internal void ContactBoneCollisionExit(ContactBone contactBone, IInteractionBehaviour interactionObj, bool wasTrigger) {
      if (interactionObj.ignoreContact) {
        if (_contactBehaviours.ContainsKey(interactionObj)) _contactBehaviours.Remove(interactionObj);
        return;
      }

      int count = _contactBehaviours[interactionObj];
      if (count == 1) {
        _contactBehaviours.Remove(interactionObj);
      } else {
        _contactBehaviours[interactionObj] = count - 1;
      }
    }

    /// <summary>
    /// Called as a part of the Interaction Hand's general fixed frame update,
    /// before any specific-callback-related updates.
    /// </summary>
    private void FixedUpdateContactState(bool contactEnabled) {
      _contactEndedBuffer.Clear();
      _contactBeganBuffer.Clear();

      foreach (var interactionObj in _contactBehavioursLastFrame) {
        if (!_contactBehaviours.ContainsKey(interactionObj)
         || !_brushBoneParent.gameObject.activeInHierarchy
         || !contactEnabled) {
          _contactEndedBuffer.Add(interactionObj);
          _contactBehaviourRemovalCache.Add(interactionObj);
        }
      }
      foreach (var interactionObj in _contactBehaviourRemovalCache) {
        _contactBehavioursLastFrame.Remove(interactionObj);
      }
      _contactBehaviourRemovalCache.Clear();
      if (_brushBoneParent.gameObject.activeInHierarchy && contactEnabled) {
        foreach (var intObjCountPair in _contactBehaviours) {
          var interactionObj = intObjCountPair.Key;
          if (!_contactBehavioursLastFrame.Contains(interactionObj)) {
            _contactBeganBuffer.Add(interactionObj);
            _contactBehavioursLastFrame.Add(interactionObj);
          }
        }
      }
    }

    private List<IInteractionBehaviour> _removeContactObjsBuffer = new List<IInteractionBehaviour>();
    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Outputs interaction objects that stopped being touched by this hand this frame into contactEndedObjects
    /// and returns whether the output set is empty.
    /// </summary>
    public bool CheckContactEnd(out HashSet<IInteractionBehaviour> contactEndedObjects) {
      // Ensure contact objects haven't been destroyed or set to ignore contact
      _removeContactObjsBuffer.Clear();
      foreach (var objTouchCountPair in _contactBehaviours) {
        if (objTouchCountPair.Key.gameObject == null
            || objTouchCountPair.Key.rigidbody == null
            || objTouchCountPair.Key.ignoreContact
            || _hand == null) {
          _removeContactObjsBuffer.Add(objTouchCountPair.Key);
        }
      }

      // Clean out removed, invalid, or ignoring-contact objects
      foreach (var intObj in _removeContactObjsBuffer) {
        _contactBehaviours.Remove(intObj);
        _contactEndedBuffer.Add(intObj);
      }

      contactEndedObjects = _contactEndedBuffer;
      return _contactEndedBuffer.Count > 0;
    }

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Outputs interaction objects that started being touched by this hand this frame into contactBeganObjects
    /// and returns whether the output set is empty.
    /// </summary>
    public bool CheckContactBegin(out HashSet<IInteractionBehaviour> contactBeganObjects) {
      contactBeganObjects = _contactBeganBuffer;
      return _contactBeganBuffer.Count > 0;
    }

    private HashSet<IInteractionBehaviour> _contactedObjects = new HashSet<IInteractionBehaviour>();
    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Outputs interaction objects that are currently being touched by the hand into contactedObjects
    /// and returns whether the output set is empty.
    /// </summary>
    public bool CheckContactStay(out HashSet<IInteractionBehaviour> contactedObjects) {
      _contactedObjects.Clear();
      foreach (var objCountPair in _contactBehaviours) {
        _contactedObjects.Add(objCountPair.Key);
      }

      contactedObjects = _contactedObjects;
      return contactedObjects.Count > 0;
    }

    #endregion

    #endregion

    #region Grasping

    // Grasp Activity Manager
    private ActivityManager<IInteractionBehaviour> _graspActivityManager;
    /// <summary> Determines which objects are graspable any given frame. </summary>
    private ActivityManager<IInteractionBehaviour> graspActivityManager {
      get {
        if (_graspActivityManager == null) {
          if (graspActivityFilter == null) graspActivityFilter = graspFilterFunc;

          _graspActivityManager = new ActivityManager<IInteractionBehaviour>(1F, graspActivityFilter);

          _graspActivityManager.activationLayerMask = interactionManager.interactionLayer.layerMask
                                                    | interactionManager.interactionNoContactLayer.layerMask;
        }
        return _graspActivityManager;
      }
    }

    private Func<Collider, IInteractionBehaviour> graspActivityFilter;
    private IInteractionBehaviour graspFilterFunc(Collider collider) {
      if (collider.attachedRigidbody != null) {
        Rigidbody body = collider.attachedRigidbody;
        IInteractionBehaviour intObj;
        if (body != null && interactionManager.rigidbodyRegistry.TryGetValue(body, out intObj)
            && !intObj.ignoreGrasping) {
          return intObj;
        }
      }

      return null;
    }

    private HeuristicGrabClassifier _grabClassifier;
    /// <summary> Handles logic determining whether a hand has grabbed or released an interaction object. </summary>
    public HeuristicGrabClassifier grabClassifier {
      get {
        if (_grabClassifier == null) _grabClassifier = new HeuristicGrabClassifier(this);
        return _grabClassifier;
      }
    }

    private IInteractionBehaviour _graspedObject = null;

    private void FixedUpdateGrasping() {
      using (new ProfilerSample("Fixed Update InteractionHand Grasping")) {
        graspActivityManager.FixedUpdateQueryPosition(GetLastTrackedLeapHand().PalmPosition.ToVector3(), LeapSpace.allEnabled);
        grabClassifier.FixedUpdateClassifierHandState();
      }
    }

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Returns true if the hand just released an object and outputs the released object into releasedObject.
    /// </summary>
    public bool CheckGraspEnd(out IInteractionBehaviour releasedObject) {
      releasedObject = null;

      bool shouldReleaseObject = false;

      // Check releasing against interaction state.
      if (_graspedObject == null) {
        return false;
      } else if (_graspedObject.ignoreGrasping) {
        grabClassifier.NotifyGraspReleased(_graspedObject);
        releasedObject = _graspedObject;

        shouldReleaseObject = true;
      }

      // Update the grab classifier to determine if we should release the grasped object.
      if (!shouldReleaseObject) shouldReleaseObject = grabClassifier.FixedUpdateClassifierRelease(out releasedObject);

      if (shouldReleaseObject) {
        _graspedObject = null;
        EnableSoftContact(); // prevent objects popping out of the hand on release
        return true;
      }

      return false;
    }

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Returns true if the hand just grasped an object and outputs the grasped object into graspedObject.
    /// </summary>
    public bool CheckGraspBegin(out IInteractionBehaviour newlyGraspedObject) {
      newlyGraspedObject = null;

      // Check grasping against interaction state.
      if (_graspedObject != null) {
        // Can't grasp any object if we're already grasping one
        return false;
      }

      // Update the grab classifier to determine if we should grasp an object.
      bool shouldGraspObject = grabClassifier.FixedUpdateClassifierGrasp(out newlyGraspedObject);
      if (shouldGraspObject) {
        _graspedObject = newlyGraspedObject;

        return true;
      }

      return false;
    }

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Returns whether there the hand is currently grasping an object and, if it is, outputs that
    /// object into graspedObject.
    /// </summary>
    public bool CheckGraspHold(out IInteractionBehaviour graspedObject) {
      graspedObject = _graspedObject;
      return graspedObject != null;
    }

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Returns whether the hand began suspending an object this frame and, if it did, outputs that
    /// object into suspendedObject.
    /// </summary>
    public bool CheckSuspensionBegin(out IInteractionBehaviour suspendedObject) {
      suspendedObject = null;

      if (_graspedObject != null && _hand == null && !_graspedObject.isSuspended) {
        suspendedObject = _graspedObject;
      }

      return suspendedObject != null;
    }

    /// <summary>
    /// Called by the Interaction Manager every fixed frame.
    /// Returns whether the hand stopped suspending an object this frame and, if it did, outputs that
    /// object into resumedObject.
    /// </summary>
    public bool CheckSuspensionEnd(out IInteractionBehaviour resumedObject) {
      resumedObject = null;

      if (_graspedObject != null && _hand != null && _graspedObject.isSuspended) {
        resumedObject = _graspedObject;
      }

      return resumedObject != null;
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