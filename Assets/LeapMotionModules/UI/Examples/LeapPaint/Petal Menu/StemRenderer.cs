using Leap.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LineRenderer))]
public class StemRenderer : MonoBehaviour {

  public Transform stemTip;

  private LineRenderer _line;
  private Vector3[] _linePoints;
  private int _lastLinePointsLength;

  void Start() {
    _line = GetComponent<LineRenderer>();
    _linePoints = new Vector3[_line.numPositions];
    _lastLinePointsLength = _line.numPositions;
  }

  void OnValidate() {
    _line = GetComponent<LineRenderer>();
    _linePoints = new Vector3[_line.numPositions];
    _lastLinePointsLength = _line.numPositions;
  }

  private static Vector3[] _stemPointsCache = new Vector3[4];
  private static float[]   _timesCache = new float[4];
  void Update() {
    if (stemTip != null && _linePoints.Length > 0) {
      if (_lastLinePointsLength != _line.numPositions) {
        _linePoints = new Vector3[_line.numPositions];
        _lastLinePointsLength = _line.numPositions;
      }
      float stemDistance = Vector3.Distance(stemTip.transform.position, this.transform.position);
      _stemPointsCache[0] = this.transform.position - this.transform.up * 0.2F * stemDistance;
      _stemPointsCache[1] = this.transform.position;
      _stemPointsCache[2] = stemTip.transform.position;
      _stemPointsCache[3] = stemTip.transform.position + stemTip.transform.up * 0.2F * stemDistance;
      _timesCache[0] = -0.2F;
      _timesCache[1] = 0F;
      _timesCache[2] = 1F;
      _timesCache[3] = 1.2F;
      CatmullRom.InterpolatePoints(_stemPointsCache, _timesCache, _linePoints);
      _line.SetPositions(_linePoints);

      //s_timeCache[0] = -0.035F / Vector3.Distance(s_valueCache[1], s_valueCache[2]);
      //s_timeCache[1] = 0F;
      //s_timeCache[2] = 1F;
      //s_timeCache[3] = 1F + 0.035F / Vector3.Distance(s_valueCache[1], s_valueCache[2]);
    }
  }

  //void Update() {
  //  int idx = 0;
  //  foreach (Vector3 pos in SmoothStem.GetStemPoints(this.transform.position + Vector3.down * 0.05F,
  //                                                   this.transform.position,
  //                                                   _lastPoint.transform.position,
  //                                                   _lastPoint.transform.position + (Quaternion.AngleAxis(90F, Vector3.Cross(Vector3.up,
  //                                                                                     (_lastPoint.transform.position - this.transform.position)))
  //                                                     * Vector3.up) * 0.05F)) {
  //    _lineAnchors[idx++].transform.position = pos;
  //  }
  //}

}
