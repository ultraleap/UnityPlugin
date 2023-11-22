using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    [System.Serializable]
    public class GrabHelperObject
    {
        private const float THOWN_GRAB_COOLDOWNTIME = 0.5f;
        private const float MINIMUM_STRENGTH = 0.25f, MINIMUM_THUMB_STRENGTH = 0.2f;
        private const float REQUIRED_ENTRY_STRENGTH = 0.15f, REQUIRED_EXIT_STRENGTH = 0.05f, REQUIRED_THUMB_EXIT_STRENGTH = 0.1f, REQUIRED_PINCH_DISTANCE = 0.012f;
        private const float GRABBABLE_DIRECTIONS_DOT = -0.7f, FORWARD_DIRECTION_DOT = 0.25f;

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
        private Dictionary<ContactHand, List<ContactBone>[]> _bones = new Dictionary<ContactHand, List<ContactBone>[]>();

        private List<ContactHand> _grabbingCandidates = new List<ContactHand>();
        private List<bool> _grabbingCandidatesContact = new List<bool>();
        private List<GrabValues> _grabValues = new List<GrabValues>();

        internal List<ContactHand> GrabbingHands => _grabbingHands;
        private List<ContactHand> _grabbingHands = new List<ContactHand>();

        [System.Serializable]
        private class GrabValues
        {
            public float[] fingerStrength = new float[5];
            public float[] originalFingerStrength = new float[5];
            public Vector3[] tipPositions = new Vector3[5];
            public Matrix4x4 mat;
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

        internal bool Ignored;
        private IgnorePhysicalHands _ignorePhysicalHands;

        float ignoreGrabTime = 0f;

        private bool anyBoneGrabbable = false;

        /// <summary>
        /// Returns true if the provided hand is grabbing an object.
        /// Requires thumb or palm grabbing, and at least one (non-thumb) finger
        /// </summary>
        /// <param name="hand">The hand to check</param>
        /// <returns>If the provided hand is grabbing</returns>
        private bool Grabbed(ContactHand hand)
        {
            if (_bones.TryGetValue(hand, out var bones))
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
        /// Returns true if the finger on the provided hand is grabbing, and false if not
        /// </summary>
        /// <param name="hand">The hand to check</param>
        /// <param name="finger">The finger to check</param>
        /// <returns>If the finger on the provided hand is grabbed</returns>
        internal bool Grabbed(ContactHand hand, int finger)
        {
            if (_bones.TryGetValue(hand, out var bones))
            {
                switch (finger)
                {
                    case 0:
                    case 5:
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
            foreach (var item in _grabbingCandidates)
            {
                SetBoneGrabbing(item, false);
            }
            GrabState = State.Idle;

            //// Remove any lingering contact events
            //// Remove any lingering hover events
            if (_rigid != null)
            {
                for (int i = 0; i < _grabbingCandidatesContact.Count; i++)
                {
                    if (_grabbingCandidatesContact[i])
                    {
                        if (_rigid.TryGetComponent<IPhysicalHandContact>(out var physicalHandContact))
                        {
                            physicalHandContact.OnHandContactExit(_grabbingCandidates[i]);
                        }

                        _grabbingCandidates[i].physicalHandsManager.OnHandContactExit(_rigid);
                    }

                    if (_rigid.TryGetComponent<IPhysicalHandHover>(out var physicalHandHover))
                    {
                        physicalHandHover.OnHandHoverExit(_grabbingCandidates[i]);
                    }

                    _grabbingCandidates[i].physicalHandsManager.OnHandHoverExit(_rigid);
                }
            }

            _grabValues.Clear();
            _grabbingCandidates.Clear();
            _grabbingCandidatesContact.Clear();
            foreach (var pair in _bones)
            {
                for (int j = 0; j < pair.Value.Length; j++)
                {
                    pair.Value[j].Clear();
                }
            }
            _bones.Clear();
        }

        internal void AddHand(ContactHand hand)
        {
            if (!_grabbingCandidates.Contains(hand))
            {
                if (GrabState == State.Idle)
                {
                    GrabState = State.Hover;
                }
                _grabbingCandidates.Add(hand);
                _grabbingCandidatesContact.Add(false);
                _grabValues.Add(new GrabValues());

                if (_ignorePhysicalHands)
                {
                    _ignorePhysicalHands.AddToHands(hand);
                }
            }
        }

        internal void RemoveHand(ContactHand hand)
        {
            if (_grabbingCandidates.Contains(hand))
            {
                _grabbingCandidatesContact.RemoveAt(_grabbingCandidates.IndexOf(hand));
                _grabbingCandidates.Remove(hand);
            }
        }

        internal void ReleaseObject()
        {
            GrabState = State.Hover;
            // Make sure the object hasn't been destroyed
            if (_rigid != null)
            {
                if (!Ignored)
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

            GrabbingContactCheck();

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
                        if (!Ignored && ignoreGrabTime < Time.time)
                        {
                            MoveObject();
                            ThrowingOnHold();
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
            foreach (var pair in _bones)
            {
                for (int i = 0; i < _bones[pair.Key].Length; i++)
                {
                    for (int j = 0; j < _bones[pair.Key][i].Count; j++)
                    {
                        if (!_bones[pair.Key][i][j].GrabbableObjects.Contains(_rigid))
                        {
                            _bones[pair.Key][i].RemoveAt(j);
                            j--;
                        }
                    }
                }
            }

            anyBoneGrabbable = false;

            foreach (var hand in _grabbingCandidates)
            {
                foreach (var bone in hand.bones)
                {
                    if (bone.GrabbableObjects.Contains(_rigid))
                    {
                        anyBoneGrabbable = true;
                        if (_bones.TryGetValue(bone.contactHand, out List<ContactBone>[] storedBones))
                        {
                            storedBones[bone.Finger].Add(bone);
                        }
                        else
                        {
                            _bones.Add(bone.contactHand, new List<ContactBone>[ContactHand.FINGERS + 1]);
                            for (int i = 0; i < _bones[bone.contactHand].Length; i++)
                            {
                                _bones[bone.contactHand][i] = new List<ContactBone>();
                                if (bone.Finger == i)
                                {
                                    _bones[bone.contactHand][i].Add(bone);
                                }
                            }
                        }
                    }
                }
            }

            // Update the hand contact and hover events
            for (int i = 0; i < _grabbingCandidates.Count; i++)
            {
                IPhysicalHandContact handContactEvent = null;

                if (_grabbingCandidatesContact[i] != _grabbingCandidates[i].IsContacting)
                {
                    _grabbingCandidatesContact[i] = _grabbingCandidates[i].IsContacting;

                    // We stopped contacting, so fire the contact exit event
                    if (!_grabbingCandidates[i].IsContacting)
                    {
                        if (_rigid.TryGetComponent<IPhysicalHandContact>(out handContactEvent))
                        {
                            handContactEvent.OnHandContactExit(_grabbingCandidates[i]);
                        }

                        _grabbingCandidates[i].physicalHandsManager.OnHandContactExit(_rigid);
                    }
                }

                // Fire the contacting event whether it changed or not
                if (_grabbingCandidates[i].IsContacting)
                {
                    if (handContactEvent != null || _rigid.TryGetComponent<IPhysicalHandContact>(out handContactEvent))
                    {
                        handContactEvent.OnHandContact(_grabbingCandidates[i]);
                    }

                    _grabbingCandidates[i].physicalHandsManager.OnHandContact(_rigid);
                }

                // Fire the hovering event
                if (_rigid.TryGetComponent<IPhysicalHandHover>(out var physicalHandHover))
                {
                    physicalHandHover.OnHandHover(_grabbingCandidates[i]);
                }

                _grabbingCandidates[i].physicalHandsManager.OnHandHover(_rigid);
            }
        }

        private void GrabbingContactCheck()
        {
            //Reset grab bools
            foreach (var grabValue in _grabValues)
            {
                grabValue.handGrabbing = false;
                grabValue.facingOppositeHand = false;
            }

            for (int handIndex = 0; handIndex < _grabbingCandidates.Count; handIndex++)
            {
                ContactHand hand = _grabbingCandidates[handIndex];
                GrabValues grabValues = _grabValues[handIndex];

                if (_grabbingHands.Contains(hand))
                {
                    continue;
                }

                for (int i = 0; i < 5; i++)
                {
                    if (Grabbed(hand, i) && _manager.FingerStrengths[hand][i] > (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH))
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

                if (Grabbed(hand))
                {
                    // if two fingers are greater than 20% of their remaining distance when they grasped difference
                    int c = 0;
                    for (int i = 0; i < 5; i++)
                    {
                        if (grabValues.fingerStrength[i] == -1)
                            continue;

                        if (i == 0)
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
                                    if (c == 0)
                                    {
                                        c = 2;
                                        grabValues.fingerStrength[0] *= 0.85f;
                                    }
                                    else
                                    {
                                        c++;
                                    }
                                }
                            }

                            if (unpinched)
                            {
                                grabValues.waitingForInitialUnpinch = false;
                            }

                            if (c > 0 || grabValues.waitingForInitialUnpinch)
                            {
                                break;
                            }
                        }

                        // if waiting for ungrab AND no contact 
                        // ungrab == required ungrab strength 2

                        if (grabValues.fingerStrength[i] > grabValues.originalFingerStrength[i] + ((1 - grabValues.originalFingerStrength[i]) * REQUIRED_ENTRY_STRENGTH))
                        {
                            c++;
                        }
                    }

                    if (c >= 2)
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
            foreach (var hand in _grabbingCandidates)
            {
                if (!hand.isHandPhysical)
                {
                    return;
                }
            }

            int bone1Index = 0;

            foreach (ContactHand hand in _grabbingCandidates)
            {
                foreach (ContactHand hand2 in _grabbingCandidates)
                {
                    foreach (ContactBone bone1 in hand.bones)
                    {
                        int grabHandIndex1 = _grabbingCandidates.IndexOf(bone1.contactHand);

                        if (bone1.GrabbableDirections.TryGetValue(_rigid, out var grabbableDirectionsB1))
                        {
                            int bone2Index = 0;

                            foreach (ContactBone bone2 in hand2.bones)
                            {
                                if (bone2Index < bone1Index) // Avoid double-checking the same index combinations
                                {
                                    bone2Index++;
                                    continue;
                                }

                                // Don't compare against the same finger
                                if (bone1.finger == bone2.finger && bone1.contactHand == bone2.contactHand) continue;

                                if (bone2.GrabbableDirections.TryGetValue(_rigid, out var grabbableDirectionsB2))
                                {
                                    int grabHandIndex2 = _grabbingCandidates.IndexOf(bone2.contactHand);

                                    foreach (var directionPairB1 in grabbableDirectionsB1)
                                    {
                                        //If the grabbable direction is facing away from the bone forward direction, disregard it
                                        if (Vector3.Dot(directionPairB1.Value.direction, -bone1.transform.up) < FORWARD_DIRECTION_DOT) continue;

                                        foreach (var directionPairB2 in grabbableDirectionsB2)
                                        {
                                            //If the grabbable direction is facing away from the bone forward direction, disregard it
                                            if (Vector3.Dot(directionPairB2.Value.direction, -bone2.transform.up) < FORWARD_DIRECTION_DOT) continue;

                                            float dot = Vector3.Dot(directionPairB1.Value.direction, directionPairB2.Value.direction);

                                            //If the two bones are facing opposite directions (i.e. pushing towards each other), they're grabbing
                                            if (dot < GRABBABLE_DIRECTIONS_DOT)
                                            {
                                                if (bone1.contactHand == bone2.contactHand)
                                                {
                                                    _grabValues[grabHandIndex1].handGrabbing = true;
                                                    _grabValues[grabHandIndex2].handGrabbing = true;
                                                }
                                                else
                                                {
                                                    _grabValues[grabHandIndex1].facingOppositeHand = true;
                                                    _grabValues[grabHandIndex2].facingOppositeHand = true;
                                                }

                                                RegisterGrabbingHand(bone1.contactHand);
                                                RegisterGrabbingHand(bone2.contactHand);

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
        }

        private void RegisterGrabbingHand(ContactHand hand)
        {
            if (ignoreGrabTime > Time.time)
                return;

            if (_grabbingHands.Contains(hand))
                return;

            if (!Ignored)
            {
                // Store the original rigidbody variables
                _oldKinematic = _rigid.isKinematic;
                _rigid.isKinematic = false;
            }

            _grabbingHands.Add(hand);

            if (_grabbingHands.Count > 0)
            {
                GrabState = State.Grab;
            }

            int grabHandIndex = _grabbingCandidates.IndexOf(hand);

            _grabValues[grabHandIndex].offset = _rigid.position - hand.palmBone.transform.position;
            _grabValues[grabHandIndex].rotationOffset = Quaternion.Inverse(hand.palmBone.transform.rotation) * _rigid.rotation;
            _grabValues[grabHandIndex].originalHandRotation = hand.palmBone.transform.rotation;

            if (_rigid.TryGetComponent<IPhysicalHandGrab>(out var physicalHandGrab))
            {
                physicalHandGrab.OnHandGrab(hand);
            }

            hand.physicalHandsManager.OnHandGrab(_rigid);
        }

        private void UpdateGrabbingValues()
        {
            for(int grabHandIndex = 0; grabHandIndex < _grabbingCandidates.Count; grabHandIndex++)
            {
                // Check if this hand was grabbing and is now not grabbing
                if (_grabbingHands.Count > 1
                    && !_grabValues[grabHandIndex].handGrabbing
                    && !_grabValues[grabHandIndex].facingOppositeHand
                    // Hand has moved significantly far away from the object
                    && (_rigid.position - _grabbingCandidates[grabHandIndex].palmBone.transform.position).sqrMagnitude > _grabValues[grabHandIndex].offset.sqrMagnitude * 1.5f)
                {
                    SetBoneGrabbing(_grabbingCandidates[grabHandIndex], false);
                    _grabbingHands.Remove(_grabbingCandidates[grabHandIndex]);

                    if (_rigid.TryGetComponent<IPhysicalHandGrab>(out var physicalHandGrab))
                    {
                        physicalHandGrab.OnHandGrabExit(_grabbingCandidates[grabHandIndex]);
                    }

                    _grabbingCandidates[grabHandIndex].physicalHandsManager.OnHandGrabExit(_rigid);

                    continue;
                }

                int c = 0;
                for (int i = 0; i < 5; i++)
                {
                    // Was the finger not contacting before?
                    if (_grabValues[grabHandIndex].fingerStrength[i] == -1)
                    {
                        if (_manager.FingerStrengths[_grabbingCandidates[grabHandIndex]][i] > (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) && Grabbed(_grabbingCandidates[grabHandIndex], i))
                        {
                            // Store the strength value on contact
                            _grabValues[grabHandIndex].fingerStrength[i] = _manager.FingerStrengths[_grabbingCandidates[grabHandIndex]][i];
                        }
                    }
                    else
                    {
                        // If the finger was contacting but has uncurled by the exit percentage then it is no longer "grabbed"
                        if (_manager.FingerStrengths[_grabbingCandidates[grabHandIndex]][i] < (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) || _grabValues[grabHandIndex].fingerStrength[i] * (1 - (i == 0 ? REQUIRED_THUMB_EXIT_STRENGTH : REQUIRED_EXIT_STRENGTH)) >= _manager.FingerStrengths[_grabbingCandidates[grabHandIndex]][i])
                        {
                            _grabValues[grabHandIndex].fingerStrength[i] = -1;
                        }
                    }

                    if (_grabValues[grabHandIndex].fingerStrength[i] != -1)
                    {
                        c++;
                    }
                }

                // If we've got two fingers curled, the hand was grabbing in the grab contact checks, or the hand is facing the other, then we grab
                if (c >= 2 || _grabValues[grabHandIndex].handGrabbing || _grabValues[grabHandIndex].facingOppositeHand)
                {
                    SetBoneGrabbing(_grabbingCandidates[grabHandIndex], true);
                    continue;
                }

                // One final check to see if the original data hand has bones inside of the object to retain grabbing during physics updates
                bool data = DataHandIntersection(_grabbingCandidates[grabHandIndex]);

                if (!data)
                {
                    SetBoneGrabbing(_grabbingCandidates[grabHandIndex], false);
                    _grabValues[grabHandIndex].waitingForInitialUnpinch = true;
                    _grabbingHands.Remove(_grabbingCandidates[grabHandIndex]);

                    if (_rigid.TryGetComponent<IPhysicalHandGrab>(out var physicalHandGrab))
                    {
                        physicalHandGrab.OnHandGrabExit(_grabbingCandidates[grabHandIndex]);
                    }

                    _grabbingCandidates[grabHandIndex].physicalHandsManager.OnHandGrabExit(_rigid);
                }
                else
                {
                    SetBoneGrabbing(_grabbingCandidates[grabHandIndex], true);
                }
            }
        }

        private void SetBoneGrabbing(ContactHand hand, bool add)
        {
            if (add)
            {
                for (int j = 0; j < _bones[hand].Length; j++)
                {
                    if (_bones[hand][j].Count > 0)
                    {
                        foreach (var bone in hand.bones)
                        {
                            // Using only the first element of a foreach is cheaper than using _bones[pair.Key][j].First for GC.Alloc
                            foreach (var b in _bones[hand][j])
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

                hand.physicalHandsManager.OnHandGrab(_rigid);
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

            if (!_bones.ContainsKey(hand))
            {
                return false;
            }

            for (int i = 0; i < _bones[hand].Length; i++)
            {
                foreach (var bone in _bones[hand][i])
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

                for (int i = _grabbingHands.Count - 1; i > 0; i--)
                {
                    int grabHandndex = _grabbingCandidates.IndexOf(_grabbingHands[i]);

                    if (!_grabValues[grabHandndex].handGrabbing)
                    {
                        hand = _grabbingHands[i];
                        break;
                    }
                }

                if (hand == null)
                {
                    hand = _grabbingHands[_grabbingHands.Count - 1];
                }

                if (hand.dataHand != null)
                {
                    int grabHandndex = _grabbingCandidates.IndexOf(hand);

                    _newPosition = hand.palmBone.transform.position + (hand.Velocity * Time.fixedDeltaTime) + (hand.palmBone.transform.rotation * Quaternion.Inverse(_grabValues[grabHandndex].originalHandRotation) * _grabValues[grabHandndex].offset);
                    _newRotation = hand.palmBone.transform.rotation * Quaternion.Euler(hand.AngularVelocity * Time.fixedDeltaTime) * _grabValues[grabHandndex].rotationOffset;
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

        // the time before release to ignore velocities when throwing
        private const float WINDOW_DELAY = 0.015f;
        // the time before WINDOW_DELAY to capture velocities to be applied to objects when throwing
        private const float WINDOW_LENGTH = 0.045f;

        private const float MAX_THROW_VELOCITY = 3f;

        private Queue<VelocitySample> _velocityQueue = new Queue<VelocitySample>(16);

        private struct VelocitySample
        {
            public float time;
            public Vector3 position;

            public VelocitySample(Vector3 position, float time)
            {
                this.position = position;
                this.time = time;
            }
        }

        private void ThrowingOnHold()
        {
            _velocityQueue.Enqueue(new VelocitySample(_rigid.position,
                                                      Time.fixedTime));

            while (true)
            {
                VelocitySample oldestVelocity = _velocityQueue.Peek();

                // Dequeue conservatively
                if (oldestVelocity.time < (Time.fixedTime - WINDOW_LENGTH - WINDOW_DELAY))
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
            // Ensure that they are at least 4 frames to average
            if (_velocityQueue.Count < 4)
            {
                _velocityQueue.Clear();

                _rigid.velocity = Vector3.zero;
                _rigid.angularVelocity = Vector3.zero;
                return;
            }

            // The last frame time to use to avoid using deceleration frames
            float windowEnd = Time.fixedTime - WINDOW_DELAY;

            Vector3 averageVelocity = Vector3.zero;
            VelocitySample initialVelocity = _velocityQueue.Peek();

            int velocityCount = 0;

            while (_velocityQueue.Count > 0)
            {
                VelocitySample oldestVelocity = _velocityQueue.Dequeue();

                if (oldestVelocity.time < windowEnd)
                {
                    averageVelocity += oldestVelocity.position;
                    velocityCount++;
                }
                else
                {
                    // We are in the WINDOW_DELAY time, ignore the remaining frames
                    _velocityQueue.Clear();
                    break;
                }
            }

            // average the frames to get the average positional change
            averageVelocity /= velocityCount;

            // remove the initial position to get the delta
            averageVelocity -= initialVelocity.position;

            // convert the positional change to a velocity
            averageVelocity /= Time.fixedDeltaTime;

            averageVelocity = Vector3.ClampMagnitude(averageVelocity, MAX_THROW_VELOCITY);

            if (averageVelocity.magnitude > 0.5f)
            {
                // Check if we are safe to temporaily ignore collisions
                if (_ignorePhysicalHands == null || !_ignorePhysicalHands.DisableAllHandCollisions)
                {
                    // Ignore collision after throwing so we don't knock the object
                    foreach (var hand in _grabbingCandidates)
                    {
                        hand.IgnoreCollision(_rigid, _colliders, 0, 0.01f);
                    }

                    // Ignore Grabbing after throwing
                    ignoreGrabTime = Time.time + THOWN_GRAB_COOLDOWNTIME;
                }

                // Set the new velocty. Allow physics to solve for rotational change
                _rigid.velocity = averageVelocity;
            }
        }

        #endregion
    }
}