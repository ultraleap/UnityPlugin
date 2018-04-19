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
