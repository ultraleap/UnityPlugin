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
    /// provided finger tip and the thumb tip. Utilizes hysteresis to have different pinch and unpinch thresholds.
    /// </summary>
    public class PinchDetector : MonoBehaviour
    {
        // Enumeration for different types of pinchable fingers
        public enum PinchableFingerType
        {
            INDEX = Finger.FingerType.INDEX,
            MIDDLE = Finger.FingerType.MIDDLE,
            RING = Finger.FingerType.RING,
            PINKY = Finger.FingerType.PINKY,
        }

        public LeapProvider leapProvider;  // Reference to the Leap provider
        public Chirality chirality;        // Specifies which hand (left or right) to track

        /// <summary>
        /// The finger to check for pinches against the thumb.
        /// </summary>
        public PinchableFingerType fingerType = PinchableFingerType.INDEX;

        [Header("Pinch Activation Settings")]
        [SerializeField, Tooltip("The distance between fingertip and thumb at which to enter the pinching state.")]
        public float activateDistance = 0.018f;  // Distance to activate pinch

        [SerializeField, Tooltip("The distance between fingertip and thumb at which to leave the pinching state.")]
        public float deactivateDistance = 0.024f;  // Distance to deactivate pinch

        public Action<Hand> onPinch, onUnpinch, onPinching;  // Events for pinch state changes

        public bool IsPinching { get; private set; }  // Is the pinch currently active

        public bool PinchStartedThisFrame => startedPinchThisFrame;
        private bool startedPinchThisFrame = false;  // Indicates if pinch started this frame

        /// <summary>
        /// The percent value (0-1) between the activate distance and absolute pinch.
        /// Note that it is virtually impossible for the hand to be completely pinched.
        /// </summary>
        public float SquishPercent { get; private set; }

        private Chirality chiralityLastFrame;  // Stores chirality state from last frame

        /// <summary>
        /// Tries to get the hand based on the provided chirality.
        /// </summary>
        /// <param name="_hand">Output parameter for the hand</param>
        /// <returns>True if hand is found, otherwise false</returns>
        public bool TryGetHand(out Hand _hand)
        {
            if (leapProvider != null)
            {
                _hand = leapProvider.GetHand(chirality);
                if (_hand != null)
                {
                    return true;
                }
            }
            _hand = null;
            return false;
        }

        /// <summary>
        /// Checks if the Leap Provider is tracking hands.
        /// </summary>
        /// <returns>True if tracking, otherwise false</returns>
        public bool IsTracked()
        {
            if (leapProvider != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Initializes the PinchDetector, sets initial chirality state, and gets the LeapProvider if not assigned.
        /// </summary>
        private void Start()
        {
            chiralityLastFrame = chirality;
            if (leapProvider == null)
            {
                leapProvider = Hands.Provider;
            }
        }

        /// <summary>
        /// Updates the pinch status every frame.
        /// </summary>
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

        /// <summary>
        /// Updates the pinch status based on the hand data.
        /// </summary>
        /// <param name="_hand">Hand data from the Leap provider</param>
        private void UpdatePinchStatus(Hand _hand)
        {
            if (_hand == null)
            {
                return;
            }

            float _pinchDistance = _hand.GetFingerPinchDistance((int)fingerType);
            if (_pinchDistance < activateDistance)
            {
                if (!IsPinching)
                {
                    startedPinchThisFrame = true;
                    onPinch?.Invoke(_hand);
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
                    onUnpinch?.Invoke(_hand);
                }
                IsPinching = false;
            }

            if (IsPinching)
            {
                onPinching?.Invoke(_hand);
                SquishPercent = Mathf.InverseLerp(activateDistance, 0, _pinchDistance);
            }
            else
            {
                SquishPercent = 0;
            }
        }
    }
}