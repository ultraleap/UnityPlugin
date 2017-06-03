/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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
