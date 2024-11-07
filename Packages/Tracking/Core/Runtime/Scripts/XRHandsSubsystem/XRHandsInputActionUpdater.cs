/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.XR.Hands;

namespace Leap.InputActions
{
    /// <summary>
    /// Updates Ultraleap Input Actions with the latest available hand data
    /// Must be provided information from a LeapProvider
    /// </summary>
    public class XRHandsInputActionUpdater
    {
        private static LeapHandState leftState = new LeapHandState(), rightState = new LeapHandState();

        private static InputDevice leftDevice, rightDevice;

        static XRHandSubsystem currentSubsystem;
        static UltraleapSettings ultraleapSettings;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Startup()
        {
            ultraleapSettings = UltraleapSettings.Instance;

            // only start if requested
            if (ultraleapSettings == null ||
                (ultraleapSettings.updateLeapInputSystem == false && ultraleapSettings.updateMetaInputSystem == false))
            {
                return;
            }

            // Find the first available subsystem
            List<XRHandSubsystem> availableSubsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(availableSubsystems);

            foreach (var subsystem in availableSubsystems)
            {
                if (subsystem != null)
                {
                    currentSubsystem = subsystem;
                    break;
                }
            }

            if (currentSubsystem == null)
            {
                Debug.LogWarning("Unable to update InputActions, no Subsystem available.");
                return;
            }

            // Set up events

            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;

            currentSubsystem.updatedHands -= UpdateHands;
            currentSubsystem.updatedHands += UpdateHands;

            // Create meta hands if required
            if (ultraleapSettings.updateMetaInputSystem)
            {
                if (MetaAimHand.left == null)
                {
                    MetaAimHand.left = MetaAimHand.CreateHand(UnityEngine.XR.InputDeviceCharacteristics.Left);
                }
                if (MetaAimHand.right == null)
                {
                    MetaAimHand.right = MetaAimHand.CreateHand(UnityEngine.XR.InputDeviceCharacteristics.Right);
                }
            }

            SetupDefaultStateGetters();

            OnDeviceAdded();
        }

        private static void OnQuit()
        {
            OnDeviceRemoved();

            Application.quitting -= OnQuit;

            currentSubsystem.updatedHands -= UpdateHands;

            if (ultraleapSettings.updateMetaInputSystem)
            {
                if (MetaAimHand.left != null)
                {
                    InputSystem.RemoveDevice(MetaAimHand.left);
                    MetaAimHand.left = null;
                }

                if (MetaAimHand.right != null)
                {
                    InputSystem.RemoveDevice(MetaAimHand.right);
                    MetaAimHand.right = null;
                }
            }
        }

        private static void UpdateHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
        {
            if (Application.isPlaying)
            {
                UpdateStateWithLeapHand(rightDevice, subsystem.rightHand, ref rightState);
                UpdateStateWithLeapHand(leftDevice, subsystem.leftHand, ref leftState);
            }
        }

        /// <summary>
        /// Handles updating the inputDevice with chosen data from the hand using the cached handState structure as a base
        /// </summary>
        private static void UpdateStateWithLeapHand(InputDevice inputDevice, XRHand hand, ref LeapHandState handState)
        {
            if (hand.isTracked)
            {
                ConvertLeapHandToState(ref handState, hand);
            }
            else // No hand, so handle relevant states
            {
                if (handState.tracked != 0)
                {
                    handState.tracked = 0;

                    handState.activating = 0;
                    handState.selecting = 0;
                }
            }

            if (ultraleapSettings.updateLeapInputSystem && inputDevice != null)
            {
                InputSystem.QueueStateEvent(inputDevice, handState);
            }

            if (ultraleapSettings.updateMetaInputSystem)
            {
                SendMetaAimStateUpdate(handState, hand);
            }
        }

