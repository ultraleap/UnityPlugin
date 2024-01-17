/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.PhysicalHands
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

        private float _overallFingerDisplacement = 0f, _contactFingerDisplacement = 0f;
        internal float FingerDisplacement => _overallFingerDisplacement;
        internal float FingerContactDisplacement => _contactFingerDisplacement;

        // Interpolate back in displacement values after the hand has just released
        private float _displacementGrabCooldown = 0.25f, _displacementGrabCooldownCurrent = 0f;

        internal float DistanceFromDataHand
        {
            get
            {
                if (!tracked || dataHand == null || palmBone == null || palmBone.transform == null) return -1;
                return Vector3.Distance(palmBone.transform.position, dataHand.PalmPosition);
            }
        }

        #region Settings

        internal Vector3 computedPhysicsPosition;
        internal Quaternion computedPhysicsRotation;
        internal float computedHandDistance;

        internal float contactForceModifier; // a value between 0.1 and 1 relating to how far the tracked hand is from the articulation hand. 0.1 is far away, 1 is near. Only changes when in contact/grabbing

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
                    if (!Physics.GetIgnoreLayerCollision(physicalHandsManager.HandsLayer, i))
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
            ghosted = false;
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
            ChangeHandLayer(physicalHandsManager.HandsResetLayer);
            if (gameObject.activeInHierarchy)
            {
                CacheHandData(dataHand);

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

                if(!hasFingerGrasped)
                {
                    fingerStiffness[fingerIndex] = hardContactParent.boneStiffness;
                }
            }

            if (IsGrabbing)
            {
                _displacementGrabCooldownCurrent = _displacementGrabCooldown;
            }

            if (contactingFingers > 0)
            {
                if (IsGrabbing)
                {
                    _contactFingerDisplacement = 0f;
                }
                else
                {
                    _contactFingerDisplacement = _overallFingerDisplacement * Mathf.Max(6 - contactingFingers, 1);

                    if (_displacementGrabCooldown > 0)
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

            if (Time.frameCount - _lastFrameTeleport >= _teleportFrameCount && ghosted && !isCloseToObject)
            {
                ChangeHandLayer(physicalHandsManager.HandsLayer);

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

                    if (((HardContactBone)bones[boneArrayIndex]).BoneOverRotationCheck(eulerThreshold))
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

            Vector3 direction = dataHand.Arm.Direction;

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
            modifiedHand.GrabStrength = Hands.CalculateGrabStrength(ref modifiedHand);
            modifiedHand.PinchStrength = Hands.CalculatePinchStrength(ref modifiedHand);
            modifiedHand.PinchDistance = Hands.CalculatePinchDistance(ref modifiedHand);
            modifiedHand.PalmVelocity = (palmBone.transform.position - _oldContactPosition) / Time.fixedDeltaTime;
            modifiedHand.TimeVisible = dataHand.TimeVisible;
        }

        private Vector3 GetTipPosition(int index)
        {
            if (index > 4)
            {
                return Vector3.zero;
            }
            PhysExts.ToWorldSpaceCapsule(bones[(FINGER_BONES * index) + FINGER_BONES - 1].boneCollider, out Vector3 outPos, out var outB, out var outRadius);
            outPos += (outPos - outB).normalized * outRadius;
            return outPos;
        }
        #endregion
    }
}