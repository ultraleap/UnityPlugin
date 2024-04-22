/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    [System.Serializable]
    public class GrabHelperObject
    {
        private const float THOWN_GRAB_COOLDOWNTIME = 0.5f; // How long after a detected throw that grabbing will be ignored (in seconds)
        private const float MINIMUM_STRENGTH = 0.1f, MINIMUM_THUMB_STRENGTH = 0.1f; // Minimum finger strength to consider a grab (finger strength is 0-1 where 1 is fully curled)
        private const float REQUIRED_EXIT_STRENGTH = 0.05f, REQUIRED_THUMB_EXIT_STRENGTH = 0.08f; // Finger strength to consider a grab (finger strength is 0-1 where 1 is fully curled)
        private const float REQUIRED_PINCH_DISTANCE = 0.01f; // Distance required between a finger and the thumb to detect a pinch (in metres)
        private const float GRABBABLE_DIRECTIONS_DOT = -0.2f; // The normalized dot product required to consider one bone facing another while attempting to grab (-1 = directly facing, 1 = directly facing away)

        /// <summary>
        /// The current state of the grasp helper.
        /// </summary>
        internal enum State
        {
            /// <summary>
            /// Idle will only ever be set when the helper is deleted, and is skipped during creation. This is because a helper is created when a hand is hovered.
            /// </summary>
            Idle,
            /// <summary>
            /// Hover will be set when any <b>hand</b> is currently hovering the object.
            /// </summary>
            Hover,
            /// <summary>
            /// Contact will be set when any <b>bone</b> is touching the object.
            /// </summary>
            Contact,
            /// <summary>
            /// Grasp will be set when the <b>object</b> is currently being held or moved.
            /// </summary>
            Grab
        }
        internal State GrabState { get; private set; } = State.Idle;

        // A dictionary of each hand, with an array[5] of lists that represents each bone in each finger that is contacting and capable of grabbing this object
        private Dictionary<ContactHand, List<ContactBone>[]> _grabbableBones = new Dictionary<ContactHand, List<ContactBone>[]>();

        internal List<ContactHand> GrabbableHands => _grabbableHands;
        // Indexes of each list should always match
        private List<ContactHand> _grabbableHands = new List<ContactHand>();
        private List<GrabValues> _grabbableHandsValues = new List<GrabValues>();

        internal List<ContactHand> GrabbingHands => _grabbingHands;
        private List<ContactHand> _grabbingHands = new List<ContactHand>();

        private List<ContactHand> _grabbingHandsPrevious = new List<ContactHand>();

        internal ContactHand currentGrabbingHand;

        [System.Serializable]
        private class GrabValues
        {
            public bool wasContacting = false;
            public bool isContacting = false;

            public float[] fingerStrength = new float[5];
            public float[] originalFingerStrength = new float[5];
            public Vector3 offset;
            public Quaternion originalHandRotationInverse, rotationOffset;

            /// <summary>
            /// True while hand pinching on contact
            /// </summary>
            public bool waitingForInitialUnpinch = false;

            /// <summary>
            /// The hand is grabbing the object
            /// </summary>
            public bool handGrabbing = false;
        }

        private GrabHelper _grabHelperManager;

        internal Rigidbody RigidBody => _rigid;
        private Rigidbody _rigid;
        private Collider[] _colliders;

        private Vector3 _newPosition;
        private Quaternion _newRotation;

        private bool wasKinematic;
        private bool usedGravity;
        private float oldDrag;
        private float oldAngularDrag;

        internal IgnorePhysicalHands _ignorePhysicalHands;

        private float ignoreGrabTime = 0f;

        private bool anyBoneGrabbable = false;

        private bool IsGrabbingIgnored(ContactHand hand)
        {
            if (_ignorePhysicalHands != null && _ignorePhysicalHands.IsGrabbingIgnoredForHand(hand))
            {
                return true;
            }

            return false;
        }

        private bool IsContactIgnored(ContactHand hand)
        {
            if (_ignorePhysicalHands != null && _ignorePhysicalHands.IsCollisionIgnoredForHand(hand))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the provided hand is capable of grabbing an object.
        /// Requires thumb or palm contacting, and at least one (non-thumb) finger
        /// </summary>
        /// <param name="hand">The hand to check</param>
        /// <returns>If the provided hand is capable of grabbing</returns>
        private bool IsHandGrabbable(ContactHand hand)
        {
            if (_grabbableBones.TryGetValue(hand, out var bones))
            {
                // These values are limited by the EligibleBones
                return (bones[0].Count > 0 || bones[5].Count > 0) && // A thumb or palm bone
                    ((bones[1].Count > 0 && bones[1].Any(x => x.Joint != 0)) || // The intermediate or distal of the index
                    (bones[2].Count > 0 && bones[2].Any(x => x.Joint != 0)) || // The intermediate or distal of the middle
                    (bones[3].Count > 0 && bones[3].Any(x => x.Joint != 0)) || // The distal of the ring
                    (bones[4].Count > 0 && bones[4].Any(x => x.Joint == 2))); // The distal of the pinky
            }
            return false;
        }

        /// <summary>
        /// Returns true if the finger on the provided hand is capable of grabbing, and false if not
        /// </summary>
        /// <param name="hand">The hand to check</param>
        /// <param name="finger">The finger to check</param>
        /// <returns>If the finger on the provided hand is capable of grabbing</returns>
        internal bool IsFingerGrabbable(ContactHand hand, int finger)
        {
            if (_grabbableBones.TryGetValue(hand, out var bones))
            {
                switch (finger)
                {
                    case 0: // thumb
                    case 5: // palm
                        return bones[finger].Count > 0; // return true if the thumb or palm are contacting at all
                    case 1:
                    case 2:
                    case 3:
                        return bones[finger].Count > 0 && bones[finger].Any(x => x.Joint != 0); // return true if the fingers (except pinky) intermediat or distal are contacting and capable of grabbing this object
                    case 4:
                        return bones[finger].Count > 0 && bones[finger].Any(x => x.Joint == 2); // return true if the pinky distal is contacting and capable of grabbing this object
                }
            }

            return false;
        }

        internal GrabHelperObject(Rigidbody rigid, GrabHelper manager)
        {
            _rigid = rigid;

            // Doing this prevents objects from misaligning during rotation
            if (_rigid.maxAngularVelocity < 100f)
            {
                _rigid.maxAngularVelocity = 100f;
            }

            _colliders = rigid.GetComponentsInChildren<Collider>(true);

            HandleIgnoreContactHelper();

            _grabHelperManager = manager;

            wasKinematic = _rigid.isKinematic;
            usedGravity = _rigid.useGravity;
            oldDrag = _rigid.drag;
            oldAngularDrag = _rigid.angularDrag;
        }

        private void HandleGrabbedRigidbody()
        {
            if (_rigid != null)
            {
                if (_grabHelperManager.useNonKinematicMovementOnly)
                {
                    _rigid.isKinematic = false;
                }

                _rigid.useGravity = false;
                _rigid.drag = 0f;
                _rigid.angularDrag = 0f;
            }
        }

        private void HandleReleasedRigidbody()
        {
            if (_rigid != null)
            {
                _rigid.isKinematic = wasKinematic;
                _rigid.useGravity = usedGravity;
                _rigid.drag = oldDrag;
                _rigid.angularDrag = oldAngularDrag;
            }
        }

        /// <summary>
        /// Find if the object we are attached to has an IgnoreContactHelper script on it.
        /// If it does, save a reference to it and give it a reference to us so that it can update our ignore values.
        /// </summary>
        private void HandleIgnoreContactHelper()
        {
            if (_ignorePhysicalHands == null)
            {
                _ignorePhysicalHands = _rigid.GetComponent<IgnorePhysicalHands>();
            }

            if (_ignorePhysicalHands != null)
            {
                _ignorePhysicalHands.GrabHelperObject = this;
            }
        }

        internal void ReleaseHelper()
        {
            foreach (var item in _grabbableHands)
            {
                SetBoneGrabbing(item, false);
            }

            GrabState = State.Idle;

            ClearGrabbingHands();

            //// Remove any lingering contact and hover events
            if (_rigid != null)
            {
                for (int i = 0; i < _grabbableHandsValues.Count; i++)
                {
                    if (_grabbableHandsValues[i].isContacting)
                    {
                        if (_rigid.TryGetComponent<IPhysicalHandContact>(out var physicalHandContact))
                        {
                            physicalHandContact.OnHandContactExit(_grabbableHands[i]);
                        }

                        _grabbableHands[i].physicalHandsManager.OnHandContactExit(_grabbableHands[i], _rigid);
                    }

                    if (_rigid.TryGetComponent<IPhysicalHandHover>(out var physicalHandHover))
                    {
                        physicalHandHover.OnHandHoverExit(_grabbableHands[i]);
                    }

                    _grabbableHands[i].physicalHandsManager.OnHandHoverExit(_grabbableHands[i], _rigid);
                }
            }

            _grabbableHandsValues.Clear();
            _grabbableHands.Clear();
            foreach (var pair in _grabbableBones)
            {
                for (int j = 0; j < pair.Value.Length; j++)
                {
                    pair.Value[j].Clear();
                }
            }
            _grabbableBones.Clear();

            _grabbingHands.Clear();
            _grabbingHandsPrevious.Clear();

            HandleReleasedRigidbody();
        }

        internal void AddHand(ContactHand hand)
        {
            if (!_grabbableHands.Contains(hand))
            {
                if (GrabState == State.Idle)
                {
                    GrabState = State.Hover;
                }

                _grabbableHands.Add(hand);
                _grabbableHandsValues.Add(new GrabValues());

                if (_ignorePhysicalHands != null)
                {
                    _ignorePhysicalHands.AddToHands(hand);
                }
            }
        }

        internal void RemoveHand(ContactHand hand)
        {
            if (_grabbableHands.Contains(hand))
            {
                int index = _grabbableHands.IndexOf(hand);

                _grabbableHands.RemoveAt(index);
                _grabbableHandsValues.RemoveAt(index);

                if (_rigid != null && _rigid.TryGetComponent<IPhysicalHandHover>(out var physicalHandHover))
                {
                    physicalHandHover.OnHandHoverExit(hand);
                }

                hand.physicalHandsManager.OnHandHoverExit(hand, _rigid);
            }
        }

        internal void ReleaseObject()
        {
            GrabState = State.Hover;

            HandleReleasedRigidbody();
            ThrowingOnRelease();
        }

        internal State UpdateHelper()
        {
            currentGrabbingHand = null;

            UpdateHands();

            if (!anyBoneGrabbable && GrabState != State.Grab)
            {
                // If we don't then and we're not grabbing then just return
                return GrabState = State.Hover;
            }

            GrabbingCheck();

            switch (GrabState)
            {
                case State.Hover:
                    if (anyBoneGrabbable)
                    {
                        GrabState = State.Contact;
                    }
                    break;
                case State.Grab:
                    UpdateGrabbingValues();

                    if (_grabbingHands.Count > 0)
                    {
                        UpdateHandPositions();

                        if (ignoreGrabTime < Time.time)
                        {
                            TrackThrowingVelocities();
                            MoveObject();
                        }
                    }
                    else
                    {
                        ReleaseObject();
                    }
                    break;
            }

            return GrabState;
        }

        private void UpdateHands()
        {
            // Loop through each hand in our bone array, then the finger, then the bones in that finger
            // If we're no longer in a grabbing state with that bone we want to add it to the cooldowns
            foreach (var pair in _grabbableBones) // Loop through the hands
            {
                for (int i = 0; i < _grabbableBones[pair.Key].Length; i++) // loop through the fingers
                {
                    for (int j = 0; j < _grabbableBones[pair.Key][i].Count; j++) // loop through the bones
                    {
                        if (!_grabbableBones[pair.Key][i][j].GrabbableObjects.Contains(_rigid)) // Remove bones if it has stopped being capable of grabbing this object
                        {
                            _grabbableBones[pair.Key][i].RemoveAt(j);
                            j--;
                        }
                    }
                }
            }

            // Reset this value before use in UpdateGrabbables
            anyBoneGrabbable = false;

            for (int i = 0; i < _grabbableHands.Count; i++)
            {
                // Update references to _grabbableBones
                UpdateGrabbableBones(i);

                // Update the hand contact and hover events
                UpdateContactAndHoverEvents(i);
            }

            UpdateGrabEvents();
        }

        private void UpdateGrabbableBones(int handID)
        {
            _grabbableHandsValues[handID].wasContacting = _grabbableHandsValues[handID].isContacting;
            _grabbableHandsValues[handID].isContacting = false;

            foreach (var bone in _grabbableHands[handID].bones)
            {
                if (bone.GrabbableObjects.Contains(_rigid))
                {
                    anyBoneGrabbable = true;

                    _grabbableHandsValues[handID].isContacting = true;

                    if (_grabbableBones.TryGetValue(bone.contactHand, out List<ContactBone>[] storedBones))
                    {
                        storedBones[bone.Finger].Add(bone);
                    }
                    else
                    {
                        _grabbableBones.Add(bone.contactHand, new List<ContactBone>[ContactHand.FINGERS + 1]);

                        for (int j = 0; j < _grabbableBones[bone.contactHand].Length; j++)
                        {
                            _grabbableBones[bone.contactHand][j] = new List<ContactBone>();
                            if (bone.Finger == j)
                            {
                                _grabbableBones[bone.contactHand][j].Add(bone);
                            }
                        }
                    }
                }
            }
        }

        void UpdateContactAndHoverEvents(int handIndex)
        {
            // Fire the contacting event whether it changed or not
            if (_grabbableHandsValues[handIndex].isContacting)
            {
                if (_rigid.TryGetComponent<IPhysicalHandContact>(out var handContactEvent))
                {
                    handContactEvent.OnHandContact(_grabbableHands[handIndex]);
                }

                _grabbableHands[handIndex].physicalHandsManager.OnHandContact(_grabbableHands[handIndex], _rigid);
            }
            else if (_grabbableHandsValues[handIndex].wasContacting)
            {
                // We stopped contacting, so fire the contact exit event
                if (_rigid.TryGetComponent<IPhysicalHandContact>(out var handContactEvent))
                {
                    handContactEvent.OnHandContactExit(_grabbableHands[handIndex]);
                }

                _grabbableHands[handIndex].physicalHandsManager.OnHandContactExit(_grabbableHands[handIndex], _rigid);
            }

            // Fire the hovering event
            if (_rigid.TryGetComponent<IPhysicalHandHover>(out var physicalHandHover))
            {
                physicalHandHover.OnHandHover(_grabbableHands[handIndex]);
            }

            _grabbableHands[handIndex].physicalHandsManager.OnHandHover(_grabbableHands[handIndex], _rigid);
        }

        void UpdateGrabEvents()
        {
            // Send any active grabbing events
            foreach (var hand in _grabbingHands)
            {
                if (_rigid.TryGetComponent<IPhysicalHandGrab>(out var physicalHandGrab))
                {
                    physicalHandGrab.OnHandGrab(hand);
                }

                hand.physicalHandsManager.OnHandGrab(hand, _rigid);
            }

            // Now look for if any grabs have been released
            foreach (var prev in _grabbingHandsPrevious)
            {
                if (!_grabbingHands.Contains(prev))
                {
                    if (_rigid.TryGetComponent<IPhysicalHandGrab>(out var physicalHandGrab))
                    {
                        physicalHandGrab.OnHandGrabExit(prev);
                    }

                    prev.physicalHandsManager.OnHandGrabExit(prev, _rigid);
                }
            }

            _grabbingHandsPrevious.Clear();
            _grabbingHandsPrevious.AddRange(_grabbingHands);
        }

        /// <summary>
        /// First check if a standard distance-based grab is occuring
        /// Then check if bones are contacting the object and facing eachother for an advanced/gentle grab detection
        ///     (Thumb or palm facing any other finger bone)
        /// </summary>
        private void GrabbingCheck()
        {
            //Reset grab bools
            foreach (var grabValue in _grabbableHandsValues)
            {
                grabValue.handGrabbing = false;
            }

            for (int handIndex = 0; handIndex < _grabbableHands.Count; handIndex++)
            {
                ContactHand hand = _grabbableHands[handIndex];
                GrabValues grabValues = _grabbableHandsValues[handIndex];

                if (_grabbingHands.Contains(hand) || IsGrabbingIgnored(hand))
                {
                    continue;
                }

                for (int i = 0; i < 5; i++)
                {
                    if (IsFingerGrabbable(hand, i) && _grabHelperManager.FingerStrengths[hand][i] > (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH))
                    {
                        grabValues.fingerStrength[i] = _grabHelperManager.FingerStrengths[hand][i];
                        if (grabValues.originalFingerStrength[i] == -1)
                        {
                            grabValues.originalFingerStrength[i] = _grabHelperManager.FingerStrengths[hand][i];
                        }
                    }
                    else
                    {
                        if (hand.dataHand.GetFingerPinchDistance(i) <= REQUIRED_PINCH_DISTANCE)
                        {
                            grabValues.waitingForInitialUnpinch = true;
                        }
                        grabValues.originalFingerStrength[i] = -1;
                        grabValues.fingerStrength[i] = -1;
                    }
                }

                if (IsHandGrabbable(hand))
                {
                    // Check for how many fingers are pinching
                    int pinchingFingers = 0;
                    for (int i = 0; i < 5; i++)
                    {
                        if (grabValues.fingerStrength[i] == -1)
                        {
                            continue;
                        }

                        if (i == 0) // check the thumb differently
                        {
                            bool unpinched = true;
                            for (int j = 1; j < 5; j++)
                            {
                                //dont allow if pinching on enter
                                if (hand.dataHand.GetFingerPinchDistance(j) <= REQUIRED_PINCH_DISTANCE)
                                {
                                    if (grabValues.waitingForInitialUnpinch)
                                    {
                                        unpinched = false;
                                        continue;
                                    }

                                    // Make very small pinches more sticky
                                    grabValues.fingerStrength[j] *= 0.85f;
                                    if (pinchingFingers == 0)
                                    {
                                        pinchingFingers = 2; // include the thumb
                                        grabValues.fingerStrength[0] *= 0.85f;
                                    }
                                    else
                                    {
                                        pinchingFingers++;
                                    }
                                }
                            }

                            if (unpinched)
                            {
                                grabValues.waitingForInitialUnpinch = false;
                            }

                            if (pinchingFingers > 0 || grabValues.waitingForInitialUnpinch)
                            {
                                break;
                            }
                        }

                        // if waiting for ungrab AND no contact 
                        // ungrab == required ungrab strength 2
                        if (grabValues.fingerStrength[i] > grabValues.originalFingerStrength[i] + ((1 - grabValues.originalFingerStrength[i]) * MINIMUM_STRENGTH))
                        {
                            pinchingFingers++;
                        }
                    }

                    if (pinchingFingers >= 2)
                    {
                        grabValues.handGrabbing = true;
                        RegisterGrabbingHand(hand);
                    }
                }
            }

            CheckForBonesFacingEachOther();
        }

        private void CheckForBonesFacingEachOther()
        {
            // When hands are not Physical, we should skip this step to avoid unwanted grabs
            foreach (var hand in _grabbableHands)
            {
                if (!hand.isHandPhysical)
                {
                    return;
                }
            }

            for (int handIndex = 0; handIndex < _grabbableHands.Count; handIndex++)
            {
                // If grabbing or contact is ignored for this object,
                // we should not use per-bone checks for grabbing
                if (IsGrabbingIgnored(_grabbableHands[handIndex]) ||
                    IsContactIgnored(_grabbableHands[handIndex]))
                {
                    continue;
                }

                // This hand is already grabbing, we don't need to check again
                if (_grabbableHandsValues[handIndex].handGrabbing ||
                    _grabbingHands.Contains(_grabbableHands[handIndex]))
                {
                    continue;
                }

                int bone1Index = 0;

                foreach (ContactBone bone1 in _grabbableHands[handIndex].bones)
                {
                    if (bone1.GrabbableDirections.TryGetValue(_rigid, out var grabbableDirectionsB1))
                    {
                        int bone2Index = 0;

                        foreach (ContactBone bone2 in _grabbableHands[handIndex].bones)
                        {
                            if (bone2Index < bone1Index) // Avoid double-checking the same index combinations
                            {
                                bone2Index++;
                                continue;
                            }

                            // Don't compare against the same finger
                            if (bone1.finger == bone2.finger) continue;

                            if (bone2.GrabbableDirections.TryGetValue(_rigid, out var grabbableDirectionsB2))
                            {
                                foreach (var directionPairB1 in grabbableDirectionsB1)
                                {
                                    foreach (var directionPairB2 in grabbableDirectionsB2)
                                    {
                                        float dot = Vector3.Dot(directionPairB1.Value.direction, directionPairB2.Value.direction);

                                        if (dot >= GRABBABLE_DIRECTIONS_DOT) // As a backup, try bone directions rather than directions towards the object center
                                        {
                                            dot = Vector3.Dot(-bone1.transform.up, -bone2.transform.up);
                                        }

                                        //If the two bones are facing opposite directions (i.e. pushing towards each other), they're grabbing
                                        if (dot < GRABBABLE_DIRECTIONS_DOT)
                                        {
                                            if (bone1.contactHand == bone2.contactHand)
                                            {
                                                _grabbableHandsValues[handIndex].handGrabbing = true;
                                                RegisterGrabbingHand(bone1.contactHand);
                                            }

                                            return;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    bone1Index++;
                }
            }
        }

        private void RegisterGrabbingHand(ContactHand hand)
        {
            if (ignoreGrabTime > Time.time)
                return;

            if (hand.ghosted)
                return;

            if (IsGrabbingIgnored(hand))
                return;

            if (_grabbingHands.Contains(hand))
                return;

            _grabbingHands.Add(hand);

            GrabState = State.Grab;

            int grabHandIndex = _grabbableHands.IndexOf(hand);

            _grabbableHandsValues[grabHandIndex].offset = _rigid.position - hand.palmBone.transform.position;
            _grabbableHandsValues[grabHandIndex].rotationOffset = Quaternion.Inverse(hand.palmBone.transform.rotation) * _rigid.rotation;
            _grabbableHandsValues[grabHandIndex].originalHandRotationInverse = Quaternion.Inverse(hand.palmBone.transform.rotation);

            HandleGrabbedRigidbody();
        }

        private void ClearGrabbingHands()
        {
            for (int i = _grabbingHands.Count - 1; i >= 0; i--)
            {
                UnregisterGrabbingHand(_grabbingHands[i]);
            }

            _grabbingHands.Clear();
            _grabbingHandsPrevious.Clear();
        }

        internal void UnregisterGrabbingHand(Chirality handChirality)
        {
            foreach (var grabHand in _grabbingHands)
            {
                if (grabHand?.Handedness == handChirality)
                {
                    UnregisterGrabbingHand(grabHand);
                    return;
                }
            }
        }

        private void UnregisterGrabbingHand(ContactHand hand)
        {
            if (hand == null)
            {
                return;
            }

            if (_rigid != null && _rigid.TryGetComponent<IPhysicalHandGrab>(out var physicalHandGrab))
            {
                physicalHandGrab.OnHandGrabExit(hand);
            }

            hand.physicalHandsManager.OnHandGrabExit(hand, _rigid);

            _grabbingHands.Remove(hand);
        }

        private void UpdateGrabbingValues()
        {
            for (int grabHandIndex = 0; grabHandIndex < _grabbableHands.Count; grabHandIndex++)
            {
                // Check if this hand was grabbing and is now not grabbing
                if (_grabbingHands.Count > 0
                    && !_grabbableHandsValues[grabHandIndex].handGrabbing
                    // Hand has moved significantly far away from the object
                    && (_rigid.position - _grabbableHands[grabHandIndex].palmBone.transform.position).sqrMagnitude > _grabbableHandsValues[grabHandIndex].offset.sqrMagnitude * 1.5f)
                {
                    if (_grabbableHands[grabHandIndex].isGrabbing)
                    {
                        SetBoneGrabbing(_grabbableHands[grabHandIndex], false);
                        _grabbingHands.Remove(_grabbableHands[grabHandIndex]);
                    }
                    continue;
                }

                int curledFingerCount = 0;
                for (int i = 0; i < 5; i++)
                {
                    // Was the finger not contacting before?
                    if (_grabbableHandsValues[grabHandIndex].fingerStrength[i] == -1)
                    {
                        if (_grabHelperManager.FingerStrengths[_grabbableHands[grabHandIndex]][i] > (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) && IsFingerGrabbable(_grabbableHands[grabHandIndex], i))
                        {
                            // Store the strength value on contact
                            _grabbableHandsValues[grabHandIndex].fingerStrength[i] = _grabHelperManager.FingerStrengths[_grabbableHands[grabHandIndex]][i];
                        }
                    }
                    else
                    {
                        // If the finger was contacting but has uncurled by the exit percentage then it is no longer "grabbed"
                        if (_grabHelperManager.FingerStrengths[_grabbableHands[grabHandIndex]][i] < (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) ||
                            _grabbableHandsValues[grabHandIndex].fingerStrength[i] * (1 - (i == 0 ? REQUIRED_THUMB_EXIT_STRENGTH : REQUIRED_EXIT_STRENGTH)) >= _grabHelperManager.FingerStrengths[_grabbableHands[grabHandIndex]][i])
                        {
                            _grabbableHandsValues[grabHandIndex].fingerStrength[i] = -1;
                        }
                    }

                    if (_grabbableHandsValues[grabHandIndex].fingerStrength[i] != -1)
                    {
                        curledFingerCount++;
                    }
                }

                // If we've got two fingers curled, the hand was grabbing in the grab contact checks, or the hand is facing the other, then we grab
                if (curledFingerCount >= 2 || _grabbableHandsValues[grabHandIndex].handGrabbing)
                {
                    SetBoneGrabbing(_grabbableHands[grabHandIndex], true);
                    continue;
                }

                // One final check to see if the original data hand has bones inside of the object to retain grabbing during physics updates
                bool data = DataHandIntersection(_grabbableHands[grabHandIndex]);

                if (!data)
                {
                    SetBoneGrabbing(_grabbableHands[grabHandIndex], false);
                    _grabbableHandsValues[grabHandIndex].waitingForInitialUnpinch = true;
                    _grabbingHands.Remove(_grabbableHands[grabHandIndex]);
                }
                else
                {
                    SetBoneGrabbing(_grabbableHands[grabHandIndex], true);
                }
            }
        }

        private void SetBoneGrabbing(ContactHand hand, bool add)
        {
            if (add)
            {
                for (int j = 0; j < _grabbableBones[hand].Length; j++)
                {
                    if (_grabbableBones[hand][j].Count > 0)
                    {
                        foreach (var bone in hand.bones)
                        {
                            // Using only the first element of a foreach is cheaper than using _bones[pair.Key][j].First for GC.Alloc
                            foreach (var b in _grabbableBones[hand][j])
                            {
                                if (bone.Finger == b.finger)
                                {
                                    bone.AddGrabbing(_rigid);
                                }
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var item in hand.bones)
                {
                    item.RemoveGrabbing(_rigid);
                }
            }
        }

        private bool DataHandIntersection(ContactHand hand)
        {
            if (!hand.isHandPhysical)
                return false;

            bool thumb = false, otherFinger = false;
            bool earlyQuit;

            Leap.Hand lHand = hand.dataHand;

            if (lHand == null)
            {
                return false;
            }

            if (!_grabbableBones.ContainsKey(hand))
            {
                return false;
            }

            for (int i = 0; i < _grabbableBones[hand].Length; i++)
            {
                foreach (var bone in _grabbableBones[hand][i])
                {
                    earlyQuit = false;
                    switch (bone.Finger)
                    {
                        case 0: // thumb
                            if (thumb)
                                continue;

                            if (lHand.Fingers[bone.Finger].bones[bone.Joint].IsBoneWithinObject(_colliders))
                            {
                                thumb = true;
                                earlyQuit = true;
                            }
                            break;
                        case 1: // index
                            if (lHand.Fingers[bone.Finger].bones[bone.Joint].IsBoneWithinObject(_colliders))
                            {
                                otherFinger = true;
                                earlyQuit = true;
                            }
                            break;
                        case 2: // middle
                        case 3: // ring
                        case 4: // pinky
                            if (bone.Joint == 2 && lHand.Fingers[bone.Finger].bones[bone.Joint].IsBoneWithinObject(_colliders))
                            {
                                otherFinger = true;
                                earlyQuit = true;
                            }
                            break;
                    }
                    // If we've got we need then we can just return now
                    if (thumb && otherFinger)
                    {
                        return true;
                    }
                    if (earlyQuit)
                    {
                        break;
                    }
                }
            }
            return thumb && otherFinger;
        }

        private void UpdateHandPositions()
        {
            ContactHand hand = null;

            if (_grabbingHands.Count > 0)
            {
                int grabHandndex = -1;

                // Find the newest grabbing hand
                for (int i = _grabbingHands.Count - 1; i >= 0; i--)
                {
                    hand = _grabbingHands[i];
                    grabHandndex = _grabbableHands.IndexOf(hand);

                    if (hand != null && grabHandndex != -1)
                    {
                        break;
                    }
                    else
                    {
                        hand = null;
                    }
                }

                if (hand != null)
                {
                    _newRotation = hand.palmBone.transform.rotation * Quaternion.Euler(hand.AngularVelocity * Time.fixedDeltaTime); // for use in positioning

                    _newPosition = hand.palmBone.transform.position + (hand.Velocity * Time.fixedDeltaTime) + (_newRotation * _grabbableHandsValues[grabHandndex].originalHandRotationInverse * _grabbableHandsValues[grabHandndex].offset);
                    _newRotation = _newRotation * _grabbableHandsValues[grabHandndex].rotationOffset; // Include the original rotation offset
                }
            }

            currentGrabbingHand = hand;
        }

        private void MoveObject()
        {
            if (_rigid.isKinematic)
            {
                KinematicMovement(_newPosition, _newRotation);
            }
            else
            {
                PhysicsMovement(_newPosition, _newRotation);
            }
        }

        private const float MAX_VELOCITY_SQUARED = 100;

        private void PhysicsMovement(Vector3 solvedPosition, Quaternion solvedRotation)
        {
            Vector3 solvedCenterOfMass = solvedRotation * _rigid.centerOfMass + solvedPosition;
            Vector3 currCenterOfMass = _rigid.rotation * _rigid.centerOfMass + _rigid.position;

            Vector3 targetVelocity = ContactUtils.ToLinearVelocity(currCenterOfMass, solvedCenterOfMass, Time.fixedDeltaTime);
            Vector3 targetAngularVelocity = ContactUtils.ToAngularVelocity(_rigid.rotation, solvedRotation, Time.fixedDeltaTime);

            // Clamp targetVelocity by MAX_VELOCITY_SQUARED.
            float targetSpeedSqrd = targetVelocity.sqrMagnitude;
            if (targetSpeedSqrd > MAX_VELOCITY_SQUARED)
            {
                float targetPercent = MAX_VELOCITY_SQUARED / targetSpeedSqrd;
                targetVelocity *= targetPercent;
            }

            _rigid.velocity = targetVelocity;
            if (targetAngularVelocity.IsValid())
            {
                _rigid.angularVelocity = targetAngularVelocity;
            }
        }

        private void KinematicMovement(Vector3 solvedPosition, Quaternion solvedRotation)
        {
            _rigid.MovePosition(solvedPosition);
            _rigid.MoveRotation(solvedRotation);
        }

        #region Throwing

        // the time to capture velocities to be applied to objects when throwing
        private const float VELOCITY_HISTORY_LENGTH = 0.045f;

        private Queue<VelocitySample> _velocityQueue = new Queue<VelocitySample>();

        private struct VelocitySample
        {
            public float removeTime;
            public Vector3 velocity;

            public VelocitySample(Vector3 velocity, float time)
            {
                this.velocity = velocity;
                this.removeTime = time;
            }
        }

        private void TrackThrowingVelocities()
        {
            _velocityQueue.Enqueue(new VelocitySample(_rigid.velocity,
                                                      Time.time + VELOCITY_HISTORY_LENGTH));

            while (true)
            {
                VelocitySample oldestVelocity = _velocityQueue.Peek();

                // Dequeue conservatively
                if (Time.time > oldestVelocity.removeTime)
                {
                    _velocityQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }

        private void ThrowingOnRelease()
        {
            if (_rigid == null)
            {
                return;
            }

            // You can't throw kinematic objects
            if (_rigid.isKinematic)
            {
                _velocityQueue.Clear();
                return;
            }

            Vector3 averageVelocity = Vector3.zero;

            int velocityCount = 0;

            while (_velocityQueue.Count > 0)
            {
                VelocitySample oldestVelocity = _velocityQueue.Dequeue();

                averageVelocity += oldestVelocity.velocity;
                velocityCount++;
            }

            // average the frames to get the average positional change
            averageVelocity /= velocityCount;

            if (averageVelocity.magnitude > 0.5f)
            {
                // Ignore collision after throwing so we don't knock the object
                foreach (var hand in _grabbableHands)
                {
                    if (!IsContactIgnored(hand)) // Ensure we don't overwrite existing ignore settings
                        hand.IgnoreCollision(_rigid, _colliders, 0.1f, 0.01f);
                }

                // Ignore Grabbing after throwing
                ignoreGrabTime = Time.time + THOWN_GRAB_COOLDOWNTIME;

                // Set the new velocty. Allow physics to solve for rotational change
                _rigid.velocity = averageVelocity;
            }
            else
            {
                _rigid.velocity = Vector3.zero;
                _rigid.angularVelocity = Vector3.zero;
            }
        }

        #endregion
    }
}