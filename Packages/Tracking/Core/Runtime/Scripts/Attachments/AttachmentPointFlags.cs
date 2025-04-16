/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2025.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Attachments
{

    /// <summary>
    /// Flags for attachment points on the hand.
    /// </summary>
    [System.Flags]
    public enum AttachmentPointFlags
    {
        None = 0,

        Wrist = 1 << 1,
        Palm = 1 << 2,

        ThumbBase = 1 << 3,
        ThumbProximalJoint = 1 << 4,
        ThumbDistalJoint = 1 << 5,
        ThumbTip = 1 << 6,

        IndexBase = 1 << 7,
        IndexKnuckle = 1 << 8,
        IndexMiddleJoint = 1 << 9,
        IndexDistalJoint = 1 << 10,
        IndexTip = 1 << 11,

        MiddleBase = 1 << 12,
        MiddleKnuckle = 1 << 13,
        MiddleMiddleJoint = 1 << 14,
        MiddleDistalJoint = 1 << 15,
        MiddleTip = 1 << 16,

        RingBase = 1 << 17,
        RingKnuckle = 1 << 18,
        RingMiddleJoint = 1 << 19,
        RingDistalJoint = 1 << 20,
        RingTip = 1 << 21,

        PinkyBase = 1 << 22,
        PinkyKnuckle = 1 << 23,
        PinkyMiddleJoint = 1 << 24,
        PinkyDistalJoint = 1 << 25,
        PinkyTip = 1 << 26,

        PinchPoint = 1 << 27,

        Elbow = 1 << 28
    }

    public static class AttachmentPointFlagsExtensions
    {

        // Takes advantage of two's complement representation for negative integers
        // to check whether the bit field has a single bit set.
        // https://en.wikipedia.org/wiki/Two%27s_complement
        public static bool IsSinglePoint(this AttachmentPointFlags points)
        {
            int mask = (int)points;
            bool isSingleBitSet = mask != 0 && mask == (mask & -mask);
            return isSingleBitSet;
        }

        /// <summary>
        /// Returns whether these AttachmentPointsFlags contain the single flag specified by singlePoint.
        /// Will raise a warning in the editor if the argument is not a single flag constant.
        /// </summary>
        public static bool ContainsPoint(this AttachmentPointFlags points, AttachmentPointFlags singlePoint)
        {
#if UNITY_EDITOR
            // Validation for ensuring singlePoint is really a single point.
            if (!singlePoint.IsSinglePoint())
            {
                Debug.LogWarning("'ContainsPoint' called with an argument that contains more than one attachment point flag set.");
            }
#endif
            return points.Contains(singlePoint);
        }

        /// <summary>
        /// Returns whether these AttachmentPointFlags contain the flags set in otherPoints.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="otherPoints"></param>
        /// <returns></returns>
        public static bool Contains(this AttachmentPointFlags points, AttachmentPointFlags otherPoints)
        {
            if (points == AttachmentPointFlags.None || otherPoints == AttachmentPointFlags.None) return false;
            return (points & otherPoints) == otherPoints;
        }

    }

}