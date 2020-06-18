/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Recording {

  public class TransitionOnMarker : TransitionBehaviour {

    public MarkerController controller;
    public string markerName;
    public MarkerController.MarkerTest condition = MarkerController.MarkerTest.AfterMarkerStarts;

    private void OnEnable() {
      controller.updateMarkersIfNeeded();
    }

    private void Update() {
      if (controller.TestMarker(markerName, condition)) {
        Transition();
      }
    }
  }
}
