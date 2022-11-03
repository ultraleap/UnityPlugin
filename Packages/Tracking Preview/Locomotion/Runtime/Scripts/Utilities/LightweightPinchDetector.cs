using System;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    public class LightweightPinchDetector : MonoBehaviour
    {
        public LeapProvider leapProvider;
        public Chirality chirality;

        [Header("Pinch Activation Settings")]
        [Tooltip("The distance between index and thumb at which to enter the pinching state.")]
        public float activateDistance = .018f;
        [Tooltip("The distance between index and thumb at which to leave the pinching state.")]
        public float deactivateDistance = .024f;

        public Action<Hand> OnPinch, OnUnpinch, OnPinching;

        /// <summary>
        /// The percent value (0-1) between the activate distance and absolute pinch.
        /// Note that it is virtually impossible for the hand to be completely pinched.
        /// </summary>
        public float SquishPercent { get; private set; }

        private Chirality _chiralityLastFrame;

        public bool IsPinching { get; private set; }

        private void Start()
        {
            _chiralityLastFrame = chirality;
            if (leapProvider == null)
            {
                leapProvider = FindObjectOfType<LeapProvider>();
            }
        }

        private void Update()
        {
            if (_chiralityLastFrame != chirality)
            {
                IsPinching = false;
            }
            UpdatePinchStatus(leapProvider.CurrentFrame.GetHand(chirality));
            _chiralityLastFrame = chirality;
        }

        private void UpdatePinchStatus(Hand hand)
        {
            if (hand == null)
            {
                return;
            }

            if (hand.PinchDistance * 0.001f < activateDistance)
            {
                if (!IsPinching)
                {
                    OnPinch?.Invoke(hand);
                }
                else
                {
                    OnPinching?.Invoke(hand);
                }

                IsPinching = true;
            }
            else if (hand.PinchDistance * 0.001f > deactivateDistance)
            {
                if (IsPinching)
                {
                    OnUnpinch?.Invoke(hand);
                }
                IsPinching = false;
            }

            if (IsPinching)
            {
                SquishPercent = Mathf.InverseLerp(activateDistance, 0, hand.PinchDistance * 0.001f);
            }
            else
            {
                SquishPercent = 0;
            }
        }
    }
}