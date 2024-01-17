/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    [RequireComponent(typeof(PhysicalHandsManager))]
    public class GrabHelper : MonoBehaviour
    {
        [SerializeField]
        private PhysicalHandsManager physicalHandsManager;

        private ContactHand _leftContactHand { get { return physicalHandsManager.ContactParent.LeftHand; } }
        private ContactHand _rightContactHand { get { return physicalHandsManager.ContactParent.RightHand; } }

        private Collider[] _colliderCache = new Collider[128];

        #region Interaction Data
        private Dictionary<ContactHand, float[]> _fingerStrengths = new Dictionary<ContactHand, float[]>(); // Values are from 0-1 where 1 in fully curled
        internal Dictionary<ContactHand, float[]> FingerStrengths => _fingerStrengths;

        // Helpers
        private Dictionary<Rigidbody, GrabHelperObject> _grabHelpers = new Dictionary<Rigidbody, GrabHelperObject>();
        private HashSet<Rigidbody> _grabRigids = new HashSet<Rigidbody>();
        private HashSet<Rigidbody> _hoveredItems = new HashSet<Rigidbody>();

        #endregion

        private void OnEnable()
        {
            if (physicalHandsManager != null)
            {
                physicalHandsManager.OnPrePhysicsUpdate -= OnPrePhysicsUpdate;
                physicalHandsManager.OnPrePhysicsUpdate += OnPrePhysicsUpdate;
                physicalHandsManager.OnFixedFrame -= OnFixedFrame;
                physicalHandsManager.OnFixedFrame += OnFixedFrame;

                physicalHandsManager.OnContactModeChanged -= ResetHelper;
                physicalHandsManager.OnContactModeChanged += ResetHelper;
            }
        }

        private void OnDisable()
        {
            if (physicalHandsManager != null)
            {
                physicalHandsManager.OnFixedFrame -= OnFixedFrame;
            }
        }

        private void OnPrePhysicsUpdate()
        {
            UpdateOverlaps();
        }

        private void OnFixedFrame(Frame frame)
        {
            UpdateHandHeuristics();
            UpdateHelpers();
            UpdateHandStates();
        }

        internal void ResetHelper()
        {
            // The current hands need to be cleared, including firing all relevand exit events on all grab helper objects
            foreach (var helper in _grabHelpers.Values)
            {
                if (helper.GrabState == GrabHelperObject.State.Grab)
                {
                    helper.ReleaseObject();
                }

                helper.ReleaseHelper();
            }

            _grabHelpers.Clear();
            _grabRigids.Clear();
            _hoveredItems.Clear();

            _fingerStrengths.Clear();
            _colliderCache = new Collider[128];
        }

        #region Hand Updating
        private void UpdateHandHeuristics()
        {
            if (_leftContactHand.tracked)
            {
                UpdateHandStatistics(_leftContactHand);
            }

            if (_rightContactHand.tracked)
            {
                UpdateHandStatistics(_rightContactHand);
            }
        }

        private void UpdateOverlaps()
        {
            HandOverlaps(_leftContactHand);
            HandOverlaps(_rightContactHand);
        }

        private void HandOverlaps(ContactHand hand)
        {
            hand.isHovering = false;
            hand.isContacting = false;
            hand.isCloseToObject = false;

            HelperOverlaps(hand);
        }

        /// <summary>
        /// Used to create helper objects.
        /// </summary>
        private void HelperOverlaps(ContactHand hand)
        {
            float lerp = float.MaxValue;
            if (_fingerStrengths.ContainsKey(hand))
            {
                // Get the least curled finger excluding the thumb
                var curls = _fingerStrengths[hand];

                for(int i = 1; i < curls.Length; i++)
                {
                    if (curls[i] < lerp)
                    {
                        lerp = curls[i];
                    }
                }
            }

            float radiusAmount = PhysExts.MaxVec3(Vector3.Scale(hand.palmBone.palmCollider.size, PhysExts.AbsVec3(hand.palmBone.transform.lossyScale)));

            int results = Physics.OverlapCapsuleNonAlloc(hand.palmBone.transform.position + 
                (-hand.palmBone.transform.up * 0.025f) + ((hand.Handedness == Chirality.Left ? hand.palmBone.transform.right : -hand.palmBone.transform.right) * 0.015f),
                // Interpolate the tip position so we keep it relative to the straightest finger
                hand.palmBone.transform.position + (-hand.palmBone.transform.up * Mathf.Lerp(0.025f, 0.07f, lerp)) + (hand.palmBone.transform.forward * Mathf.Lerp(0.06f, 0.02f, lerp)),
                radiusAmount + physicalHandsManager.HoverDistance, 
                _colliderCache, 
                physicalHandsManager.InteractionMask);

            GrabHelperObject tempHelper;
            for (int i = 0; i < results; i++)
            {
                if (_colliderCache[i].attachedRigidbody != null)
                {
                    _hoveredItems.Add(_colliderCache[i].attachedRigidbody);
                    if (_grabHelpers.TryGetValue(_colliderCache[i].attachedRigidbody, out tempHelper))
                    {
                        tempHelper.AddHand(hand);
                    }
                    else
                    {
                        GrabHelperObject helper = new GrabHelperObject(_colliderCache[i].attachedRigidbody, this);
                        helper.AddHand(hand);
                        _grabHelpers.Add(_colliderCache[i].attachedRigidbody, helper);
                    }
                }
            }

            hand.palmBone.ProcessColliderQueue(_colliderCache, results);
            foreach (var bone in hand.bones)
            {
                bone.ProcessColliderQueue(_colliderCache, results);
            }
        }

        private void UpdateHandStatistics(ContactHand hand)
        {
            if (!_fingerStrengths.ContainsKey(hand))
            {
                _fingerStrengths.Add(hand, new float[5]);
            }
            Leap.Hand lHand = hand.dataHand;
            for (int i = 0; i < 5; i++)
            {
                _fingerStrengths[hand][i] = lHand.GetFingerStrength(i);
            }
        }
        #endregion

        #region Helper Updating
        private void UpdateHelpers()
        {
            ApplyHovers();

            foreach (var helper in _grabHelpers)
            {
                helper.Value.UpdateHelper();
            }
        }

        private void ApplyHovers()
        {
            _grabRigids.RemoveWhere(ValidateUnhoverRigids);

            foreach (var rigid in _hoveredItems)
            {
                _grabRigids.Add(rigid);
            }

            _hoveredItems.Clear();
        }

        private bool ValidateUnhoverRigids(Rigidbody rigid)
        {
            if (!_hoveredItems.Contains(rigid))
            {
                if (_grabHelpers.ContainsKey(rigid))
                {
                    if (_grabHelpers[rigid].GrabState == GrabHelperObject.State.Grab)
                    {
                        // Ensure we release the object first
                        _grabHelpers[rigid].ReleaseObject();
                    }
                    _grabHelpers[rigid].ReleaseHelper();
                    _grabHelpers.Remove(rigid);
                }
                return true;
            }
            return false;
        }
        #endregion

        #region Hand States
        private void UpdateHandStates()
        {
            FindHandState(_leftContactHand);
            FindHandState(_rightContactHand);
        }

        private void FindHandState(ContactHand hand)
        {
            if (hand == null)
            {
                return;
            }

            bool found = false;
            float mass = 0;
            foreach (var item in _grabHelpers)
            {
                if (item.Value.GrabbingHands.Contains(hand))
                {
                    found = true;
                    mass += item.Value.OriginalMass;
                    break;
                }
            }
            if (found != hand.IsGrabbing)
            {
                hand.isGrabbing = found;
            }
            hand.grabMass = mass;
        }
        #endregion

        private void OnValidate()
        {
            if (physicalHandsManager == null)
            {
                physicalHandsManager = GetComponent<PhysicalHandsManager>();
            }
        }
    }
}