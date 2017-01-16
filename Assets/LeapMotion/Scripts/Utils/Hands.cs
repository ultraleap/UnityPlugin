using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Leap.Unity {

  public static class Hands {

    private static LeapServiceProvider s_provider;
    private static GameObject s_leapRig;

    static Hands() {
      InitStatic();
      SceneManager.activeSceneChanged += InitStaticOnNewScene;
    }

    private static void InitStaticOnNewScene(Scene unused, Scene unused2) {
      InitStatic();
    }

    private static void InitStatic() {
      s_provider = GameObject.FindObjectOfType<LeapServiceProvider>();
      s_leapRig = s_provider.transform.parent.parent.parent.gameObject;
    }

    /// <summary>
    /// Static convenience accessor for the Leap Rig GameObject.
    /// </summary>
    public static GameObject Rig {
      get { return s_leapRig; }
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
    /// Returns the direction the Hand's palm is facing.
    /// For the other two axis directions, see ThumbAxis and FingerAxis.
    /// </summary>
    public static Vector3 PalmAxis(this Hand hand) {
      return -hand.Basis.yBasis.ToVector3();
    }

    /// <summary>
    /// Returns the general (NOT exact) direction of the thumb, always exactly 90 degrees from the direction of the palm.
    /// For the left hand, will return a rightward vector, or for the right hand, will return a leftward vector.
    /// </summary>
    public static Vector3 ThumbAxis(this Hand hand) {
      if (hand.IsRight) {
        return hand.Basis.xBasis.ToVector3();
      }
      else {
        return -hand.Basis.xBasis.ToVector3();
      }
    }

    /// <summary>
    /// Returns the general (NOT exact) direction of the fingers, always exactly 90 degrees from the direction of the palm.
    /// This and ThumbAxis can be compared to finger and thumb pointing directions respectively, to, e.g., measure fist-closing.
    /// </summary>
    public static Vector3 FingerAxis(this Hand hand) {
      return hand.Basis.zBasis.ToVector3();
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
    /// Returns whether this vector faces from a given world position towards another world position within a maximum angle of error.
    /// </summary>
    public static bool IsFacing(this Vector3 facingVector, Vector3 fromWorldPosition, Vector3 towardsWorldPosition, float maxOffsetAngleAllowed) {
      Vector3 actualVectorTowardsWorldPosition = (towardsWorldPosition - fromWorldPosition).normalized;
      return Vector3.Angle(facingVector, actualVectorTowardsWorldPosition) <= maxOffsetAngleAllowed;
    }

    /// <summary>
    /// Returns a confidence value from 0 to 1 indicating how strongly the Hand is making a fist.
    /// </summary>
    public static float FistStrength(this Hand hand) {
      return (Vector3.Dot(hand.Fingers[1].Direction.ToVector3(), -hand.FingerAxis() )
            + Vector3.Dot(hand.Fingers[2].Direction.ToVector3(), -hand.FingerAxis() )
            + Vector3.Dot(hand.Fingers[3].Direction.ToVector3(), -hand.FingerAxis() )
            + Vector3.Dot(hand.Fingers[4].Direction.ToVector3(), -hand.FingerAxis() )
            + Vector3.Dot(hand.Fingers[0].Direction.ToVector3(),  hand.ThumbAxis()  )
            ).Map(-5, 5, 0, 1);
    }

  }

}
