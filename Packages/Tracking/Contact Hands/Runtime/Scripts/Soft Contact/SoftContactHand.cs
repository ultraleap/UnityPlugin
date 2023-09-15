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

        private bool _hasReset = false;
        private int _resetCounter = 2, _teleportFrameCount = 10;
        private int _layerMask = 0;

        #region Settings

        internal float currentPalmVelocity, currentPalmAngularVelocity;
        internal float currentPalmVelocityInterp, currentPalmWeightInterp;

        internal float computedHandDistance;

        internal float graspingWeight, currentPalmWeight, fingerDisplacement;
        internal float currentResetLerp;

        internal int[] grabbingFingers;
        internal float[] grabbingFingerDistances;
        #endregion

        protected override void ProcessOutputHand()
        {
        }

        internal override void BeginHand(Hand hand)
        {
            if (!resetting)
            {
                _resetCounter = RESET_FRAME_COUNT;
                _hasReset = false;
                resetting = true;
                tracked = true;
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
            }
            else
            {

            }
        }

        internal override void FinishHand()
        {
            tracked = false;
            resetting = false;
            gameObject.SetActive(false);
            ResetSoftContactHand(false);
        }

        private void ResetSoftContactHand(bool active)
        {
            ChangeHandLayer(contactManager.HandsResetLayer);
        }

        internal override void GenerateHandLogic()
        {
            grabbingFingerDistances = new float[FINGERS];
            grabbingFingers = new int[FINGERS];

            GenerateHandObjects(typeof(SoftContactBone));

            ((SoftContactBone)palmBone).SetupBoneBody();
            // Set the colliders to ignore eachother
            foreach (var bone in bones)
            {
                ((SoftContactBone)bone).SetupBoneBody();
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

        private void ResetHand()
        {
            ContactUtils.SetupPalmCollider(palmBone.palmCollider, dataHand);

            for (int fingerIndex = 0; fingerIndex < FINGERS; fingerIndex++)
            {
                Bone knuckleBone = dataHand.Fingers[fingerIndex].Bone(0);

                for (int jointIndex = 0; jointIndex < FINGER_BONES; jointIndex++)
                {
                    Bone prevBone = dataHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));
                    Bone bone = dataHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)); // +1 to skip first bone.

                    int boneArrayIndex = fingerIndex * FINGER_BONES + jointIndex;

                    if (jointIndex == 0)
                    {
                        bones[boneArrayIndex].transform.position = fingerIndex == 0 ? knuckleBone.PrevJoint : knuckleBone.NextJoint;
                    }
                    else
                    {
                        bones[boneArrayIndex].transform.localPosition = transform.InverseTransformPoint(prevBone.PrevJoint);
                    }

                    bones[boneArrayIndex].transform.rotation = knuckleBone.Rotation;

                    if (bones[boneArrayIndex].transform.parent != null)
                    {
                        bones[boneArrayIndex].transform.localScale = new Vector3(
                            1f / bones[boneArrayIndex].transform.parent.lossyScale.x,
                            1f / bones[boneArrayIndex].transform.parent.lossyScale.y,
                            1f / bones[boneArrayIndex].transform.parent.lossyScale.z);
                    }

                    ContactUtils.SetupBoneCollider(bones[boneArrayIndex].boneCollider, bone);

                    // Move the anchor positions to account for hand sizes
                    if (jointIndex > 0)
                    {
                        ((SoftContactBone)bones[boneArrayIndex]).articulationBody.parentAnchorPosition = ContactUtils.InverseTransformPoint(prevBone.PrevJoint, prevBone.Rotation, bone.PrevJoint);
                        ((SoftContactBone)bones[boneArrayIndex]).articulationBody.parentAnchorRotation = Quaternion.identity;
                    }
                    else
                    {
                        ((SoftContactBone)bones[boneArrayIndex]).articulationBody.parentAnchorPosition = ContactUtils.InverseTransformPoint(dataHand.PalmPosition, dataHand.Rotation, knuckleBone.NextJoint);
                        if (fingerIndex == 0)
                        {
                            ((SoftContactBone)bones[boneArrayIndex]).articulationBody.parentAnchorRotation = Quaternion.Euler(0,
                                dataHand.IsLeft ? ContactUtils.HAND_ROTATION_OFFSET_Y : -ContactUtils.HAND_ROTATION_OFFSET_Y,
                                dataHand.IsLeft ? ContactUtils.HAND_ROTATION_OFFSET_Z : -ContactUtils.HAND_ROTATION_OFFSET_Z);
                        }
                    }
                }
            }
        }

        internal override void PostFixedUpdateHand()
        {
        }

        protected override void UpdateHandLogic(Hand hand)
        {

        }
    }
}
