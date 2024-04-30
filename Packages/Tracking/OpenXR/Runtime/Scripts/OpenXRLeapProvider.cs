using Leap;
using Leap.Unity;
using Leap.Unity.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
using Ultraleap.Tracking.OpenXR.Interop;
using UnityEngine;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using Bone = Leap.Bone;
using Hand = Leap.Hand;

namespace Ultraleap.Tracking.OpenXR
{
    public class OpenXRLeapProvider : LeapProvider
    {
        private HandTrackingFeature _handTracking = OpenXRSettings.Instance.GetFeature<HandTrackingFeature>();
        
        private LeapTransform _trackerTransform = new(Vector3.zero, Quaternion.identity);

        private Frame _currentFrame = new();
        private Hand _leftHand = new();
        private Hand _rightHand = new();

        private int _handId;
        private int _leftHandId;
        private int _rightHandId;
        private long _leftHandFirstSeenTicks;
        private long _rightHandFirstSeenTicks;

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

        private const float DefaultHandScale = 0.08425f;

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
                if (!_mainCamera)
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

        private TrackedPoseDriver _trackedPoseDriver;
        private XrHandJointLocationExt[] _jointLocations;
        private XrHandJointVelocityExt[] _jointVelocities;

        public override TrackingSource TrackingDataSource { get { return CheckOpenXRAvailable(); } }

        private TrackingSource CheckOpenXRAvailable()
        {
            if (_trackingSource != TrackingSource.NONE)
            {
                return _trackingSource;
            }

            if (XRGeneralSettings.Instance != null &&
                XRGeneralSettings.Instance.Manager != null &&
                XRGeneralSettings.Instance.Manager.ActiveLoaderAs<OpenXRLoaderBase>() != null &&
                OpenXRSettings.Instance != null &&
                OpenXRSettings.Instance.GetFeature<HandTrackingFeature>() != null &&
                OpenXRSettings.Instance.GetFeature<HandTrackingFeature>().SupportsHandTracking)
            {
                if (OpenXRSettings.Instance.GetFeature<HandTrackingFeature>().IsUltraleapHandTracking)
                {
                    _trackingSource = TrackingSource.OPENXR_LEAP;
                }
                else
                {
                    _trackingSource = TrackingSource.OPENXR;
                }
            }
            else
            {
                _trackingSource = TrackingSource.NONE;
            }

            return _trackingSource;
        }

        private void Start()
        {
            _trackedPoseDriver = mainCamera.GetComponent<TrackedPoseDriver>();
        }

        private void Update()
        {
            PopulateLeapFrame(ref _currentFrame);

            _trackerTransform.translation = Vector3.zero;
            _trackerTransform.rotation = Quaternion.identity;
            _trackerTransform.scale = mainCamera.transform.lossyScale;

            // Adjust for relative transform if it's in use.
            if (_trackedPoseDriver != null && _trackedPoseDriver.UseRelativeTransform)
            {
                _trackerTransform.translation += _trackedPoseDriver.originPose.position;
                _trackerTransform.rotation *= _trackedPoseDriver.originPose.rotation;
            }

            // Adjust for the camera parent transform if this camera is part of a rig.
            var parentTransform = mainCamera.transform.parent;
            if (parentTransform != null)
            {
                _trackerTransform.translation += parentTransform.position;
                _trackerTransform.rotation *= parentTransform.rotation;
            }

            _currentFrame.Transform(_trackerTransform);

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

            if (PopulateLeapHandFromOpenXRJoints(XrHandExt.Left, ref _leftHand))
            {
                leapFrame.Hands.Add(_leftHand);
            }

            if (PopulateLeapHandFromOpenXRJoints(XrHandExt.Right, ref _rightHand))
            {
                leapFrame.Hands.Add(_rightHand);
            }
        }

