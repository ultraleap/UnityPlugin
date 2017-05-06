/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Animation {

  /// <summary>
  /// This is a wrapper MonoBehaviour that demonstrates and
  /// exposes some of the basic functionality of the Tween
  /// library. Tweens can interpolate between more than just
  /// Transform properties, so don't be afraid to roll your own.
  /// </summary>
  public class TransformTweenBehaviour : MonoBehaviour {

    [Tooltip("The transform to which to apply the tweened properties.")]
    public Transform targetTransform;

    [Tooltip("The transform whose position/rotation/localScale provide the start state of the tween.")]
    public Transform startTransform;
    [Tooltip("The transform whose position/rotation/localScale provide the end state of the tween.")]
    public Transform endTransform;

    [Header("Tween Settings")]
    public bool tweenLocalPosition = true;
    public bool tweenLocalRotation = true;
    public bool tweenLocalScale    = true;
    public float tweenDuration = 0.33F;
    public SmoothType tweenSmoothType = SmoothType.Smooth;

    [Header("Tween Callbacks - Progress")]
    public FloatEvent OnProgress = new FloatEvent();
    [Header("Tween Callbacks - Forward")]
    public UnityEvent OnLeaveStart = new UnityEvent();
    public UnityEvent OnReachEnd = new UnityEvent();
    [Header("Tween Callbacks - Backward")]
    public UnityEvent OnLeaveEnd = new UnityEvent();
    public UnityEvent OnReachStart = new UnityEvent();

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

    void OnValidate() {
      if (targetTransform != null) {
        if (startTransform == targetTransform) {
          Debug.LogError("The start transform of the TransformTweenBehaviour should be "
                       + "a different transform than the target transform; the start "
                       + "transform provides starting position/rotation/scale information "
                       + "for the tween.", this.gameObject);
        }
        else if (endTransform == targetTransform) {
          Debug.LogError("The end transform of the TransformTweenBehaviour should be "
                       + "a different transform than the target transform; the end "
                       + "transform provides ending position/rotation/scale information "
                       + "for the tween.", this.gameObject);
        }
      }
    }

    void Start() {
      // Tween setup methods return the Tween object itself, so you can chain your setup
      // method calls.
      _tween = Tween.Persistent().OverTime(tweenDuration)
                                 .Smooth(tweenSmoothType);

      if (tweenLocalPosition) _tween = _tween.Target(targetTransform)
                                             .LocalPosition(startTransform, endTransform);

      if (tweenLocalRotation) _tween = _tween.Target(targetTransform)
                                             .LocalRotation(startTransform, endTransform);

      if (tweenLocalScale)    _tween = _tween.Target(targetTransform)
                                             .LocalScale(startTransform, endTransform);

      // Hook up the UnityEvents to the actual Tween callbacks.
      _tween.OnProgress(OnProgress.Invoke);
      _tween.OnLeaveStart(OnLeaveStart.Invoke);
      _tween.OnReachEnd(OnReachEnd.Invoke);
      _tween.OnLeaveEnd(OnLeaveEnd.Invoke);
      _tween.OnReachStart(OnReachStart.Invoke);

      // TODO: This isn't great but it's the only way I've seen to make sure the tween
      // updates with its progress = 0 state :(
      _tween.progress = 0.0001F;
      _tween.Play(Direction.Backward);
    }

    void OnDestroy() {
      if (_tween.isValid) {
        _tween.Release();
      }
    }

    /// <summary>
    /// Tweens play forward by default, but at any time past the starting
    /// state they can also be played backwards to return to the starting
    /// state. See the tween property for more direct control of this
    /// behaviour's tween.
    /// </summary>
    public void PlayTween(Direction tweenDirection = Direction.Forward) {
      tween.Play(tweenDirection);
    }
    
    /// <summary>
    /// Stops the underlying tween and resets it to the starting state.
    /// </summary>
    public void StopTween() {
      tween.Stop();
    }

    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

  }

}
