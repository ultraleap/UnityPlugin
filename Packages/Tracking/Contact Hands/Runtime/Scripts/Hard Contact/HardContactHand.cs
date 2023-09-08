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

        private float _overallFingerDisplacement = 0f, _averageFingerDisplacement = 0f, _contactFingerDisplacement = 0f;
        // Interpolate back in displacement values after the hand has just released
        private float _displacementGrabCooldown = 0.25f, _displacementGrabCooldownCurrent = 0f;

        private Vector3 _elbowPosition;

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

        internal float computedHandDistance;

        internal float graspingWeight, currentPalmWeight, fingerDisplacement;
        internal float currentResetLerp;

        internal int[] grabbingFingers;
        internal float[] grabbingFingerDistances, fingerStiffness;
        #endregion

        protected override void ProcessOutputHand()
        {
        }

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
            _timeOnReset = Time.time;
            UpdateIterations();
            resetting = false;
            tracked = true;
        }

        internal override void PostFixedUpdateHand()
        {
            CalculateDisplacementsAndLimits();
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
    }
}