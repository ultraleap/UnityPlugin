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

namespace Leap.Unity.Animation {
  using Internal;

  public partial struct Tween {
    private int _id;
    private TweenInstance _instance;

    private Tween(bool isSingle) {
      _instance = Pool<TweenInstance>.Spawn();
      _id = _instance.instanceId;
      _instance.returnToPoolUponStop = isSingle;
    }

    /// <summary>
    /// Create a single-use Tween that will auto-release itself as soon as it is finished playing.
    /// </summary>
    public static Tween Single() {
      return new Tween(isSingle: true);
    }

    /// <summary>
    /// Creates a persistant Tween that will not ever auto-release itself.  You must specifically 
    /// call Release in order for the resources of the tween to be returned to the pool.
    /// </summary>
    public static Tween Persistent() {
      return new Tween(isSingle: false);
    }

    /// <summary>
    /// Create a single-use Tween that will fire onReachEnd after the specified delay in seconds.
    /// </summary>
    public static Tween AfterDelay(float delay, Action onReachEnd) {
      return Single().Value(0F, 1F, (x) => { }).OverTime(delay).OnReachEnd(onReachEnd).Play();
    }

    #region PUBLIC PROPERTIES
    /// <summary>
    /// Returns whether or not this Tween is considered valid.  A Tween can become invalid under the following conditions:
    ///  - It was constructed with the default constructor.  Only ever use Tween.Single or Tween.Persistant to create Tweens.
    ///  - It was constructed with Tween.Single and has already finished playing.  Use Tween.Persistant if you want to keep it for longer.
    ///  - It had Release called.
    /// </summary>
    public bool isValid {
      get {
        return _instance != null && _id == _instance.instanceId;
      }
    }

    /// <summary>
    /// Returns whether or not the Tween is currently running.
    /// </summary>
    public bool isRunning {
      get {
        throwIfInvalid();
        return _instance.runnerIndex != -1;
      }
    }

    /// <summary>
    /// Gets or sets whether or not this Tween is moving forwards or backwards.
    /// </summary>
    public Direction direction {
      get {
        throwIfInvalid();
        return _instance.direction;
      }
      set {
        throwIfInvalid();
        _instance.direction = value;
        _instance.dstPercent = value == Direction.Backward ? 0 : 1;
      }
    }

    /// <summary>
    /// Gets how much time is left before the Tween stops.
    /// </summary>
    public float timeLeft {
      get {
        throwIfInvalid();
        return Mathf.Abs((_instance.curPercent - _instance.dstPercent) / _instance.velPercent);
      }
    }

    /// <summary>
    /// Gets or sets how far along completion this Tween is.  This value is a percent that
    /// ranges from 0 to 1.
    /// </summary>
    public float progress {
      get {
        throwIfInvalid();
        return _instance.curPercent;
      }
      set {
        throwIfInvalid();

        if (_instance.curPercent == value) {
          return;
        }

        if (value < 0 || value > 1) {
          throw new ArgumentException("Progress must be a value from 0 - 1");
        }

        if (_instance.curPercent == 0.0f) {
          if (_instance.OnLeaveStart != null) {
            _instance.OnLeaveStart();
          }
        } else if (_instance.curPercent == 1.0f) {
          if (_instance.OnLeaveEnd != null) {
            _instance.OnLeaveEnd();
          }
        }

        _instance.curPercent = value;

        if (_instance.curPercent == 0.0f) {
          if (_instance.OnReachStart != null) {
            _instance.OnReachStart();
          }
        } else if (_instance.curPercent == 1.0f) {
          if (_instance.OnReachEnd != null) {
            _instance.OnReachEnd();
          }
        }

        if (_instance.runnerIndex == -1) {
          _instance.interpolatePercent();
        }
      }
    }
    #endregion

    #region PUBLIC METHODS

    /// <summary>
    /// Adds a new Interpolator to this Tween.  This Interpolator will have it's Interpolate method
    /// called every step of the Tween with the smoothed progress value.  All Interpolators are recycled
    /// when the Tween itself is Recycled.
    /// </summary>
    public Tween AddInterpolator(IInterpolator interpolator) {
      throwIfInvalid();

      if (_instance.interpolatorCount >= _instance.interpolators.Length) {
        Utils.DoubleCapacity(ref _instance.interpolators);
      }

      _instance.interpolators[_instance.interpolatorCount++] = interpolator;
      return this;
    }

    /// <summary>
    /// Specifies that this Tween should travel from begining to end over a certain number of seconds.
    /// </summary>
    public Tween OverTime(float seconds) {
      throwIfInvalid();
      _instance.velPercent = 1.0f / seconds;
      return this;
    }

    /// <summary>
    /// Specifies that this Tween should travel at the given rate.  This rate is measured against the
    /// FIRST interpolator added to this Tween, at the moment AtRate is called.
    /// </summary>
    public Tween AtRate(float unitsPerSecond) {
      throwIfInvalid();
      _instance.velPercent = unitsPerSecond / _instance.interpolators[0].length;
      return this;
    }

    /// <summary>
    /// Specifies that this Tween should the given smoothing method.
    /// </summary>
    public Tween Smooth(SmoothType type = SmoothType.Smooth) {
      throwIfInvalid();
      _instance.smoothType = type;
      _instance.smoothFunction = null;
      return this;
    }
    /// <summary>
    /// Specifies that this Tween should use the given Animation curve for its
    /// smoothing.  The curve should map from [0-1] to [0-1].
    /// </summary>
    public Tween Smooth(AnimationCurve curve) {
      throwIfInvalid();
      _instance.smoothType = 0;
      _instance.smoothFunction = curve.Evaluate;
      return this;
    }

