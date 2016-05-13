using UnityEngine;
using System.Collections;

public class CameraFacer : MonoBehaviour {

  public Vector3 objectForward = Vector3.forward;
  public Vector3 MaxTilt = Vector3.one * 15;//degrees
  public Vector3 MinTilt = Vector3.one * 15;//degrees
  public bool FreezeX = false;
  public bool FreezeY = false;
  public bool FreezeZ = false;

  [Range(-760, 760)]
  public float InputAngle = 180;
  [Range(-360, 360)]
  public float TargetAngle = 180;
  [Range(0, 360)]
  public float Min = 20;
  [Range(0, 360)]
  public float Max = 20;

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
      angleX = clampAngle(transform.localEulerAngles.x, startingEulers.x - MinTilt.x, startingEulers.x + MaxTilt.x);
      //angleX = transform.localEulerAngles.x;
    }
    if(FreezeY){
      angleY = startingEulers.y;
    } else {
      //angleY = transform.localEulerAngles.y;
      angleY = clampAngle(transform.localEulerAngles.y, startingEulers.y - MinTilt.y, startingEulers.y + MaxTilt.y);
    }
    if(FreezeZ){
      angleZ = startingEulers.z;
    } else {
      //angleZ = transform.localEulerAngles.z;
      angleZ = clampAngle(transform.localEulerAngles.z, startingEulers.z - MinTilt.z, startingEulers.z + MaxTilt.z);
    }
    transform.localEulerAngles = new Vector3(angleX, angleY, angleZ);
    TestAngleCode();
    Debug.Log("Eulers: " + FreezeX + " " + FreezeY + " " + FreezeZ + " " + startingEulers + " " + targetEulers + " " + transform.localEulerAngles);
  }

  float clampAngle (float angle, float min, float max) {
    if(min >= 0){
      angle = Mathf.Clamp(angle, min, max);
    }else {
      float delta = -min;
      angle = Mathf.Clamp(angle + delta, min + delta, max + delta);
      angle -= delta;
    }
    return normalizeAngle(angle);
  }

  void TestAngleCode(){
    Vector3 target = new Vector3(Mathf.Cos(TargetAngle * Mathf.Deg2Rad), 0, Mathf.Sin(TargetAngle * Mathf.Deg2Rad));
    Vector3 min = new Vector3(Mathf.Cos((TargetAngle - Min) * Mathf.Deg2Rad), 0, Mathf.Sin((TargetAngle - Min) * Mathf.Deg2Rad));
    Vector3 max = new Vector3(Mathf.Cos((TargetAngle + Max) * Mathf.Deg2Rad), 0, Mathf.Sin((TargetAngle + Max) * Mathf.Deg2Rad));
    float testAngle = clampAngle(InputAngle, TargetAngle - Min, TargetAngle + Max);
    Vector3 test = new Vector3(Mathf.Cos(testAngle * Mathf.Deg2Rad), 0, Mathf.Sin(testAngle * Mathf.Deg2Rad));
    Debug.DrawLine(Vector3.zero, target, Color.blue);
    Debug.DrawLine(Vector3.zero, min, Color.yellow);
    Debug.DrawLine(Vector3.zero, max, Color.red);
    Debug.DrawLine(Vector3.zero, test, Color.green);
  }
  float normalizeAngle (float angle) {
     float normalizedDeg = angle % 360;

     if (normalizedDeg <= -180)
         normalizedDeg += 360;
     else if (normalizedDeg > 180)
         normalizedDeg -= 360;

     return normalizedDeg;
 }
}
