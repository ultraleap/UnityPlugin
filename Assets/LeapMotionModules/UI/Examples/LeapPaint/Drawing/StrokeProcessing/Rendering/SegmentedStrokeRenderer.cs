using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.StrokeProcessing.Rendering {

  [RequireComponent(typeof(SegmentRenderer))]
  public class SegmentedStrokeRenderer : StrokeRendererBase {

    private SegmentRenderer _segmentRenderer;
    public SegmentRenderer SegmentRenderer {
      get {
        if (_segmentRenderer == null) _segmentRenderer = GetComponent<SegmentRenderer>();
        return _segmentRenderer;
      }
    }

    public override void UpdateRenderer(Stroke stroke, StrokeModificationHint modHint) {

      switch (modHint.modType) {
        case StrokeModificationType.Overhaul:
          SegmentRenderer.Initialize();
          for (int i = 0; i < stroke.Count; i++) {
            ProcessAddedPoint(stroke, i);
          }
          break;
        case StrokeModificationType.AddedPoint:
          ProcessAddedPoint(stroke, stroke.Count - 1);
          break;
        case StrokeModificationType.AddedPoints:
          for (int i = stroke.Count - modHint.numPointsAdded; i < stroke.Count; i++) {
            ProcessAddedPoint(stroke, i);
          }
          break;
        case StrokeModificationType.ModifiedPoints:
          ProcessModifiedPoints(modHint.pointsModified);
          break;
        default:
          throw new System.NotImplementedException();
      }

    }

    private void ProcessAddedPoint(Stroke stroke, int addedPointIdx) {
      if (addedPointIdx == 0) {
        SegmentRenderer.AddPoint(stroke[0]);
        SegmentRenderer.AddStartCap(0);
        SegmentRenderer.AddEndCap(0);
      }
      else {
        SegmentRenderer.RemoveEndCapAtEnd();
        SegmentRenderer.AddPoint(stroke[addedPointIdx]);
        SegmentRenderer.AddEndCap(addedPointIdx);
      }
    }

    private void ProcessModifiedPoints(List<int> modifiedPointsIndices) {
      SegmentRenderer.ModifyPoints(modifiedPointsIndices);
    }

  }

}