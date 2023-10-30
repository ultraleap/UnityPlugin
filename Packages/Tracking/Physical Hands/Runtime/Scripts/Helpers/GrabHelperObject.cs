using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    [System.Serializable]
    public class GrabHelperObject
    {
        private const float GRAB_COOLDOWNTIME = 0.02f;
        private const float MINIMUM_STRENGTH = 0.25f, MINIMUM_THUMB_STRENGTH = 0.2f;
        private const float REQUIRED_ENTRY_STRENGTH = 0.15f, REQUIRED_EXIT_STRENGTH = 0.05f, REQUIRED_THUMB_EXIT_STRENGTH = 0.1f, REQUIRED_PINCH_DISTANCE = 0.012f;
        private const float GRABBABLE_DIRECTIONS_DOT = -0.7f, FORWARD_DIRECTION_DOT = 0.25f;

        /// <summary>
        /// The current state of the grasp helper.
        /// </summary>
        public enum State
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
        public State GrabState { get; private set; } = State.Idle;

        public HashSet<ContactBone> BoneHash => _boneHash;
        private HashSet<ContactBone> _boneHash = new HashSet<ContactBone>();
        public int TotalBones { get { return _boneHash.Count; } }

        /// <summary>
        /// Bones is a hand indexed dictionary, of eligible to grab physics bone hashsets of the hand's fingers 
        /// </summary>
        public Dictionary<ContactHand, HashSet<ContactBone>[]> Bones => _bones;
        private Dictionary<ContactHand, HashSet<ContactBone>[]> _bones = new Dictionary<ContactHand, HashSet<ContactBone>[]>();

        public List<ContactHand> GrabbingCandidates => _grabbingCandidates;
        private List<ContactHand> _grabbingCandidates = new List<ContactHand>();
        private List<bool> _grabbingCandidatesContact = new List<bool>();

        public List<ContactHand> GrabbingHands => _grabbingHands;
        private List<ContactHand> _grabbingHands = new List<ContactHand>();

        private Dictionary<ContactHand, GrabValues> _grabbingValues = new Dictionary<ContactHand, GrabValues>();

        [System.Serializable]
        public class GrabValues
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

        public GrabHelper Manager { get; private set; }

        private Rigidbody _rigid;
        public Rigidbody Rigidbody => _rigid;
        private List<Collider> _colliders = new List<Collider>();

        private Vector3 _newPosition;
        private Quaternion _newRotation;

        private bool _oldKinematic;
        private float _originalMass = 1f;
        public float OriginalMass => _originalMass;

        // This means we can have bones stay "attached" for a small amount of time
        private List<ContactBone> _boneCooldownItems = new List<ContactBone>();
        private List<float> _boneCooldownTime = new List<float>();

        public bool Ignored;
        private IgnorePhysicalHands _ignorePhysicsHands;

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
        public bool Grabbed(ContactHand hand, int finger)
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

        public GrabHelperObject(Rigidbody rigid, GrabHelper manager)
        {
            _rigid = rigid;
            _originalMass = _rigid.mass;
            // Doing this prevents objects from misaligning during rotation
            if (_rigid.maxAngularVelocity < 100f)
            {
                _rigid.maxAngularVelocity = 100f;
            }
            _colliders = rigid.GetComponentsInChildren<Collider>(true).ToList();
            HandleIgnoreContactHelper();
            //CalculateWindowLengthAndDelay(out _windowLength, out _windowDelay); //to calculate throw window and delay
            Manager = manager;
        }


        /// <summary>
        /// Find if the object we are attached to has an IgnoreContactHelper script on it.
        /// If it does, save a reference to it and give it a reference to us so that it can update our ignore values.
        /// </summary>
        private void HandleIgnoreContactHelper()
        {
            _ignorePhysicsHands = _rigid.GetComponentInChildren<IgnorePhysicalHands>();

            if (_ignorePhysicsHands != null)
            {
                _ignorePhysicsHands.GrabHelperObject = this;
            }
        }

        public void ReleaseHelper()
        {
            foreach (var item in _grabbingValues)
            {
                SetBoneGrabbing(item, false);
            }
            GrabState = State.Idle;
            _grabbingValues.Clear();

            //// Remove any lingering contact events
            //if (_rigid != null && _rigid.TryGetComponent<IContactHandContact>(out var ContactHandContact))
            //{
            //    for (int i = 0; i < _graspingCandidatesContact.Count; i++)
            //    {
            //        if (_graspingCandidatesContact[i])
            //        {
            //            ContactHandContact.OnHandContactExit(_graspingCandidates[i]);
            //        }
            //    }
            //}

            //// Remove any lingering hover events
            //if (_rigid != null && _rigid.TryGetComponent<IContactHandHover>(out var ContactHandHover))
            //{
            //    foreach (var hand in _graspingCandidates)
            //    {
            //        ContactHandHover.OnHandHoverExit(hand);
            //    }
            //}
            _grabbingCandidates.Clear();
            _grabbingCandidatesContact.Clear();
            _boneHash.Clear();
            foreach (var pair in _bones)
            {
                for (int j = 0; j < pair.Value.Length; j++)
                {
                    pair.Value[j].Clear();
                }
            }
            _bones.Clear();
        }

        public void AddHand(ContactHand hand)
        {
            if (!_grabbingCandidates.Contains(hand))
            {
                if (GrabState == State.Idle)
                {
                    GrabState = State.Hover;
                }
                _grabbingCandidates.Add(hand);
                _grabbingCandidatesContact.Add(false);
                //if (_rigid.TryGetComponent<IContactHandHover>(out var ContactHandHover))
                //{
                //    ContactHandHover.OnHandHover(hand);
                //}

                if (_ignorePhysicsHands)
                {
                    _ignorePhysicsHands.AddToHands(hand);
                }
            }
        }

        public void RemoveHand(ContactHand hand)
        {
            if (_grabbingCandidates.Contains(hand))
            {
                _grabbingCandidatesContact.RemoveAt(_grabbingCandidates.IndexOf(hand));
                _grabbingCandidates.Remove(hand);
                //if (_rigid != null && _rigid.TryGetComponent<IContactHandHover>(out var ContactHandHover))
                //{
                //    ContactHandHover.OnHandHoverExit(hand);
                //}
            }
        }

        public void ReleaseObject()
        {
            GrabState = State.Hover;
            // Make sure the object hasn't been destroyed
            if (_rigid != null)
            {
                if (!Ignored)
                {
                    // Only ever unset the rigidbody values here otherwise outside logic will get confused
                    _rigid.isKinematic = _oldKinematic;
                    ThrowingOnRelease(true);
                }
            }
        }

        public State UpdateHelper()
        {
            UpdateHands();

            if (_boneHash.Count == 0 && GrabState != State.Grab)
            {
                // If we don't then and we're not grabbing then just return
                return GrabState = State.Hover;
            }

            GrabbingContactCheck();

            switch (GrabState)
            {
                case State.Hover:
                    if (_boneHash.Count > 0)
                    {
                        GrabState = State.Contact;
                    }
                    break;
                case State.Grab:
                    UpdateGrabbingValues();
                    if (_grabbingHands.Count > 0)
                    {
                        UpdateHandPositions();
                        if (!Ignored)
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
            // Remove bones from the hashset if we're not able to grab with them
            _boneHash.RemoveWhere(RemoveOldBones);

            // Loop through each hand in our bone array, then the finger, then the bones in that finger
            // If we're no longer in a grabbing state with that bone we want to add it to the cooldowns
            foreach (var pair in _bones)
            {
                for (int i = 0; i < _bones[pair.Key].Length; i++)
                {
                    foreach (var bone in _bones[pair.Key][i])
                    {
                        if (!bone.GrabbableObjects.Contains(_rigid))
                        {
                            AddBoneToCooldowns(bone);
                        }
                    }
                }
            }

            foreach (var hand in _grabbingCandidates)
            {
                foreach (var bone in hand.bones)
                {
                    if (bone.GrabbableObjects.Contains(_rigid))
                    {
                        _boneHash.Add(bone);
                        if (_bones.TryGetValue(bone.contactHand, out HashSet<ContactBone>[] storedBones))
                        {
                            storedBones[bone.Finger].Add(bone);
                            // Make sure we remove the bone from the cooldown if it's good again
                            RemoveBoneFromCooldowns(bone);
                        }
                        else
                        {
                            _bones.Add(bone.contactHand, new HashSet<ContactBone>[ContactHand.FINGERS + 1]);
                            for (int i = 0; i < _bones[bone.contactHand].Length; i++)
                            {
                                _bones[bone.contactHand][i] = new HashSet<ContactBone>();
                                if (bone.Finger == i)
                                {
                                    _bones[bone.contactHand][i].Add(bone);
                                    RemoveBoneFromCooldowns(bone);
                                }
                            }
                        }
                    }
                }
            }

            UpdateRemovedBones();

            // Update the hand contact events
            for (int i = 0; i < _grabbingCandidates.Count; i++)
            {
                if (_grabbingCandidatesContact[i] != _grabbingCandidates[i].IsContacting)
                {
                    //if (_rigid.TryGetComponent<IContactHandContact>(out var ContactHandContact))
                    //{
                    //    if (_graspingCandidates[i].IsContacting)
                    //    {
                    //        ContactHandContact.OnHandContact(_graspingCandidates[i]);
                    //    }
                    //    else
                    //    {
                    //        ContactHandContact.OnHandContactExit(_graspingCandidates[i]);
                    //    }
                    //}
                    _grabbingCandidatesContact[i] = _grabbingCandidates[i].IsContacting;
                }
            }
        }

        private void AddBoneToCooldowns(ContactBone bone)
        {
            int ind = _boneCooldownItems.IndexOf(bone);
            // If we've found it then it's already cooling down.
            if (ind == -1)
            {
                _boneCooldownItems.Add(bone);
                _boneCooldownTime.Add(GRAB_COOLDOWNTIME);
            }
        }

        private void RemoveBoneFromCooldowns(ContactBone bone)
        {
            int ind = _boneCooldownItems.IndexOf(bone);
            if (ind != -1)
            {
                _boneCooldownItems.RemoveAt(ind);
                _boneCooldownTime.RemoveAt(ind);
            }
        }

        private bool RemoveOldBones(ContactBone bone)
        {
            if (bone.GrabbableObjects.Contains(_rigid))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void GrabbingContactCheck()
        {
            //Reset grab bools
            foreach (var grabValue in _grabbingValues)
            {
                grabValue.Value.handGrabbing = false;
                grabValue.Value.facingOppositeHand = false;
            }

            foreach (var hand in _grabbingCandidates)
            {
                if (_grabbingHands.Contains(hand))
                {
                    continue;
                }

                if (!_grabbingValues.ContainsKey(hand))
                {
                    _grabbingValues.Add(hand, new GrabValues());

                }

                for (int i = 0; i < 5; i++)
                {
                    if (Grabbed(hand, i) && Manager.FingerStrengths[hand][i] > (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH))
                    {
                        _grabbingValues[hand].fingerStrength[i] = Manager.FingerStrengths[hand][i];
                        if (_grabbingValues[hand].originalFingerStrength[i] == -1)
                        {
                            _grabbingValues[hand].originalFingerStrength[i] = Manager.FingerStrengths[hand][i];
                        }
                    }
                    else
                    {
                        if (hand.dataHand.GetFingerPinchDistance(i) <= REQUIRED_PINCH_DISTANCE)
                        {
                            _grabbingValues[hand].waitingForInitialUnpinch = true;
                        }
                        _grabbingValues[hand].originalFingerStrength[i] = -1;
                        _grabbingValues[hand].fingerStrength[i] = -1;
                    }
                }

                if (Grabbed(hand))
                {
                    // if two fingers are greater than 20% of their remaining distance when they grasped difference
                    int c = 0;
                    for (int i = 0; i < 5; i++)
                    {
                        if (_grabbingValues[hand].fingerStrength[i] == -1)
                            continue;

                        if (i == 0)
                        {
                            bool unpinched = true;
                            for (int j = 1; j < 5; j++)
                            {

                                //dont allow if pinching on enter

                                if (hand.dataHand.GetFingerPinchDistance(j) <= REQUIRED_PINCH_DISTANCE)
                                {
                                    if (_grabbingValues[hand].waitingForInitialUnpinch)
                                    {
                                        unpinched = false;
                                        continue;
                                    }

                                    // Make very small pinches more sticky
                                    _grabbingValues[hand].fingerStrength[j] *= 0.85f;
                                    if (c == 0)
                                    {
                                        c = 2;
                                        _grabbingValues[hand].fingerStrength[0] *= 0.85f;
                                    }
                                    else
                                    {
                                        c++;
                                    }
                                }
                            }

                            if (unpinched)
                            {
                                _grabbingValues[hand].waitingForInitialUnpinch = false;
                            }

                            if (c > 0 || _grabbingValues[hand].waitingForInitialUnpinch)
                            {
                                break;
                            }
                        }

                        // if waiting for ungrab AND no contact 
                        // ungrab == required ungrab strength 2

                        if (_grabbingValues[hand].fingerStrength[i] > _grabbingValues[hand].originalFingerStrength[i] + ((1 - _grabbingValues[hand].originalFingerStrength[i]) * REQUIRED_ENTRY_STRENGTH))
                        {
                            c++;
                        }
                    }

                    if (c >= 2)
                    {
                        _grabbingValues[hand].handGrabbing = true;
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

            foreach (ContactBone bone1 in BoneHash)
            {
                if (bone1.GrabbableDirections.TryGetValue(_rigid, out var grabbableDirectionsB1))
                {
                    int bone2Index = 0;

                    foreach (ContactBone bone2 in BoneHash)
                    {
                        if (bone2Index < bone1Index) // Avoid double-checking the same index combinations
                        {
                            bone2Index++;
                            continue;
                        }

                        // Don't compare against the same bone
                        if (bone1 == bone2) continue;

                        if (bone2.GrabbableDirections.TryGetValue(_rigid, out var grabbableDirectionsB2))
                        {
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
                                            _grabbingValues[bone1.contactHand].handGrabbing = true;
                                            _grabbingValues[bone2.contactHand].handGrabbing = true;
                                        }
                                        else
                                        {
                                            _grabbingValues[bone1.contactHand].facingOppositeHand = true;
                                            _grabbingValues[bone2.contactHand].facingOppositeHand = true;
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

        private void RegisterGrabbingHand(ContactHand hand)
        {
            if (_grabbingHands.Contains(hand)) return;

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
            _grabbingValues[hand].offset = _rigid.position - hand.palmBone.transform.position;
            _grabbingValues[hand].rotationOffset = Quaternion.Inverse(hand.palmBone.transform.rotation) * _rigid.rotation;
            _grabbingValues[hand].originalHandRotation = hand.palmBone.transform.rotation;
            //if (_rigid.TryGetComponent<IContactHandGrab>(out var ContactHandGrab))
            //{
            //    ContactHandGrab.OnHandGrab(hand);
            //}
        }

        private void UpdateRemovedBones()
        {
            for (int i = 0; i < _boneCooldownItems.Count; i++)
            {
                _boneCooldownTime[i] -= Time.fixedDeltaTime;
                if (_boneCooldownTime[i] <= 0)
                {
                    if (_bones.ContainsKey(_boneCooldownItems[i].contactHand))
                    {
                        _bones[_boneCooldownItems[i].contactHand][_boneCooldownItems[i].Finger].Remove(_boneCooldownItems[i]);
                    }
                    _boneCooldownItems.RemoveAt(i);
                    _boneCooldownTime.RemoveAt(i);
                    i--;
                }
            }
        }

        private void UpdateGrabbingValues()
        {
            foreach (KeyValuePair<ContactHand, GrabValues> grabbedHand in _grabbingValues)
            {
                // Check if this hand was grabbing and is now not grabbing
                if (_grabbingHands.Count > 1
                    && !grabbedHand.Value.handGrabbing
                    && !grabbedHand.Value.facingOppositeHand
                    // Hand has moved significantly far away from the object
                    && (_rigid.position - grabbedHand.Key.palmBone.transform.position).sqrMagnitude > grabbedHand.Value.offset.sqrMagnitude * 1.5f)
                {
                    SetBoneGrabbing(grabbedHand, false);
                    _grabbingHands.Remove(grabbedHand.Key);
                    //if (_rigid.TryGetComponent<IContactHandGrab>(out var ContactHandGrab))
                    //{
                    //    ContactHandGrab.OnHandGrabExit(grabbedHand.Key);
                    //}
                    continue;
                }

                int c = 0;
                for (int i = 0; i < 5; i++)
                {
                    // Was the finger not contacting before?
                    if (grabbedHand.Value.fingerStrength[i] == -1)
                    {
                        if (Manager.FingerStrengths[grabbedHand.Key][i] > (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) && Grabbed(grabbedHand.Key, i))
                        {
                            // Store the strength value on contact
                            grabbedHand.Value.fingerStrength[i] = Manager.FingerStrengths[grabbedHand.Key][i];
                        }
                    }
                    else
                    {
                        // If the finger was contacting but has uncurled by the exit percentage then it is no longer "grabbed"
                        if (Manager.FingerStrengths[grabbedHand.Key][i] < (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) || grabbedHand.Value.fingerStrength[i] * (1 - (i == 0 ? REQUIRED_THUMB_EXIT_STRENGTH : REQUIRED_EXIT_STRENGTH)) >= Manager.FingerStrengths[grabbedHand.Key][i])
                        {
                            grabbedHand.Value.fingerStrength[i] = -1;
                        }
                    }

                    if (grabbedHand.Value.fingerStrength[i] != -1)
                    {
                        c++;
                    }
                }

                // If we've got two fingers curled, the hand was grabbing in the grab contact checks, or the hand is facing the other, then we grab
                if (c >= 2 || grabbedHand.Value.handGrabbing || grabbedHand.Value.facingOppositeHand)
                {
                    SetBoneGrabbing(grabbedHand, true);
                    continue;
                }

                // One final check to see if the original data hand has bones inside of the object to retain grabbing during physics updates
                bool data = DataHandIntersection(grabbedHand.Key);

                if (!data)
                {
                    SetBoneGrabbing(grabbedHand, false);
                    _grabbingValues[grabbedHand.Key].waitingForInitialUnpinch = true;
                    _grabbingHands.Remove(grabbedHand.Key);
                    //if (_rigid.TryGetComponent<IContactHandGrab>(out var ContactHandGrab))
                    //{
                    //    ContactHandGrab.OnHandGrabExit(graspedHand.Key);
                    //}
                }
                else
                {
                    SetBoneGrabbing(grabbedHand, true);
                }
            }
        }

        private void SetBoneGrabbing(KeyValuePair<ContactHand, GrabValues> pair, bool add)
        {
            if (add)
            {
                for (int j = 0; j < _bones[pair.Key].Length; j++)
                {
                    if (_bones[pair.Key][j].Count > 0)
                    {
                        foreach (var bone in pair.Key.bones)
                        {
                            // Using only the first element of a foreach is cheaper than using _bones[pair.Key][j].First for GC.Alloc
                            foreach (var b in _bones[pair.Key][j])
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
                foreach (var item in pair.Key.bones)
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
                    if (!_grabbingValues[_grabbingHands[i]].handGrabbing)
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
                    _newPosition = hand.palmBone.transform.position + (hand.Velocity * Time.fixedDeltaTime) + (hand.palmBone.transform.rotation * Quaternion.Inverse(_grabbingValues[hand].originalHandRotation) * _grabbingValues[hand].offset);
                    _newRotation = hand.palmBone.transform.rotation * Quaternion.Euler(hand.AngularVelocity * Time.fixedDeltaTime) * _grabbingValues[hand].rotationOffset;
                }
            }
        }

        public void Gizmo()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_newPosition, 0.01f);
            Gizmos.color = Color.white;

            foreach (ContactHand hand in _grabbingHands)
            {
                if (hand.dataHand == null || !_bones.ContainsKey(hand))
                    continue;

                foreach (var bone in _boneHash)
                {
                    try
                    {
                        Gizmos.DrawSphere(hand.dataHand.Fingers[bone.Finger].bones[bone.Joint].NextJoint, 0.005f);
                    }
                    catch
                    {

                    }
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
        protected AnimationCurve _strengthByDistance = new AnimationCurve(new Keyframe(0.0f, 1.0f, 0.0f, 0.0f),
                                                                             new Keyframe(0.08f, 0.3f, 0.0f, 0.0f));

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
        private void KinematicMovement(Vector3 solvedPosition, Quaternion solvedRotation,
                               Rigidbody intObj)
        {
            intObj.MovePosition(solvedPosition);
            intObj.MoveRotation(solvedRotation);
        }

        #region Throwing

        // Taken from SlidingWindowThrow.cs

        // Length of time to average, and delay between the average and current time.
        // This is for default unity Timestep. These are set for current timestep in the constructor
        private float _windowLength = 0.05f, _windowDelay = 0.02f;


        private void CalculateWindowLengthAndDelay(out float windowLength, out float windowDelay)
        {
            // These magic numbers calculate each thing from a line. They use the equation y = mx+b to predict values at every timestep.
            // The full calculation has been ommitted to save calculations at runtime
            windowLength = (0.5625f * Time.fixedDeltaTime) + 0.03875f;

            windowDelay = (0.5625f * Time.fixedDeltaTime) + 0.00875f;
        }

        // Throwing curve
        private AnimationCurve _throwVelocityMultiplierCurve = new AnimationCurve(
                                                            new Keyframe(0.0F, 1.0F, 0, 0),
                                                            new Keyframe(2.5F, 1.5F, 0, 0));

        private Queue<VelocitySample> _velocityQueue = new Queue<VelocitySample>(64);

        private struct VelocitySample
        {
            public float time;
            public Vector3 position;
            public Quaternion rotation;

            public VelocitySample(Vector3 position, Quaternion rotation, float time)
            {
                this.position = position;
                this.rotation = rotation;
                this.time = time;
            }

            public static VelocitySample Interpolate(VelocitySample a, VelocitySample b, float time)
            {
                float alpha = Mathf.Clamp01(Mathf.InverseLerp(a.time, b.time, time));

                return new VelocitySample(Vector3.Lerp(a.position, b.position, alpha),
                                          Quaternion.Slerp(a.rotation, b.rotation, alpha),
                                          time);
            }
        }

        private void ThrowingOnHold()
        {
            _velocityQueue.Enqueue(new VelocitySample(_rigid.position,
                                                      _rigid.rotation,
                                                      Time.fixedTime));

            while (true)
            {
                VelocitySample oldestVelocity = _velocityQueue.Peek();

                // Dequeue conservatively
                if (oldestVelocity.time < (Time.fixedTime - _windowLength - _windowDelay))
                {
                    _velocityQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }

        private void ThrowingOnRelease(bool test = true)
        {
            // Ensure that they are at least 4 frames to average
            if (_velocityQueue.Count < 4)
            {
                _rigid.velocity = Vector3.zero;
                _rigid.angularVelocity = Vector3.zero;
                return;
            }

            float windowEnd = Time.fixedTime - _windowDelay;

            Vector3 averageVelocity = Vector3.zero;
            Quaternion averageAngularVelocity = Quaternion.identity;

            VelocitySample initialVelocity = _velocityQueue.Peek();

            while (_velocityQueue.Count > 0)
            {
                VelocitySample oldestVelocity = _velocityQueue.Dequeue();

                if (oldestVelocity.time < windowEnd)
                {
                    averageVelocity = Vector3.Lerp(averageVelocity, oldestVelocity.position, 0.5f);
                    averageAngularVelocity = Quaternion.Lerp(averageAngularVelocity, oldestVelocity.rotation, 0.5f);
                }
                else 
                { 
                    _velocityQueue.Clear();
                    break; 
                }
            }

            averageVelocity -= initialVelocity.position;
            averageAngularVelocity = averageAngularVelocity * Quaternion.Inverse(initialVelocity.rotation);

            averageVelocity *= Time.fixedDeltaTime;
            Vector3 averageAngularVelocityVec3 = averageAngularVelocity.eulerAngles;
            averageAngularVelocityVec3 *= Time.fixedDeltaTime;



            if (averageVelocity.magnitude > 1.0f)
            {
                // We only want to apply the forces if we actually want to cause movement to the object
                // We're still disabling collisions though to allow for the physics system to fully control if necessary
                if (!Ignored)
                {
                    // Ignore collision after throwing so we don't knock the object
                    //foreach (var hand in _grabbingCandidates)
                    //{
                    //hand.IgnoreCollision(_rigid, 0f, 0.005f);
                    //}

                    _rigid.velocity = averageVelocity;
                    _rigid.angularVelocity = averageAngularVelocityVec3;

                    _rigid.velocity *= _throwVelocityMultiplierCurve.Evaluate(_rigid.velocity.magnitude);
                }
            }
        }

        private void ThrowingOnRelease()
        {
            if (_velocityQueue.Count < 2)
            {
                _rigid.velocity = Vector3.zero;
                _rigid.angularVelocity = Vector3.zero;
                return;
            }

            float windowEnd = Time.fixedTime - _windowDelay;
            float windowStart = windowEnd - _windowLength;

            // 0 occurs before 1,
            // start occurs before end.
            VelocitySample start0, start1;
            VelocitySample end0, end1;
            VelocitySample s0, s1;

            s1 = start0 = start1 = end0 = end1 = _velocityQueue.Dequeue();

            while (_velocityQueue.Count != 0)
            {
                s0 = s1;
                s1 = _velocityQueue.Dequeue();

                if (s0.time < windowStart && s1.time >= windowStart)
                {
                    start0 = s0;
                    start1 = s1;
                }

                if (s0.time < windowEnd && s1.time >= windowEnd)
                {
                    end0 = s0;
                    end1 = s1;

                    // We have assigned both start and end and can break out of the loop.
                    _velocityQueue.Clear();
                    break;
                }
            }

            VelocitySample start = VelocitySample.Interpolate(start0, start1, windowStart);
            VelocitySample end = VelocitySample.Interpolate(end0, end1, windowEnd);

            Vector3 interpolatedVelocity = ContactUtils.ToLinearVelocity(start.position,
                                                                           end.position,
                                                                           _windowLength);

            if (interpolatedVelocity.magnitude > 1.0f)
            {
                // We only want to apply the forces if we actually want to cause movement to the object
                // We're still disabling collisions though to allow for the physics system to fully control if necessary
                if (!Ignored)
                {
                    // Ignore collision after throwing so we don't knock the object
                    //foreach (var hand in _grabbingCandidates)
                    //{
                        //hand.IgnoreCollision(_rigid, 0f, 0.005f);
                    //}

                    _rigid.velocity = interpolatedVelocity;
                    _rigid.angularVelocity = ContactUtils.ToAngularVelocity(start.rotation,
                                                                                        end.rotation,
                                                                                        _windowLength);

                    _rigid.velocity *= _throwVelocityMultiplierCurve.Evaluate(_rigid.velocity.magnitude);
                }
            }
        }

        #endregion
    }
}