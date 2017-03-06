using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity.Attributes;
using Leap.StrokeProcessing;
using Leap.StrokeProcessing.Rendering;

namespace Leap.Paint {

  public class Brush : MonoBehaviour {

    [Header("Configuration")]
    [Tooltip("The transform to use to set StrokePoints from this Brush. If no transform is specified, this transform will be used.")]
    public Transform brushTip;
    [Tooltip("Scale each piece of the output mesh by this value. (Cross sections will only scale by X and Y.)")]
    public Vector3 scalingVector = Vector3.one;
    [Tooltip("If set to null, new StrokeRenderer objects will have the root of the hierarchy as their parent.")]
    public Transform outputParent;
    [Tooltip("The minimum distance between two StrokePoints.")]
    [MinValue(0.005F)]
    public float minSegmentDistance = 0.005F;
    [Tooltip("The StrokeRenderer to use to render this Brush's strokes. If no StrokeRenderer is specified, a simple default will be set.")]
    public StrokeRendererBase strokeRendererPrefab;

    private bool              _isBrushing;
    private Stroke            _curStroke;
    private StrokePoint       _lastStrokePoint;
    private GameObject        _curStrokeObj;
    private int               _curStrokeObjIdx;
    private StrokeFilter      _curStrokeFilter;

    protected virtual void Awake() {
      if (brushTip == null) {
        brushTip = this.transform;
      }
      if (strokeRendererPrefab == null) {
        strokeRendererPrefab = new GameObject("Brush " + this.name + " StrokeRenderer Prefab").AddComponent<SimpleStrokeRenderer>();
        strokeRendererPrefab.gameObject.hideFlags = HideFlags.HideAndDontSave;
      }
    }

    public void Begin() {
      _isBrushing = true;

      _curStroke = new Stroke();
      _lastStrokePoint = StrokePointFromCurrentState();
      _curStrokeObj = Instantiate<GameObject>(strokeRendererPrefab.gameObject, outputParent);
      _curStrokeObj.name = "Brush " + this.name + " Stroke " + (++_curStrokeObjIdx);
      //var curStrokeRenderer = _curStrokeObj.GetComponent<StrokeRendererBase>();
      _curStrokeFilter = _curStrokeObj.GetComponent<StrokeFilter>();
      if (_curStrokeFilter == null) {
        _curStrokeFilter = _curStrokeObj.AddComponent<StrokeFilter>();
      }
      _curStrokeFilter.stroke = _curStroke;
      AddStrokePoint(_lastStrokePoint);
    }

    public virtual StrokePoint StrokePointFromCurrentState() {
      StrokePoint strokePoint = new StrokePoint();
      strokePoint.position    = brushTip.position;
      strokePoint.rotation    = brushTip.rotation;
      strokePoint.scale       = Vector3.Scale((brushTip.localScale * 0.01F), scalingVector);
      strokePoint.deltaTime   = Time.time - _curStroke.timeCreated;
      strokePoint.color       = Color.white;
      strokePoint.pressure    = 1F;

      return strokePoint;
    }

    public void End() {
      _isBrushing = false;
      _lastStrokePoint = null;
    }

    protected virtual void Update() {
      if (_isBrushing) {
        float curLastDistance = Vector3.Distance(brushTip.position, _lastStrokePoint.position);
        if (curLastDistance > minSegmentDistance) {
          AddStrokePoint(StrokePointFromCurrentState());
        }
      }
    }

    private void AddStrokePoint(StrokePoint s) {
      _curStroke.Add(s);
      _lastStrokePoint = s;
    }

    public void DoStroke(Stroke stroke) {
      throw new System.NotImplementedException();
    }

    public bool IsBrushing() {
      return _isBrushing;
    }

  }

}