        /// <summary>
        /// Update all InputActions within the LeapHandState based on the given Hand
        /// This uses delegates to allow users to be able to override the values based on their own use cases
        /// </summary>
        private static void ConvertLeapHandToState(ref LeapHandState state, XRHand hand)
        {
            LeapHandStateDelegates stateOverrides = hand.handedness == Handedness.Left == true ? leftHandStateDelegates : rightHandStateDelegates;

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

        private static void SendMetaAimStateUpdate(LeapHandState state, XRHand hand)
        {
            MetaAimHand metaAimHand = MetaAimHand.left;

            if (hand.handedness == Handedness.Right)
            {
                metaAimHand = MetaAimHand.right;
            }

            if (state.tracked == 0)
            {
                // reset positions etc
                InputSystem.QueueDeltaStateEvent(metaAimHand.devicePosition, Vector3.zero);
                InputSystem.QueueDeltaStateEvent(metaAimHand.deviceRotation, Quaternion.identity);

                InputSystem.QueueDeltaStateEvent(metaAimHand.trackingState, UnityEngine.XR.InputTrackingState.None);
                InputSystem.QueueDeltaStateEvent(metaAimHand.isTracked, false);
            }
            else
            {
                InputSystem.QueueDeltaStateEvent(metaAimHand.trackingState, UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation);
                InputSystem.QueueDeltaStateEvent(metaAimHand.isTracked, true);

                InputSystem.QueueDeltaStateEvent(metaAimHand.indexPressed, hand.CalculatePinchDistance(XRHandJointID.IndexTip) < 0.02f ? true : false);
                InputSystem.QueueDeltaStateEvent(metaAimHand.middlePressed, hand.CalculatePinchDistance(XRHandJointID.MiddleTip) < 0.02f ? true : false);
                InputSystem.QueueDeltaStateEvent(metaAimHand.ringPressed, hand.CalculatePinchDistance(XRHandJointID.RingTip) < 0.02f ? true : false);
                InputSystem.QueueDeltaStateEvent(metaAimHand.littlePressed, hand.CalculatePinchDistance(XRHandJointID.LittleTip) < 0.02f ? true : false);

                InputSystem.QueueDeltaStateEvent(metaAimHand.pinchStrengthIndex, hand.CalculatePinchStrength(XRHandJointID.IndexTip));
                InputSystem.QueueDeltaStateEvent(metaAimHand.pinchStrengthMiddle, hand.CalculatePinchStrength(XRHandJointID.MiddleTip));
                InputSystem.QueueDeltaStateEvent(metaAimHand.pinchStrengthRing, hand.CalculatePinchStrength(XRHandJointID.RingTip));
                InputSystem.QueueDeltaStateEvent(metaAimHand.pinchStrengthLittle, hand.CalculatePinchStrength(XRHandJointID.LittleTip));

                InputSystem.QueueDeltaStateEvent(metaAimHand.devicePosition, state.aimPosition);
                InputSystem.QueueDeltaStateEvent(metaAimHand.deviceRotation, state.aimDirection);
            }
        }

        #region Default State Getters

        /// <summary>
        /// Set each of the default "Getters" to their relevant delegate. Delegates can be overridden via <LeapHandStateDelegates>
        /// </summary>
        static void SetupDefaultStateGetters()
        {
            // only set them up if they are already null
            leftHandStateDelegates.trackedDelegate ??= GetTrackedState;
            leftHandStateDelegates.selectDelegate ??= GetSelecting;
            leftHandStateDelegates.activateDelegate ??= GetActvating;
            leftHandStateDelegates.palmPositionDelegate ??= GetPalmPosition;
            leftHandStateDelegates.palmDirectionDelegate ??= GetPalmDirection;
            leftHandStateDelegates.aimPositionDelegate ??= GetAimPosition;
            leftHandStateDelegates.aimDirectionDelegate ??= GetAimDirection;
            leftHandStateDelegates.pinchPositionDelegate ??= GetPinchPosition;
            leftHandStateDelegates.pinchDirectionDelegate ??= GetPinchDirection;
            leftHandStateDelegates.pokePositionDelegate ??= GetPokePosition;
            leftHandStateDelegates.pokeDirectionDelegate ??= GetPokeDirection;

            rightHandStateDelegates.trackedDelegate ??= GetTrackedState;
            rightHandStateDelegates.selectDelegate ??= GetSelecting;
            rightHandStateDelegates.activateDelegate ??= GetActvating;
            rightHandStateDelegates.palmPositionDelegate ??= GetPalmPosition;
            rightHandStateDelegates.palmDirectionDelegate ??= GetPalmDirection;
            rightHandStateDelegates.aimPositionDelegate ??= GetAimPosition;
            rightHandStateDelegates.aimDirectionDelegate ??= GetAimDirection;
            rightHandStateDelegates.pinchPositionDelegate ??= GetPinchPosition;
            rightHandStateDelegates.pinchDirectionDelegate ??= GetPinchDirection;
            rightHandStateDelegates.pokePositionDelegate ??= GetPokePosition;
            rightHandStateDelegates.pokeDirectionDelegate ??= GetPokeDirection;
        }

        static int GetTrackedState(XRHand hand)
        {
            return (int)(UnityEngine.XR.InputTrackingState.Position | UnityEngine.XR.InputTrackingState.Rotation);
        }

        static float GetSelecting(XRHand hand)
        {
            return hand.CalculateGrabStrength() > 0.8 ? 1 : 0;
        }

        static float GetActvating(XRHand hand)
        {
            return hand.CalculatePinchDistance() < 0.02f ? 1 : 0;
        }

        static Vector3 GetPalmPosition(XRHand hand)
        {
            if (hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palmPose))
            {
                return palmPose.position;
            }

            return Vector3.zero;
        }

