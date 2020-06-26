/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

public class SetAnimatorParam : MonoBehaviour {

  #pragma warning disable 0649
  [SerializeField]
  private Animator _animator;

  [SerializeField]
  private bool _revertOnDisable = true;

  [SerializeField]
  private AnimatorControllerParameterType _type = AnimatorControllerParameterType.Float;

  [SerializeField]
  private string _paramName;

  [SerializeField]
  private bool _boolValue;

  [SerializeField]
  private int _intValue;

  [SerializeField]
  private float _floatValue;
  #pragma warning restore 0649

  private object _defaultValue;

  private void Awake() {
    switch (_type) {
      case AnimatorControllerParameterType.Bool:
        _defaultValue = _animator.GetBool(_paramName);
        break;
      case AnimatorControllerParameterType.Int:
        _defaultValue = _animator.GetInteger(_paramName);
        break;
      case AnimatorControllerParameterType.Float:
        _defaultValue = _animator.GetFloat(_paramName);
        break;
    }
  }

  private void OnEnable() {
    switch (_type) {
      case AnimatorControllerParameterType.Bool:
        _animator.SetBool(_paramName, _boolValue);
        break;
      case AnimatorControllerParameterType.Int:
        _animator.SetInteger(_paramName, _intValue);
        break;
      case AnimatorControllerParameterType.Float:
        _animator.SetFloat(_paramName, _floatValue);
        break;
      case AnimatorControllerParameterType.Trigger:
        _animator.SetTrigger(_paramName);
        break;
    }
  }

  private void OnDisable() {
    if (_revertOnDisable) {
      switch (_type) {
        case AnimatorControllerParameterType.Bool:
          _animator.SetBool(_paramName, (bool)_defaultValue);
          break;
        case AnimatorControllerParameterType.Int:
          _animator.SetInteger(_paramName, (int)_defaultValue);
          break;
        case AnimatorControllerParameterType.Float:
          _animator.SetFloat(_paramName, (float)_defaultValue);
          break;
        case AnimatorControllerParameterType.Trigger:
          _animator.ResetTrigger(_paramName);
          break;
      }
    }
  }
}
