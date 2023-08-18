using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Leap.Unity.ContactHands
{
    [RequireComponent(typeof(ContactManager))]
    public class GrabHelper : MonoBehaviour
    {
        [Tooltip("If this is null then the Contact Manager's input provider will be used.")]
        public LeapProvider dataProvider;
        public ContactManager contactManager;

        private Collider[] _colliderCache = new Collider[10];

        private bool _jobsCreated = false;
        private PhysCastJob _safetyCastJob;

        private void Start()
        {
            InitJobs();
        }

        private void OnDestroy()
        {
            DisposeJobs();
        }

        private void OnEnable()
        {
            if (contactManager != null)
            {
                contactManager.OnPrePhysicsUpdate -= OnPrePhysicsUpdate;
                contactManager.OnPrePhysicsUpdate += OnPrePhysicsUpdate;
                contactManager.OnFixedFrame -= OnFixedFrame;
                contactManager.OnFixedFrame += OnFixedFrame;
            }
        }

        private void OnDisable()
        {
            if(contactManager != null)
            {
                contactManager.OnFixedFrame -= OnFixedFrame;
            }
        }

        private void InitJobs()
        {
            // Two sets of jobs because of two hands, two repetitions because of inside hand and close to hand
            _safetyCastJob = new PhysCastJob(2, ContactHand.FINGERS * ContactHand.FINGER_BONES * 2, 2, contactManager.InteractionMask.value);

            _jobsCreated = true;
        }

        private void DisposeJobs()
        {
            if (_jobsCreated)
            {
                _safetyCastJob.Dispose();
            }
        }

        private void OnPrePhysicsUpdate()
        {
            UpdateSafetyOverlaps();
        }

        private void OnFixedFrame(Frame frame)
        {
            UpdateHandHeuristics(dataProvider == null ? contactManager.InputProvider.CurrentFixedFrame : dataProvider.CurrentFixedFrame, frame);
        }

        private void UpdateHandHeuristics(Frame dataFrame, Frame modifiedFrame)
        {
            if (contactManager.contactHands.leftHand.tracked)
            {
                UpdateHandOverlaps(contactManager.contactHands.leftHand);
                UpdateBoneQueues(contactManager.contactHands.leftHand);
            }
            if (contactManager.contactHands.rightHand.tracked)
            {
                UpdateHandOverlaps(contactManager.contactHands.rightHand);
                UpdateBoneQueues(contactManager.contactHands.rightHand);
            }
        }
        
        private void UpdateHandOverlaps(ContactHand hand)
        {
#if UNITY_2022_3_OR_NEWER

#else
            // Pre 2022 overlaps
            PalmOverlaps(hand.palmBone);
            for (int i = 0; i < hand.bones.Length; i++)
            {
                JointOverlaps(hand.bones[i]);
            }
#endif
        }

        private void PalmOverlaps(ContactBone palmBone)
        {
            int count = PhysExts.OverlapBoxNonAllocOffset(palmBone.palmCollider, Vector3.zero, _colliderCache, contactManager.InteractionMask, QueryTriggerInteraction.Ignore, extraRadius: contactManager.HoverDistance);
            for (int i = 0; i < count; i++)
            {
                if (_colliderCache[i] != null && _colliderCache[i].attachedRigidbody != null)
                    palmBone.QueueHoverCollider(_colliderCache[i]);
            }

            // Contact
            count = PhysExts.OverlapBoxNonAllocOffset(palmBone.palmCollider, Vector3.zero, _colliderCache, contactManager.InteractionMask, QueryTriggerInteraction.Ignore, extraRadius: contactManager.ContactDistance);
            for (int i = 0; i < count; i++)
            {
                if (_colliderCache[i] != null && _colliderCache[i].attachedRigidbody != null)
                    palmBone.QueueContactCollider(_colliderCache[i]);
            }
        }

        private void JointOverlaps(ContactBone bone)
        {
            int count = PhysExts.OverlapCapsuleNonAllocOffset(bone.boneCollider, Vector3.zero, _colliderCache, contactManager.InteractionMask, QueryTriggerInteraction.Ignore, extraRadius: contactManager.HoverDistance);
            for (int i = 0; i < count; i++)
            {
                if (_colliderCache[i] != null && _colliderCache[i].attachedRigidbody != null)
                    bone.QueueHoverCollider(_colliderCache[i]);
            }

            // Contact
            count = PhysExts.OverlapCapsuleNonAllocOffset(bone.boneCollider, Vector3.zero, _colliderCache, contactManager.InteractionMask, QueryTriggerInteraction.Ignore, extraRadius: contactManager.ContactDistance);
            for (int i = 0; i < count; i++)
            {
                if (_colliderCache[i] != null && _colliderCache[i].attachedRigidbody != null)
                    bone.QueueContactCollider(_colliderCache[i]);
            }
        }

        // Unity 2021+
        private void UpdateSafetyOverlaps()
        {
            // run the safety cast job logic here
        }

        // Unity 2022+
        private void PalmOverlapJobs()
        {

        }

        private void JointOverlapJobs()
        {

        }

        private void UpdateBoneQueues(ContactHand hand)
        {
            hand.palmBone.ProcessColliderQueue();
            for (int i = 0; i < hand.bones.Length; i++)
            {
                hand.bones[i].ProcessColliderQueue();
            }
        }

        private void OnValidate()
        {
            if (contactManager == null)
            {
                contactManager = GetComponent<ContactManager>();
            }
        }
    }
}