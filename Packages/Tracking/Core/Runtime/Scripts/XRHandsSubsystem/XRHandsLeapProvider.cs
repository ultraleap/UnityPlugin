/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Encoding;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Processing;

namespace Leap
{
    /// <summary>
    /// A LeapProvider that converts XRHands Subsystem data to a  Leap.Frame for use in features that use  Leap.Frame and LeapProviders as a data source.
    /// </summary>
    public class XRHandsLeapProvider : LeapProvider
    {
        XRHandSubsystem currentSubsystem;

        private LeapTransform trackerTransform = new LeapTransform(Vector3.zero, Quaternion.identity);

        private Frame _currentFrame = new Frame();

        private Hand _leftHand = new Hand();
        private Hand _rightHand = new Hand();

        private int _handId = 0;
        private int _leftHandId = 0;
        private int _rightHandId = 0;
        private long _leftHandFirstSeen_ticks;
        private long _rightHandFirstSeen_ticks;

        // Correction for the 0th thumb bone rotation offsets to match LeapC
        private static readonly Quaternion[] ThumbMetacarpalRotationOffset =
        {
            Quaternion.Euler(0, +25.9f, -63.45f),
            Quaternion.Euler(0, -25.9f, +63.45f),
        };

        // Correction for the Palm position & rotation to match LeapC. Also used for the hand rotation.
        private static readonly Pose[] PalmOffset =
        {
            new Pose(new Vector3(-0.001039105f, -0.008749885f, 0.01165112f), Quaternion.Euler(-8f, -0.63f, -8.4f)),
            new Pose(new Vector3(0.001039105f, -0.008749885f, 0.01165112f), Quaternion.Euler(-8f, +0.63f, +8.4f)),
        };

        private const float DEFAULT_HAND_SCALE = 0.08425f;

        private long _frameId = 0;

        private TrackedPoseDriver _trackedPoseDriver;

        private float lastCheckForSubsystemTime = 0;

        [Tooltip("Automatically adds a TrackedPoseDriver to the MainCamera if there is not one already")]
        public bool _autoCreateTrackedPoseDriver = true;

        private void Start()
        {
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
                Debug.LogWarning("No XRHands Subsystem available.");
                return;
            }


            currentSubsystem.updatedHands -= UpdateHands;
            currentSubsystem.updatedHands += UpdateHands;

            _trackedPoseDriver = Camera.main.GetComponent<TrackedPoseDriver>();

            if (_trackedPoseDriver == null && _autoCreateTrackedPoseDriver)
            {
                Debug.Log("Automatically assigning a TrackedPoseDriver to the Main Camera");
                _trackedPoseDriver = Camera.main.gameObject.AddComponent<TrackedPoseDriver>();
            }
        }

        private void OnDestroy()
        {
            if (currentSubsystem != null)
                currentSubsystem.updatedHands -= UpdateHands;
        }

        private void Update()
        {
            if (currentSubsystem == null && Time.time > lastCheckForSubsystemTime + 1)
            {
                CheckForSubsystem();
            }
        }

        void CheckForSubsystem()
        {
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
                Debug.LogWarning("No XRHands Subsystem available.");

                lastCheckForSubsystemTime = Time.time;
                return;
            }

