/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/
using System;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    /// <summary>
    /// A lightweight Grab Detector, that calculates a pinch value based on the hand's reported grab strength 
    /// Utilises hysteresis in order to have different grab and ungrab thresholds.
    /// </summary>
    public class LightweightGrabDetector : MonoBehaviour
    {
        public LeapProvider leapProvider;
        public Chirality chirality;

        [Header("Fist Pose Activation Settings")]
        [Range(0,1), Tooltip("The grab strength at which grabbing is true")]
        public float activateStrength = 1;
        [Range(0, 1), Tooltip("The grab strength at which grabbing is false")]
        public float deactivateStrength = 0.65f;

        public Action<Hand> OnGrab, OnUngrab, OnGrabbing;

        private Chirality _chiralityLastFrame;

        public bool IsGrabbing { get; private set; }

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
                IsGrabbing = false;
            }
            UpdateGrabDetection(leapProvider.CurrentFrame.GetHand(chirality));
            _chiralityLastFrame = chirality;
        }

        private void UpdateGrabDetection(Hand hand)
        {
            if (hand == null)
            {
                return;
            }

            if (hand.GrabStrength >= activateStrength)
            {
                if (!IsGrabbing)
                {
                    OnGrab?.Invoke(hand);
                }
                else
                {
                    OnGrabbing?.Invoke(hand);
                }

                IsGrabbing = true;
            }
            else if (hand.GrabStrength <= deactivateStrength)
            {
                if (IsGrabbing)
                {
                    OnUngrab?.Invoke(hand);
                }
                IsGrabbing = false;
            }
        }
    }
}