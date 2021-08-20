/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Leap.Unity.Interaction {

  /// <summary>
  /// A physics-enabled toggle. Toggling is triggered by physically pushing the toggle to its compressed position.
  /// </summary>
  public class InteractionToggle : InteractionButton {

    [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
    [Range(0f, 1f)]
    ///<summary> The height that this toggle rests at when it is toggled. </summary>
    public float toggledRestingHeight = 0.25f;

    [SerializeField, FormerlySerializedAs("_toggled")]
    private bool _isToggled = false;

    [SerializeField]
    private bool _startToggled = false;

    ///<summary> Whether or not this toggle is currently toggled. </summary>
    public bool isToggled {
      get {
        return _isToggled;
      }
      set {
        if (_isToggled != value) {
          _isToggled = value;
          if (_isToggled) {
            OnToggle();
          } else {
            OnUntoggle();
          }
          restingHeight = isToggled ? toggledRestingHeight : _originalRestingHeight;
          rigidbody.WakeUp();
          _pressedThisFrame = value;
          _unpressedThisFrame = !value;
        }
      }
    }

    ///<summary> Triggered when this toggle is toggled. </summary>
    [FormerlySerializedAs("toggleEvent")]
    [SerializeField]
    private UnityEvent _toggleEvent = new UnityEvent();

    /// <summary>
    /// Called when the toggle is ticked (not when unticked; for that, use OnUntoggle.)
    /// </summary>
    public Action OnToggle = () => { };

    ///<summary> Triggered when this toggle is untoggled. </summary>
    [FormerlySerializedAs("unToggleEvent")]
    [SerializeField]
    public UnityEvent _untoggleEvent = new UnityEvent();

    /// <summary>
    /// Called when the toggle is unticked.
    /// </summary>
    public Action OnUntoggle = () => { };

    private float _originalRestingHeight;
    public float untoggledRestingHeight {
      get {
        return _originalRestingHeight;
      }
    }

    /// <summary>
    /// Returns the local position of this toggle when it is able to relax into its untoggled position.
    /// </summary>
    public Vector3 RelaxedToggledLocalPosition {
      get {
        return initialLocalPosition + Vector3.back * Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, toggledRestingHeight);
      }
    }

    /// <summary>
    /// Returns the local position of this toggle when it is able to relax into its untoggled position.
    /// </summary>
    public override Vector3 RelaxedLocalPosition {
      get {
        if (!Application.isPlaying) {
          return initialLocalPosition + Vector3.back * Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight);
        }
        else {
          return initialLocalPosition + Vector3.back * Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, untoggledRestingHeight);
        }
      }
    }

    protected override void Awake() {
      base.Awake();
      
      _originalRestingHeight = restingHeight;
    }

    protected override void Start() {
      base.Start();

      if (_startToggled) {
        isToggled = true;
      }

      OnToggle += _toggleEvent.Invoke;
      OnUntoggle += _untoggleEvent.Invoke;
    }

    protected override void OnEnable() {
      OnPress += OnPressed;
      base.OnEnable();
    }

    protected override void OnDisable() {
      base.OnDisable();
      OnPress -= OnPressed;
    }

    private void OnPressed() {
      isToggled = !isToggled;
    }

    /// <summary>
    /// Sets this InteractionToggle to the "toggled" state. Calling this function won't
    /// oscillate the state of the toggle; to 'untoggle' the control, call Untoggle().
    /// </summary>
    public void Toggle() {
      isToggled = true;
    }

    /// <summary>
    /// Sets this InteractionToggle to the "untoggled" state.
    /// </summary>
    public void Untoggle() {
      isToggled = false;
    }
  }
}
