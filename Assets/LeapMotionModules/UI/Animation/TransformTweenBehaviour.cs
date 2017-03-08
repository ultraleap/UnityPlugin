using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.Animation {

  public class TransformTweenBehaviour : MonoBehaviour {

    public Transform targetTransform;

    public Transform startTransform;
    public Transform endTransform;

    private Tween _tween;
    /// <summary>
    /// Returns the Tween object the TransformTween behaviour produces on Start().
    /// 
    /// Use this to play or otherwise manipulate the animation.
    /// </summary>
    public Tween tween {
      get { return _tween; }
      set { _tween = value; }
    }

    void Start() {
      _tween = Tween.Persistent().Target(targetTransform)
                                   .LocalPosition(startTransform, endTransform)
                                   .Target(targetTransform)
                                   .LocalRotation(startTransform, endTransform)
                                   .Target(targetTransform)
                                   .LocalScale(startTransform, endTransform)
                                   .OverTime(0.33F)
                                   .Smooth(SmoothType.Smooth);

      _tween.progress = 0.0001F;
      _tween.Play(Direction.Backward);
    }

  }

}