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
    /// A lightweight Grab Detector, that calculates a grab value based on the grab strength of the hand.
    /// Utilizes hysteresis to have different grab and ungrab thresholds.
    /// </summary>
    public class GrabDetector : ActionDetector
    {
        [SerializeField, Range(0, 1), Tooltip("The grab strength needed to initiate a grab. 1 being hand fully closed, 0 being open hand")]
        public float activateStrength = 0.8f;

        [SerializeField, Range(0, 1), Tooltip("The grab strength needed to release a grab. 1 being hand fully closed, 0 being open hand")]
        public float deactivateStrength = 0.6f;

        /// <summary>
        /// Did the Grab start this frame?
        /// </summary>
        public bool GrabStartedThisFrame => actionStartedThisFrame;

        /// <summary>
        /// Is the PinchDetector currenty detecting a pinch?
        /// </summary>
        public bool IsGrabbing => IsDoingAction;

        // Accessor actions for readability
        public Action<Hand> OnGrabStart => onActionStart;
        public Action<Hand> OnGrabEnd => onActionEnd;
        public Action<Hand> OnGrabbing => onAction;

        /// <summary>
        /// Updates the grab status based on the hand data.
        /// </summary>
        /// <param name="_hand">Hand data from the Leap provider</param>
        protected override void UpdateActionStatus(Hand _hand)
        {
            if (_hand == null)
            {
                return;
            }

            float _grabStrength = _hand.GrabStrength;
            if (_grabStrength < activateStrength)
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
            else if (_grabStrength > deactivateStrength)
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
            }
        }
    }
}