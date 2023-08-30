using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    [System.Serializable]
    public class GrabHelperObject
    {
        private const float GRAB_COOLDOWNTIME = 0.025f;
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
            Grasp
        }
        public State GraspState { get; private set; } = State.Idle;

        public HashSet<ContactBone> BoneHash => _boneHash;
        private HashSet<ContactBone> _boneHash = new HashSet<ContactBone>();
        public int TotalBones { get { return _boneHash.Count; } }

        /// <summary>
        /// Bones is a hand indexed dictionary, of eligible to grasp physics bone hashsets of the hand's fingers 
        /// </summary>
        public Dictionary<ContactHand, HashSet<ContactBone>[]> Bones => _bones;
        private Dictionary<ContactHand, HashSet<ContactBone>[]> _bones = new Dictionary<ContactHand, HashSet<ContactBone>[]>();

        public List<ContactHand> GraspingCandidates => _graspingCandidates;
        private List<ContactHand> _graspingCandidates = new List<ContactHand>();
        private List<bool> _graspingCandidatesContact = new List<bool>();

        public List<ContactHand> GraspingHands => _graspingHands;
        private List<ContactHand> _graspingHands = new List<ContactHand>();

        private Dictionary<ContactHand, GraspValues> _graspingValues = new Dictionary<ContactHand, GraspValues>();

        [System.Serializable]
        public class GraspValues
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
            /// the bones in this hand are facing other grasping applicable bones in a different hand 
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

        public bool Ignored { get { return false; } }
        //private PhysicsIgnoreHelpers _ignored = null;

        /// <summary>
        /// Returns true if the provided hand is grasping an object.
        /// Requires thumb or palm grasping, and at least one (non-thumb) finger
        /// </summary>
        /// <param name="hand">The hand to check</param>
        /// <returns>If the provided hand is grasping</returns>
        private bool Grasped(ContactHand hand)
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
        /// Returns true if the finger on the provided hand is grasping, and false if not
        /// </summary>
        /// <param name="hand">The hand to check</param>
        /// <param name="finger">The finger to check</param>
        /// <returns>If the finger on the provided hand is grasped</returns>
        public bool Grasped(ContactHand hand, int finger)
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
            //_ignored = rigid.GetComponentInChildren<PhysicsIgnoreHelpers>();
            Manager = manager;
        }

        public void ReleaseHelper()
        {
            foreach (var item in _graspingValues)
            {
                SetBoneGrasping(item, false);

            }
            GraspState = State.Idle;
            _graspingValues.Clear();

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
            _graspingCandidates.Clear();
            _graspingCandidatesContact.Clear();
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
            if (!_graspingCandidates.Contains(hand))
            {
                if (GraspState == State.Idle)
                {
                    GraspState = State.Hover;
                }
                _graspingCandidates.Add(hand);
                _graspingCandidatesContact.Add(false);
                //if (_rigid.TryGetComponent<IContactHandHover>(out var ContactHandHover))
                //{
                //    ContactHandHover.OnHandHover(hand);
                //}
            }
        }

        public void RemoveHand(ContactHand hand)
        {
            if (_graspingCandidates.Contains(hand))
            {
                _graspingCandidatesContact.RemoveAt(_graspingCandidates.IndexOf(hand));
                _graspingCandidates.Remove(hand);
                //if (_rigid != null && _rigid.TryGetComponent<IContactHandHover>(out var ContactHandHover))
                //{
                //    ContactHandHover.OnHandHoverExit(hand);
                //}
            }
        }

        public void ReleaseObject()
        {
            GraspState = State.Hover;
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

        public State UpdateHelper()
        {
            UpdateHands();

            if (_boneHash.Count == 0 && GraspState != State.Grasp)
            {
                // If we don't then and we're not grasping then just return
                return GraspState = State.Hover;
            }

            GraspingContactCheck();

            switch (GraspState)
            {
                case State.Hover:
                    if (_boneHash.Count > 0)
                    {
                        GraspState = State.Contact;
                    }
                    break;
                case State.Grasp:
                    UpdateGraspingValues();
                    if (_graspingHands.Count > 0)
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

            return GraspState;
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

            foreach (var hand in _graspingCandidates)
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
            for (int i = 0; i < _graspingCandidates.Count; i++)
            {
                if (_graspingCandidatesContact[i] != _graspingCandidates[i].IsContacting)
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
                    _graspingCandidatesContact[i] = _graspingCandidates[i].IsContacting;
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

        private void GraspingContactCheck()
        {
            //Reset grab bools
            foreach (var graspValue in _graspingValues)
            {
                graspValue.Value.handGrabbing = false;
                graspValue.Value.facingOppositeHand = false;
            }

            foreach (var hand in _graspingCandidates)
            {
                if (_graspingHands.Contains(hand))
                {
                    continue;
                }

                if (!_graspingValues.ContainsKey(hand))
                {
                    _graspingValues.Add(hand, new GraspValues());

                    for (int j = 1; j < 5; j++)
                    {
                        if (hand.dataHand.GetFingerPinchDistance(j) <= REQUIRED_PINCH_DISTANCE)
                        {
                            _graspingValues[hand].waitingForInitialUnpinch = true;
                            break;
                        }
                    }
                }

                for (int i = 0; i < 5; i++)
                {
                    if (Grasped(hand, i) && Manager.FingerStrengths[hand][i] > (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH))
                    {
                        _graspingValues[hand].fingerStrength[i] = Manager.FingerStrengths[hand][i];
                        if (_graspingValues[hand].originalFingerStrength[i] == -1)
                        {
                            _graspingValues[hand].originalFingerStrength[i] = Manager.FingerStrengths[hand][i];
                        }
                    }
                    else
                    {
                        _graspingValues[hand].originalFingerStrength[i] = -1;
                        _graspingValues[hand].fingerStrength[i] = -1;
                    }
                }

                if (Grasped(hand))
                {
                    // if two fingers are greater than 20% of their remaining distance when they grasped difference
                    int c = 0;
                    for (int i = 0; i < 5; i++)
                    {
                        if (_graspingValues[hand].fingerStrength[i] == -1)
                            continue;

                        if (i == 0)
                        {
                            bool unpinched = true;
                            for (int j = 1; j < 5; j++)
                            {

                                //dont allow if pinching on enter

                                if (hand.dataHand.GetFingerPinchDistance(j) <= REQUIRED_PINCH_DISTANCE)
                                {
                                    if (_graspingValues[hand].waitingForInitialUnpinch)
                                    {
                                        unpinched = false;
                                        continue;
                                    }

                                    // Make very small pinches more sticky
                                    _graspingValues[hand].fingerStrength[j] *= 0.85f;
                                    if (c == 0)
                                    {
                                        c = 2;
                                        _graspingValues[hand].fingerStrength[0] *= 0.85f;
                                    }
                                    else
                                    {
                                        c++;
                                    }
                                }
                            }

                            if (unpinched)
                            {
                                _graspingValues[hand].waitingForInitialUnpinch = false;
                            }

                            if (c > 0 || _graspingValues[hand].waitingForInitialUnpinch)
                            {
                                break;
                            }
                        }

                        // if waiting for ungrab AND no contact 
                        // ungrab == required ungrab strength 2

                        if (_graspingValues[hand].fingerStrength[i] > _graspingValues[hand].originalFingerStrength[i] + ((1 - _graspingValues[hand].originalFingerStrength[i]) * REQUIRED_ENTRY_STRENGTH))
                        {
                            c++;
                        }
                    }

                    if (c >= 2)
                    {
                        _graspingValues[hand].handGrabbing = true;
                        RegisterGraspingHand(hand);
                    }
                }
            }

            CheckForBonesFacingEachOther();
        }

        private void CheckForBonesFacingEachOther()
        {
            HashSet<Tuple<int, int>> checkedPairs = new HashSet<Tuple<int, int>>();

            foreach (ContactBone b1 in BoneHash)
            {
                if (b1.GrabbableDirections.TryGetValue(_rigid, out var grabbableDirectionsB1))
                {
                    int idB1 = b1.GetInstanceID();
                    foreach (ContactBone b2 in BoneHash)
                    {
                        // Don't compare against the same bone
                        if (b1 == b2) continue;

                        int idB2 = b2.GetInstanceID();

                        Tuple<int, int> idPair1 = new Tuple<int, int>(idB1, idB2);
                        Tuple<int, int> idPair2 = new Tuple<int, int>(idB2, idB1);

                        // Don't compare against a pair that has already been compared against
                        if (checkedPairs.Contains(idPair1) || checkedPairs.Contains(idPair2)) continue;

                        // Register this pair of bones as checked, so that we don't check against it again
                        checkedPairs.Add(idPair1);

                        if (b2.GrabbableDirections.TryGetValue(_rigid, out var grabbableDirectionsB2))
                        {
                            foreach (var directionPairB1 in grabbableDirectionsB1)
                            {
                                //If the grabbable direction is facing away from the bone forward direction, disregard it
                                if (Vector3.Dot(directionPairB1.Value.direction, -b1.transform.up) < FORWARD_DIRECTION_DOT) continue;

                                foreach (var directionPairB2 in grabbableDirectionsB2)
                                {
                                    //If the grabbable direction is facing away from the bone forward direction, disregard it
                                    if (Vector3.Dot(directionPairB2.Value.direction, -b2.transform.up) < FORWARD_DIRECTION_DOT) continue;

                                    float dot = Vector3.Dot(directionPairB1.Value.direction, directionPairB2.Value.direction);

                                    //If the two bones are facing opposite directions (i.e. pushing towards each other), they're grabbing
                                    if (dot < GRABBABLE_DIRECTIONS_DOT)
                                    {
                                        if (b1.contactHand == b2.contactHand)
                                        {
                                            _graspingValues[b1.contactHand].handGrabbing = true;
                                            _graspingValues[b2.contactHand].handGrabbing = true;
                                        }
                                        else
                                        {
                                            _graspingValues[b1.contactHand].facingOppositeHand = true;
                                            _graspingValues[b2.contactHand].facingOppositeHand = true;
                                        }

                                        if (!_graspingHands.Contains(b1.contactHand))
                                        {
                                            RegisterGraspingHand(b1.contactHand);
                                        }

                                        if (!_graspingHands.Contains(b2.contactHand))
                                        {
                                            RegisterGraspingHand(b2.contactHand);
                                        }
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void RegisterGraspingHand(ContactHand hand)
        {
            if (_graspingHands.Contains(hand)) return;

            if (!Ignored && _graspingHands.Count == 0)
            {
                // Store the original rigidbody variables
                _oldKinematic = _rigid.isKinematic;
                _rigid.isKinematic = false;
            }

            _graspingHands.Add(hand);

            if (_graspingHands.Count > 0)
            {
                GraspState = State.Grasp;
            }
            _graspingValues[hand].offset = _rigid.position - hand.palmBone.transform.position;
            _graspingValues[hand].rotationOffset = Quaternion.Inverse(hand.palmBone.transform.rotation) * _rigid.rotation;
            _graspingValues[hand].originalHandRotation = hand.palmBone.transform.rotation;
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

        private void UpdateGraspingValues()
        {
            foreach (KeyValuePair<ContactHand, GraspValues> graspedHand in _graspingValues)
            {
                // Check if this hand was grasping and is now not grasping
                if (_graspingHands.Count > 1
                    && !graspedHand.Value.handGrabbing
                    && !graspedHand.Value.facingOppositeHand
                    // Hand has moved significantly far away from the object
                    && (_rigid.position - graspedHand.Key.palmBone.transform.position).sqrMagnitude > graspedHand.Value.offset.sqrMagnitude * 1.5f)
                {
                    SetBoneGrasping(graspedHand, false);
                    _graspingHands.Remove(graspedHand.Key);
                    //if (_rigid.TryGetComponent<IContactHandGrab>(out var ContactHandGrab))
                    //{
                    //    ContactHandGrab.OnHandGrabExit(graspedHand.Key);
                    //}
                    continue;
                }

                int c = 0;
                for (int i = 0; i < 5; i++)
                {
                    // Was the finger not contacting before?
                    if (graspedHand.Value.fingerStrength[i] == -1)
                    {
                        if (Manager.FingerStrengths[graspedHand.Key][i] > (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) && Grasped(graspedHand.Key, i))
                        {
                            // Store the strength value on contact
                            graspedHand.Value.fingerStrength[i] = Manager.FingerStrengths[graspedHand.Key][i];
                        }
                    }
                    else
                    {
                        // If the finger was contacting but has uncurled by the exit percentage then it is no longer "grabbed"
                        if (Manager.FingerStrengths[graspedHand.Key][i] < (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) || graspedHand.Value.fingerStrength[i] * (1 - (i == 0 ? REQUIRED_THUMB_EXIT_STRENGTH : REQUIRED_EXIT_STRENGTH)) >= Manager.FingerStrengths[graspedHand.Key][i])
                        {
                            graspedHand.Value.fingerStrength[i] = -1;
                        }
                    }

                    if (graspedHand.Value.fingerStrength[i] != -1)
                    {
                        c++;
                    }
                }

                // If we've got two fingers curled, the hand was grabbing in the grab contact checks, or the hand is facing the other, then we grab
                if (c >= 2 || graspedHand.Value.handGrabbing || graspedHand.Value.facingOppositeHand)
                {
                    SetBoneGrasping(graspedHand, true);
                    continue;
                }

                // One final check to see if the original data hand has bones inside of the object to retain grasping during physics updates
                bool data = DataHandIntersection(graspedHand.Key);

                if (!data)
                {
                    SetBoneGrasping(graspedHand, false);
                    _graspingValues[graspedHand.Key].waitingForInitialUnpinch = true;
                    _graspingHands.Remove(graspedHand.Key);
                    //if (_rigid.TryGetComponent<IContactHandGrab>(out var ContactHandGrab))
                    //{
                    //    ContactHandGrab.OnHandGrabExit(graspedHand.Key);
                    //}
                }
                else
                {
                    SetBoneGrasping(graspedHand, true);
                }
            }
        }

        private void SetBoneGrasping(KeyValuePair<ContactHand, GraspValues> pair, bool add)
        {
            if (add)
            {
                for (int j = 0; j < _bones[pair.Key].Length; j++)
                {
                    if (_bones[pair.Key][j].Count > 0)
                    {
                        foreach (var bone in pair.Key.bones)
                        {
                            if (bone.Finger == _bones[pair.Key][j].First().Finger)
                            {
                                bone.AddGrabbing(_rigid);
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
            if (_graspingHands.Count > 0)
            {
                /*
                 * Grasp priority
                 * Last hand to grab object
                 * Otherwise, last hand to be added to graspingHands
                 */

                ContactHand hand = null;

                for (int i = _graspingHands.Count - 1; i > 0; i--)
                {
                    if (!_graspingValues[_graspingHands[i]].handGrabbing)
                    {
                        hand = _graspingHands[i];
                        break;
                    }
                }

                if (hand == null)
                {
                    hand = _graspingHands[_graspingHands.Count - 1];
                }

                if (hand.dataHand != null)
                {
                    _newPosition = hand.palmBone.transform.position + (hand.Velocity * Time.fixedDeltaTime) + (hand.palmBone.transform.rotation * Quaternion.Inverse(_graspingValues[hand].originalHandRotation) * _graspingValues[hand].offset);
                    _newRotation = hand.palmBone.transform.rotation * Quaternion.Euler(hand.AngularVelocity * Time.fixedDeltaTime) * _graspingValues[hand].rotationOffset;
                }
            }
        }

        public void Gizmo()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_newPosition, 0.01f);
            Gizmos.color = Color.white;

            foreach (ContactHand hand in _graspingHands)
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
        private float _windowLength = 0.045f, _windowDelay = 0.015f;

        // Throwing curve
        private AnimationCurve _throwVelocityMultiplierCurve = new AnimationCurve(
                                                            new Keyframe(0.0F, 0.7F, 0, 0),
                                                            new Keyframe(3.0F, 1.0F, 0, 0));

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
                this.time = Time.fixedTime;
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

                // Dequeue conservatively if the oldest velocity is more than 4 frames later
                // than the start of the window.
                if (oldestVelocity.time + (Time.fixedDeltaTime * 4) < Time.fixedTime
                                                                      - _windowLength
                                                                      - _windowDelay)
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

            s0 = s1 = start0 = start1 = end0 = end1 = _velocityQueue.Dequeue();

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
                foreach (var hand in _graspingCandidates)
                {
                    //hand.IgnoreCollision(_rigid, 0f, 0.005f);
                }
                // We only want to apply the forces if we actually want to cause movement to the object
                // We're still disabling collisions though to allow for the physics system to fully control if necessary
                if (!Ignored)
                {
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