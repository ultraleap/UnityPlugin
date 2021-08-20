/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity {

  /// <summary>
  /// A simple interface that allows an object to act as a 'proxy'
  /// interface to another object.  The proxy can store a serialized
  /// representation of a value on another object.  The value of
  /// the proxy can either be updated from the object (pull), or
  /// be pushed out to the object (push).
  /// 
  /// This interface is normally used in animation systems where
  /// something that needs to be animated does not have an easily
  /// animatable representation.  The proxy stands in as the animatable
  /// representation, while still allowing normal reads and writes.
  /// </summary>
  public interface IValueProxy {

    /// <summary>
    /// Called when this proxy should push its serialized representation
    /// out to the target object.
    /// </summary>
    void OnPushValue();

    /// <summary>
    /// Called when this proxy should pull from the target object into
    /// its serialized representation.
    /// </summary>
    void OnPullValue();
  }

  /// <summary>
  /// A helpful implementation of IValueProxy.  The class is a monobehaviour and so
  /// can be attached to game objects.  Auto-pushing can also be turned on and off.
  /// When Auto-pushing is enabled, the behaviour will push the value on every
  /// LateUpdate.
  /// </summary>
  public abstract class AutoValueProxy : MonoBehaviour, IValueProxy {

    [SerializeField, HideInInspector]
    private bool _autoPushingEnabled = false;
    public bool autoPushingEnabled {
      get {
        return _autoPushingEnabled;
      }
      set {
        _autoPushingEnabled = value;
      }
    }

    public abstract void OnPullValue();
    public abstract void OnPushValue();

    private void LateUpdate() {
      if (_autoPushingEnabled) {
        OnPushValue();
      }
    }
  }
}
