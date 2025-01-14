/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.XR.Hands;
using static Leap.InputActions.XRHandsInputActionUpdater;

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
            Head = Camera.main.transform;

            ShoulderPositions = new Vector3[2];
            ShoulderPositionsLocalSpace = new Vector3[2];

            HipPositions = new Vector3[2];
            //EyePositions = new Vector3[2];

            storedNeckRotation = Quaternion.identity;
            //neckYawDeadzone = new EulerAngleDeadzone(25, true, 1.5f, 10, 1);

            ResetFilters();

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

                UpdateBodyPositions();
                CalculateRay(subsystem.rightHand);
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

        public static Vector3 GetAimPosition(XRHand hand)
        {
            return hand.GetStablePinchPosition();
        }

        static Quaternion GetAimDirection(XRHand hand)
        {
            //return GetSimpleShoulderPinchDirection(hand).normalized;
            return Quaternion.LookRotation(rayDirection);
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


        public static Vector3 GetRayOrigin(XRHand hand)
        {
            return rayOrigin;
        }

        public static Vector3 GetRayDirection(XRHand hand)
        {
            return rayDirection;
        }

        public static Vector3 GetInferredShoulderPosition(int isRight)
        {
            return ShoulderPositions[isRight];
        }

        public static Vector3 GetWristOffsetPosition(XRHand hand)
        {
            Vector3 localWristPosition = pinchWristOffset;
            if (hand.handedness == Handedness.Right)
            {
                localWristPosition.x = -localWristPosition.x;
            }

            Pose wristPose;
            if (hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out wristPose))
            {
                transformHelper.transform.position = wristPose.position;
                transformHelper.transform.rotation = wristPose.rotation;
            }

            return transformHelper.TransformPoint(localWristPosition);
        }

        /// <summary>
        /// Uses an assumed shoulder position relative to the main camera to determine a pinch direction
        /// </summary>
        static Quaternion GetSimpleShoulderPinchDirection(XRHand hand)
        {
            // First get the shoulder position
            // Note: UNKNOWN as to why we require localposition here as the StablePinchPosition should be in world space by now
            Vector3 rayStartPoint = Camera.main.transform.localPosition;
            rayStartPoint += (Vector3.down * 0.1f);

            float directionMagnitude = hand.handedness == Handedness.Left ? -0.1f : 0.1f;
            if (Application.platform == RuntimePlatform.Android)
            {
                // Needs reversing on Meta Quest (tested on Quest 3)
                if (SystemInfo.deviceModel == "Oculus Quest")
                {
                    directionMagnitude = hand.handedness == Handedness.Right ? -0.1f : 0.1f;
                }
            }

            rayStartPoint += Vector3.right * directionMagnitude;

            // Re-apply camera Y height
            //Vector3 rayTargetPoint = GetRayTargetPoint_PositionLerp(hand);
            //Vector3 rayTargetPoint = GetRayTargetPoint_FingerDirection(hand);
            Vector3 rayTargetPoint = GetRayTargetPoint_PalmRotationLerp(hand);

            //Debug.Log($"({stablePinchPos.x}, {stablePinchPos.y}) -> ({rayPinchPoint.x}, {rayPinchPoint.y})");
            Debug.DrawLine(rayStartPoint, hand.GetStablePinchPosition(), Color.red);
            Debug.DrawLine(rayStartPoint, rayTargetPoint, Color.green);

            // Use the shoulder position to determine an aim direction
            rayStartPoint = rayTargetPoint - rayStartPoint;

            return Quaternion.LookRotation(rayStartPoint);
        }

        static Vector3 GetRayTargetPoint_PalmRotationLerp(XRHand hand)
        {
            Pose palmPose;
            hand.GetJoint(XRHandJointID.Palm).TryGetPose(out palmPose);

            Vector3 rayTargetPoint = palmPose.position;

            // Remove Y height of camera to center numbers around zero origin
            rayTargetPoint.y -= Camera.main.transform.localPosition.y;
            float debugOriginalX = rayTargetPoint.x;
            float debugOriginalY = rayTargetPoint.y;

            //TODO: joint Pose values are in world space, so needs to be done in Camera local space??

            // --- Hand left/right rotation = move point in the X
            float xAddition = 0f;
            /*float yRotation = palmPose.rotation.eulerAngles.y;
            float origYRot = yRotation;
            yRotation -= Camera.main.transform.rotation.eulerAngles.y;
            Debug.Log($"{origYRot} -> {yRotation}");
            //if (yRotation > 180f) yRotation -= 360f;
            xAddition = MapClamp(-45f, 45f, -0.3f, 0.3f, yRotation);*/

            // --- Hand up/down rotation = move point in the Y
            float xRotation = palmPose.rotation.eulerAngles.x;
            if (xRotation > 180f) xRotation -= 360f;
            float yAddition = MapClamp(45f, -45f, -0.3f, 0.3f, xRotation);

            rayTargetPoint.x += xAddition;
            rayTargetPoint.y += yAddition;

            //Debug.Log($"X angle: {xRotation}, yAddition: {yAddition}, original vs target Y: {debugOriginalY} / {rayTargetPoint.y}");
            //Debug.Log($"Y angle: {yRotation}, xAddition: {xAddition}, original vs target X: {debugOriginalX} / {rayTargetPoint.x}");

            // Re-apply camera Y height
            rayTargetPoint.y += Camera.main.transform.localPosition.y;

            return rayTargetPoint;
        }

        static Vector3 GetRayTargetPoint_FingerDirection(XRHand hand)
        {
            Pose targetPose;
            hand.GetJoint(XRHandJointID.IndexProximal).TryGetPose(out targetPose);
            //targetPose = hand.rootPose;

            //Pose pose = new Pose(Vector3.zero, Quaternion.identity);
            //targetPose = targetPose.GetTransformedBy(pose);

            //Debug.DrawRay(targetPose.position, targetPose.position + (targetPose.right * 0.1f), Color.red);
            //Debug.DrawRay(targetPose.position, targetPose.position + (targetPose.up * 0.1f), Color.green);
            //Debug.DrawRay(targetPose.position, targetPose.position + (targetPose.forward * 0.1f), Color.blue);
            //Debug.Log(palmPose.forward.ToString());

            //Vector3 targetDirection = (palmPose.position - wristPose.position).normalized;
            Vector3 targetDirection = targetPose.forward;
            Vector3 rayTargetPoint = targetPose.position + (targetDirection * 0.5f);

            return rayTargetPoint;
        }

        static Vector3 GetRayTargetPoint_PositionLerp(XRHand hand)
        {
            Vector3 rayTargetPoint;

            // Allow expanded ray movement based on direction of pinch
            var stablePinchPos = hand.GetStablePinchPosition();
            // Remove Y height of camera to center numbers around zero origin
            stablePinchPos.y -= Camera.main.transform.localPosition.y;

            float xPinchAddition;
            if (hand.handedness == Handedness.Left)
            {
                xPinchAddition = Map(-0.2f, 0f, -0.08f, 0.08f, stablePinchPos.x);
            }
            else
            {
                xPinchAddition = Map(0f, 0.2f, -0.08f, 0.08f, stablePinchPos.x);
            }

            float yPinchAddition = Map(-0.2f, 0.0f, -0.08f, 0.08f, stablePinchPos.y);

            rayTargetPoint = stablePinchPos;
            rayTargetPoint.x += xPinchAddition;
            rayTargetPoint.y += yPinchAddition;

            // Re-apply camera Y height
            rayTargetPoint.y += Camera.main.transform.localPosition.y;

            return rayTargetPoint;
        }

        static float Map(float from1, float from2, float to1, float to2, float value)
        {
            return to1 + (value - from1) * (to2 - to1) / (from2 - from1);
        }

        static float MapClamp(float from1, float from2, float to1, float to2, float value)
        {
            float outValue = Map(from1, from2, to1, to2, value);
            return Mathf.Clamp(outValue, to1, to2);
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

        #region WristShoulderHandRay 

        /// <summary>
        /// Inferred Neck position
        /// </summary>
        public static Vector3 NeckPosition { get; private set; }

        /// <summary>
        /// Inferred neck position, based purely off of a local space offset to the head
        /// </summary>
        public static Vector3 NeckPositionLocalSpace { get; private set; }

        /// <summary>
        /// Inferred neck position, based purely off of a world space offset to the head
        /// </summary>
        public static Vector3 NeckPositionWorldSpace { get; private set; }

        /// <summary>
        /// Inferred neck rotation
        /// </summary>
        public static Quaternion NeckRotation { get; private set; }

        /// <summary>
        /// Inferred shoulder position
        /// index:0 = left shoulder, index:1 = right shoulder
        /// </summary>
        public static Vector3[] ShoulderPositions { get; private set; }

        /// <summary>
        /// Inferred shoulder position, based purely off of a local space offset to the head
        /// index:0 = left shoulder, index:1 = right shoulder
        /// </summary>
        public static Vector3[] ShoulderPositionsLocalSpace { get; private set; }

        /// <summary>
        /// The head position, taken as the mainCamera from a LeapXRServiceProvider if found in the scene,
        /// otherwise Camera.main
        /// </summary>
        public static Transform Head { get; private set; }

        /// <summary>
        /// The eye position, based off a local space offset from the head. 
        /// index:0 = left eye, index:1 = right eye
        /// </summary>
        // public Vector3[] EyePositions { get; private set; }

        /// <summary>
        /// Inferred hip position. Based as a vertical offset from the shoulder positions
        /// index:0 = left hip, index:1 = right hip
        /// </summary>
        public static Vector3[] HipPositions { get; private set; }

        /// <summary>
        /// Inferred waist position, calculated the midpoint between the two hips
        /// </summary>
        public static Vector3 WaistPosition { get { return Vector3.Lerp(HipPositions[0], HipPositions[1], 0.5f); } }

        [Header("Inferred Hip Settings")]
        /// <summary>
        /// The hip's vertical offset from the shoulders.
        /// </summary>
        public static float verticalHipOffset = -0.5f;

        /// <summary>
        /// Blends between the NeckPositionLocalOffset and the NeckPositionWorldOffset.
        /// At 0, only the local position is into account.
        /// At 1, only the world position is into account.
        /// A blend between the two stops head roll & pitch rotation (z & x rotation) 
        /// having a large effect on the neck position
        /// </summary>
        [Tooltip("Blends between the NeckPositionLocalOffset and the NeckPositionWorldOffset.\n" +
            " - At 0, only the local position is into account.\n" +
            " - At 1, only the world position is into account.\n" +
            " - A blend between the two stops head roll & pitch rotation (z & x rotation) " +
            "having a large effect on the neck position")]
        [Range(0f, 1)]
        public static float worldLocalNeckPositionBlend = 0.5f;

        [Header("Inferred Neck Settings")]
        /// <summary>
        /// The neck's vertical offset from the head
        /// </summary>
        [Tooltip("The neck's vertical offset from the head")]
        public static float neckOffset = -0.1f;


        /// <summary>
        /// How quickly the neck rotation updates
        /// Used to smooth out sudden large rotations
        /// </summary>
        [Tooltip("How quickly the neck rotation updates\n" +
            "Used to smooth out sudden large rotations")]
        [Range(0.01f, 30)]
        public static float neckRotationLerpSpeed = 22;

        /// <summary>
        /// Use a deadzone for a neck's y-rotation.
        /// If true, the neck's Yaw is not affected by the head's Yaw 
        /// until the head's Yaw has moved over a certain threshold - this has the
        /// benefit of keeping the neck's rotation fairly stable.
        /// </summary>
        [Tooltip("Use a deadzone for a neck's y-rotation.\n" +
            "If true, the neck's Yaw is not affected by the head's Yaw until the head's" +
            " Yaw has moved over a certain threshold - this has the benefit of keeping the" +
            " neck's rotation fairly stable.")]
        public static bool useNeckYawDeadzone = true;


        [Header("Inferred Shoulder Settings")]
        /// <summary>
        /// Should the Neck Position be used to predict shoulder positions?
        /// If true, the shoulders are less affected by head roll & pitch rotation (z & x rotation)
        /// </summary>
        [Tooltip("Should the Neck Position be used to predict shoulder positions?\n" +
            "If true, the shoulders are less affected by head roll & pitch rotation (z & x rotation)")]
        public static bool useNeckPositionForShoulders = true;

        /// <summary>
        /// The shoulder's horizontal offset from the neck.
        /// </summary>
        [Tooltip("The shoulder's horizontal offset from the neck.")]
        public static float shoulderOffset = 0.1f;

        /// <summary>
        /// The wrist shoulder lerp amount is only used when the rayOrigin is wristShoulderLerp. 
        /// It specifies how much the wrist vs the shoulder is used as a ray origin.
        /// At 0, only the wrist position and rotation are taken into account.
        /// At 1, only the shoulder position and rotation are taken into account.
        /// For a more responsive far field ray, blend towards the wrist. For a more stable far field ray,
        /// blend towards the shoulder. Keep the value central for a blend between the two.
        /// </summary>
        public static float wristShoulderBlendAmount = 0.532f;

        private static Quaternion storedNeckRotation;
        //private EulerAngleDeadzone neckYawDeadzone;

        /// <summary>
        /// This local-space offset from the wrist is used to better align the ray to the pinch position
        /// </summary>
        public static Vector3 pinchWristOffset = new Vector3(0.0425f, 0.0652f, 0.0f);

        public static Transform transformHelper;

        private static void UpdateBodyPositions()
        {
            UpdateNeckPositions();
            UpdateNeckRotation();
            UpdateHeadOffsetShoulderPositions();
            UpdateShoulderPositions();
            UpdateHipPositions();;
            //UpdateEyePositions();
        }

        private static void UpdateNeckPositions()
        {
            UpdateLocalOffsetNeckPosition();
            UpdateWorldOffsetNeckPosition();
            UpdateNeckPosition();
        }

        private static void UpdateNeckPosition()
        {
            NeckPosition = Vector3.Lerp(NeckPositionLocalSpace, NeckPositionWorldSpace, worldLocalNeckPositionBlend);
        }

        private static void UpdateNeckRotation()
        {
            //float neckYRotation = useNeckYawDeadzone ? neckYawDeadzone.DeadzoneCentre : Head.rotation.eulerAngles.y;
            float neckYRotation = Head.rotation.eulerAngles.y;


            Quaternion newNeckRotation = Quaternion.Euler(0, neckYRotation, 0);

            // Rotated too far in a single frame
            if (Quaternion.Angle(storedNeckRotation, newNeckRotation) > 15)
            {
                NeckRotation = newNeckRotation;
            }

            storedNeckRotation = newNeckRotation;

            NeckRotation = Quaternion.Lerp(NeckRotation, storedNeckRotation, Time.deltaTime * neckRotationLerpSpeed);
        }

        private static void UpdateLocalOffsetNeckPosition()
        {
            Vector3 localNeckOffset = new Vector3()
            {
                x = 0,
                y = neckOffset,
                z = 0
            };

            if (transformHelper != null)
            {
                transformHelper.position = Head.position;
                transformHelper.rotation = Head.rotation;
                NeckPositionLocalSpace = transformHelper.TransformPoint(localNeckOffset);
            }
        }

        private static void UpdateWorldOffsetNeckPosition()
        {
            Vector3 worldNeckOffset = new Vector3()
            {
                x = 0,
                y = neckOffset,
                z = 0
            };

            Vector3 headPosition = Head.position;
            NeckPositionWorldSpace = headPosition + worldNeckOffset;
        }

        private static void UpdateHeadOffsetShoulderPositions()
        {
            ShoulderPositionsLocalSpace[0] = Head.TransformPoint(-shoulderOffset, neckOffset, 0);
            ShoulderPositionsLocalSpace[1] = Head.TransformPoint(shoulderOffset, neckOffset, 0);
        }

        private static void UpdateShoulderPositions()
        {
            if (useNeckPositionForShoulders)
            {
                ShoulderPositions[0] = GetShoulderPosAtRotation(true, NeckRotation);
                ShoulderPositions[1] = GetShoulderPosAtRotation(false, NeckRotation);
            }
            else
            {
                ShoulderPositions = ShoulderPositionsLocalSpace;
            }
        }

        private static Vector3 GetShoulderPosAtRotation(bool isLeft, Quaternion neckRotation)
        {
            transformHelper.position = NeckPosition;
            transformHelper.rotation = neckRotation;

            Vector3 shoulderNeckOffset = new Vector3
            {
                x = isLeft ? -shoulderOffset : shoulderOffset,
                y = 0,
                z = 0
            };

            return transformHelper.TransformPoint(shoulderNeckOffset);
        }
        private static void UpdateHipPositions()
        {
            HipPositions[0] = ShoulderPositions[0];
            HipPositions[1] = ShoulderPositions[1];

            HipPositions[0].y += verticalHipOffset;
            HipPositions[1].y += verticalHipOffset;
        }

        //private void UpdateEyePositions()
        //{
        //    EyePositions[0] = Head.TransformPoint(-eyeOffset.x, eyeOffset.y, 0);
        //    EyePositions[1] = Head.TransformPoint(eyeOffset.x, eyeOffset.y, 0);
        //}

        private static Vector3 visualAimPosition;
        private static Vector3 aimPosition;
        private static Vector3 rayOrigin;
        private static Vector3 rayDirection;

        private static OneEuroFilter<Vector3> aimPositionFilter;
        private static OneEuroFilter<Vector3> rayOriginFilter;

        /// <summary>
        /// Calculates the Ray Direction
        /// </summary>
        private static void CalculateRay(XRHand hand)
        {
            if (hand == null)
            {
                return;
            }

            visualAimPosition = CalculateVisualAimPosition(hand);

            // Filtering using the One Euro filter reduces jitter from both positions
            aimPosition = CalculateAimPosition(hand);
            rayOrigin = CalculateRayOrigin(hand);
            rayDirection = CalculateDirection();
        }

        protected static Vector3 CalculateVisualAimPosition(XRHand hand)
        {
            return hand.GetPredictedPinchPosition();
        }

        protected static Vector3 CalculateAimPosition(XRHand hand)
        {
            return aimPositionFilter.Filter(hand.GetStablePinchPosition());
        }

        protected static Vector3 CalculateRayOrigin(XRHand hand)
        {
            int index = hand.handedness == Handedness.Left ? 0 : 1;
            return rayOriginFilter.Filter(GetRayOrigin(hand, ShoulderPositions[index]));
        }

        protected static Vector3 GetRayOrigin(XRHand hand, Vector3 shoulderPosition)
        {
            return Vector3.Lerp(GetWristOffsetPosition(hand), shoulderPosition, wristShoulderBlendAmount);
        }

        protected static Vector3 CalculateDirection()
        {
            return (aimPosition - rayOrigin).normalized;
        }

        public void ResetRay()
        {
            ResetFilters();
        }

        private static readonly float oneEurofreq = 30;

        /// <summary>
        /// Beta param for OneEuroFilter (see https://cristal.univ-lille.fr/~casiez/1euro/)
        /// If you're experiencing high speed lag, increase beta
        /// </summary>
        private static float oneEuroBeta = 100;

        /// <summary>
        /// MinCutoff for OneEuroFilter (see https://cristal.univ-lille.fr/~casiez/1euro/)
        /// If you're experiencing slow speed jitter, decrease MinCutoff
        /// </summary>
        private static float oneEuroMinCutoff = 5;

        protected static void ResetFilters()
        {
            aimPositionFilter = new OneEuroFilter<Vector3>(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
            rayOriginFilter = new OneEuroFilter<Vector3>(oneEurofreq, oneEuroMinCutoff, oneEuroBeta);
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /// Euro filter related classes **copied** from the Tracking Preview OneEuroFilter code
        internal class LowPassFilter
        {
            float y, a, s;
            bool initialized;

            public void setAlpha(float _alpha)
            {
                if (_alpha <= 0.0f || _alpha > 1.0f)
                {
                    Debug.LogError("alpha should be in (0.0., 1.0]");
                    return;
                }
                a = _alpha;
            }

            public LowPassFilter(float _alpha, float _initval = 0.0f)
            {
                y = s = _initval;
                setAlpha(_alpha);
                initialized = false;
            }

            public float Filter(float _value)
            {
                float result;
                if (initialized)
                    result = a * _value + (1.0f - a) * s;
                else
                {
                    result = _value;
                    initialized = true;
                }
                y = _value;
                s = result;
                return result;
            }

            public float filterWithAlpha(float _value, float _alpha)
            {
                setAlpha(_alpha);
                return Filter(_value);
            }

            public bool hasLastRawValue()
            {
                return initialized;
            }

            public float lastRawValue()
            {
                return y;
            }

        };

        internal class OneEuroFilter
        {
            float freq;
            float mincutoff;
            float beta;
            float dcutoff;
            LowPassFilter x;
            LowPassFilter dx;
            float lasttime;

            // currValue contains the latest value which have been succesfully filtered
            // prevValue contains the previous filtered value
            public float currValue { get; protected set; }
            public float prevValue { get; protected set; }

            float alpha(float _cutoff)
            {
                float te = 1.0f / freq;
                float tau = 1.0f / (2.0f * Mathf.PI * _cutoff);
                return 1.0f / (1.0f + tau / te);
            }

            void setFrequency(float _f)
            {
                if (_f <= 0.0f)
                {
                    Debug.LogError("freq should be > 0");
                    return;
                }
                freq = _f;
            }

            void setMinCutoff(float _mc)
            {
                if (_mc <= 0.0f)
                {
                    Debug.LogError("mincutoff should be > 0");
                    return;
                }
                mincutoff = _mc;
            }

            void setBeta(float _b)
            {
                beta = _b;
            }

            void setDerivateCutoff(float _dc)
            {
                if (_dc <= 0.0f)
                {
                    Debug.LogError("dcutoff should be > 0");
                    return;
                }
                dcutoff = _dc;
            }

            public OneEuroFilter(float _freq, float _mincutoff = 1.0f, float _beta = 0.0f, float _dcutoff = 1.0f)
            {
                setFrequency(_freq);
                setMinCutoff(_mincutoff);
                setBeta(_beta);
                setDerivateCutoff(_dcutoff);
                x = new LowPassFilter(alpha(mincutoff));
                dx = new LowPassFilter(alpha(dcutoff));
                lasttime = -1.0f;

                currValue = 0.0f;
                prevValue = currValue;
            }

            public void UpdateParams(float _freq, float _mincutoff = 1.0f, float _beta = 0.0f, float _dcutoff = 1.0f)
            {
                setFrequency(_freq);
                setMinCutoff(_mincutoff);
                setBeta(_beta);
                setDerivateCutoff(_dcutoff);
                x.setAlpha(alpha(mincutoff));
                dx.setAlpha(alpha(dcutoff));
            }

            public float Filter(float value, float timestamp = -1.0f)
            {
                prevValue = currValue;

                // update the sampling frequency based on timestamps
                if (lasttime != -1.0f && timestamp != -1.0f)
                    freq = 1.0f / (timestamp - lasttime);
                lasttime = timestamp;
                // estimate the current variation per second 
                float dvalue = x.hasLastRawValue() ? (value - x.lastRawValue()) * freq : 0.0f; // FIXME: 0.0 or value? 
                float edvalue = dx.filterWithAlpha(dvalue, alpha(dcutoff));
                // use it to update the cutoff frequency
                float cutoff = mincutoff + beta * Mathf.Abs(edvalue);
                // filter the given value
                currValue = x.filterWithAlpha(value, alpha(cutoff));

                return currValue;
            }
        };

        // this class instantiates an array of OneEuroFilter objects to filter each component of Vector2, Vector3, Vector4 or Quaternion types
        internal class OneEuroFilter<T> where T : struct
        {
            // containst the type of T
            Type type;
            // the array of filters
            OneEuroFilter[] oneEuroFilters;

            // filter parameters
            public float freq { get; protected set; }
            public float mincutoff { get; protected set; }
            public float beta { get; protected set; }
            public float dcutoff { get; protected set; }

            // currValue contains the latest value which have been succesfully filtered
            // prevValue contains the previous filtered value
            public T currValue { get; protected set; }
            public T prevValue { get; protected set; }

            // initialization of our filter(s)
            public OneEuroFilter(float _freq, float _mincutoff = 1.0f, float _beta = 0.0f, float _dcutoff = 1.0f)
            {
                type = typeof(T);
                currValue = new T();
                prevValue = new T();

                freq = _freq;
                mincutoff = _mincutoff;
                beta = _beta;
                dcutoff = _dcutoff;

                if (type == typeof(Vector2))
                    oneEuroFilters = new OneEuroFilter[2];

                else if (type == typeof(Vector3))
                    oneEuroFilters = new OneEuroFilter[3];

                else if (type == typeof(Vector4) || type == typeof(Quaternion))
                    oneEuroFilters = new OneEuroFilter[4];
                else
                {
                    Debug.LogError(type + " is not a supported type");
                    return;
                }

                for (int i = 0; i < oneEuroFilters.Length; i++)
                    oneEuroFilters[i] = new OneEuroFilter(freq, mincutoff, beta, dcutoff);
            }

            // updates the filter parameters
            public void UpdateParams(float _freq, float _mincutoff = 1.0f, float _beta = 0.0f, float _dcutoff = 1.0f)
            {
                freq = _freq;
                mincutoff = _mincutoff;
                beta = _beta;
                dcutoff = _dcutoff;

                for (int i = 0; i < oneEuroFilters.Length; i++)
                    oneEuroFilters[i].UpdateParams(freq, mincutoff, beta, dcutoff);
            }


            // filters the provided _value and returns the result.
            // Note: a timestamp can also be provided - will override filter frequency.
            public T Filter<U>(U _value, float timestamp = -1.0f) where U : struct
            {
                prevValue = currValue;

                if (typeof(U) != type)
                {
                    Debug.LogError("WARNING! " + typeof(U) + " when " + type + " is expected!\nReturning previous filtered value");
                    currValue = prevValue;

                    return (T)Convert.ChangeType(currValue, typeof(T));
                }

                if (type == typeof(Vector2))
                {
                    Vector2 output = Vector2.zero;
                    Vector2 input = (Vector2)Convert.ChangeType(_value, typeof(Vector2));

                    for (int i = 0; i < oneEuroFilters.Length; i++)
                        output[i] = oneEuroFilters[i].Filter(input[i], timestamp);

                    currValue = (T)Convert.ChangeType(output, typeof(T));
                }

                else if (type == typeof(Vector3))
                {
                    Vector3 output = Vector3.zero;
                    Vector3 input = (Vector3)Convert.ChangeType(_value, typeof(Vector3));

                    for (int i = 0; i < oneEuroFilters.Length; i++)
                        output[i] = oneEuroFilters[i].Filter(input[i], timestamp);

                    currValue = (T)Convert.ChangeType(output, typeof(T));
                }

                else if (type == typeof(Vector4))
                {
                    Vector4 output = Vector4.zero;
                    Vector4 input = (Vector4)Convert.ChangeType(_value, typeof(Vector4));

                    for (int i = 0; i < oneEuroFilters.Length; i++)
                        output[i] = oneEuroFilters[i].Filter(input[i], timestamp);

                    currValue = (T)Convert.ChangeType(output, typeof(T));
                }

                else
                {
                    Quaternion output = Quaternion.identity;
                    Quaternion input = (Quaternion)Convert.ChangeType(_value, typeof(Quaternion));

                    // Workaround that take into account that some input device sends
                    // quaternion that represent only a half of all possible values.
                    // this piece of code does not affect normal behaviour (when the
                    // input use the full range of possible values).
                    if (Vector4.SqrMagnitude(new Vector4(oneEuroFilters[0].currValue, oneEuroFilters[1].currValue, oneEuroFilters[2].currValue, oneEuroFilters[3].currValue).normalized
                        - new Vector4(input[0], input[1], input[2], input[3]).normalized) > 2)
                    {
                        input = new Quaternion(-input.x, -input.y, -input.z, -input.w);
                    }

                    for (int i = 0; i < oneEuroFilters.Length; i++)
                        output[i] = oneEuroFilters[i].Filter(input[i], timestamp);

                    currValue = (T)Convert.ChangeType(output, typeof(T));
                }

                return (T)Convert.ChangeType(currValue, typeof(T));
            }
        }

        #endregion
    }
}