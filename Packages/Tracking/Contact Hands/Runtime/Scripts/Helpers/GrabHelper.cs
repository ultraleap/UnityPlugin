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

        #region Safety Overlap Data
        private bool _jobsCreated = false;
        private const int SAFETY_CYCLES = 2;
        private float[] _safetyCheckDistances;
        #endregion

        #region Interaction Data
        private Dictionary<ContactHand, float[]> _fingerStrengths = new Dictionary<ContactHand, float[]>();
        public Dictionary<ContactHand, float[]> FingerStrengths => _fingerStrengths;
        #endregion


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
            _safetyCheckDistances = new float[] { 0.005f, contactManager.ContactDistance };
            
            _jobsCreated = true;
        }

        private void DisposeJobs()
        {
            if (_jobsCreated)
            {
                
            }
        }

        private void OnPrePhysicsUpdate()
        {
            UpdateSafetyOverlaps();
        }

        private void OnFixedFrame(Frame frame)
        {
            UpdateHandHeuristics();
        }

        private void UpdateHandHeuristics()
        {
            if (contactManager.contactHands.leftHand.tracked)
            {
                UpdateHandOverlaps(contactManager.contactHands.leftHand);
                UpdateBoneQueues(contactManager.contactHands.leftHand);
                UpdateHandStatistics(contactManager.contactHands.leftHand);
            }
            if (contactManager.contactHands.rightHand.tracked)
            {
                UpdateHandOverlaps(contactManager.contactHands.rightHand);
                UpdateBoneQueues(contactManager.contactHands.rightHand);
                UpdateHandStatistics(contactManager.contactHands.rightHand);
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

        // Unity 2021
        private void UpdateSafetyOverlaps()
        {
#if UNITY_2022_3_OR_NEWER

#else
            // Pre 2022 overlaps
            HandSafetyOverlaps(contactManager.contactHands.leftHand);
            HandSafetyOverlaps(contactManager.contactHands.rightHand);
#endif
        }

        private void HandSafetyOverlaps(ContactHand hand)
        {
            hand.isCloseToObject = false;
            hand.isContacting = false;
            if (hand.tracked)
            {
                PalmSafetyOverlaps(hand.palmBone);

                if (hand.isCloseToObject && hand.isContacting)
                    return;

                for (int i = 0; i < hand.bones.Length; i++)
                {
                    JointSafetyOverlaps(hand.bones[i]);
                }
            }
        }

        private void PalmSafetyOverlaps(ContactBone palmBone)
        {
            int count = PhysExts.OverlapBoxNonAllocOffset(palmBone.palmCollider, Vector3.zero, _colliderCache, contactManager.InteractionMask, QueryTriggerInteraction.Ignore, extraRadius: _safetyCheckDistances[0]);
            for (int i = 0; i < count; i++)
            {
                if (_colliderCache[i] != null && WillColliderAffectHand(_colliderCache[i]))
                {
                    palmBone.contactHand.isCloseToObject = true;
                    break;
                }
            }

            // Contact
            count = PhysExts.OverlapBoxNonAllocOffset(palmBone.palmCollider, Vector3.zero, _colliderCache, contactManager.InteractionMask, QueryTriggerInteraction.Ignore, extraRadius: _safetyCheckDistances[1]);
            for (int i = 0; i < count; i++)
            {
                if (_colliderCache[i] != null && WillColliderAffectHand(_colliderCache[i]))
                {
                    palmBone.contactHand.isContacting = true;
                    break;
                }
            }
        }

        private void JointSafetyOverlaps(ContactBone bone)
        {
            int count = PhysExts.OverlapCapsuleNonAllocOffset(bone.boneCollider, Vector3.zero, _colliderCache, contactManager.InteractionMask, QueryTriggerInteraction.Ignore, extraRadius: _safetyCheckDistances[0]);
            for (int i = 0; i < count; i++)
            {
                if (_colliderCache[i] != null && WillColliderAffectHand(_colliderCache[i]))
                {
                    bone.contactHand.isCloseToObject = true;
                    break;
                }
            }

            // Contact
            count = PhysExts.OverlapCapsuleNonAllocOffset(bone.boneCollider, Vector3.zero, _colliderCache, contactManager.InteractionMask, QueryTriggerInteraction.Ignore, extraRadius: _safetyCheckDistances[1]);
            for (int i = 0; i < count; i++)
            {
                if (_colliderCache[i] != null && WillColliderAffectHand(_colliderCache[i]))
                {
                    bone.contactHand.isContacting = true;
                    break;
                }
            }
        }

#if UNITY_2022_3_OR_NEWER
        private void UpdateSafetyOverlapJobs()
        {
            JobHandle combinedHandle = default;

            contactManager.contactHands.leftHand.isContacting = false;
            contactManager.contactHands.leftHand.isCloseToObject = false;

            contactManager.contactHands.rightHand.isContacting = false;
            contactManager.contactHands.rightHand.isCloseToObject = false;

            int trackedSwitch = 0;
            if(contactManager.contactHands.leftHand.tracked)
            {
                _leftSafetyCasts.UpdateHand(contactManager.contactHands.leftHand);
                _leftSafetyCasts.ScheduleJobs(out combinedHandle);
                trackedSwitch++;
            }
            if (contactManager.contactHands.rightHand.tracked)
            {
                _rightSafetyCasts.UpdateHand(contactManager.contactHands.rightHand);
                _rightSafetyCasts.ScheduleJobs(out JobHandle secondHandle);
                if(trackedSwitch == 1)
                {
                    combinedHandle = JobHandle.CombineDependencies(combinedHandle, secondHandle);
                }
                else
                {
                    combinedHandle = secondHandle;
                }
                trackedSwitch += 2;
            }

            if(trackedSwitch > 0)
            {
                combinedHandle.Complete();
                switch (trackedSwitch)
                {
                    // Left tracked
                    case 1:
                        ProcessSafetyResults(contactManager.contactHands.leftHand, ref _leftSafetyCasts);
                        break;
                    // Right tracked
                    case 2:
                        ProcessSafetyResults(contactManager.contactHands.rightHand, ref _rightSafetyCasts);
                        break;
                    // Both tracked
                    case 3:
                        // Do the right one because it's dependant on the left hands
                        ProcessSafetyResults(contactManager.contactHands.leftHand, ref _leftSafetyCasts);
                        ProcessSafetyResults(contactManager.contactHands.rightHand, ref _rightSafetyCasts);
                        break;
                }
            }
        }

        private void ProcessSafetyResults(ContactHand contactHand, ref HandCastJob castJob)
        {
            RaycastHit tempHit;
            int cycle;
            for (int i = 0; i < castJob.palmResults.Length; i++)
            {
                if (contactHand.isCloseToObject && contactHand.isContacting)
                    break;

                tempHit = castJob.palmResults[i];
                if (tempHit.colliderInstanceID == 0)
                    continue;

                cycle = i / castJob.palmJobsPerCycle;

                if (WillColliderAffectHand(tempHit.collider))
                {
                    switch (cycle)
                    {
                        case 0:
                            contactHand.isCloseToObject = true;
                            break;
                        case 1:
                            contactHand.isContacting = true;
                            break;
                    }
                }
            }

            if (contactHand.isCloseToObject && contactHand.isContacting)
                return;

            for (int i = 0; i < castJob.jointResults.Length; i++)
            {
                if (contactHand.isCloseToObject && contactHand.isContacting)
                    break;

                tempHit = castJob.jointResults[i];
                if (tempHit.colliderInstanceID == 0)
                    continue;

                cycle = i / castJob.jointJobsPerCycle;

                if (WillColliderAffectHand(tempHit.collider))
                {
                    switch (cycle)
                    {
                        case 0:
                            contactHand.isCloseToObject = true;
                            break;
                        case 1:
                            contactHand.isContacting = true;
                            break;
                    }
                }
            }
        }
#endif

        // Unity 2022+
        private void PalmOverlapJobs()
        {

        }

        private void JointOverlapJobs()
        {

        }

        private bool WillColliderAffectHand(Collider collider)
        {
            if (collider.gameObject.TryGetComponent<ContactBone>(out var bone)/* && collider.attachedRigidbody.TryGetComponent<PhysicsIgnoreHelpers>(out var temp) && temp.IsThisHandIgnored(this)*/)
            {
                return false;
            }
            if (collider.attachedRigidbody != null || collider != null)
            {
                return true;
            }
            return false;
        }

        private void UpdateBoneQueues(ContactHand hand)
        {
            hand.palmBone.ProcessColliderQueue();
            for (int i = 0; i < hand.bones.Length; i++)
            {
                hand.bones[i].ProcessColliderQueue();
            }
        }

        private void UpdateHandStatistics(ContactHand hand)
        {
            if (!_fingerStrengths.ContainsKey(hand))
            {
                _fingerStrengths.Add(hand, new float[5]);
            }
            Leap.Hand lHand = hand.dataHand;
            for (int i = 0; i < 5; i++)
            {
                _fingerStrengths[hand][i] = lHand.GetFingerStrength(i);
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