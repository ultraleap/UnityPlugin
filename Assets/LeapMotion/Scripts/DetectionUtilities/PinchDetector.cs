using UnityEngine;
using Leap.Unity.Attributes;
using UnityEngine.Serialization;

namespace Leap.Unity {

  /// <summary>
  /// A basic utility class to aid in creating pinch based actions.  Once linked with an IHandModel, it can
  /// be used to detect pinch gestures that the hand makes.
  /// </summary>
  public class PinchDetector : AbstractHoldDetector {
    protected const float MM_TO_M = 0.001f;

    [Tooltip("The distance at which to enter the pinching state.")]
    [Header("Distance Settings")]
    [MinValue(0)]
    [Units("meters")]
    [FormerlySerializedAs("_activatePinchDist")]
    public float ActivateDistance = .03f; //meters
    [Tooltip("The distance at which to leave the pinching state.")]
    [MinValue(0)]
    [Units("meters")]
    [FormerlySerializedAs("_deactivatePinchDist")]
    public float DeactivateDistance = .04f; //meters

    public bool IsPinching { get { return this.IsHolding; } }
    public bool DidStartPinch { get { return this.DidStartHold; } }
    public bool DidEndPinch { get { return this.DidRelease; } }

    protected bool _isPinching = false;

    protected float _lastPinchTime = 0.0f;
    protected float _lastUnpinchTime = 0.0f;

    protected Vector3 _pinchPos;
    protected Quaternion _pinchRotation;

    protected virtual void OnValidate() {
      ActivateDistance = Mathf.Max(0, ActivateDistance);
      DeactivateDistance = Mathf.Max(0, DeactivateDistance);

      //Activate value cannot be less than deactivate value
      if (DeactivateDistance < ActivateDistance) {
        DeactivateDistance = ActivateDistance;
      }
    }

    protected override void ensureUpToDate() {
      if (Time.frameCount == _lastUpdateFrame) {
        return;
      }
      _lastUpdateFrame = Time.frameCount;

      _didChange = false;

      Hand hand = _handModel.GetLeapHand();

      if (hand == null || !_handModel.IsTracked) {
        changeState(false);
        return;
      }

      _distance = hand.PinchDistance * MM_TO_M;
      _rotation = hand.Basis.CalculateRotation();
      _position = ((hand.Fingers[0].TipPosition + hand.Fingers[1].TipPosition) * .5f).ToVector3();

      if (IsActive) {
        if (_distance > DeactivateDistance) {
          changeState(false);
          //return;
        }
      } else {
        if (_distance < ActivateDistance) {
          changeState(true);
        }
      }

      if (IsActive) {
        _lastPosition = _position;
        _lastRotation = _rotation;
        _lastDistance = _distance;
        _lastDirection = _direction;
        _lastNormal = _normal;
      }
      if (ControlsTransform) {
        transform.position = _position;
        transform.rotation = _rotation;
      }
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos () {
      if (ShowGizmos && _handModel != null && _handModel.IsTracked) {
        Color centerColor = Color.clear;
        Vector3 centerPosition = Vector3.zero;
        Quaternion circleRotation = Quaternion.identity;
        if (IsHolding) {
          centerColor = Color.green;
          centerPosition = Position;
          circleRotation = Rotation;
        } else {
          Hand hand = _handModel.GetLeapHand();
          if (hand != null) {
            Finger thumb = hand.Fingers[0];
            Finger index = hand.Fingers[1];
            centerColor = Color.red;
            centerPosition = ((thumb.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint + index.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint) / 2).ToVector3();
            circleRotation = hand.Basis.CalculateRotation();
          }
        }
        Vector3 axis;
        float angle;
        circleRotation.ToAngleAxis(out angle, out axis);
        Utils.DrawCircle(centerPosition, axis, ActivateDistance / 2, centerColor);
        Utils.DrawCircle(centerPosition, axis, DeactivateDistance / 2, Color.blue);
      }
    }
    #endif
  }
}
