using Leap.Unity.Animation;
using Leap.Unity.Attributes;
using System;
using System.Collections;
using UnityEngine;

public class Pulsator : MonoBehaviour {

  [SerializeField]
  [MinValue(0.01F)]
  private float _speed = 1F;
  [SerializeField]
  private float _restValue = 0F;
  [SerializeField]
  private float _pulseValue = 1F;
  [SerializeField]
  private float _holdValue = 0.5F;

  void OnValidate() {
    _value = _restValue;
  }

  void Start() {
    _value = _restValue;
  }

  public Pulsator SetSpeed(float value) {
    _speed = value;
    return this;
  }
  public Pulsator SetRestValue(float value) {
    _restValue = value;
    return this;
  }
  public Pulsator SetPulseValue(float value) {
    _pulseValue = value;
    return this;
  }
  public Pulsator SetHoldValue(float value) {
    _holdValue = value;
    return this;
  }
  public Pulsator SetValues(float restValue, float holdValue, float pulseValue) {
    _restValue = restValue; _holdValue = holdValue; _pulseValue = pulseValue;
    return this;
  }

  public float Value { get { return _value; } }
  public Action<float> OnValueChanged = (x) => { };

  private float _value;
  private Tween _valueTween;
  private bool _pulsing = false;
  private bool _awaitingRelease = false;
  private bool _awaitingWarmUp = false;
  private bool _releasing = false;
  public bool IsReleasing { get { return _releasing; } }
  public bool AtRest { get { return _value == _restValue; } }

  private void OnTweenValue(float value) {
    _value = value;
    OnValueChanged(_value);
  }

  private void TweenTo(float to, Action OnReachEnd, float speedMultiplier) {
    if (_valueTween.isValid) {
      _valueTween.Release();
    }
    _valueTween = MakeTween(to, OnReachEnd, speedMultiplier);
    _valueTween.progress = 0F;
    _valueTween.Play();
  }

  private Tween MakeTween(float to, Action OnReachEnd, float speedMultiplier) {
    return Tween.Persistent().Value(_value, to, OnTweenValue)
      .Smooth(SmoothType.Smooth)
      .AtRate(_speed * speedMultiplier)
      .OnReachEnd(OnReachEnd);
  }

  public void Activate() {
    if (!_pulsing) {
      StartPulse();
    }
  }

  public void Release() {
    if (_pulsing) {
      _awaitingRelease = true;
    }
    else {
      TweenTo(_restValue, () => { _releasing = false; }, 1F);
      _awaitingRelease = false;
      _releasing = true;
    }
  }

  public void WarmUp() {
    if (!_pulsing && !_awaitingRelease && !_releasing) {
      TweenTo(_holdValue, () => { }, 0.75F);
    }
    else {
      _awaitingWarmUp = true;
    }
  }

  private void StartPulse() {
    TweenTo(_pulseValue, DoOnReachedPulsePeak, 3F);
    _pulsing = true;
  }

  private void DoOnReachedPulsePeak() {
    _pulsing = false;

    if (!_awaitingRelease && !_releasing && !_pulsing) {
      TweenTo(_holdValue, () => { }, 1F);
    }
    else {
      TweenTo(_restValue, () => {
        _releasing = false;
        if (_awaitingRelease) {
          _awaitingRelease = false;
        }
        else if (_awaitingWarmUp) {
          _awaitingWarmUp = false;
          WarmUp();
        }
      }, 1F);
      _releasing = true;
    }
  }

}
