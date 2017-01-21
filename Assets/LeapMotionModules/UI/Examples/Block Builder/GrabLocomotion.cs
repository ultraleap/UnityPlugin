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
  private Vector3 _grabAnchorLookVector;

  private Vector3 _oldGrabPositionFromRig;
  private Vector3 _curGrabPositionFromRig;

  void Update() {
    _lHand = Hands.Left;
    _rHand = Hands.Right;

    bool grabStateChanged = false;
    UpdateGrabState(out grabStateChanged);
    if (grabStateChanged) {
      _grabAnchorPosition = GetGrabAnchorPosition();
      _grabAnchorLookVector = GetGrabAnchorLookVector();
    }

    //Vector3 translationThisFrame = _grabAnchorPosition - GetGrabAnchorPosition();
    //Quaternion rotationThisFrame = Quaternion.FromToRotation(GetGrabAnchorLookVector(), _grabAnchorLookVector);

    //Hands.Rig.transform.position += translationThisFrame;
    //Hands.Rig.transform.rotation = rotationThisFrame * Hands.Rig.transform.rotation;

    Transform rig = Hands.Rig.transform;

    if (_rHand != null) {
      _curGrabPositionFromRig = _rHand.PalmPosition.ToVector3() - rig.position;
    }

    //if (_rGrab) {
    //  Quaternion.
    //  rig.RotateAround(grabPosition, )
    //}

    _oldGrabPositionFromRig = _curGrabPositionFromRig;
  }

  // Returns true if the grab state is different than the previous known grab state, false otherwise.
  private void UpdateGrabState(out bool didChange) {
    didChange = false;

    if (_lHand != null && _lHand.FistStrength() > 0.75F) {
      if (!_lGrab) didChange = true;
      _lGrab = true;
    }
    else {
      if (_lGrab) didChange = true;
      _lGrab = false;
    }

    if (_rHand != null && _rHand.FistStrength() > 0.75F) {
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

  private Vector3 GetGrabAnchorLookVector() {

    if (_rGrab) {
      return _rHand.PalmPosition.ToVector3() - (_rHand.PalmPosition.ToVector3() + _rHand.DistalAxis());
      //return _rHand.PalmPosition.ToVector3() - _rHand.Arm.ElbowPosition.ToVector3();
    }
    else if (_lGrab) {
      return _lHand.PalmPosition.ToVector3() - (_lHand.PalmPosition.ToVector3() + _lHand.DistalAxis());
      return _lHand.PalmPosition.ToVector3() - _lHand.Arm.ElbowPosition.ToVector3();
    }
    return Vector3.up;

    if (_lGrab && _rGrab) {
      //return _lHand.PalmPosition.ToVector3() - _rHand.PalmPosition.ToVector3();
    }
    //else if (_lGrab) {
    //  return _lHand.DistalAxis();
    //}
    //else if (_rGrab) {
    //  return _rHand.DistalAxis();
    //}
    else {
      return Vector3.up;
    }
  }

}
