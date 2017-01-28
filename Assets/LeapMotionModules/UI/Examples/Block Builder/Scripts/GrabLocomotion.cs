using UnityEngine;
using System.Collections;
using Leap.Unity;
using Leap;

public class GrabLocomotion : MonoBehaviour {

  private Hand _lHand;
  private Hand _rHand;
  private bool _lGrab;
  private bool _rGrab;

  private Vector3 _grabAnchorPosition;
  private Quaternion _grabAnchorRotation;

  private DeltaBuffer _rigPosBuffer = new DeltaBuffer(5);

  void Update() {
    _lHand = Hands.Left;
    _rHand = Hands.Right;

    bool grabStateChanged = false;
    UpdateGrabState(out grabStateChanged);
    if (grabStateChanged) {
      _grabAnchorPosition = GetGrabAnchorPosition();
      _grabAnchorRotation = GetGrabAnchorRotation();
    }

    Vector3 grabPositionDelta = _grabAnchorPosition - GetGrabAnchorPosition();
    Hands.Rig.transform.position += grabPositionDelta;


    float rigMovementSpeed = 0F;

    _rigPosBuffer.Add(Hands.Rig.transform.position, Time.time);
    if (_rigPosBuffer.IsFull) {
      Vector3 rigVelocity = _rigPosBuffer.Delta();
      rigMovementSpeed = rigVelocity.magnitude;
    }

    float rotationSlerpCoeff = 10F * Time.deltaTime * rigMovementSpeed.Map(0F, 2F, 1F, 0F);
    Quaternion grabRotationDelta = _grabAnchorRotation * Quaternion.Inverse(GetGrabAnchorRotation());
    Hands.Rig.transform.rotation = Quaternion.Slerp(Hands.Rig.transform.rotation, grabRotationDelta * Hands.Rig.transform.rotation, rotationSlerpCoeff);

    Hands.Provider.ReTransformFrames();
  }

  // Returns true if the grab state is different than the previous known grab state, false otherwise.
  private void UpdateGrabState(out bool didChange) {
    didChange = false;

    if (_lHand != null && _lHand.PinchStrength > 0.7F) {
      if (!_lGrab) didChange = true;
      _lGrab = true;
    }
    else {
      if (_lGrab) didChange = true;
      _lGrab = false;
    }

    if (_rHand != null && _rHand.PinchStrength > 0.7F) {
      if (!_rGrab) didChange = true;
      _rGrab = true;
    }
    else {
      if (_rGrab) didChange = true;
      _rGrab = false;
    }
  }

  private Vector3 GetGrabAnchorPosition() {
    Vector3 positionSum = Vector3.zero;
    int grabCount = 0;
    if (_lGrab) {
      positionSum += _lHand.PalmPosition.ToVector3();
      grabCount += 1;
    }
    if (_rGrab) {
      positionSum += _rHand.PalmPosition.ToVector3();
      grabCount += 1;
    }
    return (grabCount > 0) ? (positionSum / grabCount) : positionSum;
  }

  private Quaternion GetGrabAnchorRotation() {
    if (_rGrab) {
      return _rHand.Rotation.ToQuaternion();
    }
    else if (_lGrab) {
      return _lHand.Rotation.ToQuaternion();
    }
    else if (_lGrab && _rGrab) {
      return Quaternion.Slerp(_rHand.Rotation.ToQuaternion(), _lHand.Rotation.ToQuaternion(), 0.5F);
    }
    else {
      return Quaternion.identity;
    }
  }

}
