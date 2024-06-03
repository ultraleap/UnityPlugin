/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.
 *
 * Use subject to the terms of the Apache License 2.0 available at
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement
 * between Ultraleap and you, your company or other organization.
 ******************************************************************************/

using System;
using UnityEngine;

namespace Leap.Unity
{
    /// <summary>
    /// A lightweight Grab Detector, that calculates a grab value based on the grab strength of the hand.
    /// Utilizes hysteresis to have different grab and ungrab thresholds.
    /// </summary>
    public class GrabDetector : MonoBehaviour
    {
        public bool IsGrabbing { get; private set; }  // Indicates if the hand is currently grabbing
        /// <summary>
        /// The percent value (0-1) between the activate strength and absolute grab.
        /// Note that it is virtually impossible for the hand to be completely grabbed.
        /// </summary>
        public float SquishPercent { get; private set; }

        [SerializeField, Range(0, 1), Tooltip("The grab strength needed to initiate a grab. 1 being hand fully closed, 0 being open hand")]
        public float activateStrength = 0.8f;  // Strength required to start grab

        [SerializeField, Range(0, 1), Tooltip("The grab strength needed to release a grab. 1 being hand fully closed, 0 being open hand")]
        public float deactivateStrength = 0.6f;  // Strength required to release grab

        public LeapProvider leapProvider;  // Reference to the Leap provider
        public Chirality chirality;        // Specifies which hand (left or right) to track
        private Chirality chiralityLastFrame;  // Stores chirality state from last frame

        public Action<Hand> onGrab, onUngrab, onGrabbing;  // Events for grab state changes

        public bool GrabStartedThisFrame => startedGrabThisFrame;
        private bool startedGrabThisFrame = false;  // Indicates if grab started this frame

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
        /// Initializes the GrabDetector, sets initial chirality state, and gets the LeapProvider if not assigned.
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
        /// Updates the grab status every frame.
        /// </summary>
        private void Update()
        {
            if (chiralityLastFrame != chirality)
            {
                IsGrabbing = false;
            }
            if (leapProvider != null && leapProvider.CurrentFrame != null)
            {
                UpdateGrabStatus(leapProvider.CurrentFrame.GetHand(chirality));
            }
            chiralityLastFrame = chirality;
        }

        /// <summary>
        /// Updates the grab status based on the hand data.
        /// </summary>
        /// <param name="_hand">Hand data from the Leap provider</param>
        private void UpdateGrabStatus(Hand _hand)
        {
            if (_hand == null)
            {
                return;
            }

            float _grabStrength = _hand.GrabStrength;
            if (_grabStrength < activateStrength)
            {
                if (!IsGrabbing)
                {
                    startedGrabThisFrame = true;
                    onGrab?.Invoke(_hand);
                }
                else
                {
                    startedGrabThisFrame = false;
                }

                IsGrabbing = true;
            }
            else if (_grabStrength > deactivateStrength)
            {
                if (IsGrabbing)
                {
                    onUngrab?.Invoke(_hand);
                }
                IsGrabbing = false;
            }

            if (IsGrabbing)
            {
                onGrabbing?.Invoke(_hand);
                SquishPercent = Mathf.InverseLerp(activateStrength, 0, _grabStrength);
            }
            else
            {
                SquishPercent = 0;
            }
        }
    }
}
