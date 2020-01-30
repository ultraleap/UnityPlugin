using Leap.Unity.RuntimeGizmos;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Geometry {

  [ExecuteInEditMode]
  public class GeometryTest_SegmentRectIntersection : MonoBehaviour,
                                                      IRuntimeGizmoComponent {

    public Transform segmentA;
    public Transform segmentB;

    public LocalRect localRect;

    private LocalSegment3? _maybeSegment;
    private Rect rect {
      get {
        return localRect.With(this.transform);
      }
    }

    private Vector3? _maybePointOnRect = null;
    private Vector3? _maybePointOnSegment = null;

    public TextMesh text;

    private void Update() {
      _maybePointOnRect = null;
      _maybePointOnSegment = null;
      _maybeSegment = null;

      if (segmentA != null && segmentB != null) {
        _maybeSegment = new LocalSegment3(segmentA.position, segmentB.position);
      }
      if (_maybeSegment.HasValue) {
        var segment = _maybeSegment.Value;

        Vector3 closestPointOnRect, closestPointOnSegment;
        Collision.Intersect(rect, segment,
                            out closestPointOnRect, out closestPointOnSegment);
        _maybePointOnRect = closestPointOnRect;
        _maybePointOnSegment = closestPointOnSegment;

        if (text != null) {
          text.text = (Vector3.Distance(_maybePointOnRect.Value,
                       _maybePointOnSegment.Value)).ToString();
        }
      }
    }

    public void OnDrawRuntimeGizmos(RuntimeGizmoDrawer drawer) {
      if (!enabled || !gameObject.activeInHierarchy) return;

      drawer.color = Rect.DEFAULT_GIZMO_COLOR;
      rect.DrawRuntimeGizmos(drawer);
      
      if (_maybeSegment.HasValue) {
        drawer.color = Color.white;
        _maybeSegment.Value.DrawRuntimeGizmos(drawer);
      }

      drawer.color = LeapColor.red;

      if (_maybePointOnRect.HasValue && _maybePointOnSegment.HasValue) {
        drawer.DrawLine(_maybePointOnRect.Value, _maybePointOnSegment.Value);
      }
    }

  }

}
