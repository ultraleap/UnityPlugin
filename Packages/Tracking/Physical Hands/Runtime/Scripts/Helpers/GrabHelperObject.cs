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

        // A dictionary of each hand, with an array[5] of lists that represents each bone in each finger
        private Dictionary<ContactHand, List<ContactBone>[]> _grabbableBones = new Dictionary<ContactHand, List<ContactBone>[]>();

        // Indexes of each list should always match
        private List<ContactHand> _grabbableHands = new List<ContactHand>();
        private List<bool> _grabbableHandsContacting = new List<bool>();
        private List<GrabValues> _grabbableHandsValues = new List<GrabValues>();

        internal List<ContactHand> GrabbingHands => _grabbingHands;
        private List<ContactHand> _grabbingHands = new List<ContactHand>();

        [System.Serializable]
        private class GrabValues
        {
            public float[] fingerStrength = new float[5];
            public float[] originalFingerStrength = new float[5];
            public Vector3 offset;
            public Quaternion originalHandRotation, rotationOffset;

            /// <summary>
            /// True while hand pinching on contact
            /// </summary>
            public bool waitingForInitialUnpinch = false;

            /// <summary>
            /// The hand is grabbing the object
            /// </summary>
            public bool handGrabbing = false;
            /// <summary>
            /// the bones in this hand are facing other grabbing applicable bones in a different hand 
            /// </summary>
            public bool facingOppositeHand = false;
        }

        private GrabHelper _manager;

        private Rigidbody _rigid;
        private Collider[] _colliders;

        private Vector3 _newPosition;
        private Quaternion _newRotation;

        private bool _oldKinematic;
        private float _originalMass = 1f;
        internal float OriginalMass => _originalMass;

        internal bool _grabbingIgnored;
        private IgnorePhysicalHands _ignorePhysicalHands;

        private float ignoreGrabTime = 0f;

        private bool anyBoneGrabbable = false;

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
                        return bones[finger].Count > 0;

                    case 1:
                    case 2:
                    case 3:
                        return bones[finger].Count > 0 && bones[finger].Any(x => x.Joint != 0);

                    case 4:
                        return bones[finger].Count > 0 && bones[finger].Any(x => x.Joint == 2);
                }
                return false;
            }
            return false;
        }

        internal GrabHelperObject(Rigidbody rigid, GrabHelper manager)
        {
            _rigid = rigid;
            _originalMass = _rigid.mass;
            // Doing this prevents objects from misaligning during rotation
            if (_rigid.maxAngularVelocity < 100f)
            {
                _rigid.maxAngularVelocity = 100f;
            }
            _colliders = rigid.GetComponentsInChildren<Collider>(true);
            HandleIgnoreContactHelper();
            _manager = manager;
        }


        /// <summary>
        /// Find if the object we are attached to has an IgnoreContactHelper script on it.
        /// If it does, save a reference to it and give it a reference to us so that it can update our ignore values.
        /// </summary>
        private void HandleIgnoreContactHelper()
        {
            _ignorePhysicalHands = _rigid.GetComponentInChildren<IgnorePhysicalHands>();

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

            //// Remove any lingering contact events
            //// Remove any lingering hover events
            if (_rigid != null)
            {
                for (int i = 0; i < _grabbableHandsContacting.Count; i++)
                {
                    if (_grabbableHandsContacting[i])
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
            _grabbableHandsContacting.Clear();
            foreach (var pair in _grabbableBones)
            {
                for (int j = 0; j < pair.Value.Length; j++)
                {
                    pair.Value[j].Clear();
                }
            }
            _grabbableBones.Clear();
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
                _grabbableHandsContacting.Add(false);
                _grabbableHandsValues.Add(new GrabValues());

                if (_ignorePhysicalHands)
                {
                    _ignorePhysicalHands.AddToHands(hand);
                }
            }
        }

        internal void RemoveHand(ContactHand hand)
        {
            if (_grabbableHands.Contains(hand))
            {
                _grabbableHandsContacting.RemoveAt(_grabbableHands.IndexOf(hand));
                _grabbableHands.Remove(hand);
            }
        }

        internal void ReleaseObject()
        {
            GrabState = State.Hover;
            // Make sure the object hasn't been destroyed
            if (_rigid != null)
            {
                if (!_grabbingIgnored)
                {
                    // Only ever unset the rigidbody values here otherwise outside logic will get confused
                    _rigid.isKinematic = _oldKinematic;
                    ThrowingOnRelease();
                }
            }
        }

        internal State UpdateHelper()
        {
            UpdateHands();

            if (!anyBoneGrabbable && GrabState != State.Grab)
            {
                // If we don't then and we're not grabbing then just return
                return GrabState = State.Hover;
            }

            if (!_grabbingIgnored)
            {
                GrabbingCheck();
            }

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
                        if (!_grabbingIgnored && ignoreGrabTime < Time.time)
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
            foreach (var pair in _grabbableBones)
            {
                for (int i = 0; i < _grabbableBones[pair.Key].Length; i++)
                {
                    for (int j = 0; j < _grabbableBones[pair.Key][i].Count; j++)
                    {
                        if (!_grabbableBones[pair.Key][i][j].GrabbableObjects.Contains(_rigid))
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
                UpdateGrabbableBones(_grabbableHands[i]);

                // Update the hand contact and hover events
                UpdateContactHoverEvents(i);
            }
        }

        private void UpdateGrabbableBones(ContactHand hand)
        {
            foreach (var bone in hand.bones)
            {
                if (bone.GrabbableObjects.Contains(_rigid))
                {
                    anyBoneGrabbable = true;
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

        void UpdateContactHoverEvents(int handIndex)
        {
            IPhysicalHandContact handContactEvent = null;

            if (_grabbableHandsContacting[handIndex] != _grabbableHands[handIndex].IsContacting)
            {
                _grabbableHandsContacting[handIndex] = _grabbableHands[handIndex].IsContacting;

                // We stopped contacting, so fire the contact exit event
                if (!_grabbableHands[handIndex].IsContacting)
                {
                    if (_rigid.TryGetComponent<IPhysicalHandContact>(out handContactEvent))
                    {
                        handContactEvent.OnHandContactExit(_grabbableHands[handIndex]);
                    }

                    _grabbableHands[handIndex].physicalHandsManager.OnHandContactExit(_grabbableHands[handIndex], _rigid);
                }
            }

            // Fire the contacting event whether it changed or not
            if (_grabbableHands[handIndex].IsContacting)
            {
                if (handContactEvent != null || _rigid.TryGetComponent<IPhysicalHandContact>(out handContactEvent))
                {
                    handContactEvent.OnHandContact(_grabbableHands[handIndex]);
                }

                _grabbableHands[handIndex].physicalHandsManager.OnHandContact(_grabbableHands[handIndex], _rigid);
            }

            // Fire the hovering event
            if (_rigid.TryGetComponent<IPhysicalHandHover>(out var physicalHandHover))
            {
                physicalHandHover.OnHandHover(_grabbableHands[handIndex]);
            }

            _grabbableHands[handIndex].physicalHandsManager.OnHandHover(_grabbableHands[handIndex], _rigid);
        }

        private void GrabbingCheck()
        {
            //Reset grab bools
            foreach (var grabValue in _grabbableHandsValues)
            {
                grabValue.handGrabbing = false;
                grabValue.facingOppositeHand = false;
            }

            for (int handIndex = 0; handIndex < _grabbableHands.Count; handIndex++)
            {
                ContactHand hand = _grabbableHands[handIndex];
                GrabValues grabValues = _grabbableHandsValues[handIndex];

                if (_grabbingHands.Contains(hand))
                {
                    continue;
                }

                for (int i = 0; i < 5; i++)
                {
                    if (IsFingerGrabbable(hand, i) && _manager.FingerStrengths[hand][i] > (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH))
                    {
                        grabValues.fingerStrength[i] = _manager.FingerStrengths[hand][i];
                        if (grabValues.originalFingerStrength[i] == -1)
                        {
                            grabValues.originalFingerStrength[i] = _manager.FingerStrengths[hand][i];
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

            int bone1Index = 0;

            foreach (ContactHand hand in _grabbableHands)
            {
                foreach (ContactBone bone1 in hand.bones)
                {
                    if (bone1.GrabbableDirections.TryGetValue(_rigid, out var grabbableDirectionsB1))
                    {
                        int bone2Index = 0;

                        int grabHandIndex1 = _grabbableHands.IndexOf(bone1.contactHand);

                        foreach (ContactBone bone2 in hand.bones)
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
                                int grabHandIndex2 = _grabbableHands.IndexOf(bone2.contactHand);

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
                                                _grabbableHandsValues[grabHandIndex1].handGrabbing = true;
                                                _grabbableHandsValues[grabHandIndex2].handGrabbing = true;

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

            if (_grabbingHands.Contains(hand))
                return;

            if (!_grabbingIgnored)
            {
                // Store the original rigidbody variables
                _oldKinematic = _rigid.isKinematic;
                _rigid.isKinematic = false;
            }

            _grabbingHands.Add(hand);

            GrabState = State.Grab;

            int grabHandIndex = _grabbableHands.IndexOf(hand);

            _grabbableHandsValues[grabHandIndex].offset = _rigid.position - hand.palmBone.transform.position;
            _grabbableHandsValues[grabHandIndex].rotationOffset = Quaternion.Inverse(hand.palmBone.transform.rotation) * _rigid.rotation;
            _grabbableHandsValues[grabHandIndex].originalHandRotation = hand.palmBone.transform.rotation;

            if (_rigid.TryGetComponent<IPhysicalHandGrab>(out var physicalHandGrab))
            {
                physicalHandGrab.OnHandGrab(hand);
            }

            hand.physicalHandsManager.OnHandGrab(hand, _rigid);
        }

        internal void SendGrabExitEvents(ContactHand hand)
        {
            if(GrabState != State.Grab)
            {
                return;
            }

            if (_rigid.TryGetComponent<IPhysicalHandGrab>(out var physicalHandGrab))
            {
                physicalHandGrab.OnHandGrabExit(hand);
            }

            hand.physicalHandsManager.OnHandGrabExit(hand, _rigid);
        }

        private void UpdateGrabbingValues()
        {
            for(int grabHandIndex = 0; grabHandIndex < _grabbableHands.Count; grabHandIndex++)
            {
                // Check if this hand was grabbing and is now not grabbing
                if (_grabbingHands.Count > 0
                    && !_grabbableHandsValues[grabHandIndex].handGrabbing
                    && !_grabbableHandsValues[grabHandIndex].facingOppositeHand
                    // Hand has moved significantly far away from the object
                    && (_rigid.position - _grabbableHands[grabHandIndex].palmBone.transform.position).sqrMagnitude > _grabbableHandsValues[grabHandIndex].offset.sqrMagnitude * 1.5f)
                {
                    SetBoneGrabbing(_grabbableHands[grabHandIndex], false);
                    _grabbingHands.Remove(_grabbableHands[grabHandIndex]);
                    SendGrabExitEvents(_grabbableHands[grabHandIndex]);
                    continue;
                }

                int curledFingerCount = 0;
                for (int i = 0; i < 5; i++)
                {
                    // Was the finger not contacting before?
                    if (_grabbableHandsValues[grabHandIndex].fingerStrength[i] == -1)
                    {
                        if (_manager.FingerStrengths[_grabbableHands[grabHandIndex]][i] > (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) && IsFingerGrabbable(_grabbableHands[grabHandIndex], i))
                        {
                            // Store the strength value on contact
                            _grabbableHandsValues[grabHandIndex].fingerStrength[i] = _manager.FingerStrengths[_grabbableHands[grabHandIndex]][i];
                        }
                    }
                    else
                    {
                        // If the finger was contacting but has uncurled by the exit percentage then it is no longer "grabbed"
                        if (_manager.FingerStrengths[_grabbableHands[grabHandIndex]][i] < (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) || 
                            _grabbableHandsValues[grabHandIndex].fingerStrength[i] * (1 - (i == 0 ? REQUIRED_THUMB_EXIT_STRENGTH : REQUIRED_EXIT_STRENGTH)) >= _manager.FingerStrengths[_grabbableHands[grabHandIndex]][i])
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
                if (curledFingerCount >= 2 || _grabbableHandsValues[grabHandIndex].handGrabbing || _grabbableHandsValues[grabHandIndex].facingOppositeHand)
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

                    if (_rigid.TryGetComponent<IPhysicalHandGrab>(out var physicalHandGrab))
                    {
                        physicalHandGrab.OnHandGrabExit(_grabbableHands[grabHandIndex]);
                    }

                    _grabbableHands[grabHandIndex].physicalHandsManager.OnHandGrabExit(_grabbableHands[grabHandIndex], _rigid);
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

                if (_rigid.TryGetComponent<IPhysicalHandGrab>(out var physicalHandGrab))
                {
                    physicalHandGrab.OnHandGrab(hand);
                }

                hand.physicalHandsManager.OnHandGrab(hand, _rigid);
            }
            else
            {
                foreach (var item in hand.bones)
                {
                    item.RemoveGrabbing(_rigid);
                }
            }
        }

        //TODO try the bone within object checks WITH width passed in
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
            if (_grabbingHands.Count > 0)
            {
                /*
                 * Grab priority
                 * Last hand to grab object
                 * Otherwise, last hand to be added to grabbingHands
                 */

                ContactHand hand = null;
                int grabHandndex = -1;

                for (int i = _grabbingHands.Count - 1; i > 0; i--)
                {
                    grabHandndex = _grabbableHands.IndexOf(_grabbingHands[i]);

                    if (_grabbableHandsValues[grabHandndex].handGrabbing || _grabbableHandsValues[grabHandndex].facingOppositeHand
                        && _grabbingHands[i].tracked)
                    {
                        hand = _grabbingHands[i];
                        break;
                    }
                }

                if (hand == null && _grabbingHands[_grabbingHands.Count - 1].tracked)
                {
                    hand = _grabbingHands[_grabbingHands.Count - 1];
                    grabHandndex = _grabbableHands.IndexOf(hand);
                }

                if (hand != null)
                {
                    _newPosition = hand.palmBone.transform.position + (hand.Velocity * Time.fixedDeltaTime) + (hand.palmBone.transform.rotation * Quaternion.Inverse(_grabbableHandsValues[grabHandndex].originalHandRotation) * _grabbableHandsValues[grabHandndex].offset);
                    _newRotation = hand.palmBone.transform.rotation * Quaternion.Euler(hand.AngularVelocity * Time.fixedDeltaTime) * _grabbableHandsValues[grabHandndex].rotationOffset;
                }
            }
        }

        private void MoveObject()
        {
            // Fixes issues with kinematic objects
            // Making them affected by physics instead of kinematic is much more stable
            if (_rigid.isKinematic)
            {
                _rigid.isKinematic = false;
            }
            PhysicsMovement(_newPosition, _newRotation, _rigid);
        }

        // Ripped from IE
        protected float _maxVelocity = 15F;

        private void PhysicsMovement(Vector3 solvedPosition, Quaternion solvedRotation,
                               Rigidbody intObj)
        {
            Vector3 solvedCenterOfMass = solvedRotation * intObj.centerOfMass + solvedPosition;
            Vector3 currCenterOfMass = intObj.rotation * intObj.centerOfMass + intObj.position;

            Vector3 targetVelocity = ContactUtils.ToLinearVelocity(currCenterOfMass, solvedCenterOfMass, Time.fixedDeltaTime);

            Vector3 targetAngularVelocity = ContactUtils.ToAngularVelocity(intObj.rotation, solvedRotation, Time.fixedDeltaTime);

            // Clamp targetVelocity by _maxVelocity.
            float targetSpeedSqrd = targetVelocity.sqrMagnitude;
            if (targetSpeedSqrd > _maxVelocity * _maxVelocity)
            {
                float targetPercent = _maxVelocity / Mathf.Sqrt(targetSpeedSqrd);
                targetVelocity *= targetPercent;
                targetAngularVelocity *= targetPercent;
            }

            intObj.velocity = targetVelocity;
            if (targetAngularVelocity.IsValid())
            {
                intObj.angularVelocity = targetAngularVelocity;
            }
        }

        // It's more stable to use physics movement currently
        // Using this will result in large amounts of spaghetti
        //private void KinematicMovement(Vector3 solvedPosition, Quaternion solvedRotation,
        //                       Rigidbody intObj)
        //{
        //    intObj.MovePosition(solvedPosition);
        //    intObj.MoveRotation(solvedRotation);
        //}

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
                // Check if we are safe to temporaily ignore collisions
                if (_ignorePhysicalHands == null || !_ignorePhysicalHands.disableAllHandCollisions)
                {
                    // Ignore collision after throwing so we don't knock the object
                    foreach (var hand in _grabbableHands)
                    {
                        hand.IgnoreCollision(_rigid, _colliders, 0.1f, 0.01f);
                    }

                    // Ignore Grabbing after throwing
                    ignoreGrabTime = Time.time + THOWN_GRAB_COOLDOWNTIME;
                }

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