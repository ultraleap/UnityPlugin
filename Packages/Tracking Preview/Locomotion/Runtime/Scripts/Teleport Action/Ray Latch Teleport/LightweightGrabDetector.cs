using System;
using UnityEngine;

namespace Leap.Unity.Preview.Locomotion
{
    public class LightweightGrabDetector : MonoBehaviour
    {
        public LeapProvider leapProvider;
        public Chirality chirality;

        [Header("Fist Pose Activation Settings")]
        [Range(0,1), Tooltip("The grab strength at which to enter the fist pose state")]
        public float activateStrength = 1;
        [Range(0, 1), Tooltip("The grab strength at which to leave the fist pose state")]
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

            if (hand.GrabStrength < activateStrength)
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
            else if (hand.GrabStrength > deactivateStrength)
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