/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity
{
    /// <summary>
    /// A lightweight Pinch Detector, that calculates a pinch value based on the distance between the 
    /// provided finger tip and the thumb tip. Utilises hysteresis in order to have different pinch and unpinch thresholds.
    /// </summary>
    public class PinchDetector : MonoBehaviour
    {
        public enum PinchableFingerType
        {
            INDEX = Finger.FingerType.INDEX,
            MIDDLE = Finger.FingerType.MIDDLE,
            RING = Finger.FingerType.RING,
            PINKY = Finger.FingerType.PINKY,
        }

        public LeapProvider leapProvider;
        public Chirality chirality;

        /// <summary>
        /// The finger to check for pinches against the thumb
        /// </summary>
        public PinchableFingerType fingerType = PinchableFingerType.INDEX;

        [Header("Pinch Activation Settings")]
        [SerializeField, Tooltip("The distance between fingertip and thumb at which to enter the pinching state.")]
        public float activateDistance = 0.025f;
        [SerializeField, Tooltip("The distance between fingertip and thumb at which to leave the pinching state.")]
        public float deactivateDistance = 0.03f;

        public Action<Hand> OnPinch, OnUnpinch, OnPinching;

        public bool IsPinching { get; private set; }

        public bool PinchStartedThisFrame => startedPinchThisFrame;
        private bool startedPinchThisFrame = false;
        /// <summary>
        /// The percent value (0-1) between the activate distance and absolute pinch.
        /// Note that it is virtually impossible for the hand to be completely pinched.
        /// </summary>
        public float SquishPercent { get; private set; }

        private Chirality chiralityLastFrame;

        public bool TryGetHand(out Hand hand)
        {
            if(leapProvider != null)
            {
                hand = leapProvider.GetHand(chirality);
                if (hand != null)
                {
                    return true;
                }
            }
            hand = null;
            return false;
        }
        public bool IsTracked()
        {
            if (leapProvider != null)
            {
                return true;
            }
            return false;
        }

        private void Start()
        {
            chiralityLastFrame = chirality;
            if (leapProvider == null)
            {
                leapProvider = Hands.Provider;
            }
        }

        private void Update()
        {
            if (chiralityLastFrame != chirality)
            {
                IsPinching = false;
            }
            if (leapProvider != null && leapProvider.CurrentFrame != null)
            {
                UpdatePinchStatus(leapProvider.CurrentFrame.GetHand(chirality));
            }
            chiralityLastFrame = chirality;
        }

        private void UpdatePinchStatus(Hand hand)
        {
            if (hand == null)
            {
                return;
            }

            float _pinchDistance = hand.GetFingerPinchDistance((int)fingerType);
            if (_pinchDistance < activateDistance)
            {
                if (!IsPinching)
                {
                    startedPinchThisFrame = true;
                    OnPinch?.Invoke(hand);
                }
                else
                {
                    startedPinchThisFrame = false;
                }

                IsPinching = true;
            }
            else if (_pinchDistance > deactivateDistance)
            {
                if (IsPinching)
                {
                    OnUnpinch?.Invoke(hand);
                }
                IsPinching = false;
            }

            if (IsPinching)
            {
                OnPinching?.Invoke(hand);
                SquishPercent = Mathf.InverseLerp(activateDistance, 0, _pinchDistance);
            }
            else
            {
                SquishPercent = 0;
            }
        }
    }
}