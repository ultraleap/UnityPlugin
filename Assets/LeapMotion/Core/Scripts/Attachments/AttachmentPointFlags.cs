/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Attachments {
  
  /// <summary>
  /// Flags for attachment points on the hand.
  /// </summary>
  [System.Flags]
  public enum AttachmentPointFlags {
    None                    = 0,

    Wrist                   = 1 << 1,
    Palm                    = 1 << 2,

    ThumbProximalJoint      = 1 << 3,
    ThumbDistalJoint        = 1 << 4,
    ThumbTip                = 1 << 5,

    IndexKnuckle            = 1 << 6,
    IndexMiddleJoint        = 1 << 7,
    IndexDistalJoint        = 1 << 8,
    IndexTip                = 1 << 9,

    MiddleKnuckle           = 1 << 10,
    MiddleMiddleJoint       = 1 << 11,
    MiddleDistalJoint       = 1 << 12,
    MiddleTip               = 1 << 13,

    RingKnuckle             = 1 << 14,
    RingMiddleJoint         = 1 << 15,
    RingDistalJoint         = 1 << 16,
    RingTip                 = 1 << 17,

    PinkyKnuckle            = 1 << 18,
    PinkyMiddleJoint        = 1 << 19,
    PinkyDistalJoint        = 1 << 20,
    PinkyTip                = 1 << 21
  }

  public static class AttachmentPointFlagsExtensions {

    // Takes advantage of two's complement representation for negative integers
    // to check whether the bit field has a single bit set.
    // https://en.wikipedia.org/wiki/Two%27s_complement
    public static bool IsSinglePoint(this AttachmentPointFlags points) {
      int mask = (int)points;
      bool isSingleBitSet = mask != 0 && mask == (mask & -mask); 
      return isSingleBitSet;
    }

    /// <summary>
    /// Returns whether these AttachmentPointsFlags contain the single flag specified by singlePoint.
    /// Will raise a warning in the editor if the argument is not a single flag constant.
    /// </summary>
    public static bool ContainsPoint(this AttachmentPointFlags points, AttachmentPointFlags singlePoint) {
#if UNITY_EDITOR
      // Validation for ensuring singlePoint is really a single point.
      if (!singlePoint.IsSinglePoint()) {
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
    public static bool Contains(this AttachmentPointFlags points, AttachmentPointFlags otherPoints) {
      if (points == AttachmentPointFlags.None || otherPoints == AttachmentPointFlags.None) return false;
      return (points & otherPoints) == otherPoints;
    }

  }

}
