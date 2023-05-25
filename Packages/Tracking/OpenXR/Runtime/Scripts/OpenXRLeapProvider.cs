using Leap;
using Leap.Unity;
using Leap.Unity.Encoding;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private LeapTransform trackerTransform = new LeapTransform(Vector3.zero, Quaternion.identity);

        private Frame _currentFrame = new Frame();

        private Hand _leftHand = new Hand();
        private Hand _rightHand = new Hand();

        private int _handId = 0;
        private int _leftHandId = 0;
        private int _rightHandId = 0;
        private long _leftHandFirstSeen_ticks;
        private long _rightHandFirstSeen_ticks;

        // Magic 0th thumb bone rotation offsets from LeapC
        public const float HAND_ROTATION_OFFSET_Y = 25.9f, HAND_ROTATION_OFFSET_Z = -63.45f;

        // Magic numbers for palm width and PinchStrength calculation
        private static readonly float[] DefaultMetacarpalLengths = { 0, 0.06812f, 0.06460f, 0.05800f, 0.05369f };
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

        private TrackedPoseDriver _trackedPoseDriver;
        private HandJointLocation[] _joints;

        public override TrackingSource TrackingDataSource { get { return CheckOpenXRAvailable(); } }

        private TrackingSource CheckOpenXRAvailable()
        {
            if (_trackingSource != TrackingSource.NONE)
            {
                return _trackingSource;
            }

            if (XRGeneralSettings.Instance != null &&
                XRGeneralSettings.Instance.Manager != null &&
                XRGeneralSettings.Instance.Manager.activeLoader != null &&
                XRGeneralSettings.Instance.Manager.activeLoader.name == "Open XR Loader" &&
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

            trackerTransform.translation = Vector3.zero;
            trackerTransform.rotation = Quaternion.identity;
            trackerTransform.scale = mainCamera.transform.lossyScale;

            // Adjust for relative transform if it's in use.
            if (_trackedPoseDriver != null && _trackedPoseDriver.UseRelativeTransform)
            {
                trackerTransform.translation += _trackedPoseDriver.originPose.position;
                trackerTransform.rotation *= _trackedPoseDriver.originPose.rotation;
            }

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
                            _joints[(int)HandJoint.ThumbMetacarpal].Pose.rotation * Vector3.forward,
                            0f,
                            _joints[(int)HandJoint.ThumbMetacarpal].Radius * 2f,
                            (Bone.BoneType)boneIndex,
                            _joints[(int)HandJoint.Palm].Pose.rotation * Quaternion.Euler(0, hand.IsLeft ? HAND_ROTATION_OFFSET_Y : -HAND_ROTATION_OFFSET_Y, hand.IsLeft ? HAND_ROTATION_OFFSET_Z : -HAND_ROTATION_OFFSET_Z));
                        continue;
                    }

                    // Populate the finger bone information
                    var bone = hand.Fingers[fingerIndex].bones[boneIndex];
                    bone.Fill(
                        prevJoint.Pose.position,
                        nextJoint.Pose.position,
                        ((prevJoint.Pose.position + nextJoint.Pose.position) / 2f),
                        (prevJoint.Pose.rotation * Vector3.forward),
                        (prevJoint.Pose.position - nextJoint.Pose.position).magnitude,
                        prevJoint.Radius * 2f,
                        (Bone.BoneType)boneIndex,
                        prevJoint.Pose.rotation);
                    fingerWidth = Mathf.Max(fingerWidth, bone.Width);
                    xrTipIndex = xrNextIndex;

                    // Ignore metacarpals when calculating finger lengths
                    if (boneIndex != 0)
                    {
                        fingerLength += bone.Length;
                    }
                }

                // Populate the higher - level finger data.
                hand.Fingers[fingerIndex].Fill(
                    _frameId,
                    (handTracker == HandTracker.Left ? 0 : 1),
                    fingerIndex,
                    timeVisible,
                    _joints[xrTipIndex].Pose.position,
                    (_joints[xrTipIndex].Pose.rotation * Vector3.forward),
                    fingerWidth,
                    fingerLength,
                    hand.GetFingerStrength(fingerIndex) < 0.4, // Fixed for now
                    (Finger.FingerType)fingerIndex);
            }

            float handScale = CalculateHandScale(ref hand);
            float palmWidth = handScale * DEFAULT_HAND_SCALE;

            // Populate the whole hand information.
            hand.Fill(
                _frameId,
                handTracker == HandTracker.Left ? _leftHandId : _rightHandId,
                1f,
                CalculateGrabStrength(hand),
                CalculatePinchStrength(ref hand, handScale),
                CalculatePinchDistance(ref hand),
                palmWidth,
                handTracker == HandTracker.Left,
                timeVisible,
                null, // Already Populated
                _joints[(int)HandJoint.Palm].Pose.position,
                _joints[(int)HandJoint.Palm].Pose.position,
                _joints[(int)HandJoint.Palm].LinearVelocity,
                _joints[(int)HandJoint.Palm].Pose.rotation * Vector3.down,
                _joints[(int)HandJoint.Palm].Pose.rotation,
                _joints[(int)HandJoint.Palm].Pose.rotation * Vector3.forward,
                _joints[(int)HandJoint.Wrist].Pose.position
            );

            // Fill arm data.
            var palmPosition = _joints[(int)HandJoint.Palm].Pose.position;
            var wristPosition = _joints[(int)HandJoint.Wrist].Pose.position;
            var wristWidth = _joints[(int)HandJoint.Wrist].Radius * 2f;

            if (handTracker.JointSet == HandJointSet.HandWithForearm)
            {
                var elbowPosition = _joints[(int)HandJoint.Elbow].Pose.position;
                var elbowRotation = _joints[(int)HandJoint.Elbow].Pose.rotation;
                var elbowDirection = elbowRotation * Vector3.back;
                var elbowLength = (elbowPosition - palmPosition).magnitude;
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
            else
            {
                var elbowRotation = _joints[(int)HandJoint.Palm].Pose.rotation;
                var elbowDirection = elbowRotation * Vector3.back;
                var elbowPosition = _joints[(int)HandJoint.Palm].Pose.position + (elbowDirection * 0.3f);
                var elbowLength = 0.3f;
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

        private float CalculateHandScale(ref Hand hand)
        {
            // Iterate through the fingers, skipping the thumb and accumulate the scale.
            float scale = 0.0f;
            for (var i = 1; i < hand.Fingers.Count; ++i)
            {
                scale += (hand.Fingers[i].Bone(Bone.BoneType.TYPE_METACARPAL).Length / DefaultMetacarpalLengths[i]) / 4.0f;
            }

            return scale;
        }

        private float CalculatePinchStrength(ref Hand hand, float handScale)
        {
            // Get the thumb position.
            var thumbTipPosition = hand.GetThumb().TipPosition;

            // Compute the distance midpoints between the thumb and the each finger and find the smallest.
            var minDistanceSquared = float.MaxValue;

            // Iterate through the fingers, skipping the thumb.
            for (var i = 1; i < hand.Fingers.Count; ++i)
            {
                var distanceSquared = (hand.Fingers[i].TipPosition - thumbTipPosition).sqrMagnitude;
                minDistanceSquared = Mathf.Min(distanceSquared, minDistanceSquared);
            }

            // Compute the pinch strength. Magic values taken from existing LeapC implementation (scaled to metres)
            float distanceZero = 0.0600f * handScale;
            float distanceOne = 0.0220f * handScale;
            return Mathf.Clamp01((Mathf.Sqrt(minDistanceSquared) - distanceZero) / (distanceOne - distanceZero));
        }

        private float CalculateBoneDistanceSquared(Bone boneA, Bone boneB)
        {
            // Denormalize directions to bone length.
            var boneAJoint = boneA.PrevJoint;
            var boneBJoint = boneB.PrevJoint;
            var boneADirection = boneA.Direction * boneA.Length;
            var boneBDirection = boneB.Direction * boneB.Length;

            // Compute the minimum (squared) distance between two bones.
            var diff = boneBJoint - boneAJoint;
            var d1 = Vector3.Dot(boneADirection, diff);
            var d2 = Vector3.Dot(boneBDirection, diff);
            var a = boneADirection.sqrMagnitude;
            var b = Vector3.Dot(boneADirection, boneBDirection);
            var c = boneBDirection.sqrMagnitude;
            var det = b * b - a * c;
            var t1 = Mathf.Clamp01((b * d2 - c * d1) / det);
            var t2 = Mathf.Clamp01((a * d2 - b * d1) / det);
            var pa = boneAJoint + t1 * boneADirection;
            var pb = boneBJoint + t2 * boneBDirection;
            return (pa - pb).sqrMagnitude;
        }

        private float CalculatePinchDistance(ref Hand hand)
        {
            // Get the farthest 2 segments of thumb and index finger, respectively, and compute distances.
            var minDistanceSquared = float.MaxValue;
            for (var thumbBoneIndex = 2; thumbBoneIndex < hand.GetThumb().bones.Length; ++thumbBoneIndex)
            {
                for (var indexBoneIndex = 2; indexBoneIndex < hand.GetIndex().bones.Length; ++indexBoneIndex)
                {
                    var distanceSquared = CalculateBoneDistanceSquared(
                        hand.GetThumb().bones[thumbBoneIndex],
                        hand.GetIndex().bones[indexBoneIndex]);
                    minDistanceSquared = Mathf.Min(distanceSquared, minDistanceSquared);
                }
            }

            // Return the pinch distance, converted to millimeters to match other providers.
            return Mathf.Sqrt(minDistanceSquared) * 1000.0f;
        }

        float CalculateGrabStrength(Hand hand)
        {
            // magic numbers so it approximately lines up with the leap results
            const float bendZero = 0.25f;
            const float bendOne = 0.85f;

            // Find the minimum bend angle for the non-thumb fingers.
            float minBend = float.MaxValue;
            for (int finger_idx = 1; finger_idx < 5; finger_idx++)
            {
                minBend = Mathf.Min(hand.GetFingerStrength(finger_idx), minBend);
            }

            // Return the grab strength.
            return Mathf.Clamp01((minBend - bendZero) / (bendOne - bendZero));
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