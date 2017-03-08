using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayerBehaviour : MonoBehaviour {

  public float posLerpCoeffPerSec = 20F;
  public float rotLerpCoeffPerSec = 20F;

  private Vector3 _initCameraGroundVector;

  void Awake() {
    _initCameraGroundVector = GetCameraGroundForwardVector();
  }

  void Update() {
    Vector3 targetPosition = CalculateTargetPosition();
    Quaternion targetRotation = CalculateTargetRotation();

    this.transform.position = Vector3.Lerp(this.transform.position, targetPosition,  posLerpCoeffPerSec * Time.deltaTime);
    this.transform.rotation = Quaternion.Slerp(this.transform.rotation, targetRotation, rotLerpCoeffPerSec * Time.deltaTime);
    // scale NYI.
	}

  private Vector3 CalculateTargetPosition() {
    return Camera.main.transform.position;
  }

  private Quaternion CalculateTargetRotation() {
    Vector3 cameraGroundForwardVector = GetCameraGroundForwardVector();
    return Quaternion.FromToRotation(_initCameraGroundVector, cameraGroundForwardVector);
  }

  private Vector3 GetCameraGroundForwardVector() {
    return Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
  }

}
