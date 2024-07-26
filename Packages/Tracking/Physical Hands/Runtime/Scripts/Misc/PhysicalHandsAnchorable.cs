using Leap.PhysicalHands;
using UnityEngine;

namespace Leap
{
    public class PhysicalHandsAnchorable : AnchorableBehaviour, IPhysicalHandGrab, IPhysicalHandHover
    {
        private IgnorePhysicalHands _ignorePhysicalHands = null;
        private ChiralitySelection originalIgnoreHandsGrab = ChiralitySelection.NONE;
        private ChiralitySelection originalIgnoreHandsCollisions = ChiralitySelection.NONE;

        private bool leftHandGrabbing;
        private bool rightHandGrabbing;

        protected override void Start()
        {
            base.Start();

            OnAttachedToAnchor -= OnAttached;
            OnAttachedToAnchor += OnAttached;

            OnDetachedFromAnchor -= OnDetached;
            OnDetachedFromAnchor += OnDetached;

            // Cache the original ignoring hands if they exist so we can revert to them when un-anchored
            if (TryGetComponent<IgnorePhysicalHands>(out _ignorePhysicalHands))
            {
                originalIgnoreHandsGrab = _ignorePhysicalHands.HandToIgnoreGrabs;
                originalIgnoreHandsCollisions = _ignorePhysicalHands.HandToIgnoreCollisions;
            }
        }

        // If the anchor is an attachment hand, we should ignore grabbing with the specific achored hand
        void OnAttached()
        {
            if (Anchor.AttahedHandChirality != ChiralitySelection.NONE)
            {
                if (!TryGetComponent<IgnorePhysicalHands>(out _ignorePhysicalHands))
                {
                    _ignorePhysicalHands = this.gameObject.AddComponent<IgnorePhysicalHands>();
                }

                _ignorePhysicalHands.HandToIgnoreGrabs = Anchor.AttahedHandChirality;
                _ignorePhysicalHands.HandToIgnoreCollisions = Anchor.AttahedHandChirality;
            }
        }

        // Revert to original ignorehand state if there was one
        void OnDetached()
        {
            if (_ignorePhysicalHands != null)
            {
                _ignorePhysicalHands.HandToIgnoreGrabs = originalIgnoreHandsGrab;
                _ignorePhysicalHands.HandToIgnoreCollisions = originalIgnoreHandsCollisions;
            }
        }

        // Keep track of grabbing hands to determine if we should detach or look for possible attachments
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

            if (!leftHandGrabbing && !rightHandGrabbing && TryAnchorNearestOnGraspEnd)
            {
                // Neither hand is grabbing. Object has been dropped
                TryAttachToNearestAnchor();
                OnPostTryAnchorOnGraspEnd();
            }
        }


        // Keep track of hovering hands to feed into the AnchorableBehaviour logic
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