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

        internal Hand modifiedHand = new Hand(), dataHand = new Hand();
        internal bool tracked = false, resetting = false, ghosted = false;
        internal bool isHandPhysical = true;
        /// <summary>
        /// Is the hand tracked from a visual sense?
        /// </summary>
        public bool Tracked => tracked;
        /// <summary>
        /// Is the hand able to interact with the physic simulation, or is it in a reset state?
        /// </summary>
        public bool Ghosted => ghosted;
        /// <summary>
        /// Is the hand both tracked and not resetting due to physics interactions?
        /// </summary>
        public bool InGoodState { get { return tracked && !resetting && !ghosted; } }

        /// <summary>
        /// Is the hand going to apply physics forces or should it pass through objects?
        /// </summary>
        public bool IsHandPhysical => isHandPhysical;

        internal ContactParent contactParent = null;
        internal ContactManager contactManager => contactParent.contactManager;

        #region Interaction Data
        internal bool isContacting = false, isCloseToObject = false, isGrabbing = false;
        public bool IsContacting => isContacting;
        public bool IsCloseToObject => isCloseToObject;

        public bool IsGrabbing => isGrabbing;

        protected Vector3 _oldDataPosition;
        protected Quaternion _oldDataRotation;

        /// <summary>
        /// These values will need to be manually calculated if there are no rigidbodies on the bones.
        /// </summary>
        protected Vector3 _velocity, _angularVelocity;
        public Vector3 Velocity => _velocity;
        public Vector3 AngularVelocity => _angularVelocity;
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
            gameObject.SetActive(false);
        }
        internal abstract void GenerateHandLogic();

        /// <summary>
        /// When the original data hand starts being tracked. You can keep calling this instead of update by manually changing tracked to true later on.
        /// </summary>
        /// <param name="hand">The original data hand coming from the input frame</param>
        internal abstract void BeginHand(Hand hand);


        internal void UpdateHand(Hand hand)
        {
            dataHand.CopyFrom(hand);
            UpdateHandLogic(hand);
            // Update the bones
            palmBone.UpdatePalmBone(hand);
            for (int i = 0; i < bones.Length - 1; i++)
            {
                bones[i].UpdateBone(hand.Fingers[bones[i].Finger].bones[bones[i].joint], hand.Fingers[bones[i].Finger].bones[bones[i].joint + 1]);
            }
            CacheHandData(dataHand);
        }
        /// <summary>
        /// Every other frame that the hand needs to update when the hand reports tracked as true. This happens before the bones are updated.
        /// </summary>
        /// <param name="hand">The original data hand coming from the input frame</param>
        protected abstract void UpdateHandLogic(Hand hand);

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

        protected void CacheHandData(Hand dataHand)
        {
            _velocity = ContactUtils.ToLinearVelocity(_oldDataPosition, modifiedHand.PalmPosition, Time.fixedDeltaTime);
            _angularVelocity = ContactUtils.ToAngularVelocity(_oldDataRotation, modifiedHand.Rotation, Time.fixedDeltaTime);
            _oldDataPosition = dataHand.PalmPosition;
            _oldDataRotation = dataHand.Rotation;
        }

        protected void ChangeHandLayer(SingleLayer layer)
        {
            gameObject.layer = layer;
            palmBone.gameObject.layer = layer;
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].gameObject.layer = layer;
            }
        }
    }
}