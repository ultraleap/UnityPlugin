/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
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

namespace Leap.Unity.Interaction.PhysicsHands
{

    public class PhysicsProvider : PostProcessProvider
    {
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
        public float Strength => _strength;
        [SerializeField, Range(0.1f, 2f)]
        private float _strength = 2f;

        private float _forceLimit = 1000f;
        private float _stiffness = 100f;

        public float PerBoneMass => _perBoneMass;
        [SerializeField, Tooltip("The mass of each finger bone; the palm will be 3x this.")]
        private float _perBoneMass = 0.6f;

        public float HandTeleportDistance => _handTeleportDistance;
        [SerializeField, Tooltip("The distance between the physics and original data hand can reach before it snaps back to the original hand position."), Range(0.01f, 0.5f)]
        private float _handTeleportDistance = 0.1f;

        public float HandGraspTeleportDistance => _handGraspTeleportDistance;
        [SerializeField, Tooltip("The distance between the physics and original data hand can reach before it snaps back to the original hand position. This is used when a hand is reported as grasping."), Range(0.01f, 0.5f)]
        private float _handGraspTeleportDistance = 0.2f;

        // Helper Settings
        [SerializeField, Tooltip("Enabling helpers is recommended as these allow the user to pick up objects they normally would not be able to. " +
            "This includes large and very small objects, as well as kinematic objects.")]
        private bool _enableHelpers = true;

        [SerializeField, Tooltip("Disabling this will allow for the grasp helper to report heuristical \"grab\" information, but will not move the objects.")]
        private bool _helperMovesObjects = true;
        public bool HelperMovesObjects => _helperMovesObjects;

        [SerializeField, Tooltip("Enabling this will cause the hand to move more slowly when grasping objects of higher weights.")]
        private bool _interpolateMass = true;
        public bool InterpolatingMass => _interpolateMass;

        [SerializeField, Tooltip("The maximum weight of the object that a helper can move.")]
        private float _maxMass = 15f;
        public float MaxMass => _maxMass;

        // Helpers
        private Dictionary<Rigidbody, PhysicsGraspHelper> _graspHelpers = new Dictionary<Rigidbody, PhysicsGraspHelper>();

        // This stores the objects as they jump between layers
        private Dictionary<Rigidbody, HashSet<PhysicsBone>> _boneQueue = new Dictionary<Rigidbody, HashSet<PhysicsBone>>();

        public HashSet<Rigidbody> GraspTargets => _graspTargets;
        private HashSet<Rigidbody> _graspTargets = new HashSet<Rigidbody>();

        // Cache for physics calculations
        private int _resultCount = 0;
        private Collider[] _resultsCache = new Collider[64];
        private Vector3 _tempVector = Vector3.zero;

        private HashSet<Rigidbody> _graspLayerRigid = new HashSet<Rigidbody>();
        private HashSet<Rigidbody> _hoveredItems = new HashSet<Rigidbody>();
        private HashSet<PhysicsHand> _hoveringHands = new HashSet<PhysicsHand>();

        private Dictionary<PhysicsHand, float[]> _fingerStrengths = new Dictionary<PhysicsHand, float[]>();
        public Dictionary<PhysicsHand, float[]> FingerStrengths => _fingerStrengths;

        private LayerMask _hoverMask, _contactMask;

        // These events are place holders
        public Action<Rigidbody> OnHover, OnHoverExit;

        public Action<Rigidbody> OnGrasp, OnGraspExit;

        public Action<Rigidbody> OnContact, OnContactExit;

        public Action<Rigidbody, PhysicsGraspHelper> OnObjectStateChange;

        private int _leftIndex = -1, _rightIndex = -1;
        private Hand _leftOriginalLeap, _rightOriginalLeap;

        private bool _leftWasNull = true, _rightWasNull = true;