    /// <summary>
    /// Specifies that this Tween should use the given Function for its
    /// smoothing.  The function should map from [0-1] to [0-1].
    /// </summary>
    public Tween Smooth(Func<float, float> smoothFunction) {
      throwIfInvalid();
      _instance.smoothType = 0;
      _instance.smoothFunction = smoothFunction;
      return this;
    }

    /// <summary>
    /// Specifies an action to be called every step of the Tween.  
    /// This callback happens after:
    ///   - OnLeaveStart
    ///   - OnLeaveEnd
    ///   - All interpolators have been interpolated
    /// This callback happens before:
    ///   - OnReachStart
    ///   - OnReachEnd
    /// </summary>
    public Tween OnProgress(Action<float> action) {
      throwIfInvalid();
      _instance.OnProgress += action;
      return this;
    }

    /// <summary>
    /// Specifies an action to be called whenever this Tween is Played
    /// forward when at the start.
    /// </summary>
    public Tween OnLeaveStart(Action action) {
      throwIfInvalid();
      _instance.OnLeaveStart += action;
      return this;
    }

    /// <summary>
    /// Specifies an action to be called whenever this Tween reaches
    /// the start.
    /// </summary>
    public Tween OnReachStart(Action action) {
      throwIfInvalid();
      _instance.OnReachStart += action;
      return this;
    }

    /// <summary>
    /// Specifies an action to be called whenever this Tween is Played
    /// backwards when at the end.
    /// </summary>
    public Tween OnLeaveEnd(Action action) {
      throwIfInvalid();
      _instance.OnLeaveEnd += action;
      return this;
    }

    /// <summary>
    /// Specifies an action to be called whenever this Tween reaches
    /// the end.
    /// </summary>
    public Tween OnReachEnd(Action action) {
      throwIfInvalid();
      _instance.OnReachEnd += action;
      return this;
    }

    /// <summary>
    /// Starts playing this Tween.  It will continue from the same position it left
    /// off on, and will continue in the same direction.
    /// </summary>
    public Tween Play() {
      throwIfInvalid();

      if (_instance.curPercent == _instance.dstPercent) {
        return this;
      }

      if (_instance.runnerIndex != TweenInstance.NOT_RUNNING) {
        return this;
      }

      TweenRunner.instance.AddTween(_instance);

      return this;
    }

    /// <summary>
    /// Starts playing this Tween in a specific direction.  It will condition from the same
    /// position it left off on.  The direction will change even if the Tween is already playing.
    /// </summary>
    public Tween Play(Direction direction) {
      throwIfInvalid();

      this.direction = direction;
      Play();

      return this;
    }

    /// <summary>
    /// Starts playing this Tween towards a destination percent.  Once it reaches that value,
    /// it will stop.  If the destination percent is not 0 or 1, it will not trigger OnReachStart
    /// or OnReachEnd.
    /// </summary>
    public Tween Play(float destinationPercent) {
      throwIfInvalid();

      if (destinationPercent < 0 || destinationPercent > 1) {
        throw new ArgumentException("Destination percent must be within the range [0-1]");
      }

      direction = destinationPercent >= _instance.curPercent ? Direction.Forward : Direction.Backward;
      _instance.dstPercent = destinationPercent;
      Play();

      return this;
    }

    /// <summary>
    /// Returns a custom yield instruction that can be yielded to in order to wait for the completion
    /// of this Tween.  This will yield correctly even if the Tween is modified as it is 
    /// playing.
    /// </summary>
    public TweenInstance.TweenYieldInstruction Yield() {
      throwIfInvalid();

      return _instance.yieldInstruction;
    }

    /// <summary>
    /// Pauses this Tween.  It retains its position and direction.
    /// </summary>
    public void Pause() {
      throwIfInvalid();

      if (_instance.runnerIndex != -1) {
        TweenRunner.instance.RemoveTween(_instance);
      }
    }

    /// <summary>
    /// Stops this Tween.  If it is not a persistant Tween, it will be recycled right away.
    /// </summary>
    public void Stop() {
      throwIfInvalid();

      progress = 0;
      direction = Direction.Forward;

      Pause();

      if (isValid && _instance.returnToPoolUponStop) {
        Release();
      }
    }

    /// <summary>
    /// Forces this Tween to be recycled right away.  Once this method is called, the Tween will 
    /// be invalid and unable to be modified.
    /// </summary>
    public void Release() {
      throwIfInvalid();

      Pause();

      TweenRunner.instance.ScheduleForRecycle(_instance);
    }

    #endregion

    private void throwIfInvalid() {
      if (!isValid) {
        //Only way for the id to be an unused ID is if the user constructed this Tween with the
        //default constructor (which we cannot remove because this is a struct).
        if (_id == TweenInstance.ID_UNUSED) {
          throw new InvalidOperationException("Tween is invalid.  Make sure you use Tween.Single or Tween.Persistant to create your Tween instead of the default constructor.");
        } else {
          throw new InvalidOperationException("Tween is invalid or was recycled.  Make sure to use Tween.Persistant if you want to keep a tween around after it finishes playing.");
        }
      }
    }
  }
}
