using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Android.Types;
using UnityEngine;


namespace Leap.Unity
{
    public abstract class ActionDetector : MonoBehaviour
    {

        public bool IsDoingAction { get; protected set; }
        public bool ActionStartedThisFrame => actionStartedThisFrame;
        public bool actionStartedThisFrame = false;

        public LeapProvider leapProvider;  // Reference to the Leap provider
        public Chirality chirality;        // Specifies which hand (left or right) to track
        public Action<Hand> onActionStart, onActionEnd, onAction;  // Events for pinch state changes

        protected Chirality chiralityLastFrame;  // Stores chirality state from last frame

        /// <summary>
        /// The percent value (0-1) between the activate distance and absolute pinch.
        /// Note that it is virtually impossible for the hand to be completely pinched.
        /// </summary>
        public float SquishPercent { get; protected set; }

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
