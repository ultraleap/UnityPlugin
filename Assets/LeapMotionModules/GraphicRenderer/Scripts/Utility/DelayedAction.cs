using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Leap.Unity.GraphicalRenderer {

  public class DelayedAction : IDisposable {
    private Action _action;
    private float _delay;
    private int _minFrameDelay;

    private double _actionTime;
    private int _framesLeft;

    public DelayedAction(Action action, float delay = 0.15f, int minFrameDelay = 3) {
      _action = action;
      _delay = delay;
      _minFrameDelay = minFrameDelay;

      _actionTime = double.MaxValue;
      _framesLeft = int.MaxValue;

      //TODO: make this class work at runtime too, will probably need to spawn monobehaviors
      //      so we can get update callbacks since there is no other way >_>
#if UNITY_EDITOR
      EditorApplication.update += update;
#endif
    }

    public void Reset() {
      _actionTime = currTime + _delay;
      _framesLeft = _minFrameDelay;
    }

    private void update() {
      _framesLeft--;

      if (currTime > _actionTime && _framesLeft <= 0) {
        _action();
        _actionTime = double.MaxValue;
      }
    }

    public void Dispose() {
#if UNITY_EDITOR
      EditorApplication.update -= update;
#endif
      _action = null;
    }

    private double currTime {
      get {
#if UNITY_EDITOR
        return EditorApplication.timeSinceStartup;
#else
      return Time.realtimeSinceStartup;
#endif
      }
    }
  }
}
