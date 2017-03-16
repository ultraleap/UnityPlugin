using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class FaceAwayFromCameraAlways : MonoBehaviour {

  void Update() {
    this.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Vector3.up);
  }

}
