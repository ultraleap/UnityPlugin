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

namespace Leap.Unity{
  public class StretchToScreen : MonoBehaviour {
  
    void Awake() {
      GetComponent<GUITexture>().pixelInset = new Rect(0.0f, 0.0f, Screen.width, Screen.height);
    }
  }
}
