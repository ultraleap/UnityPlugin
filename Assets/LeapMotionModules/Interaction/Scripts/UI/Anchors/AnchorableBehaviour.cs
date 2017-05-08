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

    // "Attach to Nearby Anchor" / "Detach from Anchor" Button

    [Disable]
    [SerializeField]
    [Tooltip("Whether or not this AnchorableBehaviour is actively attached to its anchor.")]
    private bool _isAnchored;
    public bool isAnchored { get { return _isAnchored; } }

    [Tooltip("The current anchor of this AnchorableBehaviour.")]
    [OnEditorChange("anchor"), SerializeField]
    private Anchor _anchor;
    public Anchor anchor {
      get { return _anchor; }
      set {
        if (_anchor != value) {
          if (isValidAnchor(value)) {
            _lockedToAnchor = false;
            _anchor = value;
          }
        }
      }
    }

    [Tooltip("The anchor group for this AnchorableBehaviour. If set to null, all Anchors "
           + "that aren't in any anchor group will be valid anchors for this object.")]
    [OnEditorChange("anchorGroup"), SerializeField]
    private AnchorGroup _anchorGroup;
    public AnchorGroup anchorGroup {
      get { return _anchorGroup; }
      set {
        _anchorGroup = value;
        validateAnchor();
      }
    }

    // TODO: Fixme
    private bool isValidAnchor(Anchor anchor) {
      return true;
    }

    // TODO: Fixme
    private void validateAnchor() {
      return;
    }

    #region Events

    public Action<AnchorableBehaviour, Anchor> OnAttachedToAnchor    = (anchObj, anchor) => { };
    public Action<AnchorableBehaviour, Anchor> OnDetachedFromAnchor  = (anchObj, anchor) => { };
    public Action<AnchorableBehaviour, Anchor> WhileAttachedToAnchor = (anchObj, anchor) => { };

    #endregion

    #region Motion Configuration

    [Header("Motion")]
    [Tooltip("Should the object move instantly to the anchor position?")]
    public bool lockToAnchor = false;

    [Tooltip("Should the object move smoothly towards the anchor at first, but lock to it once it reaches the anchor? "
           + "Note: Disabling the AnchorableBehaviour will stop the object from moving towards its anchor, and will "
           + "'release' it from the anchor, so that on re-enable the object will smoothly move to the anchor again.")]
    [DisableIf("lockToAnchor", isEqualTo: true)]
    public bool lockToAnchorWhenAttached = true;

    [Tooltip("While this object is moving smoothly towards its anchor, should it also inherit the motion of the "
           + "anchor itself if the anchor is not stationary? Otherwise, the anchor might be able to run away from this "
           + "AnchorableBehaviour and prevent it from actually getting to the anchor.")]
    [DisableIf("lockToAnchor", isEqualTo: true)]
    public bool matchAnchorMotionWhileReturning = true;

    [Tooltip("How fast should the object move towards its target position? Higher values are faster.")]
    [DisableIf("lockToAnchor", isEqualTo: true)]
    [Range(0, 100F)]
    public float anchorLerpCoeffPerSec = 20F;

    #endregion

    #region Rotation Configuration

    [Header("Rotation")]
    [Tooltip("Should the object also rotate to match its anchor's rotation? If checked, motion settings applied "
           + "to how the anchor translates will also apply to how it rotates.")]
    public bool anchorRotation = false;

    #endregion

    #region Interaction Configuration

    [Header("Interaction")]
    [Tooltip("Additional features are enabled when this GameObject also has an InteractionBehaviour component.")]
    [Disable]
    public InteractionBehaviour interactionBehaviour;

    [Tooltip("If the InteractionBehaviour is set, objects will automatically detach from their anchor when grasped.")]
    [Disable]
    public bool detachWhenGrasped = true;

    [Tooltip("Should the AnchorableBehaviour automatically try to anchor itself when a grasp ends?")]
    [EditTimeOnly]
    public bool tryAnchorOnGraspEnd = true;

    [Tooltip("Should the object pull away from its anchor and reach towards the user's hand when the user's hand is nearby?")]
    public bool isAttractedByHand = false;

    [Tooltip("If the object is attracted to hands, how far should the object be allowed to pull away from its anchor "
           + "towards a nearby InteractionHand? Value is in Unity distance units, WORLD space.")]
    public float maxAttractionReach = 0.1F;

    [Tooltip("This curve converts the distance of the hand (X axis) to the desired attraction reach distance for the object (Y axis). "
           + "The evaluated value is clamped between 0 and 1, and then scaled by maxAttractionReach.")]
    public AnimationCurve attractionReachByDistance;

    #endregion

    //public AnchorGroup anchorGroup {
    //  get {
    //    return _anchorGroup;
    //  }
    //  set {
    //    _anchorGroup = value;
    //    if (_anchorGroup != null && _anchor != null && !_anchorGroup.ContainsAnchor(_anchor) && this.enabled) {
    //      Debug.LogWarning("Current anchor is not a member of this object's AnchorGroup. (Setting it to null.)");
    //      anchor = null;
    //    }
    //  }
    //}

    // BEGIN CURRENTANCHOR REMOVAL COMMENT.
    ///// <summary>
    ///// Gets or sets the current target anchor. Setting this value to null will
    ///// disable the AnchorableBehaviour component (i.e. disable anchoring).
    ///// </summary>
    ///// <remarks>
    ///// If the anchor type is set to SingleAnchor, getting this property will always
    ///// return this component's public anchor field. Setting this property will change
    ///// the public anchor field.
    ///// 
    ///// If the anchor type is set to AnchorGroup, getting this property will return
    ///// the current anchor inside the AnchorGroup, or null if the object is not currently
    ///// anchored. Setting this property will set the current anchor only if the given
    ///// anchor is a member of the AnchorGroup.
    ///// </remarks>
    //public Anchor currentAnchor {
    //  get {
    //    return _currentAnchor;
    //  }
    //  set {
    //    switch (anchorType) {
    //      case AnchorType.SingleAnchor:
    //        if (_currentAnchor != value) {
    //          if (value != null && !value.allowMultipleObjects && value.anchoredObjects.Count > 0) break;

    //          if (_currentAnchor != null) _currentAnchor.NotifyUnanchored(this);

    //          _anchor = value;
    //          _currentAnchor = value;
    //          _attachedToAnchor = false;

    //          if (_currentAnchor != null) _currentAnchor.NotifyAnchored(this);
    //        }
    //        break;
    //      case AnchorType.AnchorGroup: default:
    //        if (anchorGroup == null) return;

    //        if (value == null) {
    //          if (_currentAnchor != null) _currentAnchor.NotifyUnanchored(this);

    //          _anchor = null;
    //          _currentAnchor = null;
    //          _attachedToAnchor = false;
    //        }
    //        else {
    //          if (anchorGroup.ContainsAnchor(value)) {
    //            if (_currentAnchor != value) {
    //              if (_currentAnchor != null) _currentAnchor.NotifyUnanchored(this);

    //              _anchor = value;
    //              _currentAnchor = value;
    //              _attachedToAnchor = false;

    //              _currentAnchor.NotifyAnchored(this);
    //            }
    //          }
    //          else {
    //            // Tried to set this behaviour's anchor to an anchor outside of the assigned anchor group.

    //            // Clear the inspector field for the anchor; if it is non-null, the user tried to set
    //            // the field with an invalid (out-of-group) anchor.
    //            if (_anchor != null) {
    //              Debug.LogError("The anchor \"" + _anchor.name + "\" is not a member of this object's anchor group.", this.gameObject);
    //              _anchor = null;
    //            }

    //            // But if the current anchor is inside the current anchorGroup, we don't want to lose that
    //            // information after dropping in an invalid (out-of-group) anchor, so re-set the _anchor
    //            // inspector field.
    //            if (anchorGroup.ContainsAnchor(currentAnchor)) {
    //              if (_anchor != currentAnchor) _anchor = currentAnchor;
    //            }
    //          }
    //        }

    //        break;
    //    }
    //  }
    //}
    // END CURRENTANCHOR REMOVAL COMMENT.

    private bool _lockedToAnchor = false;

    void OnValidate() {
      interactionBehaviour = GetComponent<InteractionBehaviour>();

      detachWhenGrasped = interactionBehaviour != null;
    }

    void Awake() {
      interactionBehaviour = GetComponent<InteractionBehaviour>();

      if (interactionBehaviour != null) {
        interactionBehaviour.OnObjectGraspBegin += detachAnchorOnObjectGraspBegin;

        if (tryAnchorOnGraspEnd) {
          interactionBehaviour.OnObjectGraspEnd += tryToAnchorOnObjectGraspEnd;
        }
      }

      InitUnityEvents();
    }

    void OnDisable() {
      _lockedToAnchor = false;

      // Reset anchor position storage; it can't be updated from this state.
      _hasTargetPositionLastUpdate = false;
    }

    void OnDestroy() {
      if (interactionBehaviour != null) {
        interactionBehaviour.OnObjectGraspBegin -= detachAnchorOnObjectGraspBegin;
        interactionBehaviour.OnObjectGraspEnd   -= tryToAnchorOnObjectGraspEnd;
      }
    }

    /// <summary> Wrapper method to match OnObjectGraspBegin method signature. </summary>
    private void detachAnchorOnObjectGraspBegin(List<InteractionHand> hands) {
      DetachFromAnchor();
    }

    /// <summary> Wrapper method to match OnObjectGraspEnd method signature. </summary>
    private void tryToAnchorOnObjectGraspEnd(List<InteractionHand> hands) {
      TryAttachToAnchor();
    }

    void Update() {
      UpdateAttractionToHand();

      UpdateAnchorAttachment();
    }

    public void DetachFromAnchor() {
      anchor = null;
    }

    public bool TryAttachToAnchor() {
      Anchor anchor;
      return TryAttachToAnchor(out anchor);
    }

    public bool TryAttachToAnchor(out Anchor anchor) {
      anchor = FindPreferredAnchor(this);

      if (anchor != null) {
        this.anchor = anchor;
        return true;
      }

      return false;
    }

    public static Anchor FindPreferredAnchor(AnchorableBehaviour anchObj) {
      return null;
    }

    // OLD TRYTOANCHOR METHODS
    ///// <summary>
    ///// Attempts to re-anchor the object if it is not anchored (i.e. the AnchorableBehaviour
    ///// was disabled).
    ///// 
    ///// If the anchor type is SingleAnchor, the attempt will succeed only if the anchor is
    ///// within its specified range and the anchor is enabled. If the attempt fails, the method
    ///// will return false, although the output anchor will still be provided.
    ///// 
    ///// If the anchor type is AnchorGroup, the attempt will succeed only if there is an
    ///// anchor in the anchorGroup field that is within range and is enabled, otherwise
    ///// the method will return false, and the output anchor will be null.
    ///// </summary>
    //public bool TryToAnchor(out Anchor anchor) {
    //  switch (anchorType) {
    //    case AnchorType.SingleAnchor:
    //      anchor = currentAnchor;
    //      if (this.enabled) {
    //        return true;
    //      }
    //      else if (anchor.IsWithinRange(this.transform.position)) {
    //        this.enabled = true;
    //        return true;
    //      }
    //      else {
    //        return false;
    //      }
    //    case AnchorType.AnchorGroup: default:
    //      Anchor closestValidAnchor = anchorGroup.FindClosestAnchor(this.transform.position, requireWithinAnchorRange: true, requireAnchorIsEnabled: true);
    //      if (closestValidAnchor != null) {
    //        this.enabled = true;
    //        currentAnchor = closestValidAnchor;
    //        anchor = currentAnchor;
    //        return true;
    //      }
    //      else {
    //        anchor = null;
    //        return false;
    //      }
    //  }
    //}
    //
    ///// <summary>
    ///// Attempts to re-anchor the object if it is not anchored (i.e. the AnchorableBehaviour
    ///// was disabled). This method is a convenience for when the actual anchor chosen is not
    ///// needed. (In the SingleAnchor case, the only possible anchor is the one already assigned
    ///// to this AnchorableBehaviour, in which case the component will simply enable itself if
    ///// the anchor is within range.)
    ///// </summary>
    //public bool TryToAnchor() {
    //  Anchor anchor;
    //  return TryToAnchor(out anchor);
    //}
    // END OLD TRYTOANCHOR METHODS


    // BEGIN OLD DETACHFROMANCHOR METHOD
    ///// <summary>
    ///// Detaches the AnchorableBehaviour from its current anchor by setting the current anchor
    ///// to null.
    ///// </summary>
    //public void DetachFromAnchor() {
    //  currentAnchor = null;

    //  // Reset anchor position storage; it can't be updated from this state.
    //  _hasTargetPositionLastUpdate = false;
    //}
    // END OLD DETACHFROMANCHOR METHOD

    #region Attraction To Hands

    private Vector3 _offsetTowardsHand = Vector3.zero;

    private void UpdateAttractionToHand() {
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

    #endregion

    #region Anchor Attachment

    private Vector3 _targetPositionLastUpdate = Vector3.zero;
    private bool _hasTargetPositionLastUpdate = false;

    private void UpdateAnchorAttachment() {
      if (anchor == null) return;

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
      else if (lockToAnchorWhenAttached) {
        if (_lockedToAnchor) {
          // In this state, we are already attached to the anchor.
          finalPosition = targetPosition + _offsetTowardsHand;

          // Reset anchor position storage; it can't be updated from this state.
          _hasTargetPositionLastUpdate = false;
        }
        else {
          // Undo any "reach towards hand" offset.
          finalPosition -= _offsetTowardsHand;

          // If desired, automatically correct for the anchor itself moving while attempting to return to it.
          if (matchAnchorMotionWhileReturning) {
            if (_hasTargetPositionLastUpdate) {
              finalPosition += (targetPosition - _targetPositionLastUpdate);
            }

            _targetPositionLastUpdate = targetPosition;
            _hasTargetPositionLastUpdate = true;
          }

          // Lerp towards the anchor.
          finalPosition = Vector3.Lerp(finalPosition, targetPosition, anchorLerpCoeffPerSec * Time.deltaTime);
          if (Vector3.Distance(finalPosition, targetPosition) < 0.001F) {
            _lockedToAnchor = true;
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

    #endregion

    #region Unity Events (Internal)

    [SerializeField]
    private EnumEventTable _eventTable;

    public enum EventType {
      OnAttachedToAnchor = 100,
      OnDetachedFromAnchor = 110,
      WhileAttachedToAnchor = 120
    }

    private void InitUnityEvents() {
      setupCallback(ref WhileAttachedToAnchor, EventType.OnAttachedToAnchor);
      setupCallback(ref OnDetachedFromAnchor,  EventType.OnDetachedFromAnchor);
      setupCallback(ref WhileAttachedToAnchor, EventType.WhileAttachedToAnchor);
    }

    private void setupCallback<T1, T2>(ref Action<T1, T2> action, EventType type) {
      action += (anchObj, anchor) => _eventTable.Invoke((int)type);
    }

    #endregion

  }

}
