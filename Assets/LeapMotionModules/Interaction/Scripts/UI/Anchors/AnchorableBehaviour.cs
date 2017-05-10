/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Attributes;
using Leap.Unity.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// AnchorableBehaviours mix well with InteractionBehaviours you'd like
  /// to be able to pick up and place in specific locations, specified by
  /// other GameObjects with an Anchor component.
  /// </summary>
  public class AnchorableBehaviour : MonoBehaviour {

    [Disable]
    [SerializeField]
    [Tooltip("Whether or not this AnchorableBehaviour is actively attached to its anchor.")]
    private bool _isAttached = false;
    public bool isAttached { get { return _isAttached; } }

    [Tooltip("The current anchor of this AnchorableBehaviour.")]
    [OnEditorChange("anchor"), SerializeField]
    private Anchor _anchor;
    public Anchor anchor {
      get {
        return _anchor;
      }
      set {
        if (_anchor != value) {
          if (IsValidAnchor(value)) {
            if (_anchor != null) {
              OnDetachedFromAnchor.Invoke(this, _anchor);
              _anchor.NotifyUnanchored(this);
            }

            _isLockedToAnchor = false;
            _anchor = value;
            _hasTargetPositionLastUpdate = false;

            if (_anchor != null) {
              _anchor.NotifyAnchored(this);
              OnAttachedToAnchor.Invoke(this, _anchor);
            }
          }
          else {
            Debug.LogWarning("The '" + value.name + "' anchor is not in " + this.name + "'s anchor group.", this.gameObject);
          }
        }
      }
    }

    [Tooltip("The anchor group for this AnchorableBehaviour. If set to null, all Anchors "
           + "will be valid anchors for this object.")]
    [OnEditorChange("anchorGroup"), SerializeField]
    private AnchorGroup _anchorGroup;
    public AnchorGroup anchorGroup {
      get { return _anchorGroup; }
      set {
        if (_anchorGroup != null) _anchorGroup.NotifyAnchorableObjectRemoved(this);

        _anchorGroup = value;
        if (anchor != null && !_anchorGroup.Contains(anchor)) {
          anchor = null;
          Debug.LogWarning(this.name + "'s anchor is not within its anchorGroup (setting it to null).", this.gameObject);
        }

        if (_anchorGroup != null) _anchorGroup.NotifyAnchorableObjectAdded(this);
      }
    }

    [Header("Attachment")]

    // TODO: Delete me
    //[Tooltip("If disabled, this anchorable object will disregard an anchor's range when seeking "
    //       + "an anchor to attach to.")]
    //public bool requireAnchorWithinRange = true;

    [Tooltip("Anchors beyond this range are ignored as possible anchors for this object.")]
    public float maxAnchorRange = 0.3F;

    [Tooltip("Only allowed when an InteractionBehaviour is attached to this object. If enabled, this "
           + "object's Attach() method or its variants will weigh its velocity towards an anchor along "
           + "with its proximity when seeking an anchor to attach to.")]
    [DisableIf("_interactionBehaviourIsNull", true)]
    public bool useTrajectory = true;

    [Tooltip("The maximum angle this object's trajectory can be away from an anchor to consider it as "
           + "an anchor to attach to.")]
    [SerializeField]
    [Range(20F, 90F)]
    private float _maxAttachmentAngle = 60F;
    /// <summary> Calculated via _maxAttachmentAngle. </summary>
    private float _minAttachmentDotProduct;

    [Header("Motion")]

    [Tooltip("Should the object move instantly to the anchor position?")]
    public bool lockToAnchor = false;

    [Tooltip("Should the object move smoothly towards the anchor at first, but lock to it once it reaches the anchor? "
           + "Note: Disabling the AnchorableBehaviour will stop the object from moving towards its anchor, and will "
           + "'release' it from the anchor, so that on re-enable the object will smoothly move to the anchor again.")]
    [DisableIf("lockToAnchor", isEqualTo: true)]
    public bool lockWhenAttached = true;

    [Tooltip("While this object is moving smoothly towards its anchor, should it also inherit the motion of the "
           + "anchor itself if the anchor is not stationary? Otherwise, the anchor might be able to run away from this "
           + "AnchorableBehaviour and prevent it from actually getting to the anchor.")]
    [DisableIf("lockToAnchor", isEqualTo: true)]
    public bool matchAnchorMotionWhileAttaching = true;

    [Tooltip("How fast should the object move towards its target position? Higher values are faster.")]
    [DisableIf("lockToAnchor", isEqualTo: true)]
    [Range(0, 100F)]
    public float anchorLerpCoeffPerSec = 20F;

    [Header("Rotation")]

    [Tooltip("Should the object also rotate to match its anchor's rotation? If checked, motion settings applied "
           + "to how the anchor translates will also apply to how it rotates.")]
    public bool anchorRotation = false;

    [Header("Interaction")]

    [Tooltip("Additional features are enabled when this GameObject also has an InteractionBehaviour component.")]
    [Disable]
    public InteractionBehaviour interactionBehaviour;
    [SerializeField, HideInInspector]
    private bool _interactionBehaviourIsNull = true;

    [Tooltip("If the InteractionBehaviour is set, objects will automatically detach from their anchor when grasped.")]
    [Disable]
    public bool detachWhenGrasped = true;

    [Tooltip("Should the AnchorableBehaviour automatically try to anchor itself when a grasp ends? If useTrajectory is enabled, "
           + "this object will automatically attempt to attach to the nearest valid anchor that is in the direction of its trajectory, "
           + "otherwise it will simply attempt to attach to its nearest valid anchor.")]
    [EditTimeOnly]
    public bool tryAnchorNearestOnGraspEnd = true;

    [Tooltip("Should the object pull away from its anchor and reach towards the user's hand when the user's hand is nearby?")]
    public bool isAttractedByHand = false;

    [Tooltip("If the object is attracted to hands, how far should the object be allowed to pull away from its anchor "
           + "towards a nearby InteractionHand? Value is in Unity distance units, WORLD space.")]
    public float maxAttractionReach = 0.1F;

    [Tooltip("This curve converts the distance of the hand (X axis) to the desired attraction reach distance for the object (Y axis). "
           + "The evaluated value is clamped between 0 and 1, and then scaled by maxAttractionReach.")]
    public AnimationCurve attractionReachByDistance;

    #region Events

    /// <summary>
    /// Called when this AnchorableBehaviour attaches to an Anchor.
    /// </summary>
    public Action<AnchorableBehaviour, Anchor> OnAttachedToAnchor = (anchObj, anchor) => { };

    /// <summary>
    /// Called when this AnchorableBehaviour locks to an Anchor.
    /// </summary>
    public Action<AnchorableBehaviour, Anchor> OnLockedToAnchor = (anchObj, anchor) => { };

    /// <summary>
    /// Called when this AnchorableBehaviour detaches from an Anchor.
    /// </summary>
    public Action<AnchorableBehaviour, Anchor> OnDetachedFromAnchor = (anchObj, anchor) => { };

    /// <summary>
    /// Called during every Update() in which this AnchorableBehaviour is attached to an Anchor.
    /// </summary>
    public Action<AnchorableBehaviour, Anchor> WhileAttachedToAnchor = (anchObj, anchor) => { };

    /// <summary>
    /// Called during every Update() in which this AnchorableBehaviour is locked to an Anchor.
    /// </summary>
    public Action<AnchorableBehaviour, Anchor> WhileLockedToAnchor = (anchObj, anchor) => { };

    #endregion

    private bool _isLockedToAnchor = false;
    private Vector3 _offsetTowardsHand = Vector3.zero;
    private Vector3 _targetPositionLastUpdate = Vector3.zero;
    private bool _hasTargetPositionLastUpdate = false;

    void OnValidate() {
      refreshInteractionBehaviour();

      _minAttachmentDotProduct = Mathf.Cos(_maxAttachmentAngle * Mathf.Deg2Rad);
    }

    void Awake() {
      refreshInteractionBehaviour();

      _minAttachmentDotProduct = Mathf.Cos(_maxAttachmentAngle * Mathf.Deg2Rad);

      if (interactionBehaviour != null) {
        interactionBehaviour.OnObjectGraspBegin += detachAnchorOnObjectGraspBegin;

        if (tryAnchorNearestOnGraspEnd) {
          interactionBehaviour.OnObjectGraspEnd += tryToAnchorOnObjectGraspEnd;
        }
      }

      InitUnityEvents();
    }

    void Reset() {
      refreshInteractionBehaviour();
    }

    private void refreshInteractionBehaviour() {
      interactionBehaviour = GetComponent<InteractionBehaviour>();
      _interactionBehaviourIsNull = interactionBehaviour == null;

      detachWhenGrasped = !_interactionBehaviourIsNull;
      if (_interactionBehaviourIsNull) useTrajectory = false;
    }

    void OnDisable() {
      _isLockedToAnchor = false;

      // Reset anchor position storage; it can't be updated from this state.
      _hasTargetPositionLastUpdate = false;
    }

    void OnDestroy() {
      if (interactionBehaviour != null) {
        interactionBehaviour.OnObjectGraspBegin -= detachAnchorOnObjectGraspBegin;
        interactionBehaviour.OnObjectGraspEnd   -= tryToAnchorOnObjectGraspEnd;
      }
    }

    void Update() {
      updateAttractionToHand();

      if (anchor != null && isAttached) {
        updateAnchorAttachment();

        WhileAttachedToAnchor.Invoke(this, anchor);

        if (_isLockedToAnchor) {
          WhileLockedToAnchor.Invoke(this, anchor);
        }
      }

      if (true) {
        updateAnchorCallbackState();
      }
    }

    private Anchor _curPreferredAnchor = null;

    private void updateAnchorCallbackState() {
      Anchor preferredAnchor = findPreferredAnchor();

      if (_curPreferredAnchor != preferredAnchor) {
        if (_curPreferredAnchor != null) {
          _curPreferredAnchor.NotifyEndAnchorPreference(this);
        }

        _curPreferredAnchor = preferredAnchor;

        if (_curPreferredAnchor != null) {
          _curPreferredAnchor.NotifyAnchorPreference(this);
        }
      }
    }

    /// <summary>
    /// Detaches this Anchorable object from its anchor. The anchor reference
    /// remains unchanged. Call TryAttach() to re-attach to this object's assigned anchor.
    /// </summary>
    public void Detach() {
      _isAttached = false;
      _isLockedToAnchor = false;

      if (anchor != null) {
        anchor.NotifyUnanchored(this);
        OnDetachedFromAnchor.Invoke(this, anchor);
      }

      _hasTargetPositionLastUpdate = false;
    }

    /// <summary>
    /// Returns whether the argument anchor is an acceptable anchor for this anchorable
    /// object; that is, whether the argument Anchor is within this behaviour's AnchorGroup
    /// if it has one, or if this behaviour has no AnchorGroup, returns true.
    /// </summary>
    public bool IsValidAnchor(Anchor anchor) {
      if (this.anchorGroup != null) {
        return this.anchorGroup.Contains(anchor);
      }
      else {
        return true;
      }
    }

    /// <summary>
    /// Returns whether the specified anchor is within attachment range of this Anchorable object.
    /// </summary>
    public bool IsWithinRange(Anchor anchor) {
      return (this.transform.position - anchor.transform.position).sqrMagnitude < maxAnchorRange * maxAnchorRange;
    }

    /// <summary>
    /// Attempts to attach to this Anchorable object's currently specified anchor.
    /// The attempt may fail if this anchor is out of range.
    /// </summary>
    public bool TryAttach() {
      if (anchor != null && IsWithinRange(anchor)) {
        _isAttached = true;
        return true;
      }
      else {
        return false;
      }
    }

    private List<Anchor> _nearbyAnchorsBuffer = new List<Anchor>();
    /// <summary>
    /// Returns all anchors within the max anchor range of this anchorable object. If this
    /// anchorable object has its anchorGroup property set, only anchors within that AnchorGroup
    /// will be returned. By default, this method will only return anchors that have space for
    /// an object to attach to it.
    /// 
    /// Warning: This method checks squared-distance for all anchors in teh scene if this
    /// AnchorableBehaviour has no AnchorGroup.
    /// </summary>
    public List<Anchor> GetNearbyValidAnchors(bool requireAnchorHasSpace = true) {
      HashSet<Anchor> anchorsToCheck;

      if (this.anchorGroup == null) {
        anchorsToCheck = Anchor.allAnchors;
      }
      else {
        anchorsToCheck = this.anchorGroup.anchors;
      }

      _nearbyAnchorsBuffer.Clear();
      foreach (var anchor in anchorsToCheck) {
        if (requireAnchorHasSpace && (!anchor.allowMultipleObjects && anchor.anchoredObjects.Count != 0)) continue;

        if ((anchor.transform.position - this.transform.position).sqrMagnitude <= maxAnchorRange * maxAnchorRange) {
          _nearbyAnchorsBuffer.Add(anchor);
        }
      }

      return _nearbyAnchorsBuffer;
    }

    /// <summary>
    /// Returns the nearest valid anchor to this Anchorable object. If this anchorable object has its
    /// anchorGroup property set, all anchors within that AnchorGroup are valid to be this object's
    /// anchor. If there is no valid anchor within range, returns null. By default, this method will
    /// only return anchors that are within the max anchor range of this object and that have space for
    /// an object to attach to it.
    /// 
    /// Warning: This method checks squared-distance for all anchors in the scene if this AnchorableBehaviour
    /// has no AnchorGroup.
    /// </summary>
    public Anchor GetNearestValidAnchor(bool requireWithinRange = true, bool requireAnchorHasSpace = true) {
      HashSet<Anchor> anchorsToCheck;

      if (this.anchorGroup == null) {
        anchorsToCheck = Anchor.allAnchors;
      }
      else {
        anchorsToCheck = this.anchorGroup.anchors;
      }

      Anchor closestAnchor = null;
      float closestDistSqrd = float.PositiveInfinity;
      foreach (var testAnchor in anchorsToCheck) {
        if (requireAnchorHasSpace && (!anchor.allowMultipleObjects || anchor.anchoredObjects.Count == 0)) continue;

        float testDistanceSqrd = (testAnchor.transform.position - this.transform.position).sqrMagnitude;
        if (testDistanceSqrd < closestDistSqrd) {
          closestAnchor = testAnchor;
          closestDistSqrd = testDistanceSqrd;
        }
      }

      if (!requireWithinRange || closestDistSqrd < maxAnchorRange * maxAnchorRange) {
        return closestAnchor;
      }
      else {
        return null;
      }
    }

    /// <summary>
    /// Attempts to find and attach this anchorable object to the nearest valid anchor, or the
    /// most optimal nearby anchor based on proximity and the object's trajectory if useTrajectory
    /// is enabled.
    /// </summary>
    public bool TryAttachToNearestAnchor() {
      Anchor preferredAnchor = findPreferredAnchor();

      if (preferredAnchor != null) {
        anchor = preferredAnchor;
        _isAttached = true;
        return true;
      }

      return false;
    }

    private Anchor findPreferredAnchor() {
      if (!useTrajectory) {
        // Simply try to attach to the nearest valid anchor.
        return GetNearestValidAnchor();
      }
      else {
        // Pick the nearby valid anchor with the highest score, based on proximity and trajectory.
        Anchor optimalAnchor = null;
        float optimalScore = 0F;
        Anchor testAnchor = null;
        float testScore = 0F;
        foreach (var anchor in GetNearbyValidAnchors()) {
          testAnchor = anchor;
          testScore = getAnchorScore(anchor);

          // Scores of 0 mark ineligible anchors.
          if (testScore == 0F) continue;

          if (testScore > optimalScore) {
            optimalAnchor = testAnchor;
            optimalScore = testScore;
          }
        }

        return optimalAnchor;
      }
    }

    /// <summary> Score an anchor based on its proximity and this object's trajectory relative to it. </summary>
    private float getAnchorScore(Anchor anchor) {
      return getAnchorScore(this.interactionBehaviour.rigidbody.position,
                            this.interactionBehaviour.rigidbody.velocity,
                            anchor.transform.position,
                            maxAnchorRange,
                            maxAnchorRange / 2F,
                            _minAttachmentDotProduct);
    }

    public static float getAnchorScore(Vector3 anchObjPos, Vector3 anchObjVel, Vector3 anchorPos, float maxDistance, float nonDirectedMaxDistance, float minAngleProduct) {
      // Calculated a "directedness" heuristic for determining whether the user is throwing or releasing without directed motion.
      float directedness = anchObjVel.magnitude.Map(0.2F, 1F, 0F, 1F);

      float distanceSqrd = (anchorPos - anchObjPos).sqrMagnitude;
      float effMaxDistance = directedness.Map(0F, 1F, nonDirectedMaxDistance, maxDistance);

      float distanceScore;
      if (distanceSqrd > effMaxDistance * effMaxDistance) {
        distanceScore = 0F;
      }
      else {
        distanceScore = distanceSqrd.Map(0F, effMaxDistance * effMaxDistance, 1F, 0F);
      }

      float angleScore;
      float dotProduct = Vector3.Dot(anchObjVel.normalized, (anchorPos - anchObjPos).normalized);

      // Angular score only factors in based on how directed the motion of the object is.
      dotProduct = Mathf.Lerp(1F, dotProduct, directedness);

      if (dotProduct < minAngleProduct) {
        angleScore = 0F;
      }
      else {
        angleScore = dotProduct.Map(minAngleProduct, 1F, 0F, 1F);
        angleScore *= angleScore;
      }

      return distanceScore * angleScore;
    }

    private void updateAttractionToHand() {
      if (interactionBehaviour == null || !isAttractedByHand) {
        if (_offsetTowardsHand != Vector3.zero) {
          _offsetTowardsHand = Vector3.Lerp(_offsetTowardsHand, Vector3.zero, 5F * Time.deltaTime);
        }

        return;
      }

      float reachTargetAmount = 0F;
      Vector3 towardsHand = Vector3.zero;
      if (interactionBehaviour != null) {
        if (isAttractedByHand) {
          if (interactionBehaviour.isHovered) {
            Hand hoveringHand = interactionBehaviour.closestHoveringHand;

            reachTargetAmount = Mathf.Clamp01(attractionReachByDistance.Evaluate(
                                  Vector3.Distance(hoveringHand.PalmPosition.ToVector3(), anchor.transform.position)
                                ));
            towardsHand = hoveringHand.PalmPosition.ToVector3() - anchor.transform.position;
          }

          Vector3 targetOffsetTowardsHand = towardsHand * maxAttractionReach * reachTargetAmount;

          _offsetTowardsHand = Vector3.Lerp(_offsetTowardsHand, targetOffsetTowardsHand, 5 * Time.deltaTime);
        }
      }
    }

    private void updateAnchorAttachment() {
      // Initialize position.
      Vector3 finalPosition;
      if (interactionBehaviour != null) {
        finalPosition = interactionBehaviour.rigidbody.position;
      }
      else {
        finalPosition = this.transform.position;
      }

      // Update position based on anchor state.
      Vector3 targetPosition = anchor.transform.position;
      if (lockToAnchor) {
        // In this state, we are simply locked directly to the anchor.
        finalPosition = targetPosition + _offsetTowardsHand;

        // Reset anchor position storage; it can't be updated from this state.
        _hasTargetPositionLastUpdate = false;
      }
      else if (lockWhenAttached) {
        if (_isLockedToAnchor) {
          // In this state, we are already attached to the anchor.
          finalPosition = targetPosition + _offsetTowardsHand;

          // Reset anchor position storage; it can't be updated from this state.
          _hasTargetPositionLastUpdate = false;
        }
        else {
          // Undo any "reach towards hand" offset.
          finalPosition -= _offsetTowardsHand;

          // If desired, automatically correct for the anchor itself moving while attempting to return to it.
          if (matchAnchorMotionWhileAttaching) {
            if (_hasTargetPositionLastUpdate) {
              finalPosition += (targetPosition - _targetPositionLastUpdate);
            }

            _targetPositionLastUpdate = targetPosition;
            _hasTargetPositionLastUpdate = true;
          }

          // Lerp towards the anchor.
          finalPosition = Vector3.Lerp(finalPosition, targetPosition, anchorLerpCoeffPerSec * Time.deltaTime);
          if (Vector3.Distance(finalPosition, targetPosition) < 0.001F) {
            _isLockedToAnchor = true;
          }

          // Redo any "reach toward hand" offset.
          finalPosition += _offsetTowardsHand;
        }
      }

      // Set final position.
      if (interactionBehaviour != null) {
        interactionBehaviour.rigidbody.position = finalPosition;
        this.transform.position = finalPosition;
      }
      else {
        this.transform.position = finalPosition;
      }
    }

    /// <summary> Wrapper method to match OnObjectGraspBegin method signature. </summary>
    private void detachAnchorOnObjectGraspBegin(List<InteractionHand> hands) {
      Detach();
    }

    /// <summary> Wrapper method to match OnObjectGraspEnd method signature. </summary>
    private void tryToAnchorOnObjectGraspEnd(List<InteractionHand> hands) {
      TryAttachToNearestAnchor();
    }

    #region Unity Events (Internal)

    [SerializeField]
    private EnumEventTable _eventTable;

    public enum EventType {
      OnAttachedToAnchor = 100,
      OnLockedToAnchor = 105,
      OnDetachedFromAnchor = 110,
      WhileAttachedToAnchor = 120,
      WhileLockedToAnchor = 125
    }

    private void InitUnityEvents() {
      setupCallback(ref OnAttachedToAnchor,    EventType.OnAttachedToAnchor);
      setupCallback(ref OnLockedToAnchor,      EventType.OnLockedToAnchor);
      setupCallback(ref OnDetachedFromAnchor,  EventType.OnDetachedFromAnchor);
      setupCallback(ref WhileAttachedToAnchor, EventType.WhileAttachedToAnchor);
      setupCallback(ref WhileLockedToAnchor,   EventType.WhileLockedToAnchor);
    }

    private void setupCallback<T1, T2>(ref Action<T1, T2> action, EventType type) {
      action += (anchObj, anchor) => _eventTable.Invoke((int)type);
    }

    #endregion

  }

}
