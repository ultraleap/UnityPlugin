/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
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

namespace Leap.Unity.InputActions
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
                MetaAimHand.left = MetaAimHand.CreateHand(UnityEngine.XR.InputDeviceCharacteristics.Left);
                MetaAimHand.right = MetaAimHand.CreateHand(UnityEngine.XR.InputDeviceCharacteristics.Right);
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

                InputSystem.QueueDeltaStateEvent(metaAimHand.indexPressed, state.activating > 0.5f ? true : false);

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
            return CalculateGrabStrength(hand) > 0.8 ? 1 : 0;
        }

        static float GetActvating(XRHand hand)
        {
            return CalculatePinchStrength(hand, CalculateHandScale(hand)) > 0.8 ? 1 : 0;
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
            return GetStablePinchPosition(hand);
        }

        static Quaternion GetAimDirection(XRHand hand)
        {
            return GetSimpleShoulderPinchDirection(hand).normalized;
        }

        static Vector3 GetPinchPosition(XRHand hand)
        {
            return GetStablePinchPosition(hand);
        }

        static Quaternion GetPinchDirection(XRHand hand)
        {
            if (hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out Pose proximalPose))
            {
                return Quaternion.LookRotation(GetStablePinchPosition(hand) - proximalPose.position).normalized;
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
            direction = GetStablePinchPosition(hand) - direction;

            return Quaternion.LookRotation(direction);
        }

        #endregion

        #region Adding and removing devices

        private static void OnDeviceAdded()
        {
            if (ultraleapSettings.updateMetaInputSystem)
            {
                MetaAimHand.CreateHand(UnityEngine.XR.InputDeviceCharacteristics.Left);
                MetaAimHand.CreateHand(UnityEngine.XR.InputDeviceCharacteristics.Right);
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

        #region Hand utils

        static float CalculateGrabStrength(XRHand hand)
        {
            // magic numbers so it approximately lines up with the leap results
            const float bendZero = 0.25f;
            const float bendOne = 0.85f;

            // Find the minimum bend angle for the non-thumb fingers.
            float minBend = float.MaxValue;

            Vector3 handRadialAxis = GetHandRadialAxis(hand);
            Vector3 handDistalAxis = GetHandDistalAxis(hand);

            minBend = Mathf.Min(GetFingerStrength(hand.GetJoint(XRHandJointID.IndexIntermediate), handRadialAxis, handDistalAxis), minBend);
            minBend = Mathf.Min(GetFingerStrength(hand.GetJoint(XRHandJointID.MiddleIntermediate), handRadialAxis, handDistalAxis), minBend);
            minBend = Mathf.Min(GetFingerStrength(hand.GetJoint(XRHandJointID.RingIntermediate), handRadialAxis, handDistalAxis), minBend);
            minBend = Mathf.Min(GetFingerStrength(hand.GetJoint(XRHandJointID.LittleIntermediate), handRadialAxis, handDistalAxis), minBend);

            // Return the grab strength.
            return Mathf.Clamp01((minBend - bendZero) / (bendOne - bendZero));
        }

        static float GetFingerStrength(XRHandJoint midJoint, Vector3 radialAxis, Vector3 distalAxis)
        {
            if (midJoint.id == XRHandJointID.ThumbProximal)
            {
                return Vector3.Dot(FingerDirection(midJoint), -radialAxis).Map(-1, 1, 0, 1);
            }

            return Vector3.Dot(FingerDirection(midJoint), -distalAxis).Map(-1, 1, 0, 1);
        }

        static Vector3 FingerDirection(XRHandJoint midJoint)
        {
            if (midJoint.TryGetPose(out Pose midJointPose))
            {
                return midJointPose.forward;
            }
            else
            {
                return Vector3.zero;
            }
        }

        static Vector3 GetHandRadialAxis(XRHand hand)
        {
            Quaternion rotation = hand.rootPose.rotation;
            float d = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
            float s = 2.0f / d;
            float ys = rotation.y * s, zs = rotation.z * s;
            float wy = rotation.w * ys, wz = rotation.w * zs;
            float xy = rotation.x * ys, xz = rotation.x * zs;
            float yy = rotation.y * ys, zz = rotation.z * zs;

            Vector3 xBasis = new Vector3(1.0f - (yy + zz), xy + wz, xz - wy);

            if (hand.handedness == Handedness.Left)
            {
                return xBasis;
            }
            else
            {
                return -xBasis;
            }
        }

        static Vector3 GetHandDistalAxis(XRHand hand)
        {
            Quaternion rotation = hand.rootPose.rotation;
            float d = rotation.x * rotation.x + rotation.y * rotation.y + rotation.z * rotation.z + rotation.w * rotation.w;
            float s = 2.0f / d;
            float xs = rotation.x * s, ys = rotation.y * s, zs = rotation.z * s;
            float wx = rotation.w * xs, wy = rotation.w * ys;
            float xx = rotation.x * xs, xz = rotation.x * zs;
            float yy = rotation.y * ys, yz = rotation.y * zs;

            Vector3 zBasis = new Vector3(xz + wy, yz - wx, 1.0f - (xx + yy));
            return zBasis;
        }

        static float CalculatePinchStrength(XRHand hand, float handScale)
        {
            // Get the thumb position.
            Vector3 thumbTip = Vector3.zero;
            if (hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbTipPose)) thumbTip = thumbTipPose.position;

            // Compute the distance midpoints between the thumb and the each finger and find the smallest.
            float minDistanceSquared = float.MaxValue;

            // Iterate through the fingers, skipping the thumb.
            for (var i = 1; i < 5; ++i)
            {
                Pose fingerTipPose;
                Vector3 fingerTip = Vector3.zero;

                switch (i)
                {
                    case 1:
                        if (hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out fingerTipPose)) fingerTip = fingerTipPose.position;
                        break;
                    case 2:
                        if (hand.GetJoint(XRHandJointID.MiddleTip).TryGetPose(out fingerTipPose)) fingerTip = fingerTipPose.position;
                        break;
                    case 3:
                        if (hand.GetJoint(XRHandJointID.RingTip).TryGetPose(out fingerTipPose)) fingerTip = fingerTipPose.position;
                        break;
                    case 4:
                        if (hand.GetJoint(XRHandJointID.LittleTip).TryGetPose(out fingerTipPose)) fingerTip = fingerTipPose.position;
                        break;
                    default:
                        break;
                }

                float distanceSquared = (fingerTip - thumbTip).sqrMagnitude;
                minDistanceSquared = Mathf.Min(distanceSquared, minDistanceSquared);
            }

            // Compute the pinch strength. Magic values taken from existing LeapC implementation (scaled to metres)
            float distanceZero = 0.0600f * handScale;
            float distanceOne = 0.0220f * handScale;
            return Mathf.Clamp01((Mathf.Sqrt(minDistanceSquared) - distanceZero) / (distanceOne - distanceZero));
        }

        static Vector3 GetStablePinchPosition(XRHand hand)
        {
            Vector3 indexTip = Vector3.zero;
            Vector3 thumbTip = Vector3.zero;

            if (hand.GetJoint(XRHandJointID.IndexDistal).TryGetPose(out Pose indexTipPose)) indexTip = indexTipPose.position;
            if (hand.GetJoint(XRHandJointID.ThumbDistal).TryGetPose(out Pose thumbTipPose)) thumbTip = thumbTipPose.position;
            return Vector3.Lerp(indexTip, thumbTip, 0.75f);
        }

        static float GetMetacarpalLength(XRHand hand, int fingerIndex)
        {
            float length = 0;

            Pose metacarpal = new Pose();
            Pose proximal = new Pose();

            bool positionsValid = false;

            switch (fingerIndex)
            {
                case 0:
                    if (hand.GetJoint(XRHandJointID.ThumbProximal).TryGetPose(out proximal) &&
                        hand.GetJoint(XRHandJointID.ThumbMetacarpal).TryGetPose(out metacarpal))
                    {
                        positionsValid = true;
                    }
                    break;
                case 1:
                    if (hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out proximal) &&
                        hand.GetJoint(XRHandJointID.IndexMetacarpal).TryGetPose(out metacarpal))
                    {
                        positionsValid = true;
                    }
                    break;
                case 2:
                    if (hand.GetJoint(XRHandJointID.MiddleProximal).TryGetPose(out proximal) &&
                        hand.GetJoint(XRHandJointID.MiddleMetacarpal).TryGetPose(out metacarpal))
                    {
                        positionsValid = true;
                    }
                    break;
                case 3:
                    if (hand.GetJoint(XRHandJointID.RingProximal).TryGetPose(out proximal) &&
                        hand.GetJoint(XRHandJointID.RingMetacarpal).TryGetPose(out metacarpal))
                    {
                        positionsValid = true;
                    }
                    break;
                case 4:
                    if (hand.GetJoint(XRHandJointID.LittleProximal).TryGetPose(out proximal) &&
                        hand.GetJoint(XRHandJointID.LittleMetacarpal).TryGetPose(out metacarpal))
                    {
                        positionsValid = true;
                    }
                    break;
                default:
                    break;
            }

            if (positionsValid)
            {
                length += (metacarpal.position - proximal.position).magnitude;
            }

            return length;
        }

        static float CalculateHandScale(XRHand hand)
        {
            // Iterate through the fingers, skipping the thumb and accumulate the scale.
            float scale = 0.0f;

            // Magic numbers for default metacarpal lengths
            //{ 0, 0.06812f, 0.06460f, 0.05800f, 0.05369f }
            scale += (GetMetacarpalLength(hand, 1) / 0.06812f) / 4.0f;
            scale += (GetMetacarpalLength(hand, 2) / 0.06460f) / 4.0f;
            scale += (GetMetacarpalLength(hand, 3) / 0.05800f) / 4.0f;
            scale += (GetMetacarpalLength(hand, 4) / 0.05369f) / 4.0f;

            return scale;
        }

        #endregion
    }
}