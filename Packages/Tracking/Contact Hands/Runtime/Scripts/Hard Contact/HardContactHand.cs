using System; 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class HardContactHand : ContactHand
    {
        private const int RESET_FRAME_COUNT = 2, TELEPORT_FRAME_COUNT = 5, RESET_FRAME_TELEPORT_COUNT = 10;

        private HardContactParent hardContactParent => contactParent as HardContactParent;

        private int _resetCounter = 2, _teleportFrameCount = 10;
        private int _layerMask = 0;
        private int _lastFrameTeleport;

        private bool _wasGrabbing, _justGhosted;
        private float _timeOnReset;
        internal float currentResetLerp { get { return _timeOnReset == 0 ? 1 : Mathf.InverseLerp(0.1f, 0.25f, Time.time - _timeOnReset); } }

        private float _overallFingerDisplacement = 0f, _averageFingerDisplacement = 0f, _contactFingerDisplacement = 0f;
        public float FingerDisplacement => _overallFingerDisplacement;
        public float FingerAverageDisplacement => _averageFingerDisplacement;
        public float FingerContactDisplacement => _contactFingerDisplacement;

        // Interpolate back in displacement values after the hand has just released
        private float _displacementGrabCooldown = 0.25f, _displacementGrabCooldownCurrent = 0f;

        public float DistanceFromDataHand
        {
            get
            {
                if (!tracked || dataHand == null || palmBone == null || palmBone.transform == null) return -1;
                return Vector3.Distance(palmBone.transform.position, dataHand.PalmPosition);
            }
        }

        #region Settings
        internal float currentPalmVelocity, currentPalmAngularVelocity;
        internal float currentPalmVelocityInterp, currentPalmWeightInterp;

        internal Vector3 computedPhysicsPosition;
        internal Quaternion computedPhysicsRotation;
        internal float computedHandDistance;

        internal float graspingWeight, currentPalmWeight;

        internal int[] grabbingFingers;
        internal float[] grabbingFingerDistances, fingerStiffness;
        #endregion

        internal override void BeginHand(Hand hand)
        {
            dataHand.CopyFrom(hand);
            if(!resetting)
            {
                _resetCounter = RESET_FRAME_COUNT;
                resetting = true;
                _teleportFrameCount = RESET_FRAME_TELEPORT_COUNT;
                
                ghosted = true;
                for (int i = 0; i < 32; i++)
                {
                    if (!Physics.GetIgnoreLayerCollision(contactManager.HandsLayer, i))
                    {
                        _layerMask = _layerMask | 1 << i;
                    }
                }
                gameObject.SetActive(true);
                palmBone.gameObject.SetActive(false);
            }
            else
            {
                _resetCounter--;
                switch (_resetCounter)
                {
                    case 1:
                        ResetHardContactHand();
                        break;
                    case 0:
                        CompleteReset();
                        break;
                }
            }
        }

        internal override void FinishHand()
        {
            tracked = false;
            resetting = false;
            ResetHardContactHand();
            gameObject.SetActive(false);
        }

        internal override void GenerateHandLogic()
        {
            grabbingFingerDistances = new float[FINGERS];
            grabbingFingers = new int[FINGERS];
            fingerStiffness = new float[FINGERS];

            GenerateHandObjects(typeof(HardContactBone));

            ((HardContactBone)palmBone).SetupBoneBody();
            // Set the colliders to ignore eachother
            foreach (var bone in bones)
            {
                ((HardContactBone)bone).SetupBoneBody();
                Physics.IgnoreCollision(palmBone.palmCollider, bone.boneCollider);

                foreach (var bone2 in bones)
                {
                    if (bone != bone2)
                    {
                        Physics.IgnoreCollision(bone.boneCollider, bone2.boneCollider);
                    }
                }
            }
        }

        private void ResetHardContactHand()
        {
            _lastFrameTeleport = Time.frameCount;
            ghosted = true;
            ChangeHandLayer(contactManager.HandsResetLayer);
            if (gameObject.activeInHierarchy)
            {
                CacheHandData(dataHand);

                currentPalmVelocity = hardContactParent.maxPalmVelocity;

                ((HardContactBone)palmBone).ResetPalm();

                for (int fingerIndex = 0; fingerIndex < FINGERS; fingerIndex++)
                {
                    fingerStiffness[fingerIndex] = hardContactParent.boneStiffness;
                    for (int jointIndex = 0; jointIndex < FINGER_BONES; jointIndex++)
                    {
                        Bone prevBone = dataHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
                        Bone bone = dataHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

                        int boneArrayIndex = fingerIndex * FINGER_BONES + jointIndex;

                        ((HardContactBone)bones[boneArrayIndex]).ResetBone(prevBone, bone);
                    }
                }
                palmBone.gameObject.SetActive(false);
                palmBone.gameObject.SetActive(true);
            }
        }

        private void CompleteReset()
        {
            ResetModifiedHand();
            _timeOnReset = Time.time;
            UpdateIterations();
            resetting = false;
            tracked = true;
        }

        internal override void PostFixedUpdateHandLogic()
        {
            _velocity = ((HardContactBone)palmBone).articulation.velocity;
            _angularVelocity = ((HardContactBone)palmBone).articulation.angularVelocity;
            CacheComputedPositions();
            CalculateDisplacementsAndLimits();
        }

        private void CacheComputedPositions()
        {
            computedPhysicsPosition = palmBone.transform.position;
            computedPhysicsRotation = palmBone.transform.rotation;
            computedHandDistance = Vector3.Distance(_oldDataPosition, computedPhysicsPosition);
        }

        private void CalculateDisplacementsAndLimits()
        {
            _overallFingerDisplacement = 0f;
            _averageFingerDisplacement = 0f;
            _contactFingerDisplacement = 0f;

            int contactingFingers = 0;

            ((HardContactBone)palmBone).UpdateBoneDisplacement();
            _overallFingerDisplacement += ((HardContactBone)palmBone).DisplacementAmount;
            if (palmBone.IsBoneContacting)
            {
                contactingFingers++;
            }

            bool contacting;
            bool hasFingerGrasped;
            for (int fingerIndex = 0; fingerIndex < FINGERS; fingerIndex++)
            {
                contacting = false;

                hasFingerGrasped = false;

                grabbingFingerDistances[fingerIndex] = 1f;

                grabbingFingers[fingerIndex] = -1;

                for (int jointIndex = FINGER_BONES - 1; jointIndex >= 0; jointIndex--)
                {
                    int boneArrayIndex = fingerIndex * FINGER_BONES + jointIndex;

                    Bone bone = dataHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1));

                    ((HardContactBone)bones[boneArrayIndex]).UpdateBoneDisplacement(bone);
                    _overallFingerDisplacement += ((HardContactBone)bones[boneArrayIndex]).DisplacementAmount;
                    if (bones[boneArrayIndex].IsBoneContacting)
                    {
                        contacting = true;
                    }

                    hasFingerGrasped = ((HardContactBone)bones[boneArrayIndex]).CalculateGrabbingLimits(hasFingerGrasped);
                }
                if (contacting)
                {
                    contactingFingers++;
                }
            }

            if (IsGrabbing)
            {
                _displacementGrabCooldownCurrent = _displacementGrabCooldown;
            }

            _averageFingerDisplacement = _overallFingerDisplacement / (FINGERS * FINGER_BONES);
            if (contactingFingers > 0)
            {
                _contactFingerDisplacement = _overallFingerDisplacement * Mathf.Max(6 - contactingFingers, 1);
                if (IsGrabbing)
                {
                    _contactFingerDisplacement = 0f;
                }
                else if (_displacementGrabCooldown > 0)
                {
                    _displacementGrabCooldownCurrent -= Time.fixedDeltaTime;
                    if (_displacementGrabCooldownCurrent <= 0)
                    {
                        _displacementGrabCooldownCurrent = 0f;
                    }
                    _contactFingerDisplacement = Mathf.Lerp(_contactFingerDisplacement, 0, Mathf.InverseLerp(0.5f, 1.0f, (_displacementGrabCooldownCurrent / _displacementGrabCooldown).EaseOut()));
                }
            }
        }

        private void UpdateIterations()
        {
            ((HardContactBone)palmBone).UpdateIterations();
            for (int i = 0; i < bones.Length; i++)
            {
                ((HardContactBone)bones[i]).UpdateIterations();
            }
        }

        protected override void UpdateHandLogic(Hand hand)
        {
            // Fix the hand if it gets into a bad situation by teleporting and holding in place until its bad velocities disappear
            HandleTeleportingHands();

            if(!IsGrabbing && _wasGrabbing)
            {
                _timeOnReset = Time.time;
            }

            _wasGrabbing = IsGrabbing;
        }

        private void HandleTeleportingHands()
        {
            _justGhosted = false;
            // Fix the hand if it gets into a bad situation by teleporting and holding in place until its bad velocities disappear
            if (DistanceFromDataHand > hardContactParent.teleportDistance ||
                AreBonesRotatedBeyondThreshold())
            {
                ResetHardContactHand();
                // Don't need to wait for the hand to reset as much here
                _teleportFrameCount = TELEPORT_FRAME_COUNT;

                ghosted = true;
                _justGhosted = true;
            }

            if (Time.frameCount - _lastFrameTeleport >= _teleportFrameCount && ghosted && !IsCloseToObject)
            {
                ChangeHandLayer(contactManager.HandsLayer);

                ghosted = false;
            }
        }

        /// <summary>
        /// Have the bones been forced beyond acceptable rotation amounts by external forces?
        /// </summary>
        private bool AreBonesRotatedBeyondThreshold(float eulerThreshold = 20f)
        {
            if (IsGrabbing)
            {
                return false;
            }

            for (int fingerIndex = 0; fingerIndex < FINGERS; fingerIndex++)
            {
                for (int jointIndex = 1; jointIndex < FINGER_BONES; jointIndex++)
                {
                    int boneArrayIndex = fingerIndex * FINGER_BONES + jointIndex;

                    // Skip finger if a overlapBone's contacting
                    if (bones[boneArrayIndex].IsBoneContacting)
                    {
                        break;
                    }

                    if (((HardContactBone)bones[boneArrayIndex]).BoneOverRotationCheck())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        #region Output Hand
        protected override void ProcessOutputHand(ref Hand modifiedHand)
        {
            modifiedHand.SetTransform(palmBone.transform.position, palmBone.transform.rotation);
            int boneInd = 0;
            Vector3 posA, posB;

            float r;
            if (!_justGhosted)
            {
                for (int i = 0; i < modifiedHand.Fingers.Count; i++)
                {
                    Bone b = modifiedHand.Fingers[i].bones[0];
                    PhysExts.ToWorldSpaceCapsule(bones[boneInd].boneCollider, out posA, out posB, out r);
                    b.NextJoint = posB;

                    for (int j = 1; j < modifiedHand.Fingers[i].bones.Length; j++)
                    {
                        b = modifiedHand.Fingers[i].bones[j];
                        PhysExts.ToWorldSpaceCapsule(bones[boneInd].boneCollider, out posA, out posB, out r);
                        b.PrevJoint = posB;
                        b.NextJoint = posA;
                        b.Width = r;
                        b.Center = (b.PrevJoint + b.NextJoint) / 2f;
                        b.Direction = (b.NextJoint - b.PrevJoint).normalized;
                        b.Length = Vector3.Distance(posA, posB);
                        b.Rotation = bones[boneInd].transform.rotation;
                        boneInd++;
                    }
                    modifiedHand.Fingers[i].TipPosition = GetTipPosition(i);
                }
            }

            modifiedHand.WristPosition = palmBone.transform.position - (palmBone.transform.rotation * Quaternion.Inverse(dataHand.Rotation) * (dataHand.PalmPosition - dataHand.WristPosition));

            Vector3 direction = Vector3.Lerp(modifiedHand.Arm.Direction, dataHand.Arm.Direction, Mathf.Lerp(1.0f, 0.1f, currentPalmWeight));

            modifiedHand.Arm.PrevJoint = modifiedHand.WristPosition + (-dataHand.Arm.Length * direction);
            modifiedHand.Arm.NextJoint = modifiedHand.WristPosition;
            modifiedHand.Arm.Center = (modifiedHand.Arm.PrevJoint + modifiedHand.Arm.NextJoint) / 2f;
            modifiedHand.Arm.Length = Vector3.Distance(modifiedHand.Arm.PrevJoint, modifiedHand.Arm.NextJoint);
            modifiedHand.Arm.Direction = (modifiedHand.WristPosition - modifiedHand.Arm.PrevJoint).normalized;
            modifiedHand.Arm.Rotation = Quaternion.LookRotation(modifiedHand.Arm.Direction, -dataHand.PalmNormal);
            modifiedHand.Arm.Width = dataHand.Arm.Width;

            // Add the radius of one of the palm edges to the box collider to get the correct width
            modifiedHand.PalmWidth = palmBone.palmCollider.size.y + palmBone.palmEdgeColliders[1].radius;
            modifiedHand.Confidence = dataHand.Confidence;
            modifiedHand.Direction = dataHand.Direction;
            modifiedHand.FrameId = dataHand.FrameId;
            modifiedHand.Id = dataHand.Id;
            modifiedHand.GrabStrength = CalculateGrabStrength(modifiedHand);
            modifiedHand.PinchStrength = CalculatePinchStrength(modifiedHand, palmBone.palmCollider.size.y);
            modifiedHand.PinchDistance = CalculatePinchDistance(modifiedHand);
            modifiedHand.PalmVelocity = (palmBone.transform.position - _oldContactPosition) / Time.fixedDeltaTime;
            modifiedHand.TimeVisible = dataHand.TimeVisible;
        }

        public Vector3 GetTipPosition(int index)
        {
            if (index > 4)
            {
                return Vector3.zero;
            }
            PhysExts.ToWorldSpaceCapsule(bones[(FINGER_BONES * index) + FINGER_BONES - 1].boneCollider, out Vector3 outPos, out var outB, out var outRadius);
            outPos += (outPos - outB).normalized * outRadius;
            return outPos;
        }

        // Copied from OpenXRLeapProvider
        private float CalculatePinchStrength(Hand hand, float handScale)
        {
            // Get the thumb position.
            var thumbTipPosition = hand.GetThumb().TipPosition;

            // Compute the distance midpoints between the thumb and the each finger and find the smallest.
            var minDistanceSquared = float.MaxValue;

            // Iterate through the fingers, skipping the thumb.
            for (var i = 1; i < hand.Fingers.Count; ++i)
            {
                var distanceSquared = (hand.Fingers[i].TipPosition - thumbTipPosition).sqrMagnitude;
                minDistanceSquared = Mathf.Min(distanceSquared, minDistanceSquared);
            }

            // Compute the pinch strength. Magic values taken from existing LeapC implementation (scaled to metres)
            float distanceZero = 0.0600f * handScale;
            float distanceOne = 0.0220f * handScale;
            return Mathf.Clamp01((Mathf.Sqrt(minDistanceSquared) - distanceZero) / (distanceOne - distanceZero));
        }

        private float CalculateBoneDistanceSquared(Bone boneA, Bone boneB)
        {
            // Denormalize directions to bone length.
            var boneAJoint = boneA.PrevJoint;
            var boneBJoint = boneB.PrevJoint;
            var boneADirection = boneA.Direction * boneA.Length;
            var boneBDirection = boneB.Direction * boneB.Length;

            // Compute the minimum (squared) distance between two bones.
            var diff = boneBJoint - boneAJoint;
            var d1 = Vector3.Dot(boneADirection, diff);
            var d2 = Vector3.Dot(boneBDirection, diff);
            var a = boneADirection.sqrMagnitude;
            var b = Vector3.Dot(boneADirection, boneBDirection);
            var c = boneBDirection.sqrMagnitude;
            var det = b * b - a * c;
            var t1 = Mathf.Clamp01((b * d2 - c * d1) / det);
            var t2 = Mathf.Clamp01((a * d2 - b * d1) / det);
            var pa = boneAJoint + t1 * boneADirection;
            var pb = boneBJoint + t2 * boneBDirection;
            return (pa - pb).sqrMagnitude;
        }

        private float CalculatePinchDistance(Hand hand)
        {
            // Get the farthest 2 segments of thumb and index finger, respectively, and compute distances.
            var minDistanceSquared = float.MaxValue;
            for (var thumbBoneIndex = 2; thumbBoneIndex < hand.GetThumb().bones.Length; ++thumbBoneIndex)
            {
                for (var indexBoneIndex = 2; indexBoneIndex < hand.GetIndex().bones.Length; ++indexBoneIndex)
                {
                    var distanceSquared = CalculateBoneDistanceSquared(
                        hand.GetThumb().bones[thumbBoneIndex],
                        hand.GetIndex().bones[indexBoneIndex]);
                    minDistanceSquared = Mathf.Min(distanceSquared, minDistanceSquared);
                }
            }

            // Return the pinch distance, converted to millimeters to match other providers.
            return Mathf.Sqrt(minDistanceSquared) * 1000.0f;
        }

        float CalculateGrabStrength(Hand hand)
        {
            // magic numbers so it approximately lines up with the leap results
            const float bendZero = 0.25f;
            const float bendOne = 0.85f;

            // Find the minimum bend angle for the non-thumb fingers.
            float minBend = float.MaxValue;
            for (int finger_idx = 1; finger_idx < 5; finger_idx++)
            {
                minBend = Mathf.Min(hand.GetFingerStrength(finger_idx), minBend);
            }

            // Return the grab strength.
            return Mathf.Clamp01((minBend - bendZero) / (bendOne - bendZero));
        }
        #endregion
    }
}