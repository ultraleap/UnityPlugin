using System.Collections.Generic;
using UnityEngine;
namespace Leap.Unity.PhysicalHands
{
    [RequireComponent(typeof(PhysicalHandsManager))]
    public class GrabHelper : MonoBehaviour
    {
        public PhysicalHandsManager physicalHandsManager;

        private bool _leftGoodState { get { return physicalHandsManager.contactParent.LeftHand.InGoodState; } }
        private bool _rightGoodState { get { return physicalHandsManager.contactParent.RightHand.InGoodState; } }

        private ContactHand _leftContactHand { get { return physicalHandsManager.contactParent.LeftHand; } }
        private ContactHand _rightContactHand { get { return physicalHandsManager.contactParent.RightHand; } }

        private Collider[] _colliderCache = new Collider[128];

        #region Interaction Data
        private Dictionary<ContactHand, float[]> _fingerStrengths = new Dictionary<ContactHand, float[]>();
        public Dictionary<ContactHand, float[]> FingerStrengths => _fingerStrengths;

        // Helpers
        private Dictionary<Rigidbody, GrabHelperObject> _grabHelpers = new Dictionary<Rigidbody, GrabHelperObject>();
        private HashSet<Rigidbody> _grabRigids = new HashSet<Rigidbody>();
        private HashSet<Rigidbody> _hoveredItems = new HashSet<Rigidbody>();
        private HashSet<ContactHand> _hoveringHands = new HashSet<ContactHand>();

        #endregion

        private void OnEnable()
        {
            if (physicalHandsManager != null)
            {
                physicalHandsManager.OnPrePhysicsUpdate -= OnPrePhysicsUpdate;
                physicalHandsManager.OnPrePhysicsUpdate += OnPrePhysicsUpdate;
                physicalHandsManager.OnFixedFrame -= OnFixedFrame;
                physicalHandsManager.OnFixedFrame += OnFixedFrame;
            }
        }

        private void OnDisable()
        {
            if (physicalHandsManager != null)
            {
                physicalHandsManager.OnFixedFrame -= OnFixedFrame;
            }
        }

        private void OnPrePhysicsUpdate()
        {
            UpdateOverlaps();
        }

        private void OnFixedFrame(Frame frame)
        {
            UpdateHandHeuristics();
            UpdateHelpers();
            UpdateHandStates();
        }

        #region Hand Updating
        private void UpdateHandHeuristics()
        {
            if (_leftContactHand.tracked)
            {
                UpdateHandStatistics(_leftContactHand);
            }
            else
            {
                UntrackedHand(_leftContactHand);
            }
            if (_rightContactHand.tracked)
            {
                UpdateHandStatistics(_rightContactHand);
            }
            else
            {
                UntrackedHand(_rightContactHand);
            }
        }

        private void UpdateOverlaps()
        {
            HandOverlaps(_leftContactHand);
            HandOverlaps(_rightContactHand);
        }

        private void UntrackedHand(ContactHand hand)
        {
            foreach (var helper in _grabHelpers)
            {
                helper.Value.RemoveHand(hand);
            }
        }

        private void HandOverlaps(ContactHand hand)
        {
            hand.isHovering = false;
            hand.isContacting = false;
            hand.isCloseToObject = false;

            HelperOverlaps(hand);
        }

        /// <summary>
        /// Used to create helper objects.
        /// </summary>
        private void HelperOverlaps(ContactHand hand)
        {
            float lerp = float.MaxValue;
            if (_fingerStrengths.ContainsKey(hand))
            {
                // Get the least curled finger excluding the thumb
                var curls = _fingerStrengths[hand];

                for(int i = 1; i < curls.Length; i++)
                {
                    if (curls[i] < lerp)
                    {
                        lerp = curls[i];
                    }
                }
            }

            float radiusAmount = PhysExts.MaxVec3(Vector3.Scale(hand.palmBone.palmCollider.size, PhysExts.AbsVec3(hand.palmBone.transform.lossyScale)));

            int results = Physics.OverlapCapsuleNonAlloc(hand.palmBone.transform.position + (-hand.palmBone.transform.up * 0.025f) + ((hand.handedness == Chirality.Left ? hand.palmBone.transform.right : -hand.palmBone.transform.right) * 0.015f),
                // Interpolate the tip position so we keep it relative to the straightest finger
                hand.palmBone.transform.position + (-hand.palmBone.transform.up * Mathf.Lerp(0.025f, 0.07f, lerp)) + (hand.palmBone.transform.forward * Mathf.Lerp(0.06f, 0.02f, lerp)),
                radiusAmount + (physicalHandsManager.HoverDistance * 2f), _colliderCache, physicalHandsManager.InteractionMask);

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

            hand.palmBone.ProcessColliderQueue(_colliderCache, results);
            foreach (var bone in hand.bones)
            {
                bone.ProcessColliderQueue(_colliderCache, results);
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
                oldState = helper.Value.GrabState;
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
                    if (_grabHelpers[rigid].GrabState == GrabHelperObject.State.Grab)
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

        #region Hand States
        private void UpdateHandStates()
        {
            if (_leftGoodState)
            {
                FindHandState(_leftContactHand);
            }
            else
            {
                _leftContactHand.isGrabbing = false;
            }
            if (_rightGoodState)
            {
                FindHandState(_rightContactHand);
            }
            else
            {
                _rightContactHand.isGrabbing = false;
            }
        }

        private void FindHandState(ContactHand hand)
        {
            if (hand == null)
            {
                return;
            }

            bool found = false;
            float mass = 0;
            foreach (var item in _grabHelpers)
            {
                if (item.Value.GrabbingHands.Contains(hand))
                {
                    found = true;
                    mass += item.Value.OriginalMass;
                    break;
                }
            }
            if (found != hand.IsGrabbing)
            {
                hand.isGrabbing = found;
            }
            hand.grabMass = mass;
        }
        #endregion

        private void OnValidate()
        {
            if (physicalHandsManager == null)
            {
                physicalHandsManager = GetComponent<PhysicalHandsManager>();
            }
        }
    }
}