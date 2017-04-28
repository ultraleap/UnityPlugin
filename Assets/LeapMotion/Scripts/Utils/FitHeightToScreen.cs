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
  public class FitHeightToScreen : MonoBehaviour {
  
    void Awake() {
      float width_height_ratio = GetComponent<GUITexture>().texture.width / GetComponent<GUITexture>().texture.height;
      float width = width_height_ratio * Screen.height;
      float x_offset = (Screen.width - width) / 2.0f;
      GetComponent<GUITexture>().pixelInset = new Rect(x_offset, 0.0f, width, Screen.height);
    }
  }
}
