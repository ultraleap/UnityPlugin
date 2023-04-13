/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace Leap.Unity.Preview.InputActions
{
    /// <summary>
    /// PALM_DIRECTION - Uses the palm normal to set the direction of the hands
    /// PINCH_DIRECTION - Determines a pinch direction based on an assumed shoulder position relative to the camera
    /// </summary>
    public enum LeapInputDirection
    {
        PALM_DIRECTION,
        PINCH_DIRECTION
    }

    /// <summary>
    /// Updates Ultraleap Input Actions with the latest available hand data
    /// Must be provided information from a LeapProvider
    /// </summary>
    public class LeapInputActionUpdater : PostProcessProvider
    {
        private LeapHandState _leftState = new LeapHandState(), _rightState = new LeapHandState();

        private InputDevice _leftDevice, _rightDevice;

        [Tooltip("The source of the hand direction output. Adjust depending on your use case")]
        public LeapInputDirection directionSource = LeapInputDirection.PINCH_DIRECTION;

        [Tooltip("Should the pinching InputAction be set via a binary decision, or a float PinchStrength?")]
        public bool isPinchingIsBinary = true;

        [Tooltip("Should the grabbing InputAction be set via a binary decision, or a float GrabStrength?")]
        public bool isGrabbingIsBinary = true;

        [Tooltip("Should interactions (e.g. grabbing and pinching) reset when the hand is lost?")]
        public bool resetInteractionsOnTrackingLost = true;

        private void Start()
        {
            InputSystem.onBeforeUpdate += InputSystem_onBeforeUpdate;
        }

        private void OnDestroy()
        {
            InputSystem.onBeforeUpdate -= InputSystem_onBeforeUpdate;
        }

        private void InputSystem_onBeforeUpdate()
        {
            if (Application.isPlaying)
            {
                UpdateStateWithLeapHand(_leftDevice, _inputLeapProvider.CurrentFrame.GetHand(Chirality.Left), ref _leftState);
                UpdateStateWithLeapHand(_rightDevice, _inputLeapProvider.CurrentFrame.GetHand(Chirality.Right), ref _rightState);
            }
        }

        public override void ProcessFrame(ref Frame inputFrame)
        {
            // Running this on ProcessFrame causes jittery results. Probably due to the way the QueueStateEvent is handled, use InputSystem.onBeforeUpdate instead
            //if (Application.isPlaying) 
            //{
            //    UpdateStateWithLeapHand(_leftDevice, inputFrame.GetHand(Chirality.Left), ref _leftState);
            //    UpdateStateWithLeapHand(_rightDevice, inputFrame.GetHand(Chirality.Right), ref _rightState);
            //}
        }

        /// <summary>
        /// Handles updating the inputDevice with chosen data from the hand using the cached handState structure as a base
        /// </summary>
        private void UpdateStateWithLeapHand(InputDevice inputDevice, Hand hand, ref LeapHandState handState)
        {
            if (inputDevice != null)
            {
                if (hand != null)
                {
                    ConvertLeapHandToState(ref handState, hand);
                    InputSystem.QueueStateEvent(inputDevice, handState);
                }
                else
                {
                    if (handState.tracked != 0)
                    {
                        handState.tracked = 0;

                        if (resetInteractionsOnTrackingLost)
                        {
                            handState.isGrabbing = 0;
                            handState.isPinching = 0;
                        }

                        InputSystem.QueueStateEvent(inputDevice, handState);
                    }
                }
            }
        }

        private void ConvertLeapHandToState(ref LeapHandState state, Hand hand)
        {
            state.tracked = (int)(UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation);
            state.position = hand.PalmPosition;

            switch (directionSource)
            {
                case LeapInputDirection.PALM_DIRECTION:
                    state.direction = Quaternion.LookRotation(hand.PalmNormal, hand.Direction);
                    break;
                case LeapInputDirection.PINCH_DIRECTION:
                    state.direction = GetSimpleShoulderPinchDirection(hand);
                    break;
            }

            state.indexTipPosition = hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint;

            state.isGrabbing = isGrabbingIsBinary ? (hand.GrabStrength > 0.8 ? 1 : 0) : hand.GrabStrength;
            state.isPinching = isPinchingIsBinary ? (hand.PinchStrength > 0.8 ? 1 : 0) : hand.PinchStrength;
        }

        /// <summary>
        /// Uses an assumed shoulder position relative to the main camera to determine a pinch direction
        /// </summary>
        Quaternion GetSimpleShoulderPinchDirection(Hand hand)
        {
            // First get the shoulder position
            Vector3 direction = Camera.main.transform.position;
            direction += Vector3.down * 0.1f;
            direction += Camera.main.transform.right * (hand.GetChirality() == Chirality.Left ? -0.1f : 0.1f);

            // Use the shoulder position to determine an aim direction
            direction = (hand.GetStablePinchPosition() - direction).normalized;

            return Quaternion.LookRotation(direction);
        }

        // "Leap Bone" is Not currently required as we are not sending full data
        //public static void ConvertLeapBoneToState(ref LeapBone outBone, Bone inBone)
        //{
        //    outBone.position = inBone.Center;
        //    outBone.rotation = inBone.Rotation;
        //    outBone.length = inBone.Length;
        //    outBone.width = inBone.Width;
        //}

        #region Adding and removing devices

        private void OnDeviceAdded()
        {
            InputDeviceDescription hand = new InputDeviceDescription
            {
                manufacturer = "Ultraleap",
                product = "Leap Hand"
            };

            _leftDevice = InputSystem.AddDevice(hand);

            try
            {
                InputSystem.SetDeviceUsage(_leftDevice, CommonUsages.LeftHand);
            }
            catch
            {
                ShowWarning();
            }

            _rightDevice = InputSystem.AddDevice(hand);

            try
            {
                InputSystem.SetDeviceUsage(_rightDevice, CommonUsages.RightHand);
            }
            catch
            {
                ShowWarning();
            }
        }

        private void ShowWarning()
        {
            Debug.LogWarning("Your current input map may not fully support Leap Hands.");
        }

        private void OnDeviceRemoved()
        {
            if (_leftDevice != null)
            {
                InputSystem.RemoveDevice(_leftDevice);
            }
            if (_rightDevice != null)
            {
                InputSystem.RemoveDevice(_rightDevice);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnDeviceAdded();
        }

        protected void OnDisable()
        {
            OnDeviceRemoved();
        }

        #endregion
    }
}