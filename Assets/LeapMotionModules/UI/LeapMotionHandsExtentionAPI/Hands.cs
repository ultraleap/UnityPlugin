using Leap;
using Leap.Unity;
using UnityEngine;
using System.Collections.Generic;

namespace Leap.Unity {

  public static class Hands {

    private static LeapServiceProvider s_provider;

    static Hands() {
      s_provider = GameObject.FindObjectOfType<LeapServiceProvider>();
    }

    public static Hand Get(Chirality chirality) {
      if (chirality == Chirality.Left) return Left;
      else return Right;
    }

    /// <summary>
    /// Returns the first left hand found by Leap in the current frame, otherwise returns null if no such hand is found.
    /// </summary>
    public static Hand Left {
      get {
        List<Hand> hands = s_provider.CurrentFrame.Hands;
        for (int i = 0; i < hands.Count; i++) {
          if (hands[i].IsLeft) {
            return hands[i];
          }
        }
        return null;
      }
    }

    /// <summary>
    /// Returns the first right hand found by Leap in the current frame, otherwise returns null if no such hand is found.
    /// </summary>
    public static Hand Right {
      get {
        List<Hand> hands = s_provider.CurrentFrame.Hands;
        for (int i = 0; i < hands.Count; i++) {
          if (hands[i].IsRight) {
            return hands[i];
          }
        }
        return null;
      }
    }

    /// <summary>
    /// Returns the thumb object for the given Hand.
    /// </summary>
    public static Finger Thumb(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_THUMB];
    }

    /// <summary>
    /// Returns the index Finger object for the given Hand.
    /// </summary>
    public static Finger Index(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_INDEX];
    }

    /// <summary>
    /// Returns the middle Finger object for the given Hand.
    /// </summary>
    public static Finger Middle(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_MIDDLE];
    }

    /// <summary>
    /// Returns the ring Finger object for the given Hand.
    /// </summary>
    public static Finger Ring(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_RING];
    }

    /// <summary>
    /// Returns the pinky Finger object for the given Hand.
    /// </summary>
    public static Finger Pinky(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_PINKY];
    }

    /// <summary>
    /// Returns whether the pinch strength for the hand is greater than 0.8. For more reliable pinch behavior,
    /// consider applying hysteresis to the Hand.PinchStrength property.
    /// </summary>
    public static bool IsPinching(this Hand hand) {
      return hand.PinchStrength > 0.8F;
    }

    /// <summary>
    /// Returns the average of the index finger and thumb tip positions.
    /// </summary>
    public static Vector3 GetPinchPosition(this Hand hand) {
      return (hand.Fingers[(int)Finger.FingerType.TYPE_INDEX].TipPosition + hand.Fingers[(int)Finger.FingerType.TYPE_THUMB].TipPosition).ToVector3() * 0.5F;
    }

    /// <summary>
    /// Returns the fingertip position of the argument finger type, e.g. Leap.FingerType.Index. See Leap.FingerType.
    /// </summary>
    public static Vector3 GetTipPosition(this Hand hand, int fingerType) {
      return hand.Fingers[fingerType].TipPosition.ToVector3();
    }

    public static bool IsFacing(this Vector3 facingVector, Vector3 fromWorldPosition, Vector3 towardsWorldPosition, float maxOffsetAngleAllowed) {
      Vector3 actualVectorTowardsWorldPosition = (towardsWorldPosition - fromWorldPosition).normalized;
      return Vector3.Angle(facingVector, actualVectorTowardsWorldPosition) <= maxOffsetAngleAllowed;
    }

  }



}