        private void Awake()
        {
            _leftOriginalLeap = new Hand();
            _rightOriginalLeap = new Hand();
            GenerateLayers();
            SetupAutomaticCollisionLayers();
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

            _hoverMask = new LayerMask();
            _contactMask = new LayerMask();
            for (int i = 0; i < _interactableLayers.Count; i++)
            {
                _hoverMask = _hoverMask | _interactableLayers[i].layerMask;
                _contactMask = _contactMask | _interactableLayers[i].layerMask;
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
            LeftHand = PhysicsHandsUtils.GenerateHand(Chirality.Left, _perBoneMass, _strength, _forceLimit, _stiffness, _handsLayer, gameObject);
            RightHand = PhysicsHandsUtils.GenerateHand(Chirality.Right, _perBoneMass, _strength, _forceLimit, _stiffness, _handsLayer, gameObject);
        }

        #endregion

        public override void ProcessFrame(ref Frame inputFrame)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (LeftHand != null)
            {
                _leftIndex = inputFrame.Hands.FindIndex(x => x.IsLeft);
                if (_leftIndex != -1)
                {
                    _leftOriginalLeap.CopyFrom(inputFrame.Hands[_leftIndex]);
                }
                ApplyHand(_leftIndex, ref inputFrame, LeftHand);
            }

            if (RightHand != null)
            {
                _rightIndex = inputFrame.Hands.FindIndex(x => x.IsRight);
                if (_rightIndex != -1)
                {
                    _rightOriginalLeap.CopyFrom(inputFrame.Hands[_rightIndex]);
                }
                ApplyHand(_rightIndex, ref inputFrame, RightHand);
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

        private void FixedUpdate()
        {
            if (LeftHand == null || RightHand == null)
            {
                Debug.LogError("Physics hands have not been correctly generated. Please exit play mode and press \"Re-generate Hands\".");
                this.enabled = false;
                return;
            }

            UpdatePhysicsHand(LeftHand, _leftIndex == -1 ? null : _leftOriginalLeap, ref _leftWasNull);
            UpdatePhysicsHand(RightHand, _rightIndex == -1 ? null : _rightOriginalLeap, ref _rightWasNull);

            if (_enableHelpers)
            {
                ComputeHelperBones();
            }

            PhysicsGraspHelper.State oldState, state;

            foreach (var helper in _graspHelpers)
            {
                if (helper.Value.Ignored)
                {
                    continue;
                }
                oldState = helper.Value.GraspState;
                state = helper.Value.UpdateHelper();
                if (state != oldState)
                {
                    OnObjectStateChange?.Invoke(helper.Value.Rigidbody, helper.Value);
                }
            }

            UpdateHandStates();
        }

        #region Helper Physics

        private void ComputeHelperBones()
        {
            _hoveringHands.Clear();

            // Check Hovers

            ComputeHelperHandLayer(LeftHand);
            ComputeHelperHandLayer(RightHand);

            // Apply Layers
            ApplyHoverLayers();

            // Check Contacts
            foreach (var hand in _hoveringHands)
            {
                UpdateHandStatistics(hand);
                PhysicsOverlapsForHand(hand);
            }

            // Send those bones to the helpers
            ApplyGraspBones();
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
                if (!_graspLayerRigid.Contains(rigid))
                {
                    OnHover?.Invoke(rigid);
                }
                _graspLayerRigid.Add(rigid);
            }
            _graspLayerRigid.RemoveWhere(ValidateUnhoverRigids);
            _hoveredItems.Clear();
        }

        private bool ValidateUnhoverRigids(Rigidbody rigid)
        {
            if (!_hoveredItems.Contains(rigid))
            {
                OnHoverExit?.Invoke(rigid);
                if (_boneQueue.ContainsKey(rigid))
                {
                    _boneQueue.Remove(rigid);
                }
                if (_graspHelpers.ContainsKey(rigid))
                {
                    if (_graspHelpers[rigid].GraspState == PhysicsGraspHelper.State.Grasp)
                    {
                        OnGraspExit?.Invoke(rigid);
                    }
                    _graspHelpers[rigid].ReleaseHelper();
                    OnObjectStateChange?.Invoke(rigid, _graspHelpers[rigid]);
                    _graspHelpers.Remove(rigid);
                }
                return true;
            }
            return false;
        }

        // Simple check to see if we actually find some rigidbodies to interact with
        // Store them in the layer hash
        private void PhysicsLayerChecksForHand(PhysicsHand hand)
        {
            PhysicsHand.Hand pH = hand.GetPhysicsHand();
            _resultCount = Physics.OverlapSphereNonAlloc(pH.transform.position + (-pH.transform.up * 0.05f) + (pH.transform.forward * 0.02f), 0.08f, _resultsCache, _hoverMask);

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

        private void PhysicsOverlapsForHand(PhysicsHand hand)
        {
            PhysicsHand.Hand pH = hand.GetPhysicsHand();

            _tempVector.x = 0;
            _tempVector.y = -pH.triggerDistance;
            _resultCount = PhysExts.OverlapBoxNonAllocOffset(pH.palmCollider, _tempVector, _resultsCache, _contactMask);
            for (int i = 0; i < _resultCount; i++)
            {
                if (_resultsCache[i].attachedRigidbody != null)
                {
                    if (_boneQueue.ContainsKey(_resultsCache[i].attachedRigidbody))
                    {
                        _boneQueue[_resultsCache[i].attachedRigidbody].Add(pH.palmBone);
                    }
                    else
                    {
                        _boneQueue.Add(_resultsCache[i].attachedRigidbody, new HashSet<PhysicsBone>() { pH.palmBone });
                    }
                }
            }

            for (int i = 0; i < pH.jointColliders.Length; i++)
            {
                // Skip the first bone as we don't need it for grasping
                if (pH.jointBones[i].Joint == 0)
                    continue;

                if (pH.jointBones[i].Finger == 0)
                {
                    _tempVector.x = -pH.triggerDistance / 2f;
                    _tempVector.z = pH.triggerDistance / 2f;
                }
                else
                {
                    _tempVector.x = 0;
                    _tempVector.z = 0;
                }
                _resultCount = PhysExts.OverlapCapsuleNonAllocOffset(pH.jointColliders[i], _tempVector, _resultsCache, _contactMask, radius: pH.jointBones[i].Finger == 0 ? pH.jointColliders[i].radius * 1.8f : -1);
                for (int j = 0; j < _resultCount; j++)
                {
                    if (_resultsCache[j].attachedRigidbody != null)
                    {
                        // Stop the bone from repeatedly being added
                        if (_boneQueue.ContainsKey(_resultsCache[j].attachedRigidbody))
                        {
                            _boneQueue[_resultsCache[j].attachedRigidbody].Add(pH.jointBones[i]);
                        }
                        else
                        {
                            _boneQueue.Add(_resultsCache[j].attachedRigidbody, new HashSet<PhysicsBone>() { pH.jointBones[i] });
                        }
                    }
                }
            }
        }

        private void ApplyGraspBones()
        {
            PhysicsGraspHelper pghTemp;
            foreach (var hashset in _boneQueue)
            {
                if (_graspHelpers.TryGetValue(hashset.Key, out pghTemp))
                {
                    pghTemp.UpdateBones(hashset.Value);
                }
                hashset.Value.Clear();
            }
        }

        #endregion

        private void UpdateHandStates()
        {
            FindHandState(LeftHand);
            FindHandState(RightHand);
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
                    mass += item.Value.Rigidbody.mass;
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
        public bool IsGraspingObject(Rigidbody rigid, out Hand hand)
        {
            hand = null;
            if (_graspHelpers.TryGetValue(rigid, out PhysicsGraspHelper helper))
            {
                if (helper.GraspState == PhysicsGraspHelper.State.Grasp && helper.GraspingHands.Count > 0)
                {
                    hand = helper.GraspingHands[helper.GraspingHands.Count - 1].GetLeapHand();
                    return true;
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