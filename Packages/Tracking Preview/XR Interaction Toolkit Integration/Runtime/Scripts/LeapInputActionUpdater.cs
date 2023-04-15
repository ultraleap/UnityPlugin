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
    /// Updates Ultraleap Input Actions with the latest available hand data
    /// Must be provided information from a LeapProvider
    /// </summary>
    [DefaultExecutionOrder(-1)]
    public class LeapInputActionUpdater : PostProcessProvider
    {
        private LeapHandState leftState = new LeapHandState(), rightState = new LeapHandState();

        private InputDevice leftDevice, rightDevice;

        [Tooltip("Should the pinching InputAction be set via a binary decision, or a float PinchStrength?")]
        public bool pinchingIsBinary = true;

        [Tooltip("Should the grabbing InputAction be set via a binary decision, or a float GrabStrength?")]
        public bool grabbingIsBinary = true;

        [Tooltip("Should interactions (e.g. grabbing and pinching) reset when the hand is lost?")]
        public bool resetInteractionsOnTrackingLost = true;

        private void Start()
        {
            InputSystem.onBeforeUpdate += InputSystem_onBeforeUpdate;

            SetupDefaultStateGetters();
        }

        private void OnDestroy()
        {
            InputSystem.onBeforeUpdate -= InputSystem_onBeforeUpdate;
        }

        private void InputSystem_onBeforeUpdate()
        {
            if (Application.isPlaying)
            {
                UpdateStateWithLeapHand(leftDevice, _inputLeapProvider.CurrentFrame.GetHand(Chirality.Left), ref leftState);
                UpdateStateWithLeapHand(rightDevice, _inputLeapProvider.CurrentFrame.GetHand(Chirality.Right), ref rightState);
            }
        }

        public override void ProcessFrame(ref Frame inputFrame)
        {
            // Do nothing as we should update the input system based in its own events
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
                            handState.activating = 0;
                            handState.selecting = 0;
                        }

                        InputSystem.QueueStateEvent(inputDevice, handState);
                    }
                }
            }
        }

        private void ConvertLeapHandToState(ref LeapHandState state, Hand hand)
        {
            LeapHandStateOverrides stateOverrides = hand.IsLeft == true ? leftHandOverrides : rightHandOverrides;

            state.tracked = stateOverrides.trackedDelegate(hand);

            state.selecting = stateOverrides.selectDelegate(hand);
            state.activating = stateOverrides.activateDelegate(hand);

            state.palmPosition = stateOverrides.palmPositionDelegate(hand);
            state.palmDirection = stateOverrides.palmDirectionDelegate(hand);

            state.aimPosition = stateOverrides.aimPositionDelegate(hand);
            state.aimDirection = stateOverrides.aimDirectionDelegate(hand);

            state.pinchPosition = stateOverrides.pinchPositionDelegate(hand);
            state.pinchDirection = stateOverrides.pinchDirectionDelegate(hand);

            state.pokePosition = stateOverrides.pokePositionDelegate(hand);
            state.pokeDirection = stateOverrides.pokeDirectionDelegate(hand);
        }

        #region Default State Getters

        void SetupDefaultStateGetters()
        {
            leftHandOverrides.trackedDelegate = GetTrackedState;
            leftHandOverrides.selectDelegate = GetSelecting;
            leftHandOverrides.activateDelegate = GetActvating;
            leftHandOverrides.palmPositionDelegate = GetPalmPosition;
            leftHandOverrides.palmDirectionDelegate = GetPalmDirection;
            leftHandOverrides.aimPositionDelegate = GetAimPosition;
            leftHandOverrides.aimDirectionDelegate = GetAimDirection;
            leftHandOverrides.pinchPositionDelegate = GetPinchPosition;
            leftHandOverrides.pinchDirectionDelegate = GetPinchDirection;
            leftHandOverrides.pokePositionDelegate = GetPokePosition;
            leftHandOverrides.pokeDirectionDelegate = GetPokeDirection;

            rightHandOverrides = leftHandOverrides;
        }

        int GetTrackedState(Hand hand)
        {
            return (int)(UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation);
        }

        float GetSelecting(Hand hand)
        {
            return pinchingIsBinary ? (hand.PinchStrength > 0.8 ? 1 : 0) : hand.PinchStrength;
        }

        float GetActvating(Hand hand)
        {
            return grabbingIsBinary ? (hand.GrabStrength > 0.8 ? 1 : 0) : hand.GrabStrength;
        }

        Vector3 GetPalmPosition(Hand hand)
        {
            return hand.PalmPosition;
        }

        Quaternion GetPalmDirection(Hand hand)
        {
            return Quaternion.LookRotation(hand.PalmNormal, hand.Direction);
        }

        Vector3 GetAimPosition(Hand hand)
        {
            return hand.GetPredictedPinchPosition();
        }

        Quaternion GetAimDirection(Hand hand)
        {
            return GetSimpleShoulderPinchDirection(hand);
        }

        Vector3 GetPinchPosition(Hand hand)
        {
            return hand.GetPredictedPinchPosition();
        }

        Quaternion GetPinchDirection(Hand hand)
        {
            return Quaternion.LookRotation(hand.GetPredictedPinchPosition() - hand.Fingers[1].Bone(Bone.BoneType.TYPE_PROXIMAL).PrevJoint);
        }

        Vector3 GetPokePosition(Hand hand)
        {
            return hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint;
        }

        Quaternion GetPokeDirection(Hand hand)
        {
            return Quaternion.LookRotation(hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint - hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).PrevJoint);
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

        #endregion

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

            leftDevice = InputSystem.AddDevice(hand);

            try
            {
                InputSystem.SetDeviceUsage(leftDevice, CommonUsages.LeftHand);
            }
            catch
            {
                ShowWarning();
            }

            rightDevice = InputSystem.AddDevice(hand);

            try
            {
                InputSystem.SetDeviceUsage(rightDevice, CommonUsages.RightHand);
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
            if (leftDevice != null)
            {
                InputSystem.RemoveDevice(leftDevice);
            }
            if (rightDevice != null)
            {
                InputSystem.RemoveDevice(rightDevice);
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

        #region User Possible Overrides
        public struct LeapHandStateOverrides
        {
            public IntFromHandDelegate trackedDelegate;

            public FloatFromHandDelegate selectDelegate;
            public FloatFromHandDelegate activateDelegate;

            public PositionFromHandDelegate palmPositionDelegate;
            public RotationFromHandDelegate palmDirectionDelegate;

            public PositionFromHandDelegate aimPositionDelegate;
            public RotationFromHandDelegate aimDirectionDelegate;

            public PositionFromHandDelegate pinchPositionDelegate;
            public RotationFromHandDelegate pinchDirectionDelegate;

            public PositionFromHandDelegate pokePositionDelegate;
            public RotationFromHandDelegate pokeDirectionDelegate;
        }

        public delegate int IntFromHandDelegate(Hand hand);
        public delegate float FloatFromHandDelegate(Hand hand);
        public delegate Vector3 PositionFromHandDelegate(Hand hand);
        public delegate Quaternion RotationFromHandDelegate(Hand hand);

        public LeapHandStateOverrides leftHandOverrides;
        public LeapHandStateOverrides rightHandOverrides;

        #endregion
    }
}