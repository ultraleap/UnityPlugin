/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Interaction
{
    /// <summary>
    /// AnchorableBehaviours mix well with InteractionBehaviours you'd like to be able to
    /// pick up and place in specific locations, specified by other GameObjects with an
    /// Anchor component.
    /// </summary>
    public class AnchorableBehaviour : MonoBehaviour
    {

        [Disable]
        [SerializeField]
        [Tooltip("Whether or not this AnchorableBehaviour is actively attached to its anchor.")]
        private bool _isAttached = false;
        public bool isAttached
        {
            get
            {
                return _isAttached;
            }
            set
            {
                if (_isAttached != value)
                {
                    if (value == true)
                    {
                        if (_anchor != null)
                        {
                            _isAttached = value;
                            _anchor.NotifyAttached(this);
                            OnAttachedToAnchor.Invoke();
                        }
                        else
                        {
                            Debug.LogWarning("Tried to attach an anchorable behaviour, but it has no assigned anchor.", this.gameObject);
                        }
                    }
                    else
                    {
                        _isAttached = false;
                        _isLockedToAnchor = false;
                        _isRotationLockedToAnchor = false;

                        OnDetachedFromAnchor.Invoke();
                        _anchor.NotifyDetached(this);

                        _hasTargetPositionLastUpdate = false;
                        _hasTargetRotationLastUpdate = false;

                        // TODO: A more robust gravity fix.
                        if (_reactivateGravityOnDetach)
                        {
                            if (interactionBehaviour != null)
                            {
                                interactionBehaviour.rigidbody.useGravity = true;
                            }
                            _reactivateGravityOnDetach = false;
                        }
                    }
                }
            }
        }

        [Tooltip("The current anchor of this AnchorableBehaviour.")]
        [OnEditorChange("anchor"), SerializeField]
        private Anchor _anchor;
        public Anchor anchor
        {
            get
            {
                return _anchor;
            }
            set
            {
                if (_anchor != value)
                {
                    if (IsValidAnchor(value))
                    {
                        if (_anchor != null && _isAttached)
                        {
                            OnDetachedFromAnchor.Invoke();
                            _anchor.NotifyDetached(this);
                        }

                        _isLockedToAnchor = false;
                        _isRotationLockedToAnchor = false;
                        _anchor = value;
                        _hasTargetPositionLastUpdate = false;
                        _hasTargetRotationLastUpdate = false;

                        if (_anchor != null)
                        {
                            isAttached = true;
                        }
                        else
                        {
                            isAttached = false;
                        }
                    }
                    else
                    {
                        Debug.LogWarning("The '" + value.name + "' anchor is not in " + this.name + "'s anchor group.", this.gameObject);
                    }
                }
            }
        }

        [Tooltip("The anchor group for this AnchorableBehaviour. If set to null, all Anchors "
               + "will be valid anchors for this object.")]
        [OnEditorChange("anchorGroup"), SerializeField]
        private AnchorGroup _anchorGroup;
        public AnchorGroup anchorGroup
        {
            get { return _anchorGroup; }
            set
            {
                if (_anchorGroup != null)
                {
                    _anchorGroup.NotifyAnchorableObjectRemoved(this);
                }

                _anchorGroup = value;

                if (anchor != null && !_anchorGroup.Contains(anchor))
                {
                    anchor = null;
                    Debug.LogWarning(this.name + "'s anchor is not within its anchorGroup (setting it to null).", this.gameObject);
                }

                if (_anchorGroup != null)
                {
                    _anchorGroup.NotifyAnchorableObjectAdded(this);
                }
            }
        }

        [Header("Attachment")]

        [Tooltip("Anchors beyond this range are ignored as possible anchors for this object.")]
        public float maxAnchorRange = 0.3F;

        [Tooltip("Only allowed when an InteractionBehaviour is attached to this object. If enabled, this "
               + "object's Attach() method or its variants will weigh its velocity towards an anchor along "
               + "with its proximity when seeking an anchor to attach to.")]
        [DisableIf("_interactionBehaviourIsNull", true)]
        public bool useTrajectory = true;

        [Tooltip("The fraction of the maximum anchor range to use as the effective max range when "
               + "useTrajectory is enabled, but the object attempts to find an anchor without any "
               + "velocity.")]
        [SerializeField]
        [Range(0.01F, 1F)]
        private float _motionlessRangeFraction = 0.40F;
        [SerializeField, Disable]
        private float _maxMotionlessRange;

        [Tooltip("The maximum angle this object's trajectory can be away from an anchor to consider it as "
               + "an anchor to attach to.")]
        [SerializeField]
        [Range(20F, 90F)]
        private float _maxAttachmentAngle = 60F;
        /// <summary> Calculated via _maxAttachmentAngle. </summary>
        private float _minAttachmentDotProduct;

        [Tooltip("Always attach an anchor if there is one within this distance, regardless "
               + "of trajectory.")]
        [SerializeField]
        [MinValue(0f)]
        private float _alwaysAttachDistance = 0f;

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
        [SerializeField]
        [OnEditorChange("tryAnchorNearestOnGraspEnd")]
        private bool _tryAnchorNearestOnGraspEnd = true;
        public bool tryAnchorNearestOnGraspEnd
        {
            get
            {
                return _tryAnchorNearestOnGraspEnd;
            }
            set
            {
                if (interactionBehaviour != null)
                {
                    // Prevent duplicate subscription.
                    interactionBehaviour.OnGraspEnd -= tryToAnchorOnGraspEnd;
                }

                _tryAnchorNearestOnGraspEnd = value;
                if (interactionBehaviour != null && _tryAnchorNearestOnGraspEnd)
                {
                    interactionBehaviour.OnGraspEnd += tryToAnchorOnGraspEnd;
                }
            }
        }

        [Tooltip("Should the object pull away from its anchor and reach towards the user's hand when the user's hand is nearby?")]
        public bool isAttractedByHand = false;

        [Tooltip("If the object is attracted to hands, how far should the object be allowed to pull away from its anchor "
               + "towards a nearby InteractionHand? Value is in Unity distance units, WORLD space.")]
        public float maxAttractionReach = 0.1F;

        [Tooltip("This curve converts the distance of the hand (X axis) to the desired attraction reach distance for the object (Y axis). "
               + "The evaluated value is clamped between 0 and 1, and then scaled by maxAttractionReach.")]
        public AnimationCurve attractionReachByDistance;

        private Anchor _preferredAnchor = null;
        /// <summary>
        /// Gets the anchor this AnchorableBehaviour would most prefer to attach to.
        /// This value is refreshed every Update() during which the AnchorableBehaviour
        /// has no anchor or is detached from its current anchor.
        /// </summary>
        public Anchor preferredAnchor { get { return _preferredAnchor; } }

        #region Events

        /// <summary>
        /// Called when this AnchorableBehaviour attaches to an Anchor.
        /// </summary>
        public Action OnAttachedToAnchor = () => { };

        /// <summary>
        /// Called when this AnchorableBehaviour locks to an Anchor.
        /// </summary>
        public Action OnLockedToAnchor = () => { };

        /// <summary>
        /// Called when this AnchorableBehaviour detaches from an Anchor.
        /// </summary>
        public Action OnDetachedFromAnchor = () => { };

        /// <summary>
        /// Called during every Update() in which this AnchorableBehaviour is attached to an Anchor.
        /// </summary>
        public Action WhileAttachedToAnchor = () => { };

        /// <summary>
        /// Called during every Update() in which this AnchorableBehaviour is locked to an Anchor.
        /// </summary>
        public Action WhileLockedToAnchor = () => { };

        /// <summary>
        /// Called just after this anchorable behaviour's InteractionBehaviour OnObjectGraspEnd for
        /// this anchor. This callback will never fire if tryAttachAnchorOnGraspEnd is not enabled.
        ///
        /// If tryAttachAnchorOnGraspEnd is enabled, the anchor will be attached to
        /// an anchor only if its preferredAnchor property is non-null; otherwise, the
        /// attempt to anchor failed.
        /// </summary>
        public Action OnPostTryAnchorOnGraspEnd = () => { };

        #endregion

        private bool _isLockedToAnchor = false;
        private Vector3 _offsetTowardsHand = Vector3.zero;
        private Vector3 _targetPositionLastUpdate = Vector3.zero;
        private bool _hasTargetPositionLastUpdate = false;

        private bool _isRotationLockedToAnchor = false;
        private Quaternion _targetRotationLastUpdate = Quaternion.identity;
        private bool _hasTargetRotationLastUpdate = false;

        void OnValidate()
        {
            refreshInteractionBehaviour();
            refreshInspectorConveniences();
        }

        void Reset()
        {
            refreshInteractionBehaviour();
        }

        void Awake()
        {
            refreshInteractionBehaviour();
            refreshInspectorConveniences();

            if (anchorGroup != null)
            {
                anchorGroup.NotifyAnchorableObjectAdded(this);
            }

            if (interactionBehaviour != null)
            {
                interactionBehaviour.OnGraspBegin += detachAnchorOnGraspBegin;

                if (_tryAnchorNearestOnGraspEnd)
                {
                    interactionBehaviour.OnGraspEnd += tryToAnchorOnGraspEnd;
                }
            }

            initUnityEvents();
        }

        void Start()
        {
            if (anchor != null && _isAttached)
            {
                anchor.NotifyAttached(this);
                OnAttachedToAnchor();
            }
        }

        private bool _reactivateGravityOnDetach = false;

        void Update()
        {
            updateAttractionToHand();

            if (anchor != null && isAttached)
            {
                if (interactionBehaviour != null && interactionBehaviour.rigidbody.useGravity)
                {
                    // TODO: This is a temporary fix for gravity to be fixed in a future IE PR.
                    // The proper solution involves switching the behaviour to FixedUpdate and more
                    // intelligently communicating with the attached InteractionBehaviour.
                    interactionBehaviour.rigidbody.useGravity = false;
                    _reactivateGravityOnDetach = true;
                }

                updateAnchorAttachment();
                if (anchorRotation)
                {
                    updateAnchorAttachmentRotation();
                }

                WhileAttachedToAnchor.Invoke();

                if (_isLockedToAnchor)
                {
                    WhileLockedToAnchor.Invoke();
                }
            }

            updateAnchorPreference();
        }

        void OnDisable()
        {
            if (!this.enabled)
            {
                Detach();
            }

            // Make sure we don't leave dangling anchor-preference state.
            endAnchorPreference();
        }

        void OnDestroy()
        {
            if (interactionBehaviour != null)
            {
                interactionBehaviour.OnGraspBegin -= detachAnchorOnGraspBegin;
                interactionBehaviour.OnGraspEnd -= tryToAnchorOnGraspEnd;
            }

            // Make sure we don't leave dangling anchor-preference state.
            endAnchorPreference();
        }

        private void refreshInspectorConveniences()
        {
            _minAttachmentDotProduct = Mathf.Cos(_maxAttachmentAngle * Mathf.Deg2Rad);
            _maxMotionlessRange = maxAnchorRange * _motionlessRangeFraction;
        }

        private void refreshInteractionBehaviour()
        {
            interactionBehaviour = GetComponent<InteractionBehaviour>();
            _interactionBehaviourIsNull = interactionBehaviour == null;

            detachWhenGrasped = !_interactionBehaviourIsNull;
            if (_interactionBehaviourIsNull)
            {
                useTrajectory = false;
            }
        }

        /// <summary>
        /// Detaches this Anchorable object from its anchor. The anchor reference
        /// remains unchanged. Call TryAttach() to re-attach to this object's assigned anchor.
        /// </summary>
        public void Detach()
        {
            isAttached = false;
        }

        /// <summary>
        /// Returns whether the argument anchor is an acceptable anchor for this anchorable
        /// object; that is, whether the argument Anchor is within this behaviour's AnchorGroup
        /// if it has one, or if this behaviour has no AnchorGroup, returns true.
        /// </summary>
        public bool IsValidAnchor(Anchor anchor)
        {
            if (anchor == null)
            {
                return true;
            }

            if (this.anchorGroup != null)
            {
                return this.anchorGroup.Contains(anchor);
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Returns whether the specified anchor is within attachment range of this Anchorable object.
        /// </summary>
        public bool IsWithinRange(Anchor anchor)
        {
            return (this.transform.position - anchor.transform.position).sqrMagnitude < maxAnchorRange * maxAnchorRange;
        }

        /// <summary>
        /// Attempts to find and return the best anchor for this anchorable object to attach to
        /// based on its current configuration. If useTrajectory is enabled, the object will
        /// consider anchor proximity as well as its own trajectory towards a particular anchor,
        /// and may return null if the object is moving away from all of its possible anchors.
        /// Otherwise, the object will simply return the nearest valid anchor, or null if there
        /// is no valid anchor nearby.
        ///
        /// This method is called every Update() automatically by anchorable objects, and its
        /// result is stored in preferredAnchor. Only call this if you need a new calculation.
        /// </summary>
        public Anchor FindPreferredAnchor()
        {
            if (!useTrajectory)
            {
                // Simply try to attach to the nearest valid anchor.
                return GetNearestValidAnchor();
            }
            else
            {
                // Pick the nearby valid anchor with the highest score, based on proximity and trajectory.
                Anchor optimalAnchor = null;
                float optimalScore = 0F;
                Anchor testAnchor = null;
                float testScore = 0F;
                foreach (var anchor in GetNearbyValidAnchors())
                {
                    testAnchor = anchor;
                    testScore = getAnchorScore(anchor);

                    // Scores of 0 mark ineligible anchors.
                    if (testScore == 0F)
                    {
                        continue;
                    }

                    if (testScore > optimalScore)
                    {
                        optimalAnchor = testAnchor;
                        optimalScore = testScore;
                    }
                }

                return optimalAnchor;
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
        public List<Anchor> GetNearbyValidAnchors(bool requireAnchorHasSpace = true,
                                                  bool requireAnchorActiveAndEnabled = true)
        {
            HashSet<Anchor> anchorsToCheck;

            if (this.anchorGroup == null)
            {
                anchorsToCheck = Anchor.allAnchors;
            }
            else
            {
                anchorsToCheck = this.anchorGroup.anchors;
            }

            _nearbyAnchorsBuffer.Clear();
            foreach (var anchor in anchorsToCheck)
            {
                if ((requireAnchorHasSpace && (!anchor.allowMultipleObjects && anchor.anchoredObjects.Count != 0))
                    || (requireAnchorActiveAndEnabled && !anchor.isActiveAndEnabled))
                {
                    continue;
                }

                if ((anchor.transform.position - this.transform.position).sqrMagnitude <= maxAnchorRange * maxAnchorRange)
                {
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
        public Anchor GetNearestValidAnchor(bool requireWithinRange = true,
                                            bool requireAnchorHasSpace = true,
                                            bool requireAnchorActiveAndEnabled = true)
        {
            HashSet<Anchor> anchorsToCheck;

            if (this.anchorGroup == null)
            {
                anchorsToCheck = Anchor.allAnchors;
            }
            else
            {
                anchorsToCheck = this.anchorGroup.anchors;
            }

            Anchor closestAnchor = null;
            float closestDistSqrd = float.PositiveInfinity;
            foreach (var testAnchor in anchorsToCheck)
            {
                if (requireAnchorHasSpace)
                {
                    bool anchorHasSpace = testAnchor.anchoredObjects.Count == 0
                                          || testAnchor.allowMultipleObjects;
                    if (!anchorHasSpace)
                    {
                        // Skip the anchor for consideration.
                        continue;
                    }
                }
                if (requireAnchorActiveAndEnabled && !testAnchor.isActiveAndEnabled)
                {
                    // Skip the anchor for consideration.
                    continue;
                }

                float testDistanceSqrd = (testAnchor.transform.position - this.transform.position).sqrMagnitude;
                if (testDistanceSqrd < closestDistSqrd)
                {
                    closestAnchor = testAnchor;
                    closestDistSqrd = testDistanceSqrd;
                }
            }

            if (!requireWithinRange || closestDistSqrd < maxAnchorRange * maxAnchorRange)
            {
                return closestAnchor;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Attempts to attach to this Anchorable object's currently specified anchor.
        /// The attempt may fail if this anchor is out of range. Optionally, the range
        /// requirement can be ignored.
        /// </summary>
        public bool TryAttach(bool ignoreRange = false)
        {
            if (anchor != null && (ignoreRange || IsWithinRange(anchor)))
            {
                isAttached = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to find and attach this anchorable object to the nearest valid anchor, or the
        /// most optimal nearby anchor based on proximity and the object's trajectory if useTrajectory
        /// is enabled.
        /// </summary>
        public bool TryAttachToNearestAnchor()
        {
            Anchor preferredAnchor = FindPreferredAnchor();

            if (preferredAnchor != null)
            {
                _preferredAnchor = preferredAnchor;
                anchor = preferredAnchor;
                isAttached = true;
                return true;
            }

            return false;
        }

        /// <summary> Score an anchor based on its proximity and this object's trajectory relative to it. </summary>
        private float getAnchorScore(Anchor anchor)
        {
            return GetAnchorScore(this.interactionBehaviour.rigidbody.position,
                                  this.interactionBehaviour.rigidbody.velocity,
                                  anchor.transform.position,
                                  maxAnchorRange,
                                  _maxMotionlessRange,
                                  _minAttachmentDotProduct,
                                  _alwaysAttachDistance);
        }

        /// <summary>
        /// Calculates and returns a score from 0 (non-valid anchor) to 1 (ideal anchor) based on
        /// the argument configuration, using an anchorable object's position and velocity, an
        /// anchor position, and distance/angle settings. A score of zero indicates an invalid
        /// anchor no matter what; a non-zero score indicates a possible anchor, with more optimal
        /// anchors receiving a score closer to 1.
        /// </summary>
        public static float GetAnchorScore(Vector3 anchObjPos, Vector3 anchObjVel, Vector3 anchorPos, float maxDistance, float nonDirectedMaxDistance, float minAngleProduct,
                                           float alwaysAttachDistance = 0f)
        {
            // Calculated a "directedness" heuristic for determining whether the user is throwing or releasing without directed motion.
            float directedness = anchObjVel.magnitude.Map(0.20F, 1F, 0F, 1F);

            float effMaxDistance = directedness.Map(0F, 1F, nonDirectedMaxDistance, maxDistance);
            Vector3 effPos = Utils.Map(Mathf.Sqrt(Mathf.Sqrt(directedness)), 0f, 1f,
                                       anchObjPos, (anchObjPos - anchObjVel.normalized * effMaxDistance * 0.30f));

            float distanceSqrd = (anchorPos - effPos).sqrMagnitude;
            float distanceScore;
            if (distanceSqrd > effMaxDistance * effMaxDistance)
            {
                distanceScore = 0F;
            }
            else
            {
                distanceScore = distanceSqrd.Map(0F, effMaxDistance * effMaxDistance, 1F, 0F);
            }

            float angleScore;
            float dotProduct = Vector3.Dot(anchObjVel.normalized, (anchorPos - effPos).normalized);

            // Angular score only factors in based on how directed the motion of the object is.
            dotProduct = Mathf.Lerp(1F, dotProduct, directedness);

            angleScore = dotProduct.Map(minAngleProduct, 1f, 0f, 1f);
            angleScore *= angleScore;

            // Support an "always-attach distance" within which only distanceScore matters
            float semiDistanceSqrd = (anchorPos - Vector3.Lerp(anchObjPos, effPos, 0.5f)).sqrMagnitude;
            float useAlwaysAttachDistanceAmount = semiDistanceSqrd.Map(0f, Mathf.Max(0.0001f, (0.25f * alwaysAttachDistance * alwaysAttachDistance)),
                                                                       1f, 0f);
            angleScore = useAlwaysAttachDistanceAmount.Map(0f, 1f, angleScore, 1f);

            return distanceScore * angleScore;
        }

        private void updateAttractionToHand()
        {
            if (interactionBehaviour == null || anchor == null || !isAttractedByHand)
            {
                if (_offsetTowardsHand != Vector3.zero)
                {
                    _offsetTowardsHand = Vector3.Lerp(_offsetTowardsHand, Vector3.zero, 5F * Time.deltaTime);
                }

                return;
            }

            float reachTargetAmount = 0F;
            Vector3 towardsHand = Vector3.zero;
            if (interactionBehaviour.isHovered)
            {
                Vector3 hoverTarget = Vector3.zero;

                InteractionController hoveringController = interactionBehaviour.closestHoveringController;
                if (hoveringController is InteractionHand)
                {
                    Hand hoveringHand = interactionBehaviour.closestHoveringHand;
                    hoverTarget = hoveringHand.PalmPosition;
                }
                else
                {
                    hoverTarget = hoveringController.hoverPoint;
                }

                reachTargetAmount = Mathf.Clamp01(attractionReachByDistance.Evaluate(
                   Vector3.Distance(hoverTarget, anchor.transform.position)));
                towardsHand = hoverTarget - anchor.transform.position;
            }

            Vector3 targetOffsetTowardsHand = towardsHand * maxAttractionReach * reachTargetAmount;
            _offsetTowardsHand = Vector3.Lerp(_offsetTowardsHand, targetOffsetTowardsHand, 5 * Time.deltaTime);
        }

        private void updateAnchorAttachment()
        {
            // Initialize position.
            Vector3 finalPosition;
            if (interactionBehaviour != null)
            {
                finalPosition = interactionBehaviour.rigidbody.position;
            }
            else
            {
                finalPosition = this.transform.position;
            }

            // Update position based on anchor state.
            Vector3 targetPosition = anchor.transform.position;
            if (lockToAnchor)
            {
                // In this state, we are simply locked directly to the anchor.
                finalPosition = targetPosition + _offsetTowardsHand;

                // Reset anchor position storage; it can't be updated from this state.
                _hasTargetPositionLastUpdate = false;
            }
            else if (lockWhenAttached)
            {
                if (_isLockedToAnchor)
                {
                    // In this state, we are already attached to the anchor.
                    finalPosition = targetPosition + _offsetTowardsHand;

                    // Reset anchor position storage; it can't be updated from this state.
                    _hasTargetPositionLastUpdate = false;
                }
                else
                {
                    // Undo any "reach towards hand" offset.
                    finalPosition -= _offsetTowardsHand;

                    // If desired, automatically correct for the anchor itself moving while attempting to return to it.
                    if (matchAnchorMotionWhileAttaching && this.transform.parent != anchor.transform)
                    {
                        if (_hasTargetPositionLastUpdate)
                        {
                            finalPosition += (targetPosition - _targetPositionLastUpdate);
                        }

                        _targetPositionLastUpdate = targetPosition;
                        _hasTargetPositionLastUpdate = true;
                    }

                    // Lerp towards the anchor.
                    finalPosition = Vector3.Lerp(finalPosition, targetPosition, anchorLerpCoeffPerSec * Time.deltaTime);
                    if (Vector3.Distance(finalPosition, targetPosition) < 0.001F)
                    {
                        _isLockedToAnchor = true;
                    }

                    // Redo any "reach toward hand" offset.
                    finalPosition += _offsetTowardsHand;
                }
            }

            // Set final position.
            if (interactionBehaviour != null)
            {
                interactionBehaviour.rigidbody.position = finalPosition;
                this.transform.position = finalPosition;
            }
            else
            {
                this.transform.position = finalPosition;
            }
        }

        private void updateAnchorAttachmentRotation()
        {
            // Initialize rotation.
            Quaternion finalRotation;
            if (interactionBehaviour != null)
            {
                finalRotation = interactionBehaviour.rigidbody.rotation;
            }
            else
            {
                finalRotation = this.transform.rotation;
            }

            // Update rotation based on anchor state.
            Quaternion targetRotation = anchor.transform.rotation;
            if (lockToAnchor)
            {
                // In this state, we are simply locked directly to the anchor.
                finalRotation = targetRotation;

                // Reset anchor rotation storage; it can't be updated from this state.
                _hasTargetPositionLastUpdate = false;
            }
            else if (lockWhenAttached)
            {
                if (_isRotationLockedToAnchor)
                {
                    // In this state, we are already attached to the anchor.
                    finalRotation = targetRotation;

                    // Reset anchor rotation storage; it can't be updated from this state.
                    _hasTargetRotationLastUpdate = false;
                }
                else
                {
                    // If desired, automatically correct for the anchor itself moving while attempting to return to it.
                    if (matchAnchorMotionWhileAttaching && this.transform.parent != anchor.transform)
                    {
                        if (_hasTargetRotationLastUpdate)
                        {
                            finalRotation = (Quaternion.Inverse(_targetRotationLastUpdate) * targetRotation) * finalRotation;
                        }

                        _targetRotationLastUpdate = targetRotation;
                        _hasTargetRotationLastUpdate = true;
                    }

                    // Slerp towards the anchor rotation.
                    finalRotation = Quaternion.Slerp(finalRotation, targetRotation, anchorLerpCoeffPerSec * 0.8F * Time.deltaTime);

                    if (Quaternion.Angle(targetRotation, finalRotation) < 2F)
                    {
                        _isRotationLockedToAnchor = true;
                    }
                }
            }

            // Set final rotation.
            if (interactionBehaviour != null)
            {
                interactionBehaviour.rigidbody.rotation = finalRotation;
                this.transform.rotation = finalRotation;
            }
            else
            {
                this.transform.rotation = finalRotation;
            }
        }

        private void updateAnchorPreference()
        {
            Anchor newPreferredAnchor;
            if (!isAttached)
            {
                newPreferredAnchor = FindPreferredAnchor();
            }
            else
            {
                newPreferredAnchor = null;
            }

            if (_preferredAnchor != newPreferredAnchor)
            {
                if (_preferredAnchor != null)
                {
                    _preferredAnchor.NotifyEndAnchorPreference(this);
                }

                _preferredAnchor = newPreferredAnchor;

                if (_preferredAnchor != null)
                {
                    _preferredAnchor.NotifyAnchorPreference(this);
                }
            }
        }

        private void endAnchorPreference()
        {
            if (_preferredAnchor != null)
            {
                _preferredAnchor.NotifyEndAnchorPreference(this);
                _preferredAnchor = null;
            }
        }

        private void detachAnchorOnGraspBegin()
        {
            Detach();
        }

        private void tryToAnchorOnGraspEnd()
        {
            TryAttachToNearestAnchor();

            OnPostTryAnchorOnGraspEnd();
        }

        #region Unity Events (Internal)

        [SerializeField]
        private EnumEventTable _eventTable;

        public enum EventType
        {
            OnAttachedToAnchor = 100,
            OnLockedToAnchor = 105,
            OnDetachedFromAnchor = 110,
            WhileAttachedToAnchor = 120,
            WhileLockedToAnchor = 125,
            OnPostTryAnchorOnGraspEnd = 130
        }

        private void initUnityEvents()
        {
            // If the interaction component is added at runtime, _eventTable won't have been
            // constructed yet.
            if (_eventTable == null)
            {
                _eventTable = new EnumEventTable();
            }

            setupCallback(ref OnAttachedToAnchor, EventType.OnAttachedToAnchor);
            setupCallback(ref OnLockedToAnchor, EventType.OnLockedToAnchor);
            setupCallback(ref OnDetachedFromAnchor, EventType.OnDetachedFromAnchor);
            setupCallback(ref WhileAttachedToAnchor, EventType.WhileAttachedToAnchor);
            setupCallback(ref WhileLockedToAnchor, EventType.WhileLockedToAnchor);
            setupCallback(ref OnPostTryAnchorOnGraspEnd, EventType.OnPostTryAnchorOnGraspEnd);
        }

        private void setupCallback(ref Action action, EventType type)
        {
            if (_eventTable.HasUnityEvent((int)type))
            {
                action += () => _eventTable.Invoke((int)type);
            }
            else
            {
                action += () => { };
            }
        }

        #endregion

    }
}