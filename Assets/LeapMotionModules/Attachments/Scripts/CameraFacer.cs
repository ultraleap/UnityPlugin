using UnityEngine;
using System.Collections;

public class CameraFacer : MonoBehaviour {

  public Vector3 objectForward = Vector3.forward;
  public Vector3 MaxTilt = Vector3.one * 15;//degrees
  public Vector3 MinTilt = Vector3.one * 15;//degrees
  public bool FreezeX = false;
  public bool FreezeY = false;
  public bool FreezeZ = false;

  private Quaternion offset;
  private Quaternion startingLocalRotation;

  void Awake(){
    offset = Quaternion.Inverse(Quaternion.LookRotation(objectForward));
    startingLocalRotation = transform.localRotation;
  }

  void Update () {
    Vector3 cameraDirection = (Camera.main.transform.position - transform.position).normalized;
    Vector3 objectDirection = transform.TransformDirection(objectForward);
    Quaternion towardCamera = Quaternion.LookRotation(cameraDirection);
    towardCamera *= offset;
    transform.rotation =Quaternion.Slerp(towardCamera, towardCamera, Time.deltaTime);
    Vector3 startingEulers = startingLocalRotation.eulerAngles;
    Vector3 targetEulers = transform.localEulerAngles;
    float angleX, angleY, angleZ;
    if(FreezeX){
      angleX = startingEulers.x;
    } else {
      angleX = clampAngle(transform.localEulerAngles.x, startingEulers.x, startingEulers.x - MinTilt.x, startingEulers.x + MaxTilt.x);
      //angleX = transform.localEulerAngles.x;
    }
    if(FreezeY){
      angleY = startingEulers.y;
    } else {
      //angleY = transform.localEulerAngles.y;
      angleY = clampAngle(transform.localEulerAngles.y, startingEulers.y, startingEulers.y - MinTilt.y, startingEulers.y + MaxTilt.y);
    }
    if(FreezeZ){
      angleZ = startingEulers.z;
    } else {
      //angleZ = transform.localEulerAngles.z;
      angleZ = clampAngle(transform.localEulerAngles.z, startingEulers.z, startingEulers.z - MinTilt.z, startingEulers.z + MaxTilt.z);
    }
    transform.localEulerAngles = new Vector3(angleX, angleY, angleZ);
    Debug.Log("Eulers: " + FreezeX + " " + FreezeY + " " + FreezeZ + " " + startingEulers + " " + targetEulers + " " + transform.localEulerAngles);
  }

  float clampAngle (float angle, float targetAngle, float min, float max) {
    if (angle >= min) {
      angle = Mathf.Clamp(angle, min, max);
    }
    angle = angle % 360;
    if (angle < min) {
      angle = Mathf.Clamp(angle, 0, max % 360);
    }
    return angle;
  }

  //float normalizeAngle (float angle) {

  //}
}
