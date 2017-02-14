using Leap.Unity.Query;
using System.Collections.Generic;
using UnityEngine;
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
      Camera providerCamera = s_provider.GetComponentInParent<Camera>();
      if (providerCamera == null) return;
      if (providerCamera.transform.parent == null) return;
      s_leapRig = providerCamera.transform.parent.gameObject;
    }

    /// <summary>
    /// Static convenience accessor for the Leap camera rig. This is the parent
    /// of the Camera that contains a LeapServiceProvider in one of its children,
    /// or null if there is no such GameObject.
    /// </summary>
    public static GameObject Rig {
      get {
        if (s_leapRig == null) {
          InitStatic();
          if (s_leapRig == null) {
            Debug.LogWarning("Camera has no parent; Rig will return null.");
          }
        }
        return s_leapRig;
      }
    }

    /// <summary>
    /// Static convenience accessor for the LeapServiceProvider.
    /// </summary>
    public static LeapServiceProvider Provider {
      get {
        if (s_provider == null) {
          InitStatic();
          if (s_provider == null) {
            Debug.LogWarning("No LeapServiceProvider found in the scene.");
          }
        }
        return s_provider;
      }
    }

    public static Hand Get(Chirality chirality) {
      if (chirality == Chirality.Left) return Left;
      else return Right;
    }

    /// <summary> Returns the first left hand found by Leap in the current
    /// frame, otherwise returns null if no such hand is found. </summary>
    public static Hand Left {
      get {
        if (Provider == null) return null;
        return Provider.CurrentFrame.Hands.Query().FirstOrDefault(hand => hand.IsLeft);
      }
    }

    /// <summary> Returns the first right hand found by Leap in the current
    /// frame, otherwise returns null if no such hand is found. </summary>
    public static Hand Right {
      get {
        if (Provider == null) return null;
        else return Provider.CurrentFrame.Hands.Query().FirstOrDefault(hand => hand.IsRight);
      }
    }

    /// <summary> Returns the first left hand found by Leap in the current
    /// FIXED frame (as in FixedUpdate), otherwise returns null if no such hand is found. </summary>
    public static Hand FixedLeft {
      get {
        if (Provider == null) return null;
        return Provider.CurrentFixedFrame.Hands.Query().FirstOrDefault(hand => hand.IsLeft);
      }
    }

    /// <summary> Returns the first right hand found by Leap in the current
    /// FIXED frame (as in FixedUpdate), otherwise returns null if no such hand is found. </summary>
    public static Hand FixedRight {
      get {
        if (Provider == null) return null;
        return Provider.CurrentFixedFrame.Hands.Query().FirstOrDefault(hand => hand.IsRight);
      }
    }

    public static Chirality Handedness(this Hand hand) {
      return hand.IsLeft ? Chirality.Left : Chirality.Right;
    }

    public static void FromFrame(Frame frame, out Hand leftHand, out Hand rightHand) {
      bool assignedLeft = false, assignedRight = false;
      leftHand = rightHand = null;
      foreach (Hand hand in frame.Hands) {
        if (hand.IsLeft && !assignedLeft) {
          leftHand = hand;
          assignedLeft = true;
        }
        if (hand.IsRight && !assignedRight) {
          rightHand = hand;
          assignedRight = true;
        }
        if (assignedLeft && assignedRight) break;
      }
    }

    public static Finger Thumb(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_THUMB];
    }

    public static Finger Index(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_INDEX];
    }

    public static Finger Middle(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_MIDDLE];
    }

    public static Finger Ring(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_RING];
    }

    public static Finger Pinky(this Hand hand) {
      return hand.Fingers[(int)Leap.Finger.FingerType.TYPE_PINKY];
    }

    /// <summary> Returns the direction the Hand's palm is facing. For the 
    /// other two palm-basis directions, see RadialAxis and DistalAxis.
    /// The direction out of the back of the hand would be called the dorsal axis. </summary>
    public static Vector3 PalmarAxis(this Hand hand) {
      return -hand.Basis.yBasis.ToVector3();
    }

    /// <summary> Returns the the direction towards the thumb
    /// that is perpendicular to the palmar and distal axes.
    /// Left and right hands will return opposing directions.
    /// The direction away from the thumb would be called the ulnar axis. </summary>
    public static Vector3 RadialAxis(this Hand hand) {
      if (hand.IsRight) {
        return -hand.Basis.xBasis.ToVector3();
      }
      else {
        return hand.Basis.xBasis.ToVector3();
      }
    }

    /// <summary> Returns the direction towards the fingers that is
    /// perpendicular to the palmar and radial axes.
    /// The direction towards the wrist would be called the proximal axis. </summary>
    public static Vector3 DistalAxis (this Hand hand) {
      return hand.Basis.zBasis.ToVector3();
    }

    /// <summary>
    /// Returns whether the pinch strength for the hand is greater than 0.8.
    /// For more reliable pinch behavior, consider applying hysteresis to the Hand.PinchStrength property.
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
      return (Vector3.Dot(hand.Fingers[1].Direction.ToVector3(), -hand.DistalAxis() )
            + Vector3.Dot(hand.Fingers[2].Direction.ToVector3(), -hand.DistalAxis() )
            + Vector3.Dot(hand.Fingers[3].Direction.ToVector3(), -hand.DistalAxis() )
            + Vector3.Dot(hand.Fingers[4].Direction.ToVector3(), -hand.DistalAxis() )
            + Vector3.Dot(hand.Fingers[0].Direction.ToVector3(), -hand.RadialAxis() )
            ).Map(-5, 5, 0, 1);
    }

    /// <summary>
    /// Transforms a bone by a position and rotation.
    /// </summary>
    public static void Transform(this Bone bone, Vector3 position, Quaternion rotation) {
      bone.Transform(new LeapTransform(position.ToVector(), rotation.ToLeapQuaternion()));
    }

    /// <summary>
    /// Transforms a finger by a position and rotation.
    /// </summary>
    public static void Transform(this Finger finger, Vector3 position, Quaternion rotation) {
      finger.Transform(new LeapTransform(position.ToVector(), rotation.ToLeapQuaternion()));
    }

    /// <summary>
    /// Transforms a hand by a position and rotation.
    /// </summary>
    public static void Transform(this Hand hand, Vector3 position, Quaternion rotation) {
      hand.Transform(new LeapTransform(position.ToVector(), rotation.ToLeapQuaternion()));
    }

    /// <summary>
    /// Transforms a frame by a position and rotation.
    /// </summary>
    public static void Transform(this Frame frame, Vector3 position, Quaternion rotation) {
      frame.Transform(new LeapTransform(position.ToVector(), rotation.ToLeapQuaternion()));
    }

    /// <summary>
    /// Transforms a bone to a position and rotation.
    /// </summary>
    public static void SetTransform(this Bone bone, Vector3 position, Quaternion rotation) {
      bone.Transform(Vector3.zero, (rotation * Quaternion.Inverse(bone.Rotation.ToQuaternion())));
      bone.Transform(position - bone.PrevJoint.ToVector3(), Quaternion.identity);
    }

    /// <summary>
    /// Transforms a hand to a position and rotation.
    /// </summary>
    public static void SetTransform(this Hand hand, Vector3 position, Quaternion rotation) {
      hand.Transform(Vector3.zero, (rotation * Quaternion.Inverse(hand.Rotation.ToQuaternion())));
      hand.Transform(position - hand.PalmPosition.ToVector3(), Quaternion.identity);
    }

  }

}
