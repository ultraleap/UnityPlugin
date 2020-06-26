/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

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
