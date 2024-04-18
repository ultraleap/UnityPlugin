using Leap.Unity.PhysicalHands;
using UnityEngine;

namespace Leap.Unity
{
    public class PhysicalHandsAnchorable : AnchorableBehaviour, IPhysicalHandGrab, IPhysicalHandHover
    {
        private IgnorePhysicalHands _ignorePhysicalHands = null;
        private ChiralitySelection originalIgnoreHands = ChiralitySelection.NONE;

        private bool leftHandGrabbing;
        private bool rightHandGrabbing;

        protected override void Start()
        {
            base.Start();

            OnAttachedToAnchor -= OnAttached;
            OnAttachedToAnchor += OnAttached;

            OnDetachedFromAnchor -= OnDetached;
            OnDetachedFromAnchor += OnDetached;

            if (TryGetComponent<IgnorePhysicalHands>(out _ignorePhysicalHands))
            {
                originalIgnoreHands = _ignorePhysicalHands.HandToIgnore;
            }
        }

        void OnAttached()
        {
            if (Anchor.AttahedHandChirality != ChiralitySelection.NONE)
            {
                if (!TryGetComponent<IgnorePhysicalHands>(out _ignorePhysicalHands))
                {
                    _ignorePhysicalHands = this.gameObject.AddComponent<IgnorePhysicalHands>();
                }

                _ignorePhysicalHands.HandToIgnore = Anchor.AttahedHandChirality;
            }
        }

        void OnDetached()
        {
            if(_ignorePhysicalHands != null)
            {
                _ignorePhysicalHands.HandToIgnore = originalIgnoreHands;
            }
        }

        void IPhysicalHandGrab.OnHandGrab(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Left)
            {
                leftHandGrabbing = true;
            }
            else
            {
                rightHandGrabbing = true;
            }

            if (detachWhenGrasped)
            {
                Detach();
            }
        }

        void IPhysicalHandGrab.OnHandGrabExit(ContactHand hand)
        {
            if (hand.Handedness == Chirality.Left)
            {
                leftHandGrabbing = false;
            }
            else
            {
                rightHandGrabbing = false;
            }

            if(!leftHandGrabbing && !rightHandGrabbing && TryAnchorNearestOnGraspEnd)
            {
                // Neither hand is grabbing. Object has been dropped
                TryAttachToNearestAnchor();
                OnPostTryAnchorOnGraspEnd();
            }
        }

        void IPhysicalHandHover.OnHandHover(ContactHand hand)
        {
            _hoveringHands.Add(hand.DataHand);
        }

        void IPhysicalHandHover.OnHandHoverExit(ContactHand hand)
        {
            _hoveringHands.Remove(hand.DataHand);
        }
    }
}