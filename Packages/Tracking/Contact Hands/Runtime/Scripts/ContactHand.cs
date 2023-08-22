using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public abstract class ContactHand : MonoBehaviour
    {
        public const int FINGERS = 5, FINGER_BONES = 3;

        public ContactBone[] bones;
        public ContactBone palmBone;

        public Chirality handedness = Chirality.Left;

        internal Hand modifiedHand = new Hand();
        internal bool tracked = false;

        internal ContactParent contactParent = null;

        #region Interaction Data
        public bool isContacting = false, isCloseToObject = false;
        public bool IsContacting => isContacting;
        public bool IsCloseToObject => isCloseToObject;
        #endregion

        private void Awake()
        {
            bones = new ContactBone[(FINGERS * FINGER_BONES)];
        }

        /// <summary>
        /// Called on initial awake of the ContactParent. Should generate the structure of your hand.
        /// </summary>
        internal void GenerateHand()
        {
            contactParent = GetComponentInParent<ContactParent>();
            GenerateHandLogic();
        }
        internal abstract void GenerateHandLogic();

        /// <summary>
        /// When the original data hand starts being tracked. You can keep calling this instead of update by manually changing tracked to true later on.
        /// </summary>
        /// <param name="hand">The original data hand coming from the input frame</param>
        internal abstract void BeginHand(Hand hand);

        /// <summary>
        /// Every other frame that the hand needs to update when the hand reports tracked as true.
        /// </summary>
        /// <param name="hand">The original data hand coming from the input frame</param>
        internal abstract void UpdateHand(Hand hand);

        /// <summary>
        /// When the original data hand has lost tracking.
        /// </summary>
        internal abstract void FinishHand();

        protected abstract void ProcessOutputHand();

        internal Hand OutputHand()
        {
            if (tracked)
            {
                return modifiedHand;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Any logic that needs to occur after the physics simulation has run, but before update.
        /// </summary>
        internal abstract void PostFixedUpdateHand();

        /// <summary>
        /// Generates a basic hand structure with accompanying bone components.
        /// </summary>
        /// <param name="boneType"></param>
        internal void GenerateHandObjects(System.Type boneType)
        {
            if (boneType.BaseType != typeof(ContactBone))
            {
                Debug.LogError("Bone type must extend from ContactBone. Hand has not been generated.", this);
                return;
            }

            Leap.Hand leapHand = TestHandFactory.MakeTestHand(isLeft: handedness == Chirality.Left ? true : false, pose: TestHandFactory.TestHandPose.HeadMountedB);
            palmBone = new GameObject($"{(handedness == Chirality.Left ? "Left" : "Right")} Palm", boneType, typeof(BoxCollider)).GetComponent<ContactBone>();

            palmBone.gameObject.layer = contactParent.contactManager.HandsResetLayer;
            palmBone.transform.SetParent(transform);
            palmBone.transform.position = leapHand.PalmPosition;
            palmBone.transform.rotation = leapHand.Rotation;
            palmBone.isPalm = true;
            palmBone.finger = 5;

            palmBone.palmCollider = palmBone.GetComponent<BoxCollider>();
            ContactUtils.SetupPalmCollider(palmBone.palmCollider, leapHand);
            palmBone.contactHand = this;

            Transform lastTransform;
            Bone knuckleBone, prevBone;
            ContactBone bone;
            int boneArrayIndex;

            for (int fingerIndex = 0; fingerIndex < FINGERS; fingerIndex++)
            {
                lastTransform = palmBone.transform;
                knuckleBone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(0));

                for (int jointIndex = 0; jointIndex < FINGER_BONES; jointIndex++)
                {
                    prevBone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));

                    boneArrayIndex = fingerIndex * FINGER_BONES + jointIndex;

                    bones[boneArrayIndex] = new GameObject($"{ContactUtils.IndexToFinger(fingerIndex)} {ContactUtils.IndexToJoint(jointIndex)}", boneType, typeof(CapsuleCollider)).GetComponent<ContactBone>();
                    bone = bones[boneArrayIndex];
                    bone.gameObject.layer = contactParent.contactManager.HandsResetLayer;
                    bone.transform.SetParent(lastTransform);

                    bone.finger = fingerIndex;
                    bone.joint = jointIndex;

                    bone.boneCollider = bone.GetComponent<CapsuleCollider>();
                    ContactUtils.SetupBoneCollider(bone.boneCollider, leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex + 1)));
                    bone.contactHand = this;

                    if (jointIndex == 0)
                    {
                        bone.transform.position = fingerIndex == 0 ? knuckleBone.PrevJoint : knuckleBone.NextJoint;
                    }
                    else
                    {
                        bone.transform.localPosition = Vector3.forward * prevBone.Length;
                    }

                    bone.transform.rotation = knuckleBone.Rotation;

                    if (bone.transform.parent != null)
                    {
                        bone.transform.localScale = new Vector3(
                            1f / bone.transform.parent.lossyScale.x,
                            1f / bone.transform.parent.lossyScale.y,
                            1f / bone.transform.parent.lossyScale.z);
                    }

                    lastTransform = bone.transform;
                }
            }
        }

        /// <summary>
        /// Helper function to obtain a specific bone of the hand.
        /// </summary>
        /// <param name="fingerIndex">0-4 starting from the thumb</param>
        /// <param name="jointIndex">0-2 starting from the proximal</param>
        /// <returns></returns>
        public ContactBone GetBone(int fingerIndex, int jointIndex)
        {
            return bones[fingerIndex * FINGER_BONES + jointIndex];
        }
    }
}