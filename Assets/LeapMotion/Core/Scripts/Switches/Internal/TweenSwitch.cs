using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Animation {

  /// <summary>
  /// An easy-to-implement IPropertySwitch implementation backed by a single Tween.
  /// </summary>
  public abstract class TweenSwitch : MonoBehaviour, IPropertySwitch {

    #region Inspector

    [Header("Tween Control")]

    [SerializeField, MinValue(0f), OnEditorChange("tweenTime")]
    [UnityEngine.Serialization.FormerlySerializedAs("tweenTime")]
    private float _tweenTime = 0.33f;
    public float tweenTime {
      get {
        return _tweenTime;
      }
      set {
        _tweenTime = value;
        refreshTween();
      }
    }

    #endregion

    #region Unity Events

    // TODO: When this has been bug/battle-tested, add HideInInspector
    [SerializeField]
    private bool _startOn = false;

    private bool _tweenInitialized = false;

    protected virtual void Start() {
      if (!_tweenInitialized) {
        // Some other call initialized the tween, don't initialize here.
        refreshTween();
      }
    }

    private bool _isDestroyed = false;

    protected virtual void OnDestroy() {
      _isDestroyed = true;

      if (_backingSwitchTween.isValid) {
        _backingSwitchTween.Release();
      }
    }

    #endregion

    #region Tween

    /// <summary>
    /// Check if this tween is valid to know if anything has forced the tween to be
    /// constructed by accessing the lazy _switchTween property.
    /// </summary>
    private Tween _backingSwitchTween;

    /// <summary>
    /// 0 for off, 1 for on.
    /// </summary>
    private Tween _switchTween {
      get {
        if (!_backingSwitchTween.isValid) {
          refreshTween();
        }

        return _backingSwitchTween;
      }
    }

    protected void refreshTween() {
      if (!_backingSwitchTween.isValid) {
        _backingSwitchTween = Tween.Persistent()
                                   .Value(0f, 1f, onTweenValue)
                                   .OnReachEnd(onTweenReachedEnd)
                                   .OnLeaveEnd(onTweenLeftEnd)
                                   .OnReachStart(onTweenReachedStart)
                                   .OnLeaveStart(onTweenLeftStart);
      }

      // Sometimes we are initialized in Start(), other times another object
      // forces an earlier initialization. Either way, the _first_ initialization
      // of the tween consumes the _startOn state to set the tween state appropriately.
      if (!_tweenInitialized && Application.isPlaying) {
        if (_startOn) {
          OnNow();
        }
        else {
          OffNow();
        }

        _tweenInitialized = true;
      }

      _backingSwitchTween.OverTime(tweenTime);
    }

    private void onTweenValue(float value) {
      updateSwitch(value);
    }

    public Action OnTweenLeftStart = () => { };
    private void onTweenLeftStart() {
      whenTweenLeavesStart();

      OnTweenLeftStart();
    }

    public Action OnTweenLeftEnd = () => { };
    private void onTweenLeftEnd() {
      whenTweenLeavesEnd();

      OnTweenLeftEnd();
    }

    public Action OnTweenReachedStart = () => { };
    private void onTweenReachedStart() {
      whenTweenReachesStart();

      OnTweenReachedStart();
    }

    public Action OnTweenReachedEnd = () => { };
    private void onTweenReachedEnd() {
      whenTweenReachesEnd();

      OnTweenReachedEnd();
    }

    private Tween? _targetModeTween;
    private float? _tweenTarget;
    public enum TweenSwitchMode { Switch, TargetValue }
    public TweenSwitchMode tweenSwitchMode {
      get {
        if (_tweenTarget.HasValue) {
          return TweenSwitchMode.TargetValue;
        }
        else {
          return TweenSwitchMode.Switch;
        }
      }
    }

    #endregion

    #region Abstract

    /// <summary>
    /// Sets the switch state somewhere between 0 (off) and 1 (on). The "immediately"
    /// argument is a hint to know OnNow() or OffNow() was called, in which case any
    /// delays should be ignored (important when TweenSwitches are chained).
    /// </summary>
    protected abstract void updateSwitch(float time, bool immediately = false);

    /// <summary>
    /// Optionally override this method to have your TweenSwitch implementation do
    /// something when the switch is first turned On() from an "Off or Turning Off" state.
    /// </summary>
    protected virtual void whenTurningOnBegins() { }

    /// <summary>
    /// Optionally override this method to have your TweenSwitch implementation do
    /// something when the switch is first turned Off() from an "On or Turning On" state.
    /// </summary>
    protected virtual void whenTurningOffBegins() { }

    /// <summary>
    /// Optionally override this method to have your TweenSwitch implementation do
    /// something when the Tween has just left its starting value (0) -- that is, it just
    /// began turning on from having been fully turned off.
    /// </summary>
    protected virtual void whenTweenLeavesStart() { }

    /// <summary>
    /// Optionally override this method to have your TweenSwitch implementation do
    /// something when the Tween has just left its ending value (1) -- that is, it just
    /// begin turning off from having been fully turned on.
    /// </summary>
    protected virtual void whenTweenLeavesEnd() { }

    /// <summary>
    /// Optionally override this method to have your TweenSwitch implementation do
    /// something when the Tween has reached its starting value (from being played
    /// backwards -- that is, turning off).
    /// </summary>
    protected virtual void whenTweenReachesStart() { }

    /// <summary>
    /// Optionally override this method to have your TweenSwitch implementation do
    /// something when the Tween has reached its ending value (from being played forwards
    /// -- that is, turning on).
    /// </summary>
    protected virtual void whenTweenReachesEnd() { }

    #endregion

    #region IPropertySwitch

    public void On() {
      // Deactivate "targeted" mode because we received a Switch call.
      cancelTargetedMode();

      if (GetIsOffOrTurningOff()) {
        whenTurningOnBegins();
      }

      _switchTween.Play(Direction.Forward);
    }

    public bool GetIsOnOrTurningOn() {
      if (!Application.isPlaying) {
        return _startOn;
      }
      else {
        // Special case for when the tween was _just_ created.
        if (_switchTween.progress == 0 && !_switchTween.isRunning) return false;

        return _switchTween.direction == Direction.Forward;
      }
    }

    public void Off() {
      // Deactivate "targeted" mode because we received a Switch call.
      cancelTargetedMode();

      if (GetIsOnOrTurningOn()) {
        whenTurningOffBegins();
      }

      _switchTween.Play(Direction.Backward);
    }

    public bool GetIsOffOrTurningOff() {
      if (!Application.isPlaying) {
        return !_startOn;
      }
      else {
        // Special case for when the tween was _just_ created.
        if (_switchTween.progress == 0 && !_switchTween.isRunning) return true;

        return _switchTween.direction == Direction.Backward;
      }
    }

    public void OnNow() {
      if (_isDestroyed) return;

      // Deactivate "targeted" mode because we received a Switch call.
      cancelTargetedMode();

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        Undo.RegisterFullObjectHierarchyUndo(gameObject, "Appear Object(s) Now");
        _startOn = true;
      }
#endif

      if (Application.isPlaying) {
        if (!gameObject.activeSelf) {
          gameObject.SetActive(true);
        }

        var tween = _switchTween;
        tween.progress = 1f;

        if (tween.isRunning) {
          tween.Stop();
        }
      }

      updateSwitch(1f, immediately: true);
    }

    public void OffNow() {
      if (_isDestroyed) return;

      // Deactivate "targeted" mode because we received a Switch call.
      cancelTargetedMode();

#if UNITY_EDITOR
      if (!Application.isPlaying) {
        Undo.RegisterFullObjectHierarchyUndo(gameObject, "Vanish Object(s) Now");
        _startOn = false;
      }
#endif

      if (Application.isPlaying) {
        var tween = _switchTween;
        tween.progress = 0f;

        if (tween.isRunning) {
          tween.Stop();
        }
      }

      updateSwitch(0f, immediately: true);
    }

    #endregion

    #region Target Value Mode

    /// <summary>
    /// This method is special to Tween Switches, and allows you to drive them to any
    /// value between 0 and 1 instead of merely specifying them to target "off" or "on".
    /// 
    /// Upon calling this method, the TweenSwitch will change modes to drive towards the
    /// argument target. In this mode, _tweenTime is used to make the motion faster or
    /// slower, but no longer represents the exact time to the target state.
    /// 
    /// GetIsOnOrTurningOn() and GetIsOffOrTurningOff() will report whether this tween
    /// most recently moved towards or away from the On state, regardless of the exact
    /// current progress of the tween.
    /// 
    /// Calling On(), Off(), OnNow(), or OffNow() will deactivate target-drive mode and
    /// the TweenSwitch will act like a normal IPropertySwitch again.
    /// </summary>
    public void SetTweenTarget(float targetValue) {
      targetValue = Mathf.Clamp01(targetValue);

      _tweenTarget = targetValue;

      if (Application.isPlaying) {
        var updater = Updater.instance;
        updater.OnUpdate -= updateSwitch_TargetValueMode;
        updater.OnUpdate += updateSwitch_TargetValueMode;
      }
    }

    private void updateSwitch_TargetValueMode() {
      if (_tweenInitialized && _switchTween.progress < _tweenTarget.Value) {
        _backingSwitchTween.direction = Direction.Forward;
      }
      if (_tweenInitialized && _switchTween.progress < _tweenTarget.Value) {
        _backingSwitchTween.direction = Direction.Backward;
      }

      var tween = _switchTween;

      var lerpCoeffPerSec = 0f;
      if (_tweenTime < 0.0001f) {
        lerpCoeffPerSec = 1000f;
      }
      else {
        lerpCoeffPerSec = (1f / _tweenTime) * 2f;
      }
      tween.progress = Mathf.Lerp(tween.progress, _tweenTarget.Value, lerpCoeffPerSec * Time.deltaTime);
    }

    private void cancelTargetedMode() {
      _tweenTarget = null;
      if (Application.isPlaying) {
        Updater.instance.OnUpdate -= updateSwitch_TargetValueMode;
      }
    }

    #endregion

  }

}
