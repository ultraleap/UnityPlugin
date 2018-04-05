/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Collections;

namespace Leap.Unity.VRVisualizer{
  public class VisualizerControls : MonoBehaviour {
  	// Update is called once per frame
  	void Update () {
  	  if (Input.GetKeyDown(KeyCode.Escape))
      {
        Application.Quit();
      }
  	}
  }
}
