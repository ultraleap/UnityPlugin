using UnityEngine;
using System.Collections;

public class CameraFacer : MonoBehaviour {

  public Vector3 objectForward = Vector3.forward;
  public Transform RotationParent;
  public float MaxTilt = 20;

  private Quaternion offset;

  void Awake(){
    offset = transform.localRotation;
  }

	void Update () {
    //Quaternion lookAtCamera = Quaternion.FromToRotation(objectForward, -Camera.main.transform.forward);

    float tiltAngle = Quaternion.Angle(Quaternion.identity, transform.localRotation);
    float toCameraAngle = Quaternion.Angle(Camera.main.transform.rotation, transform.localRotation);
    if(toCameraAngle < MaxTilt) toCameraAngle = MaxTilt;
    transform.rotation = Quaternion.RotateTowards(transform.rotation, Camera.main.transform.rotation, MaxTilt);
	}
}
