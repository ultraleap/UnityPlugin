/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Interaction.PhysicsHands
{

    public class PhysicsProvider : PostProcessProvider
    {

        #region Inspector
        // Hands
        [field: SerializeField, HideInInspector]
        public PhysicsHand LeftHand { get; private set; } = null;
        [field: SerializeField, HideInInspector]
        public PhysicsHand RightHand { get; private set; } = null;

        // Layers
        // Object Layers
        public SingleLayer DefaultLayer => _defaultLayer;
        [SerializeField, Tooltip("This layer will be used as the base when automatically generating layers.")]
        private SingleLayer _defaultLayer = 0;

        public List<SingleLayer> InteractableLayers => _interactableLayers;
        [SerializeField, Tooltip("The layers that you want hands and helpers to make contact and interact with. If no layers are added then the default layer will be included.")]
        private List<SingleLayer> _interactableLayers = new List<SingleLayer>() { 0 };

        public List<SingleLayer> NoContactLayers => _noContactLayers;
        [SerializeField, Tooltip("Layers that should be ignored by the hands. " +
            "These layers should be used throughout your scene on objects that you want hands to freely pass through. We recommend creating these layers manually. " +
            "If no layers are added then a layer will be created.")]
        private List<SingleLayer> _noContactLayers = new List<SingleLayer>();

        // Hand Layers
        public SingleLayer HandsLayer => _handsLayer;
        [SerializeField, Tooltip("The default layer for the hands.")]
        private SingleLayer _handsLayer = new SingleLayer();

        [SerializeField, Tooltip("The default layer for the hands. It is recommended to leave this as an automatically generated layer.")]
        private bool _automaticHandsLayer = true;

        public SingleLayer HandsResetLayer => _handsResetLayer;
        [SerializeField, Tooltip("The layer that the hands will be set to during non-active or reset states.")]
        private SingleLayer _handsResetLayer = new SingleLayer();
        [SerializeField, Tooltip("The layer that the hands will be set to during non-active or reset states. It is recommended to leave this as an automatically generated layer.")]
        private bool _automaticHandsResetLayer = true;

        private bool _layersGenerated = false;

        // Hand Settings
        [SerializeField, Tooltip("Allows the hands to collide with one another.")]
        private bool _interHandCollisions = false;

        public float PerBoneMass => _perBoneMass;
        [SerializeField, Tooltip("The mass of each finger bone; the palm will be 3x this. It is not recommended to modify this too far from the default (0.1)."), Range(0.01f, 0.25f)]
        private float _perBoneMass = 0.1f;

        public PhysicsHand.HandParameters HandParameters => _handParameters;
        [SerializeField]
        private PhysicsHand.HandParameters _handParameters = new PhysicsHand.HandParameters();

        public float HandTeleportDistance => _handTeleportDistance;
        [SerializeField, Tooltip("The distance between the physics and original data hand can reach before it snaps back to the original hand position."), Range(0.01f, 0.5f)]
        private float _handTeleportDistance = 0.15f;

        public int SolverIterations => _handSolverIterations;
        [SerializeField, Tooltip("The solver iterations used when calculating the hand. This can be different to your overall project iterations. Higher numbers will be more robust, but more expensive to compute."), Min(10f)]
        private int _handSolverIterations = 20;

        public int SolverVelocityIterations => _handSolverVelocityIterations;
        [SerializeField, Tooltip("The solver iterations used when calculating the hand velocity. This can be different to your overall project iterations. Higher numbers will be more robust, but more expensive to compute."), Min(5f)]
        private int _handSolverVelocityIterations = 15;

        // Helper Settings
        [SerializeField, Tooltip("Enabling helpers is recommended as these allow the user to pick up objects they normally would not be able to. " +
            "This includes large and very small objects, as well as kinematic objects.")]
        private bool _enableHelpers = true;

        [SerializeField, Tooltip("Disabling this will allow for the grasp helper to report heuristical \"grab\" information, but will not move the objects.")]
        private bool _helperMovesObjects = true;
        public bool HelperMovesObjects => _helperMovesObjects;

        [SerializeField, Tooltip("Enabling this will cause the hand to move more slowly when grasping objects of higher weights. This is an experimental feature.")]
        private bool _interpolateMass = true;
        public bool InterpolatingMass => _interpolateMass;

        [SerializeField, Tooltip("The maximum weight of the object that a helper can move.")]
        private float _maxMass = 10f;
        public float MaxMass => _maxMass;

        [SerializeField, Tooltip("This option will disable hand collisions and improve forces on the object when it is detected as being thrown.")]
        private bool _enhanceThrowing = true;

        public bool EnhanceThrowing => _enhanceThrowing;

        #endregion

        // Helpers
        private Dictionary<Rigidbody, PhysicsGraspHelper> _graspHelpers = new Dictionary<Rigidbody, PhysicsGraspHelper>();

        // This stores the objects as they jump between layers
        private Dictionary<Rigidbody, HashSet<PhysicsBone>> _boneQueue = new Dictionary<Rigidbody, HashSet<PhysicsBone>>();

        // Cache for physics calculations
        private Collider[] _resultsCache = new Collider[16];
        private bool[] _resultsFound = new bool[16];
        private int _resultCount = 0;

        private HashSet<Rigidbody> _graspLayerRigid = new HashSet<Rigidbody>();
        private HashSet<Rigidbody> _hoveredItems = new HashSet<Rigidbody>();
        private HashSet<PhysicsHand> _hoveringHands = new HashSet<PhysicsHand>();

        private Dictionary<PhysicsHand, float[]> _fingerStrengths = new Dictionary<PhysicsHand, float[]>();
        public Dictionary<PhysicsHand, float[]> FingerStrengths => _fingerStrengths;

        private LayerMask _interactionMask;
        public LayerMask InteractionMask => _interactionMask;

        [Obsolete("This event has been replaced by the PhysicsInterface calls. Please reference SubscribeToStateChanges function and PhysicsInterfaces.cs " +
            "This event will be removed in a future version.")]
        public Action<Rigidbody, PhysicsGraspHelper> OnObjectStateChange;

        private Dictionary<Rigidbody, HashSet<Action<PhysicsGraspHelper>>> _objectStateChanges = new Dictionary<Rigidbody, HashSet<Action<PhysicsGraspHelper>>>();

        private int _leftIndex = -1, _rightIndex = -1;
        private Hand _leftOriginalLeap, _rightOriginalLeap;
        private float _physicsSyncTime = 0f;

        private bool _leftWasNull = true, _rightWasNull = true;

        private WaitForFixedUpdate _waitForFixedUpdate = new WaitForFixedUpdate();

        private void Awake()
        {
#if !BURST_AVAILABLE
            Debug.LogWarning("Please install the Unity Burst package, otherwise PhysicsHands performance will be degraded.", this);
#endif
            _leftOriginalLeap = new Hand();
            _rightOriginalLeap = new Hand();
            GenerateLayers();
            SetupAutomaticCollisionLayers();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _waitForFixedUpdate = new WaitForFixedUpdate();
            StartCoroutine(LateFixedFrameProcess());
        }

        private void Reset()
        {
            PhysicsHand[] existingHands = GetComponentsInChildren<PhysicsHand>(true);
            foreach (PhysicsHand hand in existingHands)
            {
                if (Application.isPlaying)
                {
                    Destroy(hand.gameObject);
                }
                else
                {
                    DestroyImmediate(hand.gameObject);
                }
            }
        }

        #region Layer Generation

        protected void GenerateLayers()
        {
            if (_layersGenerated)
            {
                return;
            }

            if (_automaticHandsLayer || HandsLayer == DefaultLayer)
            {
                _handsLayer = -1;
            }
            if (_automaticHandsResetLayer || HandsResetLayer == DefaultLayer)
            {
                _handsResetLayer = -1;
            }
            for (int i = 0; i < _noContactLayers.Count; i++)
            {
                if (_noContactLayers[i] == DefaultLayer)
                {
                    _noContactLayers.Remove(i);
                    i--;
                }
            }

            for (int i = 8; i < 32; i++)
            {
                string layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName))
                {
                    if (_noContactLayers.Count == 0)
                    {
                        _noContactLayers.Add(new SingleLayer() { layerIndex = i });
                        continue;
                    }
                    if (_handsLayer == -1)
                    {
                        _handsLayer = i;
                        continue;
                    }
                    else if (_handsResetLayer == -1)
                    {
                        _handsResetLayer = i;
                        break;
                    }
                }
            }

            if (HandsLayer == -1 || HandsResetLayer == -1)
            {
                if (Application.isPlaying)
                {
                    enabled = false;
                }
                Debug.LogError("Could not find enough free layers for "
                              + "auto-setup; manual setup is required.", this.gameObject);
                return;
            }

            if (_interactableLayers.Count == 0)
            {
                _interactableLayers.Add(new SingleLayer() { layerIndex = 0 });
            }

            if (_interHandCollisions)
            {
                _interactableLayers.Add(_handsLayer);
            }

            _interactionMask = new LayerMask();
            for (int i = 0; i < _interactableLayers.Count; i++)
            {
                _interactionMask = _interactionMask | _interactableLayers[i].layerMask;
            }

            _layersGenerated = true;
        }

        private void SetupAutomaticCollisionLayers()
        {
            for (int i = 0; i < 32; i++)
            {
                // Copy ignore settings from template layer
                bool shouldIgnore = Physics.GetIgnoreLayerCollision(DefaultLayer, i);
                Physics.IgnoreLayerCollision(_handsLayer, i, shouldIgnore);

                for (int j = 0; j < _noContactLayers.Count; j++)
                {
                    Physics.IgnoreLayerCollision(_noContactLayers[j], i, shouldIgnore);
                }

                // Hands ignore all contact
                Physics.IgnoreLayerCollision(_handsResetLayer, i, true);
            }

            for (int i = 0; i < _interactableLayers.Count; i++)
            {
                Physics.IgnoreLayerCollision(_interactableLayers[i], _handsLayer, false);
            }

            // Disable interaction between hands and nocontact objects
            for (int i = 0; i < _noContactLayers.Count; i++)
            {
                Physics.IgnoreLayerCollision(_noContactLayers[i], _handsLayer, true);
            }

            // Setup interhand collisions
            Physics.IgnoreLayerCollision(_handsLayer, _handsLayer, !_interHandCollisions);
        }

        #endregion

        #region Hand Generation

        public void GenerateHands()
        {
            LeftHand = PhysicsHandsUtils.GenerateHand(Chirality.Left, _handParameters, _handsLayer, gameObject);
            RightHand = PhysicsHandsUtils.GenerateHand(Chirality.Right, _handParameters, _handsLayer, gameObject);
        }

        #endregion

        public override void ProcessFrame(ref Frame inputFrame)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (LeftHand == null || RightHand == null)
            {
                Debug.LogError("Physics hands have not been correctly generated. Please exit play mode and press \"Re-generate Hands\".");
                this.enabled = false;
                return;
            }

            _leftIndex = inputFrame.Hands.FindIndex(x => x.IsLeft);
            if (_leftIndex != -1)
            {
                _leftOriginalLeap.CopyFrom(inputFrame.Hands[_leftIndex]);
            }

            _rightIndex = inputFrame.Hands.FindIndex(x => x.IsRight);
            if (_rightIndex != -1)
            {
                _rightOriginalLeap.CopyFrom(inputFrame.Hands[_rightIndex]);
            }

            if (Time.inFixedTimeStep)
            {
                FixedFrameProcess();
            }

            ApplyHand(_leftIndex, ref inputFrame, LeftHand);
            // If the left hand has been removed we need to refind the right index
            _rightIndex = inputFrame.Hands.FindIndex(x => x.IsRight);
            ApplyHand(_rightIndex, ref inputFrame, RightHand);
        }

        private void FixedFrameProcess()
        {
            UpdatePhysicsHand(LeftHand, _leftIndex == -1 ? null : _leftOriginalLeap, ref _leftWasNull);
            UpdatePhysicsHand(RightHand, _rightIndex == -1 ? null : _rightOriginalLeap, ref _rightWasNull);

            if (_enableHelpers)
            {
                ComputeHelperBones();
            }

            PhysicsGraspHelper.State oldState, state;

            foreach (var helper in _graspHelpers)
            {
                // Removed ignore check here and moved into the helper so we can still get state information
                oldState = helper.Value.GraspState;
                state = helper.Value.UpdateHelper();
                if (state != oldState)
                {
#pragma warning disable 0618
                    OnObjectStateChange?.Invoke(helper.Value.Rigidbody, helper.Value);
#pragma warning restore 0618
                    SendStates(helper.Value.Rigidbody, helper.Value);
                }
            }
        }

        // Happens after the physics simulation
        private IEnumerator LateFixedFrameProcess()
        {
            yield return null;
            for (; ; )
            {
                UpdateHandStates();

                yield return _waitForFixedUpdate;
            }
        }

        private void ApplyHand(int index, ref Frame inputFrame, PhysicsHand hand)
        {
            if (index != -1)
            {
                if (hand.GetLeapHand() != null)
                {
                    inputFrame.Hands[index].CopyFrom(hand.GetLeapHand());
                }
                else
                {
                    inputFrame.Hands.RemoveAt(index);
                }
            }
        }

        private void UpdatePhysicsHand(PhysicsHand physicsHand, Leap.Hand leapHand, ref bool wasNull)
        {
            if (physicsHand == null)
            {
                return;
            }

            if (leapHand != null && leapHand.TimeVisible > 0)
            {
                if (wasNull)
                {
                    physicsHand.BeginHand(leapHand);
                    wasNull = false;
                }
                physicsHand.UpdateHand(leapHand);
            }
            else
            {
                if (!wasNull)
                {
                    physicsHand.FinishHand();
                    wasNull = true;
                }
            }
        }

        private void LateUpdate()
        {
            PostUpdateOverlapCheck(LeftHand);
            PostUpdateOverlapCheck(RightHand);
        }

        #region Helper Physics

        private void ComputeHelperBones()
        {
            _hoveringHands.Clear();

            // Check Hovers

            ComputeHelperHandLayer(LeftHand);
            ComputeHelperHandLayer(RightHand);
            UpdateHandStatistics(LeftHand);
            UpdateHandStatistics(RightHand);

            // Apply Layers
            ApplyHoverLayers();

            if (LeftHand.IsTracked)
            {
                LeftHand.UpdateHandHeuristics(ref _resultsCache, ref _resultsFound);
            }

            if (RightHand.IsTracked)
            {
                RightHand.UpdateHandHeuristics(ref _resultsCache, ref _resultsFound);
            }
        }

        /// <summary>
        /// Check to see whether an object has been moved into the hand during update, to prevent issues with flinging, pinging, or spaghetti
        /// </summary>
        private void PostUpdateOverlapCheck(PhysicsHand hand)
        {
            if (hand == null || !hand.IsTracked)
            {
                return;
            }

            // Sync transforms in case someone's done a transform.position on a rigidbody
            if (_physicsSyncTime != Time.time)
            {
                Physics.SyncTransforms();
                _physicsSyncTime = Time.time;
            }

            PhysicsHand.Hand pH = hand.GetPhysicsHand();

            Vector3 radiusAmount = Vector3.Scale(pH.palmCollider.size, PhysExts.AbsVec3(pH.palmCollider.transform.lossyScale)) * 0.5f;

            _resultCount = PhysExts.OverlapBoxNonAllocOffset(pH.palmCollider, Vector3.zero, _resultsCache, _interactionMask, QueryTriggerInteraction.Ignore, extraRadius: -PhysExts.MaxVec3(radiusAmount));
            HandleOverlaps(hand);

            for (int i = 0; i < pH.jointColliders.Length; i++)
            {
                _resultCount = PhysExts.OverlapCapsuleNonAllocOffset(pH.jointColliders[i], Vector3.zero, _resultsCache, _interactionMask, QueryTriggerInteraction.Ignore, extraRadius: -pH.jointColliders[i].radius * 0.5f);
                HandleOverlaps(hand);
            }
        }

        /// <summary>
        /// Uses a pre-defined _resultCount and _resultsCache to determine if overlaps have happened and handles the result by ignoring collisions between the hand and the foundBodies
        /// </summary>
        private void HandleOverlaps(PhysicsHand hand)
        {
            HashSet<int> foundBodies = new HashSet<int>();

            for (int i = 0; i < _resultCount; i++)
            {
                if (_resultsCache[i] != null && _resultsCache[i].attachedRigidbody != null)
                {
                    int id = _resultsCache[i].attachedRigidbody.gameObject.GetInstanceID();

                    if (foundBodies.Contains(id))
                    {
                        continue;
                    }

                    if (IsRigidbodyAlreadyColliding(_resultsCache[i].attachedRigidbody))
                    {
                        hand.IgnoreCollision(_resultsCache[i].attachedRigidbody, timeout: Time.fixedDeltaTime * 5f);
                        foundBodies.Add(id);
                    }
                }
            }
        }

        /// <summary>
        /// Simple test to ensure that a rigidbody that is now inside of a hand was already known to the system
        /// </summary>
        private bool IsRigidbodyAlreadyColliding(Rigidbody body)
        {
            if (!_enableHelpers)
            {
                return false;
            }
            // Does a helper already exist (we would be at least hovering)
            if (_graspHelpers.TryGetValue(body, out var helper))
            {
                // Is that helper only hovering?
                if (helper.GraspState == PhysicsGraspHelper.State.Hover)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
            return false;
        }

        private void ComputeHelperHandLayer(PhysicsHand hand)
        {
            if (hand != null && hand.GetPhysicsHand() != null && hand.GetOriginalLeapHand() != null && hand.gameObject.layer == HandsLayer)
            {
                PhysicsLayerChecksForHand(hand);
            }
            else
            {
                foreach (var helper in _graspHelpers)
                {
                    helper.Value.RemoveHand(hand);
                }
            }
        }

        private void ApplyHoverLayers()
        {
            foreach (var rigid in _hoveredItems)
            {
                _graspLayerRigid.Add(rigid);
            }
            _graspLayerRigid.RemoveWhere(ValidateUnhoverRigids);
            _hoveredItems.Clear();
        }

        private bool ValidateUnhoverRigids(Rigidbody rigid)
        {
            if (!_hoveredItems.Contains(rigid))
            {
                if (_boneQueue.ContainsKey(rigid))
                {
                    _boneQueue.Remove(rigid);
                }
                if (_graspHelpers.ContainsKey(rigid))
                {
                    if (_graspHelpers[rigid].GraspState == PhysicsGraspHelper.State.Grasp)
                    {
                        // Ensure we release the object first
                        _graspHelpers[rigid].ReleaseObject();
                    }
                    _graspHelpers[rigid].ReleaseHelper();
#pragma warning disable 0618
                    OnObjectStateChange?.Invoke(rigid, _graspHelpers[rigid]);
#pragma warning restore 0618
                    SendStates(rigid, _graspHelpers[rigid]);

                    _graspHelpers.Remove(rigid);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// This allows you to bind to any rigidbody and then listen to events when it's grab state changes.
        /// Use <b>PhysicsGraspHelper.GraspState</b> to access the state.
        /// Use <b>PhysicsGraspHelper.Rigidbody</b> to access your target.
        /// You will at times receive an <b>Idle</b> state, signifying the unhover of the object.
        /// </summary>
        /// <param name="target">The rigidbody you want to listen to.</param>
        /// <param name="outputFunction">The function you want to be called.</param>
        public void SubscribeToStateChanges(Rigidbody target, Action<PhysicsGraspHelper> outputFunction)
        {
            if (target == null || outputFunction == null)
            {
                Debug.LogWarning("Target object or output function cannot be null when subscribing to state changes.");
                return;
            }
            if (_objectStateChanges.TryGetValue(target, out var hashset))
            {
                hashset.Add(outputFunction);
            }
            else
            {
                _objectStateChanges.Add(target, new HashSet<Action<PhysicsGraspHelper>>() { outputFunction });
            }
        }

        /// <summary>
        /// Removes your target rigidbody and function from being called when a grab state is changed.
        /// </summary>
        /// <param name="target">The rigidbody you are lisening to.</param>
        /// <param name="outputFunction">The function you have being called.</param>
        public void UnsubscribeFromStateChanges(Rigidbody target, Action<PhysicsGraspHelper> outputFunction)
        {
            if (target == null || outputFunction == null)
            {
                Debug.LogWarning("Target object or output function cannot be null when unsubscribing from state changes.");
                return;
            }
            if (_objectStateChanges.TryGetValue(target, out var hashset))
            {
                hashset.Remove(outputFunction);
                if (hashset.Count == 0)
                {
                    _objectStateChanges.Remove(target);
                }
            }
        }

        // Sends out the current grasp helper information to the functions as requested
        private void SendStates(Rigidbody target, PhysicsGraspHelper helper)
        {
            if (_objectStateChanges.TryGetValue(target, out var hashset))
            {
                foreach (var action in hashset)
                {
                    action?.Invoke(helper);
                }
            }
        }

        // Simple check to see if we actually find some rigidbodies to interact with
        // Store them in the layer hash
        private void PhysicsLayerChecksForHand(PhysicsHand hand)
        {
            PhysicsHand.Hand pH = hand.GetPhysicsHand();

            float lerp = 0;
            if (_fingerStrengths.ContainsKey(hand))
            {
                // Get the least curled finger excluding the thumb
                lerp = _fingerStrengths[hand].Skip(1).Min();
            }

            _resultCount = Physics.OverlapCapsuleNonAlloc(pH.transform.position + (-pH.transform.up * 0.025f) + ((hand.Handedness == Chirality.Left ? pH.transform.right : -pH.transform.right) * 0.015f),
                // Interpolate the tip position so we keep it relative to the straightest finger
                pH.transform.position + (-pH.transform.up * Mathf.Lerp(0.025f, 0.07f, lerp)) + (pH.transform.forward * Mathf.Lerp(0.06f, 0.02f, lerp)),
                0.1f, _resultsCache, _interactionMask);

            PhysicsGraspHelper tempHelper;
            for (int i = 0; i < _resultCount; i++)
            {
                if (_resultsCache[i].attachedRigidbody != null)
                {
                    _hoveringHands.Add(hand);
                    _hoveredItems.Add(_resultsCache[i].attachedRigidbody);
                    if (_graspHelpers.TryGetValue(_resultsCache[i].attachedRigidbody, out tempHelper))
                    {
                        tempHelper.AddHand(hand);
                    }
                    else
                    {
                        PhysicsGraspHelper helper = new PhysicsGraspHelper(_resultsCache[i].attachedRigidbody, this);
                        helper.AddHand(hand);
                        _graspHelpers.Add(_resultsCache[i].attachedRigidbody, helper);
                        SendStates(_resultsCache[i].attachedRigidbody, helper);
                    }
                }
            }
        }

        private void UpdateHandStatistics(PhysicsHand hand)
        {
            if (!_fingerStrengths.ContainsKey(hand))
            {
                _fingerStrengths.Add(hand, new float[5]);
            }
            Leap.Hand lHand = hand.GetOriginalLeapHand();
            for (int i = 0; i < 5; i++)
            {
                _fingerStrengths[hand][i] = lHand.GetFingerStrength(i);
            }
        }

        #endregion

        private void UpdateHandStates()
        {
            if (LeftHand.IsTracked)
            {
                FindHandState(LeftHand);
                LeftHand.LateFixedUpdate();
            }
            if (RightHand.IsTracked)
            {
                FindHandState(RightHand);
                RightHand.LateFixedUpdate();
            }
        }

        private void FindHandState(PhysicsHand hand)
        {
            if (hand == null)
            {
                return;
            }

            bool found = false;
            float mass = 0;
            foreach (var item in _graspHelpers)
            {
                if (item.Value.GraspingHands.Contains(hand))
                {
                    found = true;
                    mass += item.Value.OriginalMass;
                    break;
                }
            }
            if (found != hand.IsGrasping)
            {
                hand.SetGrasping(found);
            }
            hand.SetGraspingMass(mass);
        }

        /// <summary>
        /// Reports whether a rigidbody is hovered by check if a helper has been created for it.
        /// </summary>
        public bool IsObjectHovered(Rigidbody rigid)
        {
            return _graspHelpers.ContainsKey(rigid);
        }

        /// <summary>
        /// Reports the status of a rigidbody from within the helpers.
        /// </summary>
        public bool GetObjectState(Rigidbody rigid, out PhysicsGraspHelper.State state)
        {
            state = PhysicsGraspHelper.State.Hover;
            if (_graspHelpers.TryGetValue(rigid, out PhysicsGraspHelper helper))
            {
                state = helper.GraspState;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reports the status of a rigidbody from within the helpers, while also returning the current hands of interest.
        /// </summary>
        public bool GetObjectState(Rigidbody rigid, out PhysicsGraspHelper.State state, out List<PhysicsHand> hands)
        {
            hands = null;
            state = PhysicsGraspHelper.State.Hover;
            if (_graspHelpers.TryGetValue(rigid, out PhysicsGraspHelper helper))
            {
                state = helper.GraspState;
                switch (state)
                {
                    case PhysicsGraspHelper.State.Hover:
                    case PhysicsGraspHelper.State.Contact:
                        hands = helper.GraspingCandidates.ToList();
                        break;
                    case PhysicsGraspHelper.State.Grasp:
                        hands = helper.GraspingHands;
                        break;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reports whether a rigidbody is currently being grasped by a helper.
        /// </summary>
        /// <param name="rigid">The rigidbody you want to check</param>
        public bool IsGraspingObject(Rigidbody rigid)
        {
            return IsGraspingObject(rigid, out var temp);
        }

        /// <summary>
        /// Reports whether a rigidbody is currently being grasped by a helper, while providing the current dominant hand grasping it.
        /// </summary>
        /// <param name="rigid">The rigidbody you want to check</param>
        public bool IsGraspingObject(Rigidbody rigid, out PhysicsHand hand)
        {
            hand = null;
            if (_graspHelpers.TryGetValue(rigid, out PhysicsGraspHelper helper))
            {
                if (helper.GraspState == PhysicsGraspHelper.State.Grasp && helper.GraspingHands.Count > 0)
                {
                    hand = helper.GraspingHands[helper.GraspingHands.Count - 1];
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Reports whether a rigidbody is currently being pinched.
        /// Only returns true if that rigidbody is also being grapsed.
        /// A pinch is defined as thumb tip to provided finger tip.
        /// If no finger is provided, we default to index.
        /// </summary>
        /// <param name="rigid">The rigidbody you want to check</param>
        /// <param name="finger">The finger to check if is pinching</param>
        /// <returns></returns>
        public bool IsPinchingObject(Rigidbody rigid, int finger = 1)
        {
            return IsPinchingObject(rigid, out PhysicsHand _, finger);
        }

        /// <summary>
        /// Reports whether a rigidbody is currently being pinched, while providing the current dominant hand pinching it.
        /// Only returns true if that rigidbody is also being grapsed.
        /// A pinch is defined as thumb tip to provided finger tip.
        /// If no finger is provided, we default to index.
        /// </summary>
        /// <param name="rigid">The rigidbody you want to check</param>
        /// <param name="finger">The finger to check if is pinching</param>

        public bool IsPinchingObject(Rigidbody rigid, out PhysicsHand hand, int finger = 1)
        {
            hand = null;
            if (_graspHelpers.TryGetValue(rigid, out PhysicsGraspHelper helper))
            {
                if (helper.GraspState == PhysicsGraspHelper.State.Grasp && helper.GraspingHands.Count > 0)
                {
                    hand = helper.GraspingHands[helper.GraspingHands.Count - 1];

                    // If thumb isn't grasping, then we're unable to pinch
                    if (!helper.Grasped(hand, 0))
                    {
                        return false;
                    }

                    Hand originalLeapHand = hand.GetOriginalLeapHand();
                    Vector3 thumbTipPos = originalLeapHand.GetThumb().TipPosition;

                    // If tips are close enough to count as pinching
                    if (Vector3.Distance(thumbTipPos, originalLeapHand.Fingers[finger].TipPosition) < 0.015f)
                    {
                        // If the pinched finger is grasping
                        if (helper.Grasped(hand, finger))
                        {
                            return true;
                        }
                    }

                }
            }
            return false;
        }

        private void OnDrawGizmos()
        {
            for (int i = 0; i < _graspHelpers.Count; i++)
            {
                _graspHelpers.ElementAt(i).Value.Gizmo();
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            dataUpdateMode = DataUpdateMode.UpdateAndFixedUpdate;
            passthroughOnly = false;
            if (inputLeapProvider != null)
            {
                editTimePose = inputLeapProvider.editTimePose;
            }
        }
    }
}