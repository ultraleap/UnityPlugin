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

    public static bool IsSinglePoint(this AttachmentPointFlags points) {
      // If the log-base-2 of singlePoint is not an integer, there's more than one bit set in the AttachmentPoints bit field.
      return points != AttachmentPointFlags.None && Mathf.Abs(Mathf.Log((int)points, 2) % 1F) < 0.00001F;
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