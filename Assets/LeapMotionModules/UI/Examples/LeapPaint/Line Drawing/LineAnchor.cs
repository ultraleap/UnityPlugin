using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.LeapPaint.LineDrawing {

  [ExecuteInEditMode]
  public class LineAnchor : MonoBehaviour {

    private LineRendererController _line;

    void Start() {
      _line = GetComponentInParent<LineRendererController>();
    }

    void Update() {
      if (this.transform.hasChanged) {
        _line.Refresh();
        this.transform.hasChanged = false;
      }
    }

    void OnDestroy() {
      _line.Refresh();
    }

  }

}