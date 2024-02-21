using JetBrains.Annotations;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace Ultraleap.Tracking.OpenXR
{
    /// <summary>
    /// Tracker for a single user's hand, allow tracking of individual joints.
    /// </summary>
    [PublicAPI]
    public class HandTracker
    {
        private readonly bool _enabled;
        private readonly Handedness _handedness;
        private readonly HandTrackingFeature _handTracking;

        private HandTracker(Handedness handedness)
        {
            _handedness = handedness;
            _handTracking = OpenXRSettings.Instance.GetFeature<HandTrackingFeature>();
            _enabled = _handTracking != null && _handTracking.SupportsHandTracking;
        }

        /// <summary>
        /// The current <see cref="HandJointSet"/> that this tracker is tracking.
        /// </summary>
        [PublicAPI]
        public HandJointSet JointSet => _handTracking.JointSet;

        /// <summary>
        /// The number of joints in the <see cref="JointSet"/> that this tracker is tracking.
        /// </summary>
        [PublicAPI]
        public int JointCount
        {
            get
            {
                switch (JointSet)
                {
                    default:
                    case HandJointSet.Default: return 26;
                    case HandJointSet.HandWithForearm: return 27;
                }
            }
        }

        /// <summary>
        /// Tracker for the user's left hand.
        /// </summary>
        [PublicAPI]
        public static HandTracker Left { get; } = new HandTracker(Handedness.Left);

        /// <summary>
        /// Tracker for the user's right hand.
        /// </summary>
        [PublicAPI]
        public static HandTracker Right { get; } = new HandTracker(Handedness.Right);

        /// <summary>
        /// Attempts to locate the user's hand joints, and populates the supplied array.
        /// </summary>
        /// <param name="handJointLocations">An array of <see cref="HandJointLocation"/>s which is index by the <see cref="HandJoint"/> enumberation.</param>
        /// <returns>True if the returned joint poses are valid</returns>
        [PublicAPI]
        public bool TryLocateHandJoints(HandJointLocation[] handJointLocations)
        {
            if (!_enabled)
            {
                return false;
            }

            if (handJointLocations.Length != JointCount)
            {
                Debug.LogError($"TryLocateHandJoints must be passed an array of size of JointCount ({JointCount})");
                return false;
            }

            return _handTracking.LocateHandJoints(_handedness, handJointLocations);
        }
    }

    /// <summary>
    /// Which of the user's two hands this hand tracker tracks.
    /// </summary>
    [PublicAPI]
    public enum Handedness
    {
        /// <summary>
        /// The user's left hand
        /// </summary>
        [PublicAPI] Left = 1,

        /// <summary>
        /// The user's right hand
        /// </summary>
        [PublicAPI] Right = 2
    }

    /// <summary>
    /// The hand joints according to the <see href="https://www.khronos.org/registry/OpenXR/specs/1.0/html/xrspec.html#XrHandJointEXT">OpenXR Specification</see>.
    /// </summary>
    [PublicAPI]
    public enum HandJoint
    {
        [PublicAPI] Palm = 0,
        [PublicAPI] Wrist,
        [PublicAPI] ThumbMetacarpal,
        [PublicAPI] ThumbProximal,
        [PublicAPI] ThumbDistal,
        [PublicAPI] ThumbTip,
        [PublicAPI] IndexMetacarpal,
        [PublicAPI] IndexProximal,
        [PublicAPI] IndexIntermediate,
        [PublicAPI] IndexDistal,
        [PublicAPI] IndexTip,
        [PublicAPI] MiddleMetacarpal,
        [PublicAPI] MiddleProximal,
        [PublicAPI] MiddleIntermediate,
        [PublicAPI] MiddleDistal,
        [PublicAPI] MiddleTip,
        [PublicAPI] RingMetacarpal,
        [PublicAPI] RingProximal,
        [PublicAPI] RingIntermediate,
        [PublicAPI] RingDistal,
        [PublicAPI] RingTip,
        [PublicAPI] LittleMetacarpal,
        [PublicAPI] LittleProximal,
        [PublicAPI] LittleIntermediate,
        [PublicAPI] LittleDistal,
        [PublicAPI] LittleTip,

        // Ultraleap Elbow extension
        Elbow
    }

    [PublicAPI]
    public enum HandJointSet
    {
        /// <summary>
        /// The Default OpenXR hand set of 26 joints per-hand, including the palm and wrist.
        /// </summary>
        [PublicAPI] Default = 0,

        /// <summary>
        /// Default hand-set with the addition of the elbow joint on the forearm.
        /// <remarks>This requires the Ultraleap extension <code>XR_ULTRALEAP_hand_tracking_forearm</code></remarks>
        /// </summary>
        [PublicAPI] HandWithForearm = 1000149000
    }

    /// <summary>
    /// Represents a user's hand joint with positional and optional velocity information.
    /// </summary>
    [PublicAPI, StructLayout(LayoutKind.Sequential, Pack = 8)]
    public readonly struct HandJointLocation
    {
        [Flags]
        private enum LocationFlags : ulong
        {
            OrientationValid = 0x1,
            PositionValid = 0x2,
            OrientationTracked = 0x4,
            PositionTracked = 0x8,
        }

        [Flags]
        private enum VelocityFlags : ulong
        {
            LinearVelocityValid = 0x1,
            AngularVelocityValid = 0x2,
        }

        /// <summary>
        /// Indicates if both the <see cref="Pose"/> of this hand joint is valid.
        /// </summary>
        [PublicAPI]
        public bool IsValid => PositionValid && OrientationValid;

        private bool PositionValid => _locationFlags.HasFlag(LocationFlags.PositionValid);
        private bool OrientationValid => _locationFlags.HasFlag(LocationFlags.OrientationValid);

        /// <summary>
        /// Indicates if the <see cref="Pose"/> hand joint is actively tracked or inferred.
        /// </summary>
        [PublicAPI]
        public bool IsTracked => OrientationTracked && PositionTracked;

        private bool PositionTracked => _locationFlags.HasFlag(LocationFlags.PositionTracked);
        private bool OrientationTracked => _locationFlags.HasFlag(LocationFlags.OrientationTracked);

        /// <summary>
        /// Indicates if this hand joint has valid <see cref="LinearVelocity"/>.
        /// </summary>
        [PublicAPI]
        public bool LinearVelocityValid => _velocityFlags.HasFlag(VelocityFlags.LinearVelocityValid);

        /// <summary>
        /// Indicates if this hand joint has valid <see cref="AngularVelocity"/>.
        /// </summary>
        [PublicAPI]
        public bool AngularVelocityValid => _velocityFlags.HasFlag(VelocityFlags.AngularVelocityValid);

        /// <summary>
        /// The <see cref="HandJoint"/> that this hand joint location represents.
        /// </summary>
        [PublicAPI]
        public HandJoint JointId { get; }

        /// <summary>
        /// The pose of this hand joint in world space.
        /// </summary>
        [PublicAPI]
        public Pose Pose { get; }

        /// <summary>
        /// The radius in meters of this hand joint.
        /// </summary>
        [PublicAPI]
        public float Radius { get; }

        /// <summary>
        /// The linear velocity in meters per second of this hand joint.
        /// </summary>
        [PublicAPI]
        public Vector3 LinearVelocity { get; }

        /// <summary>
        /// The angular velocity in radians per second of this hand joint.
        /// </summary>
        [PublicAPI]
        public Vector3 AngularVelocity { get; }

        private readonly LocationFlags _locationFlags;
        private readonly VelocityFlags _velocityFlags;
    }
}