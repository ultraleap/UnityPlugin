using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity {

  public class LeapTestProvider : LeapProvider {

    public Frame frame;
    public override Frame CurrentFrame { get { return frame; } }
    public override Frame CurrentFixedFrame { get { return frame; } }

    [Header("Runtime Basis Transforms")]

    [Tooltip("At runtime, if this Transform is non-null, the LeapTestProvider will "
           + "create a test-pose left hand at this transform's position and rotation."
           + "Setting this binding to null at runtime will cause the hand to disappear "
           + "from Frame data, as if it stopped tracking.")]
    public Transform leftHandBasis;
    private Hand _leftHand = null;
    private Hand _cachedLeftHand = null;

    [Tooltip("At runtime, if this Transform is non-null, the LeapTestProvider will "
           + "create a test-pose right hand at this transform's position and rotation."
           + "Setting this binding to null at runtime will cause the hand to disappear "
           + "from Frame data, as if it stopped tracking.")]
    public Transform rightHandBasis;
    private Hand _rightHand = null;
    private Hand _cachedRightHand = null;

    void Awake() {
      _cachedLeftHand = TestHandFactory.MakeTestHand(isLeft: true);
      _cachedLeftHand.TransformToUnityUnits(); // Note: sucks that this has to be here
      _cachedRightHand = TestHandFactory.MakeTestHand(isLeft: false);
      _cachedRightHand.TransformToUnityUnits(); // Note: like really this is terrible
    }

    void Update() {
      if (_leftHand == null && leftHandBasis != null) {
        _leftHand = _cachedLeftHand;
        frame.Hands.Add(_leftHand);
      }
      if (_leftHand != null && leftHandBasis == null) {
        frame.Hands.Remove(_leftHand);
        _leftHand = null;
      }
      if (_leftHand != null) {
        _leftHand.SetTransform(leftHandBasis.position, leftHandBasis.rotation);
      }

      if (_rightHand == null && rightHandBasis != null) {
        _rightHand = _cachedRightHand;
        frame.Hands.Add(_rightHand);
      }
      if (_rightHand != null && rightHandBasis == null) {
        frame.Hands.Remove(_rightHand);
        _rightHand = null;
      }
      if (_rightHand != null) {
        _rightHand.SetTransform(rightHandBasis.position, rightHandBasis.rotation);
      }

      DispatchUpdateFrameEvent(frame);
    }

    void FixedUpdate() {
      DispatchFixedFrameEvent(frame);
    }

  }

  // Note: The fact that this class needs to exist is ridiculous
  public static class LeapTestProviderExtensions {

    public static readonly float MM_TO_M = 1e-3f;

    public static LeapTransform GetLeapTransform(Vector3 position, Quaternion rotation) {
      Vector scale = new Vector(MM_TO_M, MM_TO_M, MM_TO_M); // Leap units -> Unity units.
      LeapTransform transform = new LeapTransform(position.ToVector(), rotation.ToLeapQuaternion(), scale);
      transform.MirrorZ(); // Unity is left handed.
      return transform;
    }

    public static void TransformToUnityUnits(this Hand hand) {
      hand.Transform(GetLeapTransform(Vector3.zero, Quaternion.identity));
    }

  }

}