            currentSubsystem.updatedHands -= UpdateHands;
            currentSubsystem.updatedHands += UpdateHands;
            _trackedPoseDriver = Camera.main.GetComponent<TrackedPoseDriver>();
        }

        private void UpdateHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags, XRHandSubsystem.UpdateType updateType)
        {
            PopulateLeapFrame(subsystem, ref _currentFrame);

            trackerTransform.translation = Vector3.zero;
            trackerTransform.rotation = Quaternion.identity;
            trackerTransform.scale = Camera.main.transform.lossyScale;

            // Adjust for relative transform if it's in use.
            if (_trackedPoseDriver != null && _trackedPoseDriver.UseRelativeTransform)
            {
                trackerTransform.translation += _trackedPoseDriver.originPose.position;
                trackerTransform.rotation *= _trackedPoseDriver.originPose.rotation;
            }

            // Adjust for the camera parent transform if this camera is part of a rig.
            var parentTransform = Camera.main.transform.parent;
            if (parentTransform != null)
            {
                trackerTransform.translation += parentTransform.position;
                trackerTransform.rotation *= parentTransform.rotation;
            }

            _currentFrame.Transform(trackerTransform);

            DispatchUpdateFrameEvent(_currentFrame);
        }

        private void FixedUpdate()
        {
            DispatchFixedFrameEvent(_currentFrame);
        }

        private void PopulateLeapFrame(XRHandSubsystem subsystem, ref Frame leapFrame)
        {
            leapFrame.Hands.Clear();
            leapFrame.Id = _frameId++;
            leapFrame.DeviceID = 0;
            leapFrame.Timestamp = (long)(Time.realtimeSinceStartup * 1000000f);
            leapFrame.CurrentFramesPerSecond = 1.0f / Time.smoothDeltaTime;

            if (subsystem.leftHand == null || !subsystem.leftHand.isTracked)
            {
                _leftHandFirstSeen_ticks = -1;
            }
            else
            {
                PopulateLeapHandFromXRHand(subsystem.leftHand, GetHandTimeVisible(Chirality.Left), ref _leftHand);
                leapFrame.Hands.Add(_leftHand);
            }

            if (subsystem.rightHand == null || !subsystem.rightHand.isTracked)
            {
                _rightHandFirstSeen_ticks = -1;
            }
            else
            {
                PopulateLeapHandFromXRHand(subsystem.rightHand, GetHandTimeVisible(Chirality.Right), ref _rightHand);
                leapFrame.Hands.Add(_rightHand);
            }
        }

        private void PopulateLeapHandFromXRHand(XRHand xrhand, float timeVisible, ref Hand hand)
        {
            var joints = xrhand.GetRawJointArray();

            Chirality chirality = xrhand.handedness == UnityEngine.XR.Hands.Handedness.Left ? Chirality.Left : Chirality.Right;

            joints[0].TryGetPose(out Pose wristPose);
            joints[1].TryGetPose(out Pose palmPose);

            joints[1].TryGetLinearVelocity(out Vector3 palmLinearVelocity);

            for (int fingerIndex = 0; fingerIndex < 5; fingerIndex++)
            {
                var xrTipIndex = 0;
                var xrIntermediateIndex = 0;
                var fingerWidth = 0f;
                var fingerLength = 0f;

                for (int boneIndex = 0; boneIndex < 4; boneIndex++)
                {
                    var xrPrevIndex = fingerIndex * 5 + boneIndex + 1;
                    var xrNextIndex = xrPrevIndex + 1;
                    var prevJoint = joints[xrPrevIndex];
                    var nextJoint = joints[xrNextIndex];

                    prevJoint.TryGetPose(out Pose prevJointPose);
                    nextJoint.TryGetPose(out Pose nextJointPose);
                    prevJoint.TryGetRadius(out float prevJointRadius);

                    // Ignore thumb Metacarpal
                    if (fingerIndex == 0 && boneIndex == 0)
                    {
                        var metacarpalPosition = joints[(int)XRHandJointID.ThumbProximal].TryGetPose(out Pose thumbProximalPose) ? thumbProximalPose.position : Vector3.zero;

                        // Use the average of the thumb proximal to the wrist to account for XRHands not having suitable metacarpal positions
                        metacarpalPosition += wristPose.position;
                        metacarpalPosition /= 2;

                        hand.GetBone(boneIndex).Fill(
                            metacarpalPosition,
                            metacarpalPosition,
                            metacarpalPosition,
                            thumbProximalPose.forward,
                            0f,
                            joints[(int)XRHandJointID.ThumbMetacarpal].TryGetRadius(out float thumbMeracarpalRadius) ? thumbMeracarpalRadius * 2f : 0f,
                            (Bone.BoneType)boneIndex,
                            palmPose.rotation * PalmOffset[hand.IsLeft ? 0 : 1].rotation * ThumbMetacarpalRotationOffset[hand.IsLeft ? 0 : 1]);
                        continue;
                    }

                    // Populate the finger bone information
                    var bone = hand.fingers[fingerIndex].bones[boneIndex];

                    bone.Fill(
                        prevJointPose.position,
                        nextJointPose.position,
                        ((prevJointPose.position + nextJointPose.position) / 2f),
                        prevJointPose.forward,
                        (prevJointPose.position - nextJointPose.position).magnitude,
                        prevJointRadius * 2f,
                        (Bone.BoneType)boneIndex,
                        prevJointPose.rotation);
                    fingerWidth = Mathf.Max(fingerWidth, bone.Width);

                    if (bone.Type == Bone.BoneType.INTERMEDIATE)
                    {
                        xrIntermediateIndex = xrPrevIndex;
                    }

                    if (bone.Type == Bone.BoneType.DISTAL)
                    {
                        xrTipIndex = xrNextIndex;
                    }

                    // Ignore metacarpals when calculating finger lengths
                    if (boneIndex != 0)
                    {
                        fingerLength += bone.Length;
                    }
                }

                // Populate the higher - level finger data.
                hand.fingers[fingerIndex].Fill(
                    _frameId,
                    (chirality == Chirality.Left ? 0 : 1),
                    fingerIndex,
                    timeVisible,
                    joints[xrTipIndex].TryGetPose(out Pose xrTipIndexPose) ? xrTipIndexPose.position : Vector3.zero,
                    joints[xrIntermediateIndex].TryGetPose(out Pose xrIntermediateIndexPose) ? xrTipIndexPose.forward : Vector3.zero,
                    fingerWidth,
                    fingerLength,
                    hand.GetFingerStrength(fingerIndex) < 0.4, // Fixed for now
                    (Finger.FingerType)fingerIndex);
            }

            // Populate the whole hand information.
            // NOTE: Ordering is important as some of the `Calculate*` functions requires some of this data to be set.
            float handScale = Hands.CalculateHandScale(ref hand); // Requires fingers to be set.
            hand.FrameId = _frameId;
            hand.Id = chirality == Chirality.Left ? _leftHandId : _rightHandId;
            hand.Confidence = 1.0f;
            hand.PalmWidth = handScale * DEFAULT_HAND_SCALE;
            hand.IsLeft = chirality == Chirality.Left;
            hand.TimeVisible = timeVisible;
            hand.PalmVelocity = palmLinearVelocity;
            hand.WristPosition = wristPose.position;

            // Calculate adjusted palm position, rotation and direction.
            hand.Rotation = palmPose.rotation * PalmOffset[hand.IsLeft ? 0 : 1].rotation;
            hand.PalmPosition = palmPose.position + hand.Rotation * PalmOffset[hand.IsLeft ? 0 : 1].position;
            hand.StabilizedPalmPosition = hand.PalmPosition;
            hand.PalmNormal = hand.Rotation * Vector3.down;
            hand.Direction = hand.Rotation * Vector3.forward;

            // Calculate now we have the hand data available.
            // Requires `Hand.Rotation` and fingers to be set.
            hand.GrabStrength = Hands.CalculateGrabStrength(ref hand);
            hand.PinchStrength = Hands.CalculatePinchStrength(ref hand);
            hand.PinchDistance = Hands.CalculatePinchDistance(ref hand);

            // Other hand-properties are derived.
            hand.PalmNormal = hand.Rotation * Vector3.down;

            // Fill arm data.
            var palmPosition = palmPose.position;
            var wristPosition = wristPose.position;
            var wristWidth = hand.PalmWidth * 0.6f;

            const float elbowLength = 0.3f;
            var elbowRotation = palmPose.rotation;
            var elbowDirection = elbowRotation * Vector3.back;
            var elbowPosition = palmPose.position + (elbowDirection * elbowLength);
            var centerPosition = (elbowPosition + palmPosition) / 2f;
            hand.Arm.Fill(
                elbowPosition,
                wristPosition,
                centerPosition,
                elbowDirection,
                elbowLength,
                wristWidth,
                elbowRotation
            );
        }

        float GetHandTimeVisible(Chirality chirality)
        {
            long currentDateTimeTicks = DateTime.Now.Ticks;

            float timeVisible = 0;
            if (chirality == Chirality.Left)
            {
                if (_leftHandFirstSeen_ticks == -1)
                {
                    _leftHandFirstSeen_ticks = currentDateTimeTicks;
                    _leftHandId = _handId++;
                }
                timeVisible = ((float)(currentDateTimeTicks - _leftHandFirstSeen_ticks)) / (float)TimeSpan.TicksPerSecond;
            }
            else
            {
                if (_rightHandFirstSeen_ticks == -1)
                {
                    _rightHandFirstSeen_ticks = currentDateTimeTicks;
                    _rightHandId = _handId++;
                }
                timeVisible = ((float)(currentDateTimeTicks - _rightHandFirstSeen_ticks)) /
                              (float)TimeSpan.TicksPerSecond;
            }

            return timeVisible;
        }

        #region LeapProvider Implementation

        [JetBrains.Annotations.NotNull]
        public override Frame CurrentFrame
        {
            get
            {
#if UNITY_EDITOR
                return !Application.isPlaying ? EditTimeFrame : _currentFrame;
#else
                return _currentFrame;
#endif
            }
        }

        [JetBrains.Annotations.NotNull]
        public override Frame CurrentFixedFrame => CurrentFrame;

