/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    /// <summary>
    /// A lightweight Pinch Detector, that calculates a pinch value based on the distance between the 
    /// provided finger tip and the thumb tip. Utilises hysteresis in order to have different pinch and unpinch thresholds.
    /// </summary>
    public class LightweightPinchDetector : MonoBehaviour
    {
        public enum PinchableFingerType
        {
            INDEX = Finger.FingerType.TYPE_INDEX,
            MIDDLE = Finger.FingerType.TYPE_MIDDLE,
            RING = Finger.FingerType.TYPE_RING,
            PINKY = Finger.FingerType.TYPE_PINKY,
        }

        public LeapProvider leapProvider;
        public Chirality chirality;

        /// <summary>
        /// The finger to check for pinches against the thumb
        /// </summary>
        public PinchableFingerType fingerType = PinchableFingerType.INDEX;

        [Header("Pinch Activation Settings")]
        [Tooltip("The distance between fingertip and thumb at which to enter the pinching state.")]
        public float activateDistance = 0.018f;
        [Tooltip("The distance between fingertip and thumb at which to leave the pinching state.")]
        public float deactivateDistance = 0.024f;

        public Action<Hand> OnPinch, OnUnpinch, OnPinching;

        public bool IsPinching { get; private set; }
        /// <summary>
        /// The percent value (0-1) between the activate distance and absolute pinch.
        /// Note that it is virtually impossible for the hand to be completely pinched.
        /// </summary>
        public float SquishPercent { get; private set; }

        private Chirality _chiralityLastFrame;

        private void Start()
        {
            _chiralityLastFrame = chirality;
            if (leapProvider == null)
            {
                leapProvider = FindAnyObjectByType<LeapProvider>();
            }
        }

        private void Update()
        {
            if (_chiralityLastFrame != chirality)
            {
                IsPinching = false;
            }
            if (leapProvider != null && leapProvider.CurrentFrame != null)
            {
                UpdatePinchStatus(leapProvider.CurrentFrame.GetHand(chirality));
            }
            _chiralityLastFrame = chirality;
        }

        private void UpdatePinchStatus(Hand hand)
        {
            if (hand == null)
            {
                return;
            }

            float pinchDistance = hand.GetFingerPinchDistance((int)fingerType);
            if (pinchDistance < activateDistance)
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
            else if (pinchDistance > deactivateDistance)
            {
                if (IsPinching)
                {
                    OnUnpinch?.Invoke(hand);
                }
                IsPinching = false;
            }

            if (IsPinching)
            {
                SquishPercent = Mathf.InverseLerp(activateDistance, 0, pinchDistance);
            }
            else
            {
                SquishPercent = 0;
            }
        }
    }
}