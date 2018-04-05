/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using Leap.Unity.Attributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.Animation {

  /// <summary>
  /// This is a wrapper MonoBehaviour that demonstrates and exposes some of the
  /// basic functionality of the Tween library. Tweens can interpolate between
  /// more than just Transform properties, so don't be afraid to roll your own.
  /// </summary>
  public class TransformTweenBehaviour : MonoBehaviour {

    [Tooltip("The transform to which to apply the tweened properties.")]
    public Transform targetTransform;

    [Tooltip("The transform whose position/rotation/localScale provide the start state of the tween.")]
    public Transform startTransform;
    [Tooltip("The transform whose position/rotation/localScale provide the end state of the tween.")]
    public Transform endTransform;

    public bool startAtEnd = false;

    [Header("Tween Settings")]
    public bool tweenLocalPosition = true;
    public bool tweenLocalRotation = true;
    public bool tweenLocalScale    = true;
    [MinValue(0.001F)]
    public float tweenDuration = 0.25F;
    public SmoothType tweenSmoothType = SmoothType.Smooth;
    
    #region Events
    
    public Action<float> OnProgress     = (progress) => { };

    public Action OnLeaveStart = () => { }; 
    public Action OnReachEnd   = () => { }; 
    public Action OnLeaveEnd   = () => { }; 
    public Action OnReachStart = () => { };

    #endregion

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

    void Awake() {
      initUnityEvents();

      // Tween setup methods return the Tween object itself, so you can chain your setup
      // method calls.
      _tween = Tween.Persistent().OverTime(tweenDuration)
                                 .Smooth(tweenSmoothType);

      if (tweenLocalPosition) _tween = _tween.Target(targetTransform)
                                             .LocalPosition(startTransform, endTransform);

      if (tweenLocalRotation) _tween = _tween.Target(targetTransform)
                                             .LocalRotation(startTransform, endTransform);

      if (tweenLocalScale) _tween = _tween.Target(targetTransform)
                                          .LocalScale(startTransform, endTransform);

      // Hook up the UnityEvents to the actual Tween callbacks.
      _tween.OnProgress(OnProgress);
      _tween.OnLeaveStart(OnLeaveStart);
      _tween.OnReachEnd(OnReachEnd);
      _tween.OnLeaveEnd(OnLeaveEnd);
      _tween.OnReachStart(OnReachStart);

      // TODO: This isn't great but it's the only way I've seen to make sure the tween
      // updates with its progress at the right state :(
      if (startAtEnd) {
        _tween.progress = 0.9999999F;
        _tween.Play(Direction.Forward);
      }
      else {
        _tween.progress = 0.0000001F;
        _tween.Play(Direction.Backward);
      }
    }

    void OnDestroy() {
      if (_tween.isValid) {
        _tween.Release();
      }
    }

    private Coroutine _playTweenAfterDelayCoroutine;
    private Direction _curDelayedDirection = Direction.Backward;

    /// <summary>
    /// Tweens play forward by default, but at any time past the starting
    /// state they can also be played backwards to return to the starting
    /// state. See the tween property for more direct control of this
    /// behaviour's tween.
    /// </summary>
    public void PlayTween() {
      PlayTween(Direction.Forward);
    }

    public void PlayTween(Direction tweenDirection = Direction.Forward, float afterDelay = 0F) {
      if (_playTweenAfterDelayCoroutine != null && tweenDirection != _curDelayedDirection) {
        StopCoroutine(_playTweenAfterDelayCoroutine);
        _curDelayedDirection = tweenDirection;
      }

      _playTweenAfterDelayCoroutine = StartCoroutine(playAfterDelay(tweenDirection, afterDelay));
    }

    private IEnumerator playAfterDelay(Direction tweenDirection, float delay) {
      yield return new WaitForSeconds(delay);

      tween.Play(tweenDirection);
    }

    public void PlayForward() {
      PlayTween(Direction.Forward);
    }

    public void PlayBackward() {
      PlayTween(Direction.Backward);
    }

    public void PlayForwardAfterDelay(float delay = 0F) {
      PlayTween(Direction.Forward, delay);
    }

    public void PlayBackwardAfterDelay(float delay = 0F) {
      PlayTween(Direction.Backward, delay);
    }
    
    /// <summary>
    /// Stops the underlying tween and resets it to the starting state.
    /// </summary>
    public void StopTween() {
      tween.Stop();
    }

    public void SetTargetToStart() {
      setTargetTo(startTransform);
    }

    public void SetTargetToEnd() {
      setTargetTo(endTransform);
    }

    private void setTargetTo(Transform t) {
      if (targetTransform != null && t != null) {
        if (tweenLocalPosition) targetTransform.localPosition = t.localPosition;
        if (tweenLocalRotation) targetTransform.localRotation = t.localRotation;
        if (tweenLocalScale)    targetTransform.localScale    = t.localScale;
      }
    }

    #region Unity Events (Internal)

    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }

    [SerializeField]
    private EnumEventTable _eventTable;

    public enum EventType {
      //OnProgress = 100, // Requires float Event data
      OnLeaveStart = 110,
      OnReachEnd = 120,
      OnLeaveEnd = 130,
      OnReachStart = 140
    }

    private void initUnityEvents() {
      setupCallback(ref OnLeaveStart, EventType.OnLeaveStart);
      setupCallback(ref OnReachEnd, EventType.OnReachEnd);
      setupCallback(ref OnLeaveEnd, EventType.OnLeaveEnd);
      setupCallback(ref OnReachStart, EventType.OnReachStart);
    }

    private void setupCallback(ref Action action, EventType type) {
      action += () => _eventTable.Invoke((int)type);
    }

    private void setupCallback<T>(ref Action<T> action, EventType type) {
      action += (anchObj) => _eventTable.Invoke((int)type);
    }

    #endregion

  }

}