#if UNITY_EDITOR
        private Frame _backingUntransformedEditTimeFrame;
        private Frame _backingEditTimeFrame;

        private Frame EditTimeFrame
        {
            get
            {
                (_backingEditTimeFrame ??= new Frame()).Hands.Clear();
                (_backingUntransformedEditTimeFrame ??= new Frame()).Hands.Clear();
                _backingUntransformedEditTimeFrame.Hands.Add(EditTimeLeftHand);
                _backingUntransformedEditTimeFrame.Hands.Add(EditTimeRightHand);

                if (Camera.main != null)
                {
                    _backingEditTimeFrame = _backingUntransformedEditTimeFrame.TransformedCopy(new LeapTransform(Camera.main.transform));
                }

                return _backingEditTimeFrame;
            }
        }

        private readonly Dictionary<TestHandFactory.TestHandPose, Hand> _cachedLeftHands
            = new Dictionary<TestHandFactory.TestHandPose, Hand>();

        private Hand EditTimeLeftHand
        {
            get
            {
                if (_cachedLeftHands.TryGetValue(editTimePose, out Hand cachedHand))
                {
                    return cachedHand;
                }

                cachedHand = TestHandFactory.MakeTestHand(true, editTimePose);
                _cachedLeftHands[editTimePose] = cachedHand;
                return cachedHand;
            }
        }

        private readonly Dictionary<TestHandFactory.TestHandPose, Hand> _cachedRightHands
            = new Dictionary<TestHandFactory.TestHandPose, Hand>();

        private Hand EditTimeRightHand
        {
            get
            {
                if (_cachedRightHands.TryGetValue(editTimePose, out Hand cachedHand))
                {
                    return cachedHand;
                }

                cachedHand = TestHandFactory.MakeTestHand(false, editTimePose);
                _cachedRightHands[editTimePose] = cachedHand;
                return cachedHand;
            }
        }
#endif

        #endregion
    }
}