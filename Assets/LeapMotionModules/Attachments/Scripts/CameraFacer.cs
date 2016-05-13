using UnityEngine;
using System.Collections;

public class CameraFacer : MonoBehaviour {

  [Range(.1f,20)]
  public float Speed = 10;
  public Vector3 objectForward = Vector3.forward;
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
    Quaternion towardCamera = Quaternion.LookRotation(cameraDirection);
    towardCamera *= offset;
    transform.rotation = Quaternion.Slerp(transform.rotation, towardCamera, Time.deltaTime * Speed);
    Vector3 startingEulers = startingLocalRotation.eulerAngles;
    Vector3 targetEulers = transform.localEulerAngles;
    float angleX, angleY, angleZ;
    if(FreezeX){
      angleX = startingEulers.x;
    } else {
      angleX = targetEulers.x;
    }
    if(FreezeY){
      angleY = startingEulers.y;
    } else {
      angleY = targetEulers.y;
    }
    if(FreezeZ){
      angleZ = startingEulers.z;
    } else {
      angleZ = targetEulers.z;
    }
    transform.localEulerAngles = new Vector3(angleX, angleY, angleZ);
  }
}
