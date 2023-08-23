using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        private bool _leftGoodState { get { return contactManager.contactHands.leftHand.InGoodState; } }
        private bool _rightGoodState { get { return contactManager.contactHands.rightHand.InGoodState; } }

        private ContactHand _leftContactHand { get { return contactManager.contactHands.leftHand; } }
        private ContactHand _rightContactHand { get { return contactManager.contactHands.rightHand; } }

        private Collider[] _colliderCache = new Collider[32];

        #region Safety Overlap Data
        private bool _jobsCreated = false;
        private const int SAFETY_CYCLES = 2;
        private float[] _safetyCheckDistances;
        #endregion

        #region Interaction Data
        private Dictionary<ContactHand, float[]> _fingerStrengths = new Dictionary<ContactHand, float[]>();
        public Dictionary<ContactHand, float[]> FingerStrengths => _fingerStrengths;

        // Helpers
        private Dictionary<Rigidbody, GrabHelperObject> _grabHelpers = new Dictionary<Rigidbody, GrabHelperObject>();
        private HashSet<Rigidbody> _grabRigids = new HashSet<Rigidbody>();
        private HashSet<Rigidbody> _hoveredItems = new HashSet<Rigidbody>();
        private HashSet<ContactHand> _hoveringHands = new HashSet<ContactHand>();
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
            UpdateHelpers();
        }

        #region Hand Updating
        private void UpdateHandHeuristics()
        {
            if (_leftGoodState)
            {
                UpdateHandStatistics(_leftContactHand);
                UpdateHandOverlaps(_leftContactHand);
            }
            else
            {
                UntrackedHand(_leftContactHand);
            }
            if (_rightGoodState)
            {
                UpdateHandStatistics(_rightContactHand);
                UpdateHandOverlaps(_rightContactHand);   
            }
            else
            {
                UntrackedHand(_rightContactHand);
            }

            // do the 22 jobs running here once all the batches have been created

            if (_leftGoodState)
            {
                UpdateBoneQueues(_leftContactHand);
            }
            
            if (_rightGoodState)
            {
                UpdateBoneQueues(_rightContactHand);
            }
        }

        private void UpdateHandOverlaps(ContactHand hand)
        {
#if UNITY_2022_3_OR_NEWER
            // batch the jobs here
#else
            HandOverlaps(hand);
            // Pre 2022 overlaps
            PalmOverlaps(hand.palmBone);
            for (int i = 0; i < hand.bones.Length; i++)
            {
                JointOverlaps(hand.bones[i]);
            }
#endif
        }

        private void UntrackedHand(ContactHand hand)
        {
            foreach (var helper in _grabHelpers)
            {
                helper.Value.RemoveHand(hand);
            }
        }

        /// <summary>
        /// Used to create helper objects.
        /// </summary>
        private void HandOverlaps(ContactHand hand)
        {
            float lerp = 0;
            if (_fingerStrengths.ContainsKey(hand))
            {
                // Get the least curled finger excluding the thumb
                lerp = _fingerStrengths[hand].Skip(1).Min();
            }

            int results = Physics.OverlapCapsuleNonAlloc(hand.palmBone.transform.position + (-hand.palmBone.transform.up * 0.025f) + ((hand.handedness == Chirality.Left ? hand.palmBone.transform.right : -hand.palmBone.transform.right) * 0.015f),
                // Interpolate the tip position so we keep it relative to the straightest finger
                hand.palmBone.transform.position + (-hand.palmBone.transform.up * Mathf.Lerp(0.025f, 0.07f, lerp)) + (hand.palmBone.transform.forward * Mathf.Lerp(0.06f, 0.02f, lerp)),
                0.1f, _colliderCache, contactManager.InteractionMask);

            GrabHelperObject tempHelper;
            for (int i = 0; i < results; i++)
            {
                if (_colliderCache[i].attachedRigidbody != null)
                {
                    _hoveringHands.Add(hand);
                    _hoveredItems.Add(_colliderCache[i].attachedRigidbody);
                    if (_grabHelpers.TryGetValue(_colliderCache[i].attachedRigidbody, out tempHelper))
                    {
                        tempHelper.AddHand(hand);
                    }
                    else
                    {
                        GrabHelperObject helper = new GrabHelperObject(_colliderCache[i].attachedRigidbody, this);
                        helper.AddHand(hand);
                        _grabHelpers.Add(_colliderCache[i].attachedRigidbody, helper);
                        //SendStates(_colliderCache[i].attachedRigidbody, helper);
                    }
                }
            }
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
            HandSafetyOverlaps(_leftContactHand);
            HandSafetyOverlaps(_rightContactHand);
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
        #endregion

        #region Helper Updating
        private void UpdateHelpers()
        {
            ApplyHovers();

            GrabHelperObject.State oldState, state;

            foreach (var helper in _grabHelpers)
            {
                // Removed ignore check here and moved into the helper so we can still get state information
                oldState = helper.Value.GraspState;
                state = helper.Value.UpdateHelper();
                if (state != oldState)
                {
                    //SendStates(helper.Value.Rigidbody, helper.Value);
                }
            }
        }

        private void ApplyHovers()
        {
            foreach (var rigid in _hoveredItems)
            {
                _grabRigids.Add(rigid);
            }
            _grabRigids.RemoveWhere(ValidateUnhoverRigids);
            _hoveredItems.Clear();
        }

        private bool ValidateUnhoverRigids(Rigidbody rigid)
        {
            if (!_hoveredItems.Contains(rigid))
            {
                if (_grabHelpers.ContainsKey(rigid))
                {
                    if (_grabHelpers[rigid].GraspState == GrabHelperObject.State.Grasp)
                    {
                        // Ensure we release the object first
                        _grabHelpers[rigid].ReleaseObject();
                    }
                    _grabHelpers[rigid].ReleaseHelper();
//                    SendStates(rigid, _grabHelpers[rigid]);

                    _grabHelpers.Remove(rigid);
                }
                return true;
            }
            return false;
        }
        #endregion

        private void OnValidate()
        {
            if (contactManager == null)
            {
                contactManager = GetComponent<ContactManager>();
            }
        }
    }
}