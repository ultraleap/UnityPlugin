using Leap.Interaction.Internal.InteractionEngineUtility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    [System.Serializable]
    public class PhysicsGraspHelper
    {
        private const float GRAB_COOLDOWNTIME = 0.025f;
        private const float MINIMUM_STRENGTH = 0.25f, MINIMUM_THUMB_STRENGTH = 0.2f;
        private const float REQUIRED_ENTRY_STRENGTH = 0.15f, REQUIRED_EXIT_STRENGTH = 0.05f, REQUIRED_THUMB_EXIT_STRENGTH = 0.1f, REQUIRED_PINCH_DISTANCE = 0.018f;

        public enum State
        {
            Idle,
            Contact,
            Grasp
        }

        public State GraspState { get; private set; } = State.Idle;

        public HashSet<PhysicsBone> BoneHash => _boneHash;
        private HashSet<PhysicsBone> _boneHash = new HashSet<PhysicsBone>();
        public int TotalBones { get { return _boneHash.Count; } }

        public Dictionary<PhysicsHand, HashSet<PhysicsBone>[]> Bones => _bones;
        private Dictionary<PhysicsHand, HashSet<PhysicsBone>[]> _bones = new Dictionary<PhysicsHand, HashSet<PhysicsBone>[]>();

        public HashSet<PhysicsHand> GraspingCandidates => _graspingCandidates;
        private HashSet<PhysicsHand> _graspingCandidates = new HashSet<PhysicsHand>();

        public List<PhysicsHand> GraspingHands => _graspingHands;
        private List<PhysicsHand> _graspingHands = new List<PhysicsHand>();

        private Dictionary<PhysicsHand, GraspValues> _graspingValues = new Dictionary<PhysicsHand, GraspValues>();

        private List<GraspValues> _valuesD = new List<GraspValues>();

        [System.Serializable]
        public class GraspValues
        {
            public float[] fingerStrength = new float[5];
            public float[] originalFingerStrength = new float[5];
            public Vector3[] tipPositions = new Vector3[5];
            public Matrix4x4 mat;
            public Vector3 offset;
            public Quaternion originalHandRotation, rotationOffset;
        }

        public PhysicsProvider Manager { get; private set; }

        public bool WasGrasped { get; private set; }
        private bool _justGrasped = false;
        private Rigidbody _rigid;
        public Rigidbody Rigidbody => _rigid;
        private List<Collider> _colliders = new List<Collider>();
        public Pose previousPose;
        public Pose previousPalmPose;

        private Vector3 _maxVelocityLerped = Vector3.zero;
        private float _releaseTimer = 0;

        private Vector3 _newPosition;
        private Quaternion _newRotation;

        private bool _oldKinematic, _oldGravity;

        // This means we can have bones stay "attached" for a small amount of time
        private List<PhysicsBone> _boneCooldownItems = new List<PhysicsBone>();
        private List<float> _boneCooldownTime = new List<float>();

        public bool Ignored { get { return _ignored != null; } }
        private PhysicsIgnoreHelpers _ignored = null;

        // Check we got thumb or palm and at least one finger
        private bool Grasped(PhysicsHand hand)
        {
            if (_bones.TryGetValue(hand, out var bones))
            {
                // These values are limited by the EligibleBones
                return (bones[0].Count > 0 || bones[5].Count > 0) && // A thumb or palm bone
                    ((bones[1].Count > 0) || // The intermediate or distal of the index
                    (bones[2].Count > 0) || // The distal of the middle
                    (bones[3].Count > 0) || // The distal of the ring
                    (bones[4].Count > 0)); // The distal of the pinky
            }
            return false;
        }

        private bool Grasped(PhysicsHand hand, int finger)
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
                        return bones[finger].Count > 0 && bones[finger].Any(x => x.Joint != 0);

                    case 3:
                    case 4:
                        return bones[finger].Count > 0 && bones[finger].Any(x => x.Joint == 2);
                }
                return false;
            }
            return false;
        }

        public PhysicsGraspHelper(Rigidbody rigid, PhysicsProvider manager)
        {
            _rigid = rigid;
            _oldKinematic = _rigid.isKinematic;
            _oldGravity = _rigid.useGravity;
            _colliders = rigid.GetComponentsInChildren<Collider>(true).ToList();
            _ignored = rigid.GetComponentInChildren<PhysicsIgnoreHelpers>();
            Manager = manager;
        }

        public void ReleaseHelper()
        {
            foreach (var item in _graspingValues)
            {
                SetBoneGrasping(item, false);
            }
            GraspState = State.Idle;
            _valuesD.Clear();
            _graspingValues.Clear();
            _graspingCandidates.Clear();
            if (_rigid != null)
            {
                _rigid.isKinematic = _oldKinematic;
                _rigid.useGravity = _oldGravity;
            }
            _boneHash.Clear();
            foreach (var pair in _bones)
            {
                for (int j = 0; j < pair.Value.Length; j++)
                {
                    foreach (var bone in pair.Value[j])
                    {
                        bone.RemoveContacting(_rigid);
                    }
                    pair.Value[j].Clear();
                }
            }
            _bones.Clear();
        }

        public void AddHand(PhysicsHand hand)
        {
            _graspingCandidates.Add(hand);
        }

        public void RemoveHand(PhysicsHand hand)
        {
            _graspingCandidates.Remove(hand);
        }

        public void UpdateBones(HashSet<PhysicsBone> bones)
        {
            // Remove cooldowns if bone has contacted
            for (int i = 0; i < _boneCooldownItems.Count; i++)
            {
                if (bones.Contains(_boneCooldownItems[i]))
                {
                    _boneCooldownItems.RemoveAt(i);
                    _boneCooldownTime.RemoveAt(i);
                    i--;
                }
            }

            // Apply cooldowns if bones have been removed
            _boneHash.RemoveWhere((x) => { return RemoveOldBones(x, bones); });

            foreach (var bone in bones)
            {
                // Gate the bones coming in and don't dupe the events for boneHash
                if (EligibleBone(bone) && !_boneHash.Contains(bone))
                {
                    _boneHash.Add(bone);
                    bone.AddContacting(_rigid);
                    if (_bones.TryGetValue(bone.Hand, out HashSet<PhysicsBone>[] storedBones))
                    {
                        storedBones[bone.Finger].Add(bone);
                    }
                    else
                    {
                        _bones.Add(bone.Hand, new HashSet<PhysicsBone>[PhysicsHand.Hand.FINGERS + 1]);
                        for (int i = 0; i < _bones[bone.Hand].Length; i++)
                        {
                            _bones[bone.Hand][i] = new HashSet<PhysicsBone>();
                            if (bone.Finger == i)
                            {
                                _bones[bone.Hand][i].Add(bone);
                            }
                        }
                    }
                }
            }
        }

        private bool EligibleBone(PhysicsBone bone)
        {
            return bone.Finger == 5 || bone.Joint > 0;
        }

        private bool RemoveOldBones(PhysicsBone bone, HashSet<PhysicsBone> bones)
        {
            if (!bones.Contains(bone))
            {
                int ind = _boneCooldownItems.IndexOf(bone);
                Debug.DrawLine(bone.transform.position, bone.transform.position + (Vector3.up * 0.01f), Color.green, 0.1f);
                if (ind != -1)
                {
                    _boneCooldownTime[ind] = GRAB_COOLDOWNTIME;
                }
                else
                {
                    _boneCooldownItems.Add(bone);
                    _boneCooldownTime.Add(GRAB_COOLDOWNTIME);
                }
                return true;
            }
            return false;
        }

        public void ReleaseObject()
        {
            GraspState = State.Idle;
            _releaseTimer = GRAB_COOLDOWNTIME;
            if (Manager.HelperMovesObjects)
            {
                _rigid.isKinematic = _oldKinematic;
                _rigid.useGravity = _oldGravity;
            }
        }

        public State UpdateHelper()
        {
            if (GraspState == State.Idle)
            {
                // Trying to prevent pinging....
                if (_releaseTimer > 0)
                {
                    _releaseTimer -= Time.fixedDeltaTime;
                    _rigid.velocity = Vector3.ClampMagnitude(_rigid.velocity, _maxVelocityLerped.magnitude);
                }
            }

            // Check to see if we no longer have any bones
            UpdateRemovedBones();

            if (_boneHash.Count == 0 && GraspState != State.Grasp)
            {
                // If we don't then and we're not grasping then just return
                return GraspState = State.Idle;
            }

            GraspingContactCheck();

            switch (GraspState)
            {
                case State.Idle:
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
                        if (Manager.HelperMovesObjects)
                        {
                            MoveObject();
                        }
                        _justGrasped = false;
                    }
                    else
                    {
                        ReleaseObject();
                    }
                    break;
            }

            return GraspState;
        }

        private void GraspingContactCheck()
        {
            foreach (var hand in _graspingCandidates)
            {
                if (_graspingHands.Contains(hand))
                {
                    continue;
                }

                if (!_graspingValues.ContainsKey(hand))
                {
                    _graspingValues.Add(hand, new GraspValues());
                    _valuesD.Add(_graspingValues[hand]);
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
                            if (hand.GetOriginalLeapHand().PinchDistance / 1000f <= REQUIRED_PINCH_DISTANCE)
                            {
                                c = 2;
                                break;
                            }
                        }
                        if (_graspingValues[hand].fingerStrength[i] > _graspingValues[hand].originalFingerStrength[i] + ((1 - _graspingValues[hand].originalFingerStrength[i]) * REQUIRED_ENTRY_STRENGTH))
                        {
                            c++;
                        }
                        if (c == 2)
                            break;
                    }
                    if (c == 2)
                    {
                        if (Manager.HelperMovesObjects && _graspingHands.Count == 0)
                        {
                            _rigid.useGravity = false;
                            _rigid.isKinematic = false;
                        }
                        _graspingHands.Add(hand);
                        if (_graspingHands.Count > 0)
                        {
                            _justGrasped = true;
                            GraspState = State.Grasp;
                        }
                        _graspingValues[hand].offset = _rigid.position - hand.GetPhysicsHand().palmBone.transform.position;
                        _graspingValues[hand].rotationOffset = Quaternion.Inverse(hand.GetPhysicsHand().palmBone.transform.rotation) * _rigid.rotation;
                        _graspingValues[hand].originalHandRotation = hand.GetPhysicsHand().palmBone.transform.rotation;
                    }
                }
            }
        }

        private void UpdateRemovedBones()
        {
            for (int i = 0; i < _boneCooldownItems.Count; i++)
            {
                _boneCooldownTime[i] -= Time.fixedDeltaTime;
                if (_boneCooldownTime[i] <= 0)
                {
                    if (_bones.ContainsKey(_boneCooldownItems[i].Hand))
                    {
                        foreach (var item in _bones[_boneCooldownItems[i].Hand][_boneCooldownItems[i].Finger])
                        {
                            if (item == _boneCooldownItems[i])
                            {
                                item.RemoveGrasping(_rigid);
                                break;
                            }
                        }
                        _bones[_boneCooldownItems[i].Hand][_boneCooldownItems[i].Finger].Remove(_boneCooldownItems[i]);
                    }
                    _boneCooldownItems[i].RemoveContacting(_rigid);
                    _boneCooldownItems.RemoveAt(i);
                    _boneCooldownTime.RemoveAt(i);
                    i--;
                }
            }
        }

        private void UpdateGraspingValues()
        {
            foreach (var fist in _graspingValues)
            {
                if (_graspingHands.Count > 1 && (_rigid.position - fist.Key.GetPhysicsHand().palmBone.transform.position).sqrMagnitude > fist.Value.offset.sqrMagnitude * 1.5f)
                {
                    SetBoneGrasping(fist, false);
                    _graspingHands.Remove(fist.Key);
                    continue;
                }

                int c = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (fist.Value.fingerStrength[i] == -1)
                    {
                        if (Manager.FingerStrengths[fist.Key][i] > (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) && Grasped(fist.Key, i))
                        {
                            fist.Value.fingerStrength[i] = Manager.FingerStrengths[fist.Key][i];
                        }
                    }
                    else
                    {
                        if (Manager.FingerStrengths[fist.Key][i] < (i == 0 ? MINIMUM_THUMB_STRENGTH : MINIMUM_STRENGTH) || fist.Value.fingerStrength[i] * (1 - (i == 0 ? REQUIRED_THUMB_EXIT_STRENGTH : REQUIRED_EXIT_STRENGTH)) >= Manager.FingerStrengths[fist.Key][i])
                        {
                            fist.Value.fingerStrength[i] = -1;
                        }
                    }

                    if (fist.Value.fingerStrength[i] != -1)
                    {
                        c++;
                        if (c == 2)
                            break;
                    }
                }

                if (c == 2)
                {
                    SetBoneGrasping(fist, true);
                    continue;
                }

                bool data = DataHandIntersection(fist.Key);

                if (!data)
                {
                    SetBoneGrasping(fist, false);
                    _graspingHands.Remove(fist.Key);
                }
                else
                {
                    SetBoneGrasping(fist, true);
                }
            }
        }

        private void SetBoneGrasping(KeyValuePair<PhysicsHand, GraspValues> pair, bool add)
        {
            if (add)
            {
                for (int j = 0; j < _bones[pair.Key].Length; j++)
                {
                    if (_bones[pair.Key][j].Count > 0)
                    {
                        foreach (var item in pair.Key.GetPhysicsHand().jointBones)
                        {
                            if (item.Finger == _bones[pair.Key][j].First().Finger)
                            {
                                item.AddGrasping(_rigid);
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var item in pair.Key.GetPhysicsHand().jointBones)
                {
                    item.RemoveGrasping(_rigid);
                }
            }
        }

        private bool DataHandIntersection(PhysicsHand hand)
        {
            bool thumb = false, otherFinger = false;
            bool earlyQuit;

            Leap.Hand lHand = hand.GetOriginalLeapHand();

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

                            if (IsBoneWithinObject(lHand.Fingers[bone.Finger].bones[bone.Joint]))
                            {
                                thumb = true;
                                earlyQuit = true;
                            }
                            break;
                        case 1: // index
                            if (IsBoneWithinObject(lHand.Fingers[bone.Finger].bones[bone.Joint]))
                            {
                                otherFinger = true;
                                earlyQuit = true;
                            }
                            break;
                        case 2: // middle
                        case 3: // ring
                        case 4: // pinky
                            if (bone.Joint == 2 && IsBoneWithinObject(lHand.Fingers[bone.Finger].bones[bone.Joint]))
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

        private bool IsBoneWithinObject(Leap.Bone bone)
        {
            for (int i = 0; i < _colliders.Count; i++)
            {
                if (IsPointWithinCollider(_colliders[i], bone.NextJoint) || IsPointWithinCollider(_colliders[i], bone.Center))
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsPointWithinCollider(Collider collider, Vector3 point)
        {
            return collider.ClosestPoint(point) == point;
        }

        private void UpdateHandPositions()
        {
            if (_graspingHands.Count > 0)
            {
                // take the last hand as the priority
                PhysicsHand hand = _graspingHands[_graspingHands.Count - 1];
                if (hand.GetOriginalLeapHand() != null)
                {
                    _newPosition = hand.GetPhysicsHand().palmBone.transform.position + (hand.GetPhysicsHand().palmBone.transform.rotation * Quaternion.Inverse(_graspingValues[hand].originalHandRotation) * _graspingValues[hand].offset);
                    _newRotation = hand.GetPhysicsHand().palmBone.transform.rotation * _graspingValues[hand].rotationOffset;
                }
            }
        }

        public void Gizmo()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_newPosition, 0.01f);
            Gizmos.color = Color.white;

            foreach (PhysicsHand hand in _graspingHands)
            {
                if (hand.GetOriginalLeapHand() == null || !_bones.ContainsKey(hand))
                    continue;

                foreach (var bone in _boneHash)
                {
                    try
                    {
                        Gizmos.DrawSphere(hand.GetOriginalLeapHand().Fingers[bone.Finger].bones[bone.Joint].NextJoint, 0.005f);
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
            if (_rigid.useGravity)
            {
                _rigid.useGravity = false;
            }
            PhysicsMovement(_newPosition, _newRotation, _rigid, _justGrasped);
        }

        // Ripped from IE
        protected float _maxVelocity = 6F;
        private Vector3 _lastSolvedCoMPosition = Vector3.zero;
        protected AnimationCurve _strengthByDistance = new AnimationCurve(new Keyframe(0.0f, 1.0f, 0.0f, 0.0f),
                                                                             new Keyframe(0.08f, 0.3f, 0.0f, 0.0f));

        private void PhysicsMovement(Vector3 solvedPosition, Quaternion solvedRotation,
                               Rigidbody intObj, bool justGrasped)
        {
            Vector3 solvedCenterOfMass = solvedRotation * intObj.centerOfMass + solvedPosition;
            Vector3 currCenterOfMass = intObj.rotation * intObj.centerOfMass + intObj.position;

            Vector3 targetVelocity = PhysicsUtility.ToLinearVelocity(currCenterOfMass, solvedCenterOfMass, Time.fixedDeltaTime);

            Vector3 targetAngularVelocity = PhysicsUtility.ToAngularVelocity(intObj.rotation, solvedRotation, Time.fixedDeltaTime);

            // Clamp targetVelocity by _maxVelocity.
            float targetSpeedSqrd = targetVelocity.sqrMagnitude;
            if (targetSpeedSqrd > _maxVelocity * _maxVelocity)
            {
                float targetPercent = _maxVelocity / Mathf.Sqrt(targetSpeedSqrd);
                targetVelocity *= targetPercent;
                targetAngularVelocity *= targetPercent;
            }

            float followStrength = 1F;
            if (!justGrasped)
            {
                float remainingDistanceLastFrame = Vector3.Distance(_lastSolvedCoMPosition, currCenterOfMass);
                followStrength = _strengthByDistance.Evaluate(remainingDistanceLastFrame);
            }

            Vector3 lerpedVelocity = Vector3.Lerp(intObj.velocity, targetVelocity, followStrength);
            Vector3 lerpedAngularVelocity = Vector3.Lerp(intObj.angularVelocity, targetAngularVelocity, followStrength);

            intObj.velocity = lerpedVelocity;
            if (lerpedAngularVelocity.IsValid())
            {
                intObj.angularVelocity = lerpedAngularVelocity;
            }

            _lastSolvedCoMPosition = solvedCenterOfMass;
        }

        // It's more stable to use physics movement currently
        // Using this will result in large amounts of spaghetti
        private void KinematicMovement(Vector3 solvedPosition, Quaternion solvedRotation,
                               Rigidbody intObj)
        {
            intObj.MovePosition(solvedPosition);
            intObj.MoveRotation(solvedRotation);
        }
    }
}