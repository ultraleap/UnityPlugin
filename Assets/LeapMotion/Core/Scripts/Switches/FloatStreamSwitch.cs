using Leap.Unity.Animation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class FloatStreamSwitch : TweenSwitch,
                                   IStream<float> {

    public AnimationCurve tweenCurve = DefaultCurve.LinearUp;

    public event Action OnOpen = () => { };
    public event Action<float> OnSend = (f) => { };
    public event Action OnClose = () => { };
    private bool _isStreamOpen = false;

    protected override void whenTweenReachesStart() {
      if (_isStreamOpen) {
        OnClose();
        _isStreamOpen = false;
      }
    }

    protected override void whenTweenReachesEnd() {
      if (_isStreamOpen) {
        OnClose();
        _isStreamOpen = false;
      }
    }

    protected override void whenTweenLeavesEnd() {
      if (!_isStreamOpen) {
        OnOpen();
        _isStreamOpen = true;
      }
    }

    protected override void whenTweenLeavesStart() {
      if (!_isStreamOpen) {
        OnOpen();
        _isStreamOpen = true;
      }
    }

    protected override void updateSwitch(float time, bool immediately = false) {
      OnSend(tweenCurve.Evaluate(time));
    }

  }

}
