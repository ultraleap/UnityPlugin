using Leap;
using Leap.Encoding;
using System;
using System.Collections.Generic;

using UnityEngine;

using Bone = Leap.Bone;
using Hand = Leap.Hand;

namespace Leap.Tracking.OpenXR
{
    public class OpenXRLeapProvider : LeapProvider
    {
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

        [Tooltip("Specifies the main camera. Falls back to Camera.main if not set")]
        [SerializeField]
        private Camera _mainCamera; // Redundant backing field, used to present value in editor at parent level
        /// <summary>
        /// Specifies the main camera.
        /// Falls back to Camera.main if not set.
        /// </summary>
        public Camera mainCamera
        {
            get
            {
                if (_mainCamera == null)
                {
                    _mainCamera = Camera.main;
                }
                return _mainCamera;
            }
            set
            {
                _mainCamera = value;
            }
        }

        [Tooltip("Automatically adds a TrackedPoseDriver to the MainCamera if there is not one already")]
        public bool _autoCreateTrackedPoseDriver = true;

        private HandJointLocation[] _joints;

        public override TrackingSource TrackingDataSource { get { return CheckOpenXRAvailable(); } }

        private TrackingSource CheckOpenXRAvailable()
        {
            if (_trackingSource != TrackingSource.NONE)
            {
                return _trackingSource;
            }

            if (HandTrackingSourceUtility.LeapOpenXRTrackingAvailable)
            {
                _trackingSource = TrackingSource.OPENXR_LEAP;
            }
            else if (HandTrackingSourceUtility.NonLeapOpenXRTrackingAvailable)
            {
                _trackingSource = TrackingSource.OPENXR;
            }
            else
            {
                _trackingSource = TrackingSource.NONE;
            }

            return _trackingSource;
        }

        private void OnEnable()
        {
            if (_autoCreateTrackedPoseDriver)
            {
                mainCamera.AddTrackedPoseDriverToCamera();
            }
        }

        private void Update()
        {
            PopulateLeapFrame(ref _currentFrame);

            trackerTransform.translation = Vector3.zero;
            trackerTransform.rotation = Quaternion.identity;
            trackerTransform.scale = mainCamera.transform.lossyScale;

            // Adjust for the camera parent transform if this camera is part of a rig.
            var parentTransform = mainCamera.transform.parent;
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

        private void PopulateLeapFrame(ref Frame leapFrame)
        {
            leapFrame.Hands.Clear();
            leapFrame.Id = _frameId++;
            leapFrame.DeviceID = 0;
            leapFrame.Timestamp = (long)(Time.realtimeSinceStartup * 1000000f);
            leapFrame.CurrentFramesPerSecond = 1.0f / Time.smoothDeltaTime;

            if (PopulateLeapHandFromOpenXRJoints(HandTracker.Left, ref _leftHand))
            {
                leapFrame.Hands.Add(_leftHand);
            }

            if (PopulateLeapHandFromOpenXRJoints(HandTracker.Right, ref _rightHand))
            {
                leapFrame.Hands.Add(_rightHand);
            }
        }

        private bool PopulateLeapHandFromOpenXRJoints(HandTracker handTracker, ref Hand hand)
        {
            _joints ??= new HandJointLocation[handTracker.JointCount];

            if (!handTracker.TryLocateHandJoints(_joints))
            {
                if (handTracker == HandTracker.Left)
                {
                    _leftHandFirstSeen_ticks = -1;
                }
                else
                {
                    _rightHandFirstSeen_ticks = -1;
                }

                return false;
            }

            long currentDateTimeTicks = DateTime.Now.Ticks;

            float timeVisible = 0;
            if (handTracker == HandTracker.Left)
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
                    var prevJoint = _joints[xrPrevIndex];
                    var nextJoint = _joints[xrNextIndex];

                    // Ignore thumb Metacarpal
                    if (fingerIndex == 0 && boneIndex == 0)
                    {
                        var metacarpalPosition = _joints[(int)HandJoint.ThumbMetacarpal].Pose.position;
                        hand.GetBone(boneIndex).Fill(
                            metacarpalPosition,
                            metacarpalPosition,
                            metacarpalPosition,
                            _joints[(int)HandJoint.ThumbMetacarpal].Pose.forward,
                            0f,
                            _joints[(int)HandJoint.ThumbMetacarpal].Radius * 1.5f, // 1.5 to convert from joint radius to bone width (joints bigger than bones)
                            (Bone.BoneType)boneIndex,
                            (_joints[(int)HandJoint.Palm].Pose.rotation * PalmOffset[hand.IsLeft ? 0 : 1].rotation) * ThumbMetacarpalRotationOffset[hand.IsLeft ? 0 : 1]);
                        continue;
                    }

                    // Populate the finger bone information
                    var bone = hand.fingers[fingerIndex].bones[boneIndex];
                    bone.Fill(
                        prevJoint.Pose.position,
                        nextJoint.Pose.position,
                        ((prevJoint.Pose.position + nextJoint.Pose.position) / 2f),
                        prevJoint.Pose.forward,
                        (prevJoint.Pose.position - nextJoint.Pose.position).magnitude,
                        prevJoint.Radius * 1.5f, // 1.5 to convert from joint radius to bone width (joints bigger than bones)
                        (Bone.BoneType)boneIndex,
                        prevJoint.Pose.rotation);
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
                    (handTracker == HandTracker.Left ? 0 : 1),
                    fingerIndex,
                    timeVisible,
                    _joints[xrTipIndex].Pose.position,
                    _joints[xrIntermediateIndex].Pose.forward,
                    fingerWidth,
                    fingerLength,
                    hand.GetFingerStrength(fingerIndex) < 0.4, // Fixed for now
                    (Finger.FingerType)fingerIndex);
            }

