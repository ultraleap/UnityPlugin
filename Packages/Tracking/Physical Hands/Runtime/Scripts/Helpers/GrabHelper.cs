/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
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
        private Dictionary<Rigidbody, GrabHelperObject> _grabHelperObjects = new Dictionary<Rigidbody, GrabHelperObject>();

        private HashSet<Rigidbody> _hoveredRigids = new HashSet<Rigidbody>(); // A per-frame cache of hovered rigidbodies to check for adding/removing vs previous frames
        private HashSet<Rigidbody> _previousHoveredRigids = new HashSet<Rigidbody>();

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
            // Clear and repopulate for this frame to compare to the prevous frame
            _hoveredRigids.Clear();

            HandleHandOverlapChecks(_leftContactHand);
            HandleHandOverlapChecks(_rightContactHand);
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
            foreach (var helper in _grabHelperObjects.Values)
            {
                if (helper.GrabState == GrabHelperObject.State.Grab)
                {
                    helper.ReleaseObject();
                }

                helper.ReleaseHelper();
            }

            _grabHelperObjects.Clear();
            _previousHoveredRigids.Clear();
            _hoveredRigids.Clear();

            _fingerStrengths.Clear();
            _colliderCache = new Collider[128];
        }

        #region Hand Updating
        private void UpdateHandHeuristics()
        {
            if (_leftContactHand.tracked)
            {
                UpdateFingerStrengthValues(_leftContactHand);
            }

            if (_rightContactHand.tracked)
            {
                UpdateFingerStrengthValues(_rightContactHand);
            }
        }

        /// <summary>
        /// Used to create helper objects to assist with hover/contact/grabbing detection and fire events
        /// Also trigger collision checks on bones
        /// </summary>
        private void HandleHandOverlapChecks(ContactHand hand)
        {
            if (hand == null)
            {
                return;
            }

            hand.isHovering = false;
            hand.isContacting = false;
            hand.isCloseToObject = false;

            float lerp = float.MaxValue;
            if (_fingerStrengths.ContainsKey(hand))
            {
                // Get the least curled finger excluding the thumb
                var curls = _fingerStrengths[hand];

                for (int i = 1; i < curls.Length; i++)
                {
                    if (curls[i] < lerp)
                    {
                        lerp = curls[i];
                    }
                }
            }

            float radiusAmount = PhysExts.MaxVec3(Vector3.Scale(hand.palmBone.palmCollider.size, PhysExts.AbsVec3(hand.palmBone.transform.lossyScale)));

            // Create a capsule that spans across the length of the hand and is inflated by the hover distance. Detect all interactable colliders it contains
            //      Point0 = at the base of the palm
            //      Point1 = at the tip of the most extended finger
            //      Radius = the size of the palm + the hover distance
            //      Cache the result
            //      Use the interaction mask that the hands use to collide
            int nearbyObjectCount = Physics.OverlapCapsuleNonAlloc(hand.palmBone.transform.position +
                (-hand.palmBone.transform.up * 0.025f) + ((hand.Handedness == Chirality.Left ? hand.palmBone.transform.right : -hand.palmBone.transform.right) * 0.015f),
                hand.palmBone.transform.position + (-hand.palmBone.transform.up * Mathf.Lerp(0.025f, 0.07f, lerp)) + (hand.palmBone.transform.forward * Mathf.Lerp(0.06f, 0.02f, lerp)),
                radiusAmount + physicalHandsManager.HoverDistance,
                _colliderCache,
                physicalHandsManager.InteractionMask);

            RemoveUnhoveredHandsFromGrabHelperObjects(nearbyObjectCount, hand);

            for (int i = 0; i < nearbyObjectCount; i++)
            {
                // Cache this to avoid repeatedly calling native unity functions
                Rigidbody collidersRigid = _colliderCache[i].attachedRigidbody;

                if (collidersRigid != null)
                {
                    _hoveredRigids.Add(collidersRigid);

                    // Find the associated GrabHelperObject, if there isn't one, make one
                    if (!_grabHelperObjects.TryGetValue(collidersRigid, out var helper))
                    {
                        helper = new GrabHelperObject(collidersRigid, this);
                        _grabHelperObjects.Add(collidersRigid, helper);
                    }

                    helper.AddHand(hand);
                }
            }

            hand.palmBone.ProcessColliderQueue(_colliderCache, nearbyObjectCount);
            foreach (var bone in hand.bones)
            {
                bone.ProcessColliderQueue(_colliderCache, nearbyObjectCount);
            }
        }

        private void UpdateFingerStrengthValues(ContactHand hand)
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
            RemoveUnhoveredGrabHelperObjects();

            foreach (var helper in _grabHelperObjects)
            {
                helper.Value.UpdateHelper();
            }
        }

        /// <summary>
        /// Handle any changes to the hover state of nearby rigidbodies
        /// </summary>
        private void RemoveUnhoveredGrabHelperObjects()
        {
            // Remove GrabHelperObjects where they are no longer in range of the hand
            _previousHoveredRigids.RemoveWhere(ValidateUnhoverRigids);

            // Update _previousHoveredRigids with newly hovered rigidbodies, ready for next frame
            foreach (var rigid in _hoveredRigids)
            {
                _previousHoveredRigids.Add(rigid);
            }
        }

        /// <summary>
        /// Handle any changes to the hover state of apeciic hands from nearby rigidbodies
        /// </summary>
        private void RemoveUnhoveredHandsFromGrabHelperObjects(int nearbyObjectCount, ContactHand hand)
        {
            // Loop through the current GrabHelperObejcts
            foreach (var helperObject in _grabHelperObjects.Values)
            {
                // Check if the GrabHelperObject has reference to the hand we should see if we have stopped hovering
                if (helperObject.GrabbableHands.Contains(hand))
                {
                    bool rbodyFound = false;

                    // Loop through all of the nearby RigidBodies to see if it matches the GrabHelperObject's RigidBody
                    for (int i = 0; i < nearbyObjectCount; i++)
                    {
                        // Cache this to avoid repeatedly calling native unity functions
                        Rigidbody collidersRigid = _colliderCache[i].attachedRigidbody;

                        if (collidersRigid != null && collidersRigid == helperObject.RigidBody)
                        {
                            rbodyFound = true;
                            break;
                        }
                    }

                    // We didn't find a match so should remove the hand from the GrabHelperObject to trigger Hover Exit events
                    if (!rbodyFound)
                    {
                        helperObject.RemoveHand(hand);
                    }
                }
            }
        }

        /// <summary>
        /// Remove GrabHelperObjects where they are no longer in range of the hand
        /// Also release/drop any grabbed objects
        /// </summary>
        /// <param name="rigid">The rigidbody to check against</param>
        /// <returns>True if the rigidbody is not hovered</returns>
        private bool ValidateUnhoverRigids(Rigidbody rigid)
        {
            if (!_hoveredRigids.Contains(rigid))
            {
                if (_grabHelperObjects.ContainsKey(rigid))
                {
                    if (_grabHelperObjects[rigid].GrabState == GrabHelperObject.State.Grab)
                    {
                        // Ensure we release the object first
                        _grabHelperObjects[rigid].ReleaseObject();
                    }
                    _grabHelperObjects[rigid].ReleaseHelper();
                    _grabHelperObjects.Remove(rigid);
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

            foreach (var item in _grabHelperObjects)
            {
                if (item.Value.GrabbingHands.Contains(hand))
                {
                    found = true;
                    break;
                }
            }

            if (found != hand.IsGrabbing)
            {
                hand.isGrabbing = found;
            }
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