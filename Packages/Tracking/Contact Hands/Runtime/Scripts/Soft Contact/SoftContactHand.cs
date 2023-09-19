using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public class SoftContactHand : ContactHand
    {
        private const int RESET_FRAME_COUNT = 2, TELEPORT_FRAME_COUNT = 5, RESET_FRAME_TELEPORT_COUNT = 10;

        private SoftContactParent softContactParent => contactParent as SoftContactParent;

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


        protected override void ProcessOutputHand(ref Hand modifiedHand)
        {
            modifiedHand.CopyFrom(dataHand);
        }

        internal override void BeginHand(Hand hand)
        {
            gameObject.SetActive(true);
            _oldDataPosition = hand.PalmPosition;
            _oldDataRotation = hand.Rotation;
            _velocity = Vector3.zero;
            _angularVelocity = Vector3.zero;
            tracked = true;
        }

        internal override void FinishHand()
        {
            tracked = false;
            _velocity = Vector3.zero;
            _angularVelocity = Vector3.zero;
            ResetSoftContactHand();
            gameObject.SetActive(false);
        }

        internal override void GenerateHandLogic()
        {
            grabbingFingerDistances = new float[FINGERS];
            grabbingFingers = new int[FINGERS];
            fingerStiffness = new float[FINGERS];

            GenerateHandObjects(typeof(SoftContactBone));

            ((SoftContactBone)palmBone).SetupBone();
            // Set the colliders to ignore eachother
            foreach (var bone in bones)
            {
                ((SoftContactBone)bone).SetupBone();
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

        private void ResetSoftContactHand()
        {
            _lastFrameTeleport = Time.frameCount;
            ghosted = true;
            ChangeHandLayer(contactManager.HandsResetLayer);
            if (gameObject.activeInHierarchy)
            {
                CacheHandData(dataHand);

                currentPalmVelocity = softContactParent.maxPalmVelocity;
                currentPalmVelocity = softContactParent.maxPalmVelocity;

                ((SoftContactBone)palmBone).ResetPalm();

                for (int fingerIndex = 0; fingerIndex < FINGERS; fingerIndex++)
                {
                    fingerStiffness[fingerIndex] = softContactParent.boneStiffness;
                    for (int jointIndex = 0; jointIndex < FINGER_BONES; jointIndex++)
                    {
                        Bone prevBone = dataHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
                        Bone bone = dataHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

                        int boneArrayIndex = fingerIndex * FINGER_BONES + jointIndex;

                        ((SoftContactBone)bones[boneArrayIndex]).ResetBone(prevBone, bone);
                    }
                }
                palmBone.gameObject.SetActive(false);
                palmBone.gameObject.SetActive(true);
            }
        }


        internal override void PostFixedUpdateHandLogic()
        {
            // Don't need to do anything here
        }

        protected override void UpdateHandLogic(Hand hand)
        {
            _velocity = ContactUtils.ToLinearVelocity(_oldDataPosition, hand.PalmPosition, Time.fixedDeltaTime);
            _angularVelocity = ContactUtils.ToAngularVelocity(_oldDataRotation, hand.Rotation, Time.fixedDeltaTime);
        }
    }
}
