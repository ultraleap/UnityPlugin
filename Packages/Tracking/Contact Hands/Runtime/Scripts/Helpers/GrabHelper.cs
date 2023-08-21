using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
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
        private JobHandle _safetySphereJob, _safetyBoxJob;
        private int _safetyCastJobCount;
        private PhysCastJob _safetyCastJob;
        private NativeArray<RaycastHit> _palmSafetyResults, _jointSafetyResults;

        private const int SAFETY_CAST_REPS = 2;

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
            if (contactManager != null)
            {
                contactManager.OnFixedFrame -= OnFixedFrame;
            }
        }

        private void InitJobs()
        {
            // Two sets of jobs because of two hands, two repetitions because of inside hand and close to hand
            _safetyCastJob = new PhysCastJob(1, ContactHand.FINGERS * ContactHand.FINGER_BONES, 2, SAFETY_CAST_REPS, contactManager.InteractionMask);
            _palmSafetyResults = new NativeArray<RaycastHit>(_safetyCastJob.totalPalmJobs, Allocator.Persistent);
            _jointSafetyResults = new NativeArray<RaycastHit>(_safetyCastJob.totalJointJobs, Allocator.Persistent);

            _jobsCreated = true;
        }

        private void DisposeJobs()
        {
            if (_jobsCreated)
            {
                _palmSafetyResults.Dispose();
                _jointSafetyResults.Dispose();

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

            Vector3 origin = Vector3.zero, halfExtents = Vector3.zero, direction = Vector3.zero;
            Quaternion orientation = Quaternion.identity;

            int overallOffset = 0, handOffset = 0, palmOffset = 0, jointOffset = 0;
            float distance = 0;

            _safetyCastJob.tracked[0] = contactManager.contactHands.leftHand.tracked;
            _safetyCastJob.tracked[1] = contactManager.contactHands.rightHand.tracked;

            if (_safetyCastJob.tracked[0])
            {
                UpdatePalmSafetyCasts(contactManager.contactHands.leftHand, overallOffset, handOffset, palmOffset, ref origin, ref halfExtents, ref direction, ref orientation, ref distance);
                overallOffset++;
                UpdateJointSafetyCasts(contactManager.contactHands.leftHand, overallOffset, jointOffset, ref origin, tip: ref direction, radius: ref distance);
            }

            handOffset = 1;
            palmOffset = SAFETY_CAST_REPS;
            jointOffset = SAFETY_CAST_REPS * ContactHand.FINGERS * ContactHand.FINGER_BONES;
            overallOffset = 1 + (ContactHand.FINGERS * ContactHand.FINGER_BONES);

            if (_safetyCastJob.tracked[1])
            {
                UpdatePalmSafetyCasts(contactManager.contactHands.rightHand, overallOffset, handOffset, palmOffset, ref origin, ref halfExtents, ref direction, ref orientation, ref distance);
                overallOffset++;
                UpdateJointSafetyCasts(contactManager.contactHands.leftHand, overallOffset, jointOffset, ref origin, tip: ref direction, radius: ref distance);
            }

            _safetySphereJob = _safetyCastJob.ScheduleParallel(_safetyCastJob.sphereCommands.Length, 64, default);
            _safetyCastJobCount = Mathf.Max(_safetyCastJob.sphereCommands.Length / JobsUtility.JobWorkerCount, 1);
            _safetySphereJob = SpherecastCommand.ScheduleBatch(_safetyCastJob.sphereCommands, _jointSafetyResults, _safetyCastJobCount, _safetySphereJob);

            _safetyBoxJob = _safetyCastJob.ScheduleParallel(_safetyCastJob.boxCommands.Length, 64, _safetySphereJob);
            _safetyCastJobCount = Mathf.Max(_safetyCastJob.boxCommands.Length / JobsUtility.JobWorkerCount, 1);
            _safetyBoxJob = BoxcastCommand.ScheduleBatch(_safetyCastJob.boxCommands, _palmSafetyResults, _safetyCastJobCount, _safetyBoxJob);

            _safetyBoxJob.Complete();

            // now we "just" have to process the results......
        }

        private void UpdatePalmSafetyCasts(ContactHand hand, int overallOffset, int handOffset, int palmOffset, ref Vector3 origin, ref Vector3 halfExtents, ref Vector3 direction, ref Quaternion orientation, ref float distance)
        {
            GetPalmBoxCastParams(hand.palmBone.transform, hand.palmBone.palmCollider,
                    out origin, out halfExtents, out orientation, out direction, out distance);

            _safetyCastJob.origins[overallOffset] = origin;
            _safetyCastJob.directions[overallOffset] = direction;
            _safetyCastJob.distances[overallOffset] = distance;

            _safetyCastJob.orientations[handOffset] = orientation;

            // Inside palm?
            _safetyCastJob.halfExtents[palmOffset] = halfExtents;
            // Slightly larger for is close check
            _safetyCastJob.halfExtents[palmOffset + 1] = halfExtents + Vector3.one * contactManager.ContactDistance;
        }

        private void GetPalmBoxCastParams(Transform transform, BoxCollider collider, out Vector3 origin, out Vector3 halfExtents, out Quaternion orientation, out Vector3 direction, out float distance)
        {
            distance = (Vector3.Scale(PhysExts.AbsVec3(transform.lossyScale), collider.size) * 0.25f).y;
            PhysExts.ToWorldSpaceBoxOffset(collider, -Vector3.up * distance,
                out origin, out halfExtents, out orientation);
            distance = halfExtents.y;
            halfExtents.y /= 2f;
            direction = transform.up;
        }

        private void UpdateJointSafetyCasts(ContactHand hand, int overallOffset, int jointOffset, ref Vector3 origin, ref Vector3 tip, ref float radius)
        {
            // First iteration for inside hand/core base values
            for (int i = 0; i < hand.bones.Length; i++)
            {
                PhysExts.ToWorldSpaceCapsule(hand.bones[i].boneCollider, out origin, out tip, out radius);

                _safetyCastJob.origins[overallOffset] = origin;
                _safetyCastJob.directions[overallOffset] = (tip - origin).normalized;
                _safetyCastJob.distances[overallOffset] = Vector3.Distance(tip, origin);

                _safetyCastJob.radii[overallOffset] = radius;

                _safetyCastJob.radii[overallOffset + (ContactHand.FINGERS * ContactHand.FINGER_BONES)] = radius + contactManager.ContactDistance;

                overallOffset++;
                jointOffset++;
            }
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