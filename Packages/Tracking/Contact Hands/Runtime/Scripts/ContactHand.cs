using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    public abstract class ContactHand : MonoBehaviour
    {
        public const int FINGERS = 5, FINGER_BONES = 3;

        public ContactBone[] bones;

        public Chirality handedness = Chirality.Left;

        private void Awake()
        {
            // Plus one for palm
            bones = new ContactBone[(FINGERS * FINGER_BONES) + 1];
        }

        internal abstract void GenerateHand();

        internal abstract void UpdateHand(Hand hand);

        internal abstract void PostFixedUpdateHand();

        /// <summary>
        /// Generates a basic hand structure with accompanying bone components.
        /// </summary>
        /// <param name="boneType"></param>
        internal void GenerateHandObjects(System.Type boneType)
        {
            if(boneType.BaseType != typeof(ContactBone))
            {
                Debug.LogError("Bone type must extend from ContactBone. Hand has not been generated.", this);
                return;
            }

            Leap.Hand leapHand = TestHandFactory.MakeTestHand(isLeft: handedness == Chirality.Left ? true : false, pose: TestHandFactory.TestHandPose.HeadMountedB);
            bones[bones.Length - 1] = new GameObject($"{(handedness == Chirality.Left ? "Left" : "Right")} Palm", boneType).GetComponent<ContactBone>();

            ContactBone palmBone = bones[bones.Length - 1];
            palmBone.transform.SetParent(transform);
            palmBone.transform.position = leapHand.PalmPosition;
            palmBone.transform.rotation = leapHand.Rotation;
            palmBone.isPalm = true;
            palmBone.finger = 5;

            Transform lastTransform;
            Bone knuckleBone, prevBone;
            ContactBone bone;
            int boneArrayIndex;

            for (int fingerIndex = 0; fingerIndex < FINGERS; fingerIndex++)
            {
                lastTransform = bones[bones.Length - 1].transform;
                knuckleBone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(0));

                for (int jointIndex = 0; jointIndex < FINGER_BONES; jointIndex++)
                {
                    prevBone = leapHand.Fingers[fingerIndex].Bone((Bone.BoneType)(jointIndex));

                    boneArrayIndex = fingerIndex * FINGER_BONES + jointIndex;

                    bones[boneArrayIndex] = new GameObject($"{ContactUtils.IndexToFinger(fingerIndex)} {ContactUtils.IndexToJoint(jointIndex)}",boneType).GetComponent<ContactBone>();
                    bone = bones[boneArrayIndex];
                    bone.transform.SetParent(lastTransform);

                    bone.finger = fingerIndex;
                    bone.joint = jointIndex;

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
    }
}