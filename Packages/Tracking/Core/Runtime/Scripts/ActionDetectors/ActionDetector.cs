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
    public abstract class ActionDetector : MonoBehaviour
    {
        public bool IsDoingAction { get; protected set; }

        protected bool actionStartedThisFrame = false;
        public bool ActionStartedThisFrame => actionStartedThisFrame;

        public LeapProvider leapProvider;  // Reference to the Leap provider
        public Chirality chirality;        // Specifies which hand (left or right) to track
        public Action<Hand> onActionStart, onActionEnd, onAction;  // Events for action state changes

        protected Chirality chiralityLastFrame;  // Stores chirality state from last frame

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
        /// Initializes the ActionDetector, sets initial chirality state, and gets the LeapProvider if not assigned.
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
        /// Updates the action status every frame.
        /// </summary>
        private void Update()
        {
            if (chiralityLastFrame != chirality)
            {
                IsDoingAction = false;
            }
            if (leapProvider != null && leapProvider.CurrentFrame != null)
            {
                UpdateActionStatus(leapProvider.CurrentFrame.GetHand(chirality));
            }
            chiralityLastFrame = chirality;
        }

        protected abstract void UpdateActionStatus(Hand _hand);
    }
}