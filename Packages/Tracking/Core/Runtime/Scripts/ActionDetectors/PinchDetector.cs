/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap
{
    /// <summary>
    /// A lightweight Pinch Detector, that calculates a pinch value based on the distance between the 
    /// provided finger tip and the thumb tip. Utilizes hysteresis to have different pinch and unpinch thresholds.
    /// </summary>
    public class PinchDetector : ActionDetector
    {
        // Enumeration for different types of pinchable fingers
        public enum PinchableFingerType
        {
            INDEX = Finger.FingerType.INDEX,
            MIDDLE = Finger.FingerType.MIDDLE,
            RING = Finger.FingerType.RING,
            PINKY = Finger.FingerType.PINKY,
        }

        /// <summary>
        /// The finger to check for pinches against the thumb.
        /// </summary>
        public PinchableFingerType fingerType = PinchableFingerType.INDEX;

        [Header("Pinch Activation Settings")]
        [SerializeField, Tooltip("The distance between fingertip and thumb at which to enter the pinching state.")]
        public float activateDistance = 0.025f;

        [SerializeField, Tooltip("The distance between fingertip and thumb at which to leave the pinching state.")]
        public float deactivateDistance = 0.03f;

        /// <summary>
        /// The percent value (0-1) between the activate distance and absolute pinch.
        /// Note that it is virtually impossible for the hand to be completely pinched.
        /// </summary>
        public float SquishPercent { get; protected set; }

        /// <summary>
        /// Did the Pinch start this frame?
        /// </summary>
        public bool PinchStartedThisFrame => actionStartedThisFrame;

        /// <summary>
        /// Is the PinchDetector currenty detecting a pinch?
        /// </summary>
        public bool IsPinching => IsDoingAction;

        // Accessor actions for readability
        public Action<Hand> OnPinchStart => onActionStart;
        public Action<Hand> OnPinchEnd => onActionEnd;
        public Action<Hand> OnPinching => onAction;

        /// <summary>
        /// Updates the pinch status based on the hand data.
        /// </summary>
        /// <param name="_hand">Hand data from the Leap provider</param>
        protected override void UpdateActionStatus(Hand _hand)
        {
            if (_hand == null)
            {
                return;
            }

            float _pinchDistance = _hand.GetFingerPinchDistance((int)fingerType);
            if (_pinchDistance < activateDistance)
            {
                if (!IsDoingAction)
                {
                    actionStartedThisFrame = true;
                    onActionStart?.Invoke(_hand);
                }
                else
                {
                    actionStartedThisFrame = false;
                }

                IsDoingAction = true;
            }
            else if (_pinchDistance > deactivateDistance)
            {
                if (IsDoingAction)
                {
                    onActionEnd?.Invoke(_hand);
                }
                IsDoingAction = false;
            }

            if (IsDoingAction)
            {
                onAction?.Invoke(_hand);
                SquishPercent = Mathf.InverseLerp(activateDistance, 0, _pinchDistance);
            }
            else
            {
                SquishPercent = 0;
            }
        }
    }
}