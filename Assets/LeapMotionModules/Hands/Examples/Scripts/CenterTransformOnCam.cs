using UnityEngine;
using System.Collections;

public class CenterTransformOnCam : MonoBehaviour {
  public Transform Camera;


  // Update is called once per frame
  void Update() {
    Vector3 centeredVector = new Vector3(Camera.position.x, Camera.position.y - .2f, Camera.position.z);
    transform.position = centeredVector;
  }
}