        private bool PopulateLeapHandFromOpenXRJoints(XrHandExt hand, ref Hand leapHand)
        {
            // TODO: Retrieve this properly from the feature.
            int jointCount = 27;
            
            _jointLocations ??= new XrHandJointLocationExt[jointCount];
            _jointVelocities ??= new XrHandJointVelocityExt[jointCount];
            
            bool isLeftHand = hand == XrHandExt.Left;

            if (!_handTracking.LocateHandJoints(hand, ref _jointLocations, ref _jointVelocities))
            {
                if (isLeftHand)
                {
                    _leftHandFirstSeenTicks = -1;
                }
                else
                {
                    _rightHandFirstSeenTicks = -1;
                }
                return false;
            }

            float timeVisible;
            long currentDateTimeTicks = DateTime.Now.Ticks;
            if (isLeftHand)
            {
                if (_leftHandFirstSeenTicks == -1)
                {
                    _leftHandFirstSeenTicks = currentDateTimeTicks;
                    _leftHandId = _handId++;
                }
                timeVisible = (float)(currentDateTimeTicks - _leftHandFirstSeenTicks) / TimeSpan.TicksPerSecond;
            }
            else
            {
                if (_rightHandFirstSeenTicks == -1)
                {
                    _rightHandFirstSeenTicks = currentDateTimeTicks;
                    _rightHandId = _handId++;
                }
                timeVisible = (float)(currentDateTimeTicks - _rightHandFirstSeenTicks) / TimeSpan.TicksPerSecond;
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
                    var prevJoint = _jointLocations[xrPrevIndex];
                    var nextJoint = _jointLocations[xrNextIndex];

                    // Ignore thumb Metacarpal
                    if (fingerIndex == 0 && boneIndex == 0)
                    {
                        var metacarpalPosition = _jointLocations[(int)XrHandJointExt.ThumbMetacarpal].Pose.position;
                        leapHand.GetBone(boneIndex).Fill(
                            metacarpalPosition,
                            metacarpalPosition,
                            metacarpalPosition,
                            _jointLocations[(int)XrHandJointExt.ThumbMetacarpal].Pose.forward,
                            0f,
                            _jointLocations[(int)XrHandJointExt.ThumbMetacarpal].Radius * 1.5f, // 1.5 to convert from joint radius to bone width (joints bigger than bones)
                            (Bone.BoneType)boneIndex,
                            (_jointLocations[(int)XrHandJointExt.Palm].Pose.rotation * PalmOffset[leapHand.IsLeft ? 0 : 1].rotation) * ThumbMetacarpalRotationOffset[leapHand.IsLeft ? 0 : 1]);
                        continue;
                    }

                    // Populate the finger bone information
                    var bone = leapHand.Fingers[fingerIndex].bones[boneIndex];
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

                    if (bone.Type == Bone.BoneType.TYPE_INTERMEDIATE)
                    {
                        xrIntermediateIndex = xrPrevIndex;
                    }

                    if (bone.Type == Bone.BoneType.TYPE_DISTAL)
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
                leapHand.Fingers[fingerIndex].Fill(
                    _frameId,
                    (isLeftHand ? 0 : 1),
                    fingerIndex,
                    timeVisible,
                    _jointLocations[xrTipIndex].Pose.position,
                    _jointLocations[xrIntermediateIndex].Pose.forward,
                    fingerWidth,
                    fingerLength,
                    leapHand.GetFingerStrength(fingerIndex) < 0.4, // Fixed for now
                    (Finger.FingerType)fingerIndex);
            }

            // Populate the whole hand information.
            // NOTE: Ordering is important as some of the `Calculate*` functions requires some of this data to be set.
            float handScale = Hands.CalculateHandScale(ref leapHand); // Requires fingers to be set.
            leapHand.FrameId = _frameId;
            leapHand.Id = isLeftHand ? _leftHandId : _rightHandId;
            leapHand.Confidence = 1.0f;
            leapHand.PalmWidth = handScale * DefaultHandScale;
            leapHand.IsLeft = isLeftHand;
            leapHand.TimeVisible = timeVisible;
            leapHand.PalmVelocity = _jointVelocities[(int)XrHandJointExt.Palm].LinearVelocity;
            leapHand.WristPosition = _jointLocations[(int)XrHandJointExt.Wrist].Pose.position;

            // Calculate adjusted palm position, rotation and direction.
            leapHand.Rotation = _jointLocations[(int)XrHandJointExt.Palm].Pose.rotation * PalmOffset[isLeftHand ? 0 : 1].rotation;
            leapHand.PalmPosition = _jointLocations[(int)XrHandJointExt.Palm].Pose.position + leapHand.Rotation * PalmOffset[isLeftHand ? 0 : 1].position;
            leapHand.StabilizedPalmPosition = leapHand.PalmPosition;
            leapHand.PalmNormal = leapHand.Rotation * Vector3.down;
            leapHand.Direction = leapHand.Rotation * Vector3.forward;

            // Calculate now we have the hand data available.
            // Requires `Hand.Rotation` and fingers to be set.
            leapHand.GrabStrength = Hands.CalculateGrabStrength(ref leapHand);
            leapHand.PinchStrength = Hands.CalculatePinchStrength(ref leapHand);
            leapHand.PinchDistance = Hands.CalculatePinchDistance(ref leapHand);

            // Other hand-properties are derived.
            leapHand.PalmNormal = leapHand.Rotation * Vector3.down;

            // Fill arm data.
            var palmPosition = _jointLocations[(int)XrHandJointExt.Palm].Pose.position;
            var wristPosition = _jointLocations[(int)XrHandJointExt.Wrist].Pose.position;
            var wristWidth = _jointLocations[(int)XrHandJointExt.Wrist].Radius * 2f;

            if (_handTracking.JointSet == XrHandJointSetExt.HandWithForearmUltraleap)
            {
                var elbowPosition = _jointLocations[(int)XrHandJointExt.Elbow].Pose.position;
                var elbowRotation = _jointLocations[(int)XrHandJointExt.Elbow].Pose.rotation;
                var elbowDirection = elbowRotation * Vector3.back;
                var elbowLength = (elbowPosition - wristPosition).magnitude;
                var centerPosition = (elbowPosition + wristPosition) / 2f;
                leapHand.Arm.Fill(
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
                var elbowRotation = _jointLocations[(int)XrHandJointExt.Palm].Pose.rotation;
                var elbowDirection = elbowRotation * Vector3.back;
                var elbowPosition = _jointLocations[(int)XrHandJointExt.Palm].Pose.position + (elbowDirection * elbowLength);
                var centerPosition = (elbowPosition + palmPosition) / 2f;
                leapHand.Arm.Fill(
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