/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
