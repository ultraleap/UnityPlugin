using Leap.Unity.Attributes;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.LeapPaint.LineDrawing {

  [RequireComponent(typeof(LineRenderer))]
  [ExecuteInEditMode]
  public class LineRendererController : MonoBehaviour {

    private LineRenderer _lineRenderer;

    void Start() {
      _lineRenderer = GetComponent<LineRenderer>();
    }

    private static List<Vector3> s_positionCache = new List<Vector3>();
    public void Refresh() {
      s_positionCache.Clear();
      foreach (var lineAnchor in GetComponentsInChildren<LineAnchor>()) {
        s_positionCache.Add(lineAnchor.transform.position);
      }
      _lineRenderer.numPositions = s_positionCache.Count;
      _lineRenderer.SetPositions(s_positionCache.ToArray());
    }

    public void AddLineAnchor(Vector3 position) {
      GameObject lineAnchorObj = new GameObject("Line Anchor");
      lineAnchorObj.transform.position = position;
      lineAnchorObj.transform.parent = this.transform;
      lineAnchorObj.AddComponent<LineAnchor>();
    }

  }

}