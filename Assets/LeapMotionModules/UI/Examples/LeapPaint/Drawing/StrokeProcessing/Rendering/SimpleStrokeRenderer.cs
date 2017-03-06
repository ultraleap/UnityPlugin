using Leap.Paint;
using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.StrokeProcessing.Rendering {

  public class SimpleStrokeRenderer : StrokeRendererBase, IRuntimeGizmoComponent {

    public Stroke stroke;

    public override void UpdateRenderer(Stroke stroke, StrokeModificationHint modHint) {
      this.stroke = stroke;
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (stroke == null) return;
      bool colorSet = false;
      IEnumerator<StrokePoint> pointsEnum = stroke.GetPointsEnumerator();
      for (; pointsEnum.MoveNext(); ) {
        StrokePoint point = pointsEnum.Current;
        if (!colorSet) drawer.color = point.color;
        drawer.DrawSphere(point.position, 0.01F * point.pressure);
      }
    }

  }


}