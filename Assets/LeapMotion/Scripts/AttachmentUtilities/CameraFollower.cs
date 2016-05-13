using UnityEngine;
using System.Collections;

namespace Leap.Unity{

  public class CameraFollower : MonoBehaviour {
  
    public Vector3 objectForward = Vector3.forward;
    public AnimationCurve Ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Range(1, 20)]
    public float Speed = 10;
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
      Vector3 objectFacing = transform.TransformDirection(objectForward);
      float deltaAngle = Vector3.Angle(objectFacing, cameraDirection);
      float easing = Ease.Evaluate(Speed * deltaAngle / 360);
      Quaternion towardCamera = Quaternion.LookRotation(cameraDirection);
      towardCamera *= offset;
      transform.rotation = Quaternion.Slerp(transform.rotation, towardCamera, easing);
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
}