using Leap.Unity.Attributes;
using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Attachments {

  public class AnchorableBehaviour : MonoBehaviour {

    /// <summary> Does the object have a specific anchor, or can it fit in any anchor of a certain type? </summary>
    public enum AnchorType { SingleAnchor, AnchorGroup }
    public AnchorType anchorType;

    [DisableIf("anchorType", isEqualTo: AnchorType.AnchorGroup)]
    public Anchor anchor;
    
    [DisableIf("anchorType", isEqualTo: AnchorType.SingleAnchor)]
    public AnchorGroup anchorGroup;

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

    private Anchor _currentAnchorInGroup = null;
    /// <summary>
    /// Gets or sets the current target anchor. Setting this value to null will
    /// disable the AnchorableBehaviour component (i.e. disable anchoring).
    /// 
    /// If the anchor type is set to SingleAnchor, getting this property will always
    /// return this component's public anchor field. Setting this property will change
    /// the public anchor field.
    /// 
    /// If the anchor type is set to AnchorGroup, getting this property will return
    /// the current anchor inside the AnchorGroup, or null if the object is not currently
    /// anchored. Setting this property will set the current anchor only if the given
    /// anchor is a member of the AnchorGroup.
    /// </summary>
    public Anchor currentAnchor {
      get {
        switch (anchorType) {
          case AnchorType.SingleAnchor:
            return anchor;
          case AnchorType.AnchorGroup: default:
            return _currentAnchorInGroup;
        }
      }
      set {
        switch (anchorType) {
          case AnchorType.SingleAnchor:
            anchor = value;
            break;
          case AnchorType.AnchorGroup: default:
            if (anchorGroup.ContainsAnchor(value)) {
              _currentAnchorInGroup = anchor;
            }
            else {
              Debug.LogError("Attempted to set " + this.name + "'s anchor, but the argument anchor is not in "
                           + "this AnchorableBehaviour's anchor group.");
              _currentAnchorInGroup = null;
            }
            break;
        }
      }
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
          anchor = this.anchor;
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
            return true;
          }
          else {
            anchor = null;
            return false;
          }
      }
    }

    private bool _attachedToAnchor = false;

    void OnValidate() {
      _isInteractionBehaviourNull = interactionBehaviour == null;
    }

    void OnEnable() {
      if (anchorType == AnchorType.SingleAnchor && anchor == null) {
        this.enabled = false;
      }
      else if (anchorType == AnchorType.AnchorGroup && _currentAnchorInGroup == null) {
        this.enabled = false;
      }
    }

    void OnDisable() {
      _attachedToAnchor = false;
    }

    void Update() {
      if (interactionBehaviour != null && isAttractedByHand) UpdateAttractionToHand();
      else if (_offsetTowardsHand != Vector3.zero) _offsetTowardsHand = Vector3.Lerp(_offsetTowardsHand, Vector3.zero, 5F * Time.deltaTime);
      UpdateAnchorAttachment();
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

    private void UpdateAnchorAttachment() {
      Vector3 targetPosition = anchor.transform.position;

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