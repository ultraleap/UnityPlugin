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
using System.Collections;

namespace Leap.Unity.Animation.Internal {

  public class TweenInstance : IPoolable, IDisposable {
    private static int _nextInstanceId = 1;

    public const int ID_UNUSED = 0;
    public const int ID_IN_POOL = -1;
    public const int ID_WAITING_FOR_RECYCLE = -2;
    public const int ID_INVALID_STATE = -3;
    public int instanceId = ID_INVALID_STATE;

    public const int NOT_RUNNING = -1;
    public int runnerIndex = NOT_RUNNING;

    public bool returnToPoolUponStop;

    public IInterpolator[] interpolators = new IInterpolator[1];
    public int interpolatorCount;

    public float curPercent;
    public float dstPercent;
    public float velPercent;
    public Direction direction;

    public SmoothType smoothType;
    public Func<float, float> smoothFunction;

    public Action<float> OnProgress;
    public Action OnLeaveEnd;
    public Action OnReachEnd;
    public Action OnLeaveStart;
    public Action OnReachStart;

    public TweenYieldInstruction yieldInstruction;

    public TweenInstance() {
      ResetDefaults();
    }

    public void OnSpawn() {
      instanceId = _nextInstanceId++;
      yieldInstruction = new TweenYieldInstruction(this);
    }

    public void OnRecycle() { }

    public void ResetDefaults() {
      returnToPoolUponStop = true;

      curPercent = 0;
      dstPercent = 1;
      velPercent = 1; //By default, tween over the course of 1 second
      direction = Direction.Forward;

      smoothType = SmoothType.Linear;
      smoothFunction = null;

      OnProgress = null;
      OnLeaveEnd = null;
      OnReachEnd = null;
      OnLeaveStart = null;
      OnReachStart = null;
    }

    public void Dispose() {
      instanceId = ID_IN_POOL;

      for (int i = 0; i < interpolatorCount; i++) {
        interpolators[i].Dispose();
        interpolators[i] = null;
      }
      interpolatorCount = 0;

      ResetDefaults();

      Pool<TweenInstance>.Recycle(this);
    }

    public void Step(TweenRunner runner) {
      curPercent = Mathf.MoveTowards(curPercent, dstPercent, Time.deltaTime * velPercent);

      interpolatePercent();

      if (curPercent == dstPercent) {
        runner.RemoveTween(this);
      }
    }

    public void interpolatePercent() {
      float progress;
      switch (smoothType) {
        case SmoothType.Linear:
          progress = curPercent;
          break;
        case SmoothType.Smooth:
          progress = Mathf.SmoothStep(0, 1, curPercent);
          break;
        case SmoothType.SmoothEnd:
          progress = 1.0f - (curPercent - 1.0f) * (curPercent - 1.0f);
          break;
        case SmoothType.SmoothStart:
          progress = curPercent * curPercent;
          break;
        default:
          progress = smoothFunction(curPercent);
          break;
      }

      for (int i = interpolatorCount; i-- != 0;) {
        IInterpolator interpolator = interpolators[i];
        if (interpolator.isValid) {
          interpolators[i].Interpolate(progress);
        } else {
          interpolators[i] = interpolators[--interpolatorCount];
        }
      }

      if (OnProgress != null) {
        OnProgress(curPercent);
      }
    }

    public struct TweenYieldInstruction : IEnumerator {
      private TweenInstance _instance;
      private int _instanceId;

      public TweenYieldInstruction(TweenInstance instance) {
        _instance = instance;
        _instanceId = _instance.instanceId;
      }

      public object Current {
        get {
          return null;
        }
      }

      public bool MoveNext() {
        return (_instanceId == _instance.instanceId) && (_instance.runnerIndex != -1);
      }

      public void Reset() { }
    }
  }
}
