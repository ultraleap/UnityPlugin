/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
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

    [SerializeField]
    private bool _toggled = false;

    [SerializeField]
    private bool _startToggled = false;

    ///<summary> Whether or not this toggle is currently toggled. </summary>
    public bool toggled {
      get {
        return _toggled;
      }
      set {
        if (_toggled != value) {
          _toggled = value;
          if (_toggled) {
            OnToggle();
          } else {
            OnUntoggle();
          }
          restingHeight = toggled ? toggledRestingHeight : _originalRestingHeight;
          rigidbody.WakeUp();
          depressedThisFrame = value;
          unDepressedThisFrame = !value;
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

    protected override void Start() {
      base.Start();

      _originalRestingHeight = restingHeight;

      if (_startToggled) {
        toggled = true;
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
      toggled = !toggled;
    }
  }
}
