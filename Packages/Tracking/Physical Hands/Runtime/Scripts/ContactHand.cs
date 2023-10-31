using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.PhysicalHands
{
    public abstract class ContactHand : MonoBehaviour
    {
        public const int FINGERS = 5, FINGER_BONES = 3;

        public ContactBone[] bones;
        public ContactBone palmBone;

        public Chirality handedness = Chirality.Left;

        private Hand modifiedHand = new Hand();
        internal Hand dataHand = new Hand();
        [SerializeField]
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
        /// Is the hand going to apply physics forces or should it pass through objects?
        /// </summary>
        public bool IsHandPhysical => isHandPhysical;

        internal ContactParent contactParent = null;
        internal PhysicalHandsManager physicalHandsManager => contactParent.physicalHandsManager;

        #region Interaction Data
        internal bool isContacting = false, isHovering = false, isCloseToObject = false, isGrabbing = false, isIntersecting = false;
        public bool IsHovering => isHovering;
        public bool IsContacting => isContacting;
        public bool IsCloseToObject => isCloseToObject;
        public bool IsIntersecting => isIntersecting;

        public bool IsGrabbing => isGrabbing;

        protected Vector3 _oldDataPosition, _oldContactPosition;
        protected Quaternion _oldDataRotation, _oldContactRotation;

        internal float grabMass = 0f;
        public float GrabMass => grabMass;

        /// <summary>
        /// These values will need to be manually calculated if there are no rigidbodies on the bones.
        /// </summary>
        protected Vector3 _velocity, _angularVelocity;
        public Vector3 Velocity => _velocity;
        public Vector3 AngularVelocity => _angularVelocity;
        #endregion

        #region Physics Data
        private List<IgnoreData> _ignoredData = new List<IgnoreData>();
        private class IgnoreData
        {
            public Rigidbody rigid;
            public Collider[] colliders;
            public float timeout = 0;
            public float radius = 0;

            public IgnoreData(Rigidbody rigid, Collider[] colliders)
            {
                this.rigid = rigid;
                this.colliders = colliders;
            }
        }

        private Collider[] _colliderCache = new Collider[32];
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
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].UpdateBone(hand.Fingers[bones[i].Finger].bones[bones[i].joint], hand.Fingers[bones[i].Finger].bones[bones[i].joint + 1]);
            }
            CacheHandData(dataHand);
            if (Time.inFixedTimeStep)
            {
                HandleIgnoredObjects();
            }
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

        protected void ResetModifiedHand()
        {
            modifiedHand.CopyFrom(dataHand);
        }
        protected abstract void ProcessOutputHand(ref Hand modifiedHand);

        internal Hand OutputHand()
        {
            ProcessOutputHand(ref modifiedHand);
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
        internal void PostFixedUpdateHand()
        {
            PostFixedUpdateHandLogic();
            palmBone.PostFixedUpdateBone();
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].PostFixedUpdateBone();
            }
        }

        internal abstract void PostFixedUpdateHandLogic();

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
            palmBone = new GameObject($"{(handedness == Chirality.Left ? "Left" : "Right")} Palm", boneType, typeof(BoxCollider), typeof(CapsuleCollider), typeof(CapsuleCollider), typeof(CapsuleCollider)).GetComponent<ContactBone>();

            palmBone.gameObject.layer = contactParent.physicalHandsManager.HandsResetLayer;
            palmBone.transform.SetParent(transform);
            palmBone.transform.position = leapHand.PalmPosition;
            palmBone.transform.rotation = leapHand.Rotation;
            palmBone.isPalm = true;
            palmBone.finger = 5;

            palmBone.palmCollider = palmBone.GetComponent<BoxCollider>();
            palmBone.palmEdgeColliders = palmBone.GetComponents<CapsuleCollider>();
            ContactUtils.SetupPalmCollider(palmBone.palmCollider, palmBone.palmEdgeColliders, leapHand);
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
                    bone.gameObject.layer = contactParent.physicalHandsManager.HandsResetLayer;
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
            _oldDataPosition = dataHand.PalmPosition;
            _oldDataRotation = dataHand.Rotation;
            _oldContactPosition = palmBone.transform.position;
            _oldContactRotation = palmBone.transform.rotation;

            if (palmBone.rigid != null)
            {
                _velocity = palmBone.rigid.velocity;
                _angularVelocity = palmBone.rigid.angularVelocity;
            }
            else if (palmBone.articulation != null)
            {
                _velocity = palmBone.articulation.velocity;
                _angularVelocity = palmBone.articulation.angularVelocity;
            }
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

        #region Ignored Objects
        private void HandleIgnoredObjects()
        {
            if (_ignoredData.Count > 0)
            {
                for (int i = 0; i < _ignoredData.Count; i++)
                {
                    // Handle destroyed objects
                    if (_ignoredData[i].rigid == null)
                    {
                        _ignoredData.RemoveAt(i);
                        continue;
                    }

                    if (_ignoredData[i].timeout >= 0)
                    {
                        _ignoredData[i].timeout -= Time.fixedDeltaTime;
                    }

                    // TODO: Batch call the ignored objects to see if they appear in the hand data
                    if (_ignoredData[i].timeout <= 0 && !IsObjectInHandRadius(_ignoredData[i].rigid, _ignoredData[i].radius))
                    {
                        TogglePhysicsIgnore(_ignoredData[i].rigid, false);
                        i--;
                    }
                }
            }
        }

        /// <summary>
        /// Disables all collisions between a rigidbody and hand. Will automatically handle all colliders on the rigidbody. Timeout lets you specify a minimum time to ignore collisions for.
        /// </summary>
        public void IgnoreCollision(Rigidbody rigid, float timeout = 0, float radius = 0)
        {
            TogglePhysicsIgnore(rigid, true, timeout, radius);
        }

        private void TogglePhysicsIgnore(Rigidbody rigid, bool ignore, float timeout = 0, float radius = 0)
        {
            // If the rigid has been destroyed we can't do anything
            if (rigid == null)
            {
                return;
            }

            Collider[] colliders = rigid.GetComponentsInChildren<Collider>(true);

            // If objects are already ignoring, we shouldn't add it to a timer that will re-enable collision
            // Rarely loops through more than 1 collider
            foreach (var collider in colliders)
            {
                if (Physics.GetIgnoreCollision(collider, palmBone.palmCollider))
                {
                    return;
                }
            }

            foreach (var collider in colliders)
            {
                Physics.IgnoreCollision(collider, palmBone.palmCollider, ignore);
                foreach (var bone in bones)
                {
                    Physics.IgnoreCollision(collider, bone.boneCollider, ignore);
                }
            }
            int ind = _ignoredData.FindIndex(x => x.rigid == rigid);
            if (ignore)
            {
                if (ind == -1)
                {
                    _ignoredData.Add(new IgnoreData(rigid, colliders) { timeout = timeout, radius = radius });
                }
                else
                {
                    _ignoredData[ind].timeout = timeout;
                    _ignoredData[ind].radius = radius;
                }
            }
            else
            {
                if (ind != -1)
                {
                    _ignoredData.RemoveAt(ind);
                }
            }
        }

        /// <summary>
        /// Checks to see whether the hand will be in contact with a specified object. Radius can be used to inflate the bones.
        /// </summary>
        public bool IsObjectInHandRadius(Rigidbody rigid, float extraRadius = 0f)
        {
            if (rigid == null)
                return false;

            if (IsObjectInBoneRadius(rigid, palmBone, extraRadius))
            {
                return true;
            }

            foreach (var bone in bones)
            {
                if (IsObjectInBoneRadius(rigid, bone, extraRadius))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks to see whether a specific physicsBone will be in contact with a specified object. Radius can be used to inflate the physicsBone.
        /// </summary>
        public bool IsObjectInBoneRadius(Rigidbody rigid, ContactBone bone, float extraRadius = 0f)
        {
            int overlappingColliders;
            if (bone.IsPalm)
            {
                overlappingColliders = PhysExts.OverlapBoxNonAllocOffset(bone.palmCollider, Vector3.zero, _colliderCache, physicalHandsManager.InteractionMask, QueryTriggerInteraction.Ignore, extraRadius);
            }
            else
            {
                overlappingColliders = PhysExts.OverlapCapsuleNonAllocOffset(bone.boneCollider, Vector3.zero, _colliderCache, physicalHandsManager.InteractionMask, QueryTriggerInteraction.Ignore, extraRadius);
            }
            for (int i = 0; i < overlappingColliders; i++)
            {
                if (_colliderCache[i].attachedRigidbody == rigid)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}