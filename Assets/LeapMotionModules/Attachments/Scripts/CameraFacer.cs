using UnityEngine;
using System.Collections;

public class CameraFacer : MonoBehaviour {

  public Vector3 objectForward = Vector3.forward;
  public Transform RotationParent;
  public Vector3 MaxTilt = Vector3.one * 15;//degrees

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
    float angleX = ClampAngle(transform.localEulerAngles.x, -MaxTilt.x, MaxTilt.x);
    float angleY = ClampAngle(transform.localEulerAngles.y, -MaxTilt.y, MaxTilt.y);
    float angleZ = ClampAngle(transform.localEulerAngles.z, -MaxTilt.z, MaxTilt.z);
    transform.localEulerAngles = new Vector3(angleX, angleY, angleZ);
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
}
