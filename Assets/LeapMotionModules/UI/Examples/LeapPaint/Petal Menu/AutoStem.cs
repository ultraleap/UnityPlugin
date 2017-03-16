using Leap.Unity.LeapPaint.LineDrawing;
using Leap.Unity.LeapPaint.PetalMenu;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRendererController))]
public class AutoStem : MonoBehaviour {

  private const int NUM_POINTS = 12;

  private Transform _lastPoint;
  private Transform[] _lineAnchors = new Transform[NUM_POINTS];

  void Start() {
    for (int i = 0; i < NUM_POINTS; i++) {
      GameObject child = new GameObject("Line Anchor");
      child.AddComponent<LineAnchor>();
      child.transform.position = this.transform.position;
      child.transform.parent = this.transform;
      _lineAnchors[i] = child.transform;
    }
    _lastPoint = new GameObject("Stem End").transform;
    _lastPoint.transform.position = this.transform.position + Vector3.up * 0.2F;
    _lastPoint.transform.parent = this.transform;
  }

  void Update() {
    int idx = 0;
    foreach (Vector3 pos in SmoothStem.GetStemPoints(this.transform.position + Vector3.down * 0.05F,
                                                     this.transform.position,
                                                     _lastPoint.transform.position,
                                                     _lastPoint.transform.position + (Quaternion.AngleAxis(90F, Vector3.Cross(Vector3.up,
                                                                                       (_lastPoint.transform.position - this.transform.position)))
                                                       * Vector3.up) * 0.05F)) {
      _lineAnchors[idx++].transform.position = pos;
    }
  }

}
