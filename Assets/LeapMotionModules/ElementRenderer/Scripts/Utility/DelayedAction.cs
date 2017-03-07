using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DelayedAction {
  private Action _action;
  private float _delay;
  private double _actionTime;

  public DelayedAction(Action action, float delay = 0.15f) {
    _action = action;
    _delay = delay;
    _actionTime = double.MaxValue;

    //TODO: make this class work at runtime too, will probably need to spawn monobehaviors
    //      so we can get update callbacks since there is no other way >_>
#if UNITY_EDITOR
    EditorApplication.update += update;
#endif
  }

  public void QueueAction() {
    _actionTime = currTime + _delay;
  }

  private void update() {
    if (currTime > _actionTime) {
      _action();
      _actionTime = double.MaxValue;
    }
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
