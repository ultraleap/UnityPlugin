using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.LeapPaint.LineDrawing {

  public class LineBrush3D : Brush3DBase {

    public LineRendererController lineControllerPrefab;

    private float _minSegmentDistance = 0.01F;
    private Vector3 _lastAddedPos;
    private bool _needsFirstPos = true;

    private LineRendererController _curLine;

    public override void BeginStroke() {
      base.BeginStroke();

      _curLine = Instantiate<LineRendererController>(lineControllerPrefab);
    }

    void Update() {
      if (isBrushing) {
        if (_needsFirstPos || Vector3.Distance(_lastAddedPos, this.transform.position) > _minSegmentDistance) {
          _curLine.AddLineAnchor(this.transform.position);
          _lastAddedPos = this.transform.position;
          _needsFirstPos = false;
        }
      }
    }

    public override void EndStroke() {
      base.EndStroke();
    }

  }

}