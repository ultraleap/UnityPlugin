using UnityEngine;
using System.Collections;

public class CameraFacer : MonoBehaviour {

  [Range(1,20)]
  public float Speed = 10;
  public Vector3 objectForward = Vector3.forward;
  public Vector3 MinTilt = Vector3.one * 15;//degrees
  public Vector3 MaxTilt = Vector3.one * 15;//degrees
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
    Debug.DrawRay(transform.position, cameraDirection, Color.blue);
    Vector3 objectFacing = transform.TransformDirection(objectForward);
    Quaternion fromObject = Quaternion.Inverse(Quaternion.LookRotation(objectFacing));
    Debug.DrawRay(transform.position, objectFacing, Color.red);
    Quaternion towardCamera = Quaternion.LookRotation(cameraDirection);
    towardCamera *= offset;
    transform.rotation = Quaternion.Slerp(transform.rotation, towardCamera, Time.deltaTime * Speed);

    //transform.rotation = Quaternion.FromToRotation(objectFacing, cameraDirection);
    //transform.rotation = Quaternion.Slerp(towardCamera, Quaternion.LookRotation(objectDirection), Time.deltaTime * Speed);
    Vector3 startingEulers = startingLocalRotation.eulerAngles;
    Vector3 targetEulers = transform.localEulerAngles;
    float angleX, angleY, angleZ;
    if(FreezeX){
      angleX = startingEulers.x;
    } else {
      angleX = clampAngle(transform.localEulerAngles.x, startingEulers.x - MinTilt.x, startingEulers.x + MaxTilt.x);
      angleX = targetEulers.x;
    }
    if(FreezeY){
      angleY = startingEulers.y;
    } else {
      angleY = clampAngle(transform.localEulerAngles.y, startingEulers.y - MinTilt.y, startingEulers.y + MaxTilt.y);
      angleY = targetEulers.y;
    }
    if(FreezeZ){
      angleZ = startingEulers.z;
    } else {
      angleZ = clampAngle(transform.localEulerAngles.z, startingEulers.z - MinTilt.z, startingEulers.z + MaxTilt.z);
      angleZ = targetEulers.z;
    }
    transform.localEulerAngles = new Vector3(angleX, angleY, angleZ);
  }

  float clampAngle (float angle, float min, float max) {
    if(min >= 0){
      angle = Mathf.Clamp(angle, min, max);
    } else {
      float delta = -min;
      angle = Mathf.Clamp(angle + delta, min + delta, max + delta);
      angle -= delta;
    }
    //normalize to 0-360 range
     angle = angle % 360;

    if (angle <= -180)
      angle += 360;
    else if (angle > 180)
      angle -= 360;

    return angle;
  }
}