            // Populate the whole hand information.
            // NOTE: Ordering is important as some of the `Calculate*` functions requires some of this data to be set.
            float handScale = Hands.CalculateHandScale(ref hand); // Requires fingers to be set.
            hand.FrameId = _frameId;
            hand.Id = handTracker == HandTracker.Left ? _leftHandId : _rightHandId;
            hand.Confidence = 1.0f;
            hand.PalmWidth = handScale * DEFAULT_HAND_SCALE;
            hand.IsLeft = handTracker == HandTracker.Left;
            hand.TimeVisible = timeVisible;
            hand.PalmVelocity = _joints[(int)HandJoint.Palm].LinearVelocity;
            hand.WristPosition = _joints[(int)HandJoint.Wrist].Pose.position;

            // Calculate adjusted palm position, rotation and direction.
            hand.Rotation = _joints[(int)HandJoint.Palm].Pose.rotation * PalmOffset[hand.IsLeft ? 0 : 1].rotation;
            hand.PalmPosition = _joints[(int)HandJoint.Palm].Pose.position + hand.Rotation * PalmOffset[hand.IsLeft ? 0 : 1].position;
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
            var palmPosition = _joints[(int)HandJoint.Palm].Pose.position;
            var wristPosition = _joints[(int)HandJoint.Wrist].Pose.position;
            var wristWidth = _joints[(int)HandJoint.Wrist].Radius * 2f;

            if (handTracker.JointSet == HandJointSet.HandWithForearm)
            {
                var elbowPosition = _joints[(int)HandJoint.Elbow].Pose.position;
                var elbowRotation = _joints[(int)HandJoint.Elbow].Pose.rotation;
                var elbowDirection = elbowRotation * Vector3.back;
                var elbowLength = (elbowPosition - wristPosition).magnitude;
                var centerPosition = (elbowPosition + wristPosition) / 2f;
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
            else
            {
                const float elbowLength = 0.3f;
                var elbowRotation = _joints[(int)HandJoint.Palm].Pose.rotation;
                var elbowDirection = elbowRotation * Vector3.back;
                var elbowPosition = _joints[(int)HandJoint.Palm].Pose.position + (elbowDirection * elbowLength);
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
            return true;
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

                if (mainCamera != null)
                {
                    _backingEditTimeFrame = _backingUntransformedEditTimeFrame.TransformedCopy(new LeapTransform(mainCamera.transform));
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