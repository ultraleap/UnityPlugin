using UnityEngine;
using System.Collections;

public class CameraFacer : MonoBehaviour {

  public Vector3 objectForward = Vector3.forward;
  public Transform RotationParent;
  public Vector3 MaxTilt = Vector3.one * 15;//degrees
  public Vector3 MinTilt = Vector3.one * 15;//degrees
  public bool FreezeX = false;
  public bool FreezeY = false;
  public bool FreezeZ = false;

  [Range(-360, 360)]
  public float InputAngle = 180;
  [Range(-360, 360)]
  public float TargetAngle = 180;
  [Range(0, 360)]
  public float Min = 20;
  [Range(0, 360)]
  public float Max = 20;

  private Quaternion offset;
  private Quaternion originalLocalRotation;

  void Awake(){
    transform.rotation = Quaternion.LookRotation(objectForward);
    offset = transform.rotation;
    originalLocalRotation = transform.localRotation;
  }

  void Update () {
    Vector3 cameraDirection = (Camera.main.transform.position - transform.position).normalized;
    Vector3 objectDirection = transform.TransformDirection(objectForward);
    Quaternion towardCamera = Quaternion.LookRotation(cameraDirection);
    towardCamera *= offset;
    transform.rotation = Quaternion.Slerp(towardCamera, Quaternion.LookRotation(objectDirection), Time.deltaTime / 5);
    Vector3 startEulers = originalLocalRotation.eulerAngles;

    float angleX, angleY, angleZ;
    if(FreezeX){
      angleX = startEulers.x;
    } else {
      angleX = ClampAngle(transform.localEulerAngles.x, startEulers.x - MinTilt.x, startEulers.x + MaxTilt.x);
    }
    if(FreezeY){
      angleY = startEulers.y;
    } else {
      angleY = ClampAngle(transform.localEulerAngles.y, startEulers.y - MinTilt.y, startEulers.y + MaxTilt.y);
    }
    if(FreezeZ){
      angleZ = startEulers.z;
    } else {
      angleZ = ClampAngle(transform.localEulerAngles.z, startEulers.z - MinTilt.z, startEulers.z + MaxTilt.z);
    }
    transform.localEulerAngles = new Vector3(angleX, angleY, angleZ);
    TestAngleCode();
  }

  /* Attribution: aldonaletto http://answers.unity.com/questions/141775/limit-local-rotation.html */
  float ClampAngle(float angle, float min, float max) {
    if (angle < 90 || angle > 270){       // if angle in the critic region...
        if (angle > 180) angle -= 360;  // convert all angles to -180..+180
        if (max > 180) max -= 360;
        if (min > 180) min -= 360;
    }    
    angle = Mathf.Clamp(angle, min, max);
    if (angle<0) angle += 360;  // if angle negative, convert to 0..360
    return angle;
  }

  void TestAngleCode(){
    Vector3 target = new Vector3(Mathf.Cos(TargetAngle * Mathf.Deg2Rad), 0, Mathf.Sin(TargetAngle * Mathf.Deg2Rad));
    Vector3 min = new Vector3(Mathf.Cos((TargetAngle - Min) * Mathf.Deg2Rad), 0, Mathf.Sin((TargetAngle - Min) * Mathf.Deg2Rad));
    Vector3 max = new Vector3(Mathf.Cos((TargetAngle + Max) * Mathf.Deg2Rad), 0, Mathf.Sin((TargetAngle + Max) * Mathf.Deg2Rad));
    float testAngle = ClampAngle(InputAngle, TargetAngle - Min, TargetAngle + Max);
    Vector3 test = new Vector3(Mathf.Cos(testAngle * Mathf.Deg2Rad), 0, Mathf.Sin(testAngle * Mathf.Deg2Rad));
    Debug.DrawLine(Vector3.zero, target, Color.blue);
    Debug.DrawLine(Vector3.zero, min, Color.yellow);
    Debug.DrawLine(Vector3.zero, max, Color.red);
    Debug.DrawLine(Vector3.zero, test, Color.green);
  }
}
