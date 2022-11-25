/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Leap.Unity;
using Leap;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace Leap.Unity.Preview.InputActions
{
    /// <summary>
    /// Updates Ultraleap Input Actions with the latest available hand data
    /// Must be provided information from a LeapProvider
    /// </summary>
    public class LeapInputActionUpdater : PostProcessProvider
    {
        private LeapHandState _leftState = new LeapHandState(), _rightState = new LeapHandState();

        private InputDevice _leftDevice, _rightDevice;

        public override void ProcessFrame(ref Frame inputFrame)
        {
            if (Application.isPlaying)
            {
                UpdateStateWithLeapHand(_leftDevice, inputFrame.GetHand(Chirality.Left), ref _leftState);
                UpdateStateWithLeapHand(_rightDevice, inputFrame.GetHand(Chirality.Right), ref _rightState);
            }
        }

        /// <summary>
        /// Handles updating the inputDevice with chosen data from the hand using the cached handState structure as a base
        /// </summary>
        private static void UpdateStateWithLeapHand(InputDevice inputDevice, Hand hand, ref LeapHandState handState)
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
                        InputSystem.QueueStateEvent(inputDevice, handState);
                    }
                }
            }
        }

        private static void ConvertLeapHandToState(ref LeapHandState state, Hand hand)
        {
            state.tracked = (int)(UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation);
            state.position = hand.PalmPosition;
            state.direction = Quaternion.LookRotation(hand.PalmNormal, hand.Direction);

            state.indexTipPosition = hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint;

            state.isGrabbing = hand.GrabStrength > 0.8 ? 1 : 0;
            state.isPinching = hand.PinchStrength > 0.8 ? 1 : 0;
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