        static Quaternion GetPalmDirection(XRHand hand)
        {
            if (hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palmPose))
            {
                return Quaternion.LookRotation(-palmPose.up, palmPose.forward).normalized;
            }

            return Quaternion.identity;
        }

        static Vector3 GetAimPosition(XRHand hand)
        {
            return hand.GetStablePinchPosition();
        }

        static Quaternion GetAimDirection(XRHand hand)
        {
            return GetSimpleShoulderPinchDirection(hand).normalized;
        }

        static Vector3 GetPinchPosition(XRHand hand)
        {
            return hand.GetStablePinchPosition();
        }

        static Quaternion GetPinchDirection(XRHand hand)
        {
            if (hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out Pose proximalPose))
            {
                return Quaternion.LookRotation(hand.GetStablePinchPosition() - proximalPose.position).normalized;
            }

            return Quaternion.identity;
        }

        static Vector3 GetPokePosition(XRHand hand)
        {
            if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexTip))
            {
                return indexTip.position;
            }

            return Vector3.zero;
        }

        static Quaternion GetPokeDirection(XRHand hand)
        {
            if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexTip) && hand.GetJoint(XRHandJointID.IndexDistal).TryGetPose(out Pose indexDistal))
            {
                return Quaternion.LookRotation(indexTip.position - indexDistal.position).normalized;
            }

            return Quaternion.identity;
        }

        /// <summary>
        /// Uses an assumed shoulder position relative to the main camera to determine a pinch direction
        /// </summary>
        static Quaternion GetSimpleShoulderPinchDirection(XRHand hand)
        {
            // First get the shoulder position
            // Note: UNKNOWN as to why we require localposition here as the StablePinchPosition should be in world space by now
            Vector3 direction = Camera.main.transform.localPosition;
            direction += (Vector3.down * 0.1f);
            direction += (Vector3.right * (hand.handedness == Handedness.Left ? -0.1f : 0.1f));

            // Use the shoulder position to determine an aim direction
            direction = hand.GetStablePinchPosition() - direction;

            return Quaternion.LookRotation(direction);
        }

        #endregion

        #region Adding and removing devices

        private static void OnDeviceAdded()
        {
            if (ultraleapSettings.updateMetaInputSystem)
            {
                if (MetaAimHand.left == null)
                {
                    MetaAimHand.CreateHand(UnityEngine.XR.InputDeviceCharacteristics.Left);
                }

                if (MetaAimHand.right == null)
                {
                    MetaAimHand.CreateHand(UnityEngine.XR.InputDeviceCharacteristics.Right);
                }
            }

            if (ultraleapSettings.updateLeapInputSystem)
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
        }

        private static void ShowWarning()
        {
            Debug.LogWarning("Your current input map may not fully support Leap Hands.");
        }

        private static void OnDeviceRemoved()
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

        public delegate int IntFromHandDelegate(XRHand hand);
        public delegate float FloatFromHandDelegate(XRHand hand);
        public delegate Vector3 PositionFromHandDelegate(XRHand hand);
        public delegate Quaternion RotationFromHandDelegate(XRHand hand);

        public static LeapHandStateDelegates leftHandStateDelegates;
        public static LeapHandStateDelegates rightHandStateDelegates;

        #endregion
    }
}