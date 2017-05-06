/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Attributes;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Attachments {

  public class AnchorableBehaviour : MonoBehaviour {

    /// <summary> Does the object have a specific anchor, or can it fit in any anchor of a certain type? </summary>
    public enum AnchorType { SingleAnchor, AnchorGroup }
    [Tooltip("If set to AnchorGroup, only Anchors inside the provided AnchorGroup "
           + "will be valid anchors for this AnchorableBehaviour.")]
    [OnEditorChange("anchorType"), SerializeField]
    private AnchorType _anchorType;
    
    [Tooltip("The anchor group for this AnchorableBehaviour. In the Anchor Group mode, only "
           + "anchors within the provided AnchorGroup are valid anchors for this object.")]
    [OnEditorChange("anchorGroup"), SerializeField]
    private AnchorGroup _anchorGroup;

    [Tooltip("The current anchor of this AnchorableBehaviour. If the Anchor Type is set to "
           + "Anchor Group, the current anchor must be within the assigned Anchor Group. To "
           + "remove the anchoring behaviour for this object, either disable this behaviour "
           + "or set the anchor to null.")]
    [OnEditorChange("currentAnchor"), SerializeField]
    private Anchor _anchor;

    [Header("Motion")]
    [Tooltip("Should the object move instantly to the anchor position?")]
    public bool lockToAnchor = false;

    [Tooltip("Should the object move smoothly towards the anchor at first, but lock to it once it reaches the anchor? "
           + "Note: Disabling the AnchorableBehaviour will stop the object from moving towards its anchor, and will "
           + "'release' it from the anchor, so that on re-enable the object will smoothly move to the anchor again.")]
    [DisableIf("lockToAnchor", isEqualTo: true)]
    public bool lockToAnchorWhenAttached = true;

    [Tooltip("How fast should the object move towards its target position? Higher values are faster.")]
    [DisableIf("lockToAnchor", isEqualTo: true)]
    [Range(0, 100F)]
    public float anchorLerpCoeffPerSec = 20F;

    [Header("Interaction")]
    public InteractionBehaviour interactionBehaviour;
    [SerializeField]
    [HideInInspector]
    #pragma warning disable 0414
    private bool _isInteractionBehaviourNull = true;
    #pragma warning restore 0414
    [Tooltip("Should the object pull away from the anchor, reaching towards the user's hand when the user's hand is nearby?")]
    [DisableIf("_isInteractionBehaviourNull", isEqualTo: true)]
    public bool isAttractedByHand = false;
    [Tooltip("If the object is attracted to hands, how far should the object be allowed to pull away from its anchor "
           + "towards a nearby InteractionHand? Value is in Unity distance units, WORLD space.")]
    [DisableIf("_isInteractionBehaviourNull", isEqualTo: true)]
    public float maxAttractionReach = 0.1F;
    [Tooltip("This curve converts the distance of the hand (X axis) to the desired attraction reach distance for the object (Y axis). "
           + "The evaluated value is clamped between 0 and 1, and then scaled by maxAttractionReach.")]
    [DisableIf("_isInteractionBehaviourNull", isEqualTo: true)]
    public AnimationCurve attractionReachByDistance;

    /// <summary>
    /// Gets or sets the anchor type for this AnchorableBehaviour.
    /// </summary>
    public AnchorType anchorType {
      get {
        return _anchorType;
      }
      set {
        _anchorType = value;
        currentAnchor = currentAnchor;
      }
    }

    public AnchorGroup anchorGroup {
      get {
        return _anchorGroup;
      }
      set {
        _anchorGroup = value;
        if (_anchorGroup != null && currentAnchor != null && !_anchorGroup.ContainsAnchor(currentAnchor) && this.enabled) {
          Debug.LogWarning("Current anchor is not a member of this object's AnchorGroup. (Setting it to null.)");
          currentAnchor = null;
        }
      }
    }

    // Note: This state duplicate is a bugfix.
    // It is unfortunate to have duplicate storage between this field and
    // the _anchor field, but it's necessary to fix a subtle bug where
    // Update() occurs before the currentAnchor property is able to set
    // the AnchorableBehaviour's state appropriately. (Otherwise, it sets
    // _attachedToAnchor to false after the Update() has already moved the
    // object to its new anchor.)
    private Anchor _currentAnchor;

    /// <summary>
    /// Gets or sets the current target anchor. Setting this value to null will
    /// disable the AnchorableBehaviour component (i.e. disable anchoring).
    /// </summary>
    /// <remarks>
    /// If the anchor type is set to SingleAnchor, getting this property will always
    /// return this component's public anchor field. Setting this property will change
    /// the public anchor field.
    /// 
    /// If the anchor type is set to AnchorGroup, getting this property will return
    /// the current anchor inside the AnchorGroup, or null if the object is not currently
    /// anchored. Setting this property will set the current anchor only if the given
    /// anchor is a member of the AnchorGroup.
    /// </remarks>
    public Anchor currentAnchor {
      get {
        return _currentAnchor;
      }
      set {
        switch (anchorType) {
          case AnchorType.SingleAnchor:
            if (_currentAnchor != value) {
              _anchor = value;
              _currentAnchor = value;
              _attachedToAnchor = false;
            }
            break;
          case AnchorType.AnchorGroup:
          default:
            if (anchorGroup == null) return;

            if (value == null) {
              _anchor = null;
              _currentAnchor = null;
              _attachedToAnchor = false;
            }
            else {
              if (anchorGroup.ContainsAnchor(value)) {
                if (_currentAnchor != value) {
                  _anchor = value;
                  _currentAnchor = value;
                  _attachedToAnchor = false;
                }
              }
              else {
                // Clear the inspector field for the anchor; if it is
                // non-null, the user tried to set the field with an
                // invalid (out-of-group) anchor.
                if (_anchor != null) {
                  Debug.LogError("The anchor \"" + _anchor.name + "\" is not a member of this object's anchor group.", this.gameObject);
                  _anchor = null;
                }

                // But if the current anchor is inside the current anchorGroup,
                // we don't want to lose that information after dropping in
                // an invalid (out-of-group) anchor, so re-set the _anchor
                // inspector field.
                if (anchorGroup.ContainsAnchor(currentAnchor)) {
                  if (_anchor != currentAnchor) _anchor = currentAnchor;
                }
              }
            }

            break;
        }
      }
    }

    private bool _attachedToAnchor = false;

    void OnValidate() {
      _isInteractionBehaviourNull = interactionBehaviour == null;
    }

    void Awake() {
      currentAnchor = _anchor;
    }

    void OnDisable() {
      _attachedToAnchor = false;
    }

    void Update() {
      if (interactionBehaviour != null && isAttractedByHand) UpdateAttractionToHand();
      else if (_offsetTowardsHand != Vector3.zero) _offsetTowardsHand = Vector3.Lerp(_offsetTowardsHand, Vector3.zero, 5F * Time.deltaTime);
      UpdateAnchorAttachment();
    }

    /// <summary>
    /// Attempts to re-anchor the object if it is not anchored (i.e. the AnchorableBehaviour
    /// was disabled).
    /// 
    /// If the anchor type is SingleAnchor, the attempt will succeed only if the anchor is
    /// within its specified range and the anchor is enabled. If the attempt fails, the method
    /// will return false, although the output anchor will still be provided.
    /// 
    /// If the anchor type is AnchorGroup, the attempt will succeed only if there is an
    /// anchor in the anchorGroup field that is within range and is enabled, otherwise
    /// the method will return false, and the output anchor will be null.
    /// </summary>
    public bool TryToAnchor(out Anchor anchor) {
      switch (anchorType) {
        case AnchorType.SingleAnchor:
          anchor = currentAnchor;
          if (this.enabled) {
            return true;
          }
          else if (anchor.IsWithinRange(this.transform.position)) {
            this.enabled = true;
            return true;
          }
          else {
            return false;
          }
        case AnchorType.AnchorGroup: default:
          Anchor toProvide = anchorGroup.FindClosestAnchor(this.transform.position, requireAnchorIsEnabled: true);
          if (toProvide != null) {
            this.enabled = true;
            anchor = toProvide;
            _currentAnchor = anchor;
            return true;
          }
          else {
            anchor = null;
            return false;
          }
      }
    }

    #region Attraction To Hands

    private Vector3 _offsetTowardsHand = Vector3.zero;

    private void UpdateAttractionToHand() {
      float reachTargetAmount = 0F;
      Vector3 towardsHand = Vector3.zero;
      if (interactionBehaviour != null) {
        if (isAttractedByHand) {
          if (interactionBehaviour.isHovered) {
            Hand hoveringHand = interactionBehaviour.closestHoveringHand;

            reachTargetAmount = Mathf.Clamp01(attractionReachByDistance.Evaluate(
                                  Vector3.Distance(hoveringHand.PalmPosition.ToVector3(), currentAnchor.transform.position)
                                ));
            towardsHand = hoveringHand.PalmPosition.ToVector3() - currentAnchor.transform.position;
          }

          Vector3 targetOffsetTowardsHand = towardsHand * maxAttractionReach * reachTargetAmount;

          _offsetTowardsHand = Vector3.Lerp(_offsetTowardsHand, targetOffsetTowardsHand, 5 * Time.deltaTime);
        }
      }
    }

    #endregion

    #region Anchor Attachment

    private void UpdateAnchorAttachment() {
      if (currentAnchor == null) return;

      Vector3 targetPosition = currentAnchor.transform.position;

      if (lockToAnchor) {
        this.transform.position = targetPosition + _offsetTowardsHand;
      }
      else if (lockToAnchorWhenAttached) {
        if (_attachedToAnchor) {
          this.transform.position = targetPosition + _offsetTowardsHand;
        }
        else {
          this.transform.position -= _offsetTowardsHand;
          this.transform.position = Vector3.Lerp(this.transform.position, targetPosition, anchorLerpCoeffPerSec * Time.deltaTime);
          if (Vector3.Distance(this.transform.position, targetPosition) < 0.001F) {
            _attachedToAnchor = true;
          }
          this.transform.position += _offsetTowardsHand;
        }
      }
    }

    #endregion

  }

}
