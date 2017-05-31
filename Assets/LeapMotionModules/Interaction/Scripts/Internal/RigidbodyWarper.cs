/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System;

namespace Leap.Unity.Interaction.Internal {

  /// <summary>
  /// Note: This class is incomplete; it will be integrated in a future version of the
  /// Interaction Engine.
  /// </summary>
  public class RigidbodyWarper : IDisposable {
    protected enum CallbackState {
      Physical,
      PhysicalNeedsUpdate,
      Graphical
    }

    protected InteractionManager _manager;
    protected Transform _transform;
    protected Rigidbody _rigidbody;

    protected bool _disposed = false;
    protected bool _subscribed = false;

    protected float _warpPercent = 0;
    protected float _returnTime;

    protected CallbackState _mostRecentCallback = CallbackState.Physical;

    protected Vector3 _rigidbodyPosition, _prevRigidbodyPosition, _savedPosition;
    protected Quaternion _rigidbodyRotation, _prevRigidbodyRotation, _savedRotation;

    protected bool _hasGraphicalTransform = false;
    protected Vector3 _graphicalPosition, _graphicalPositionOffset;
    protected Quaternion _graphicalRotation, _graphicalRotationOffset;

    public RigidbodyWarper(InteractionManager manager, Transform transform, Rigidbody rigidbody, float returnTime) {
      _manager = manager;
      _transform = transform;
      _rigidbody = rigidbody;
      _returnTime = returnTime;
    }

    public virtual void Dispose() {
      if (_disposed) {
        return;
      }

      if (_subscribed) {
        unsubscribe();
      }
      _disposed = true;
    }

    public Vector3 RigidbodyPosition {
      get {
        checkDisposed();

        if (_subscribed) {
          updateRigidbodyValues();
          return _rigidbodyPosition;
        }
        else {
          return _rigidbody.position;
        }
      }
    }

    public Quaternion RigidbodyRotation {
      get {
        checkDisposed();

        if (_subscribed) {
          updateRigidbodyValues();
          return _rigidbodyRotation;
        }
        else {
          return _rigidbody.rotation;
        }
      }
    }

    public virtual float WarpPercent {
      get {
        return _warpPercent;
      }
      set {
        checkDisposed();

        _warpPercent = value;

        bool shouldBeSubscribed = _warpPercent > 0;
        if (shouldBeSubscribed != _subscribed) {
          if (shouldBeSubscribed) {
            subscribe();
          }
          else {
            unsubscribe();
          }
        }
      }
    }

    public virtual void Teleport(Vector3 position, Quaternion rotation) {
      _rigidbody.position = _rigidbodyPosition = _prevRigidbodyPosition = position;
      _rigidbody.rotation = _rigidbodyRotation = _prevRigidbodyRotation = rotation;
    }

    public virtual void SetGraphicalPosition(Vector3 position, Quaternion rotation) {
      checkDisposed();

      _graphicalPosition = position;
      _graphicalRotation = rotation;
      _hasGraphicalTransform = true;
    }

    protected void checkDisposed() {
      if (_disposed) {
        throw new InvalidOperationException("Cannot invoke methods of a disposed object.");
      }
    }

    protected void subscribe() {
      _rigidbodyPosition = _prevRigidbodyPosition = _savedPosition = _rigidbody.position;
      _rigidbodyRotation = _prevRigidbodyRotation = _savedRotation = _rigidbody.rotation;
      _mostRecentCallback = CallbackState.PhysicalNeedsUpdate;

      _manager.OnGraphicalUpdate += onGraphicalUpdate;
      _manager.OnPrePhysicalUpdate += onPrePhysicalUpdate;
      _manager.OnPostPhysicalUpdate += onPostPhysicalUpdate;
      _subscribed = true;
    }

    protected void unsubscribe() {
      _manager.OnGraphicalUpdate -= onGraphicalUpdate;
      _manager.OnPrePhysicalUpdate -= onPrePhysicalUpdate;
      _manager.OnPostPhysicalUpdate -= onPostPhysicalUpdate;
      _subscribed = false;
    }

    protected virtual void onPrePhysicalUpdate() {
      updateRigidbodyValues();

      if (_mostRecentCallback == CallbackState.Graphical) {
        _transform.position = _savedPosition;
        _transform.rotation = _savedRotation;
      }

      _mostRecentCallback = CallbackState.Physical;
    }

    protected virtual void onPostPhysicalUpdate() {
      _mostRecentCallback = CallbackState.PhysicalNeedsUpdate;
    }

    protected virtual void onGraphicalUpdate() {
      updateRigidbodyValues();

      if (_mostRecentCallback == CallbackState.Physical || _mostRecentCallback == CallbackState.PhysicalNeedsUpdate) {
        _savedPosition = _rigidbody.position;
        _savedRotation = _rigidbody.rotation;
      }

      float t = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
      Vector3 interpolatedPosition = Vector3.Lerp(_prevRigidbodyPosition, _rigidbodyPosition, t);
      Quaternion interpolatedRotation = Quaternion.Slerp(_prevRigidbodyRotation, _rigidbodyRotation, t);

      if (_hasGraphicalTransform) {
        Quaternion inverseRotation = Quaternion.Inverse(interpolatedRotation);
        _graphicalPositionOffset = inverseRotation * (_graphicalPosition - interpolatedPosition);
        _graphicalRotationOffset = inverseRotation * _graphicalRotation;
        _hasGraphicalTransform = false;
      }

      _transform.position = interpolatedPosition + interpolatedRotation * (_graphicalPositionOffset) * _warpPercent;
      _transform.rotation = interpolatedRotation * Quaternion.Slerp(Quaternion.identity, _graphicalRotationOffset, _warpPercent);

      WarpPercent = Mathf.MoveTowards(WarpPercent, 0, Time.deltaTime / _returnTime);

      _mostRecentCallback = CallbackState.Graphical;
    }

    protected void updateRigidbodyValues() {
      if (_mostRecentCallback == CallbackState.PhysicalNeedsUpdate) {
        _prevRigidbodyPosition = _rigidbodyPosition;
        _prevRigidbodyRotation = _rigidbodyRotation;
        _rigidbodyPosition = _rigidbody.position;
        _rigidbodyRotation = _rigidbody.rotation;
        _mostRecentCallback = CallbackState.Physical;
      }
    }
  }
}
