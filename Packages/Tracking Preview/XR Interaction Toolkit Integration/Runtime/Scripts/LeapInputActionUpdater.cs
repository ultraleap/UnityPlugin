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

        [SerializeField, Tooltip("Should the pinching InputAction be set via a binary decision, or a float PinchStrength?")]
        private bool pinchingIsBinary = true;

        [SerializeField, Tooltip("Should the grabbing InputAction be set via a binary decision, or a float GrabStrength?")]
        private bool grabbingIsBinary = true;

        [SerializeField, Tooltip("Should interactions (e.g. selecting and activating) reset when the hand is lost?")]
        private bool resetInteractionsOnTrackingLost = true;

        private void Start()
        {
            if (inputLeapProvider == null)
            {
                inputLeapProvider = Hands.Provider;
            }

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
            // Do nothing as we should update the input system based in its own InputSystem.onBeforeUpdate events
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
                else // No hand, so handle relevant states
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

        /// <summary>
        /// Update all InputActions within the LeapHandState based on the given Hand
        /// This uses delegates to allow users to be able to override the values based on their own use cases
        /// </summary>
        private void ConvertLeapHandToState(ref LeapHandState state, Hand hand)
        {
            LeapHandStateDelegates stateOverrides = hand.IsLeft == true ? leftHandStateDelegates : rightHandStateDelegates;

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

        /// <summary>
        /// Set each of the default "Getters" to their relevant delegate
        /// </summary>
        void SetupDefaultStateGetters()
        {
            leftHandStateDelegates.trackedDelegate = GetTrackedState;
            leftHandStateDelegates.selectDelegate = GetSelecting;
            leftHandStateDelegates.activateDelegate = GetActvating;
            leftHandStateDelegates.palmPositionDelegate = GetPalmPosition;
            leftHandStateDelegates.palmDirectionDelegate = GetPalmDirection;
            leftHandStateDelegates.aimPositionDelegate = GetAimPosition;
            leftHandStateDelegates.aimDirectionDelegate = GetAimDirection;
            leftHandStateDelegates.pinchPositionDelegate = GetPinchPosition;
            leftHandStateDelegates.pinchDirectionDelegate = GetPinchDirection;
            leftHandStateDelegates.pokePositionDelegate = GetPokePosition;
            leftHandStateDelegates.pokeDirectionDelegate = GetPokeDirection;

            rightHandStateDelegates = leftHandStateDelegates;
        }

        int GetTrackedState(Hand hand)
        {
            return (int)(UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation);
        }

        float GetSelecting(Hand hand)
        {
            return grabbingIsBinary ? (hand.GrabStrength > 0.8 ? 1 : 0) : hand.GrabStrength;
        }

        float GetActvating(Hand hand)
        {
            return pinchingIsBinary ? (hand.PinchStrength > 0.8 ? 1 : 0) : hand.PinchStrength;
        }

        Vector3 GetPalmPosition(Hand hand)
        {
            return hand.PalmPosition;
        }

        Quaternion GetPalmDirection(Hand hand)
        {
            return Quaternion.LookRotation(hand.PalmNormal, hand.Direction).normalized;
        }

        Vector3 GetAimPosition(Hand hand)
        {
            return hand.GetPredictedPinchPosition();
        }

        Quaternion GetAimDirection(Hand hand)
        {
            return GetSimpleShoulderPinchDirection(hand).normalized;
        }

        Vector3 GetPinchPosition(Hand hand)
        {
            return hand.GetPredictedPinchPosition();
        }

        Quaternion GetPinchDirection(Hand hand)
        {
            return Quaternion.LookRotation(hand.GetPredictedPinchPosition() - hand.Fingers[1].Bone(Bone.BoneType.TYPE_PROXIMAL).PrevJoint).normalized;
        }

        Vector3 GetPokePosition(Hand hand)
        {
            return hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint;
        }

        Quaternion GetPokeDirection(Hand hand)
        {
            return Quaternion.LookRotation(hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).NextJoint - hand.Fingers[1].Bone(Bone.BoneType.TYPE_DISTAL).PrevJoint).normalized;
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

        #region User State Overrides

        /// <summary>
        /// A collection of delegates for users to override with their own methods if necessary.
        /// E.g. If LeapHandStateDelegates.aimPositionDelegate = OverrideAimPosToPalmPos, it would always return the palm position of hand:
        /// 
        ///     Vector3 OverrideAimPosToPalmPos(Hand hand)
        ///     { 
        ///         return hand.PalmPosition;
        ///     }
        ///     
        /// </summary>
        public struct LeapHandStateDelegates
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

        public LeapHandStateDelegates leftHandStateDelegates;
        public LeapHandStateDelegates rightHandStateDelegates;

        #endregion
    }
}