using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Splines {

  public class zzOldSplineObject : MonoBehaviour, zzOldISpline {

    private const int MAX_CONTROL_POINTS = 64;

    [Range(0F, 1F)]
    [OnEditorChange("onSplineModified")]
    public float smoothness = 0.8F;

    #region Control Points

    [System.Serializable]
    public struct SplineControlPoint {
      public Vector3 position;
      public Vector3 tangent;
    }

    [SerializeField]
    [Disable]
    private int _numControlPoints = 0;

    [SerializeField]
    [Disable]
    private SplineControlPoint[] _controlPoints;

    private bool _updateSplineOnStart = false;
    void Awake() {
      if (_controlPoints == null || _controlPoints.Length == 0) {
        _controlPoints = new SplineControlPoint[MAX_CONTROL_POINTS];
      }
      else {
        _updateSplineOnStart = true;
      }
    }

    void Start() {
      if (_updateSplineOnStart) {
        onSplineModified();
      }
    }

    /// <summary> Returns the number of control points in this spline. </summary>
    public int numControlPoints { get { return _numControlPoints; } }

    public SplineControlPoint this[int idx] {
      get {
        if (idx >= numControlPoints) { throw new System.IndexOutOfRangeException(); }
        return _controlPoints[idx];
      }
    }

    public Vector3 GetControlPosition(int idx) {
      if (idx >= numControlPoints) { throw new System.IndexOutOfRangeException(); }
      return _controlPoints[idx].position;
    }

    public Vector3 GetControlTangent(int idx) {
      if (idx >= numControlPoints) { throw new System.IndexOutOfRangeException(); }
      return _controlPoints[idx].tangent;
    }

    public ControlPointEnumerator controlPoints { get { return new ControlPointEnumerator(this); } }

    public struct ControlPointEnumerator {
      private int _curPointIdx;
      private zzOldSplineObject _spline;

      public ControlPointEnumerator(zzOldSplineObject spline) {
        _curPointIdx = -1;
        _spline = spline;
      }

      public ControlPointEnumerator GetEnumerator() { return this; }
      public bool MoveNext() {
        _curPointIdx++;
        if (_curPointIdx >= _spline.numControlPoints)  return false;
        return true;
      }
      public SplineControlPoint Current { get { return _spline[_curPointIdx]; } }

    }

    #endregion

    #region Modification

    public Action OnSplineModified = () => { };
    private void onSplineModified() { OnSplineModified(); }

    public void AddControlPoint(Vector3 position, Vector3 tangent) {
      if (_numControlPoints == MAX_CONTROL_POINTS) {
        Debug.LogError("Unable to add spline point: Spline has run out of control points.");
        return;
      }

      _controlPoints[_numControlPoints++] = new SplineControlPoint() { position = position,
                                                                       tangent = tangent };

      onSplineModified();
    }

    /// <summary>
    /// Inserts a new control point between idx0 and idx1, shifting
    /// idx1 and every control point after it further down the spline.
    /// </summary>
    public void AddControlPointBetween(int idx0, int idx1,
                                       Vector3 position, Vector3 tangent) {
      if (_numControlPoints == MAX_CONTROL_POINTS) {
        Debug.LogError("Unable to add spline point: Spline has run out of control points.");
        return;
      }

      _numControlPoints += 1;

      for (int i = numControlPoints - 1; i > idx0; i--) {
        _controlPoints[i] = _controlPoints[i - 1];
      }
      _controlPoints[idx0 + 1] = new SplineControlPoint() { position = position,
                                                            tangent = tangent };
    }

    public void SetControlPosition(int idx, Vector3 pos) {
      if (idx >= numControlPoints) { throw new System.IndexOutOfRangeException(); }
      _controlPoints[idx].position = pos;

      onSplineModified();
    }

    public void SetControlTangent(int idx, Vector3 tangent) {
      if (idx >= numControlPoints) { throw new System.IndexOutOfRangeException(); }
      _controlPoints[idx].tangent = tangent;

      onSplineModified();
    }

    public void SetControlPositions(Vector3[] positions, int idxOffset = 0) {
      int idx = 0;
      foreach (var pos in positions) {
        _controlPoints[idx++ + idxOffset].position = pos;
      }

      onSplineModified();
    }

    public void Clear() {
      _numControlPoints = 0;

      onSplineModified();
    }

    #endregion

    #region Evaluation

    private static Vector3[] s_posBuffer = new Vector3[4];
    private static float[] s_timesBuffer = new float[4];
    public Vector3 Evaluate(int idxA, int idxB, float t) {
      if (idxA < 0 || idxA >= _numControlPoints || idxB < 0 || idxB >= _numControlPoints) {
        throw new System.IndexOutOfRangeException();
      }

      s_posBuffer[0] = _controlPoints[idxA].position - _controlPoints[idxA].tangent;
      s_posBuffer[1] = _controlPoints[idxA].position;
      s_posBuffer[2] = _controlPoints[idxB].position;
      s_posBuffer[3] = _controlPoints[idxB].position + _controlPoints[idxB].tangent;

      s_timesBuffer[0] = -0.5F;
      s_timesBuffer[1] = 0F;
      s_timesBuffer[2] = 1F;
      s_timesBuffer[3] = 1.5F;

      return CatmullRom.Interpolate(s_posBuffer, s_timesBuffer, t);
    }

    public SplineInterpolatorEnumerator Traverse(float minStepProduct = 0.95F) {
      return new SplineInterpolatorEnumerator(this, minStepProduct);
    }

    #endregion

  }

}