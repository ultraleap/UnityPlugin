using UnityEngine;
using System.Collections;

public class CameraFacer : MonoBehaviour {

  public bool ConstrainX = false;
  public bool ConstrainY = false;
  public bool ConstrainZ = false;

	// Update is called once per frame
	void Update () {
    Quaternion lookRotation = Quaternion.LookRotation(Camera.main.transform.position, Camera.main.transform.up);
    float eulerX = ConstrainX ? transform.rotation.eulerAngles.x : lookRotation.eulerAngles.x;
    float eulerY = ConstrainY ? transform.rotation.eulerAngles.y : lookRotation.eulerAngles.y;
    float eulerZ = ConstrainZ ? transform.rotation.eulerAngles.z : lookRotation.eulerAngles.z;
    transform.rotation = Quaternion.Euler(eulerX, eulerY, eulerZ);
//    transform.localRotation = Quaternion.Euler(eulerX, eulerY, eulerZ);
	}
}
