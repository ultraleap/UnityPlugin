using Leap.Unity.UI.Constraints;
using Leap.Unity.UI.Interaction;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Examples {

  public class ButtonCallbacksFromLineConstraint : MonoBehaviour {

    public Action        OnButtonPressed;
    public Action<float> OnButtonProgress;
    public Action        OnButtonReady;

    public float fireThreshold = 0.8F;
    public float readyThreshold = 0.3F;

    public LeapConstraint lineConstraint;

    private Vector3 _lineConstraintStart;
    private Vector3 _lineConstraintEnd;
    private float _progress;
    private float _progressLastFrame = float.MinValue;
    private bool _ready;

    void Start() {
      _lineConstraintStart = lineConstraint.constraints[0].start;
      _lineConstraintEnd = lineConstraint.constraints[0].end;
    }

    void Update() {
      // TODO: This is wasteful, it's doing the whole constraint calculation again just to get progress. Should be
      // hooked up to a callback from the LeapConstraint -- but that might only make sense if the LeapConstraint
      // is specifically for line segment constraints or other constraints where "progress" has meaning.
      ConstraintsUtil.ConstrainToLineSegment(this.transform.localPosition, _lineConstraintStart, _lineConstraintEnd, out _progress);

      // Main button callbacks.
      if (_ready) {
        if (_progress > fireThreshold) {
          OnButtonPressed();
          _ready = false;
        }
      }
      else {
        if (_progress < readyThreshold) {
          OnButtonReady();
          _ready = true;
        }
      }

      // Button progress callbacks for visual feedback.
      if (_progress != _progressLastFrame) {
        OnButtonProgress(_progress);
      }
      _progressLastFrame = _progress;
    }

  }

}