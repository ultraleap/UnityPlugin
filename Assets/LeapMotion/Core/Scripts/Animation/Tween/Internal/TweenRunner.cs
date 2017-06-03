/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;

namespace Leap.Unity.Animation.Internal {

  public class TweenRunner : MonoBehaviour {
    private TweenInstance[] _runningTweens = new TweenInstance[16];
    private int _runningCount = 0;

    private Queue<TweenInstance> _toRecycle = new Queue<TweenInstance>();

    private static TweenRunner _cachedInstance = null;
    public static TweenRunner instance {
      get {
        if (_cachedInstance == null) {
          _cachedInstance = FindObjectOfType<TweenRunner>();
          if (_cachedInstance == null) {
            _cachedInstance = new GameObject("__Tween Runner__").AddComponent<TweenRunner>();
            _cachedInstance.gameObject.hideFlags = HideFlags.HideAndDontSave;
          }
        }
        return _cachedInstance;
      }
    }

    void Update() {
      for (int i = _runningCount; i-- != 0;) {
        var instance = _runningTweens[i];
        try {
          instance.Step(this);
        } catch (Exception e) {
          Debug.LogError("Error occured inside of tween!  Tween has been terminated");
          Debug.LogException(e);
          if (instance.runnerIndex != -1) {
            RemoveTween(instance);
          }
        }
      }

      while (_toRecycle.Count > 0) {
        var instance = _toRecycle.Dequeue();
        Assert.IsTrue(instance.instanceId < 0, "Should never try to recycle a Tween with a valid instance id.");

        if (instance.instanceId == TweenInstance.ID_WAITING_FOR_RECYCLE) {
          Assert.IsTrue(instance.runnerIndex == TweenInstance.NOT_RUNNING, "Should never try to recycle a running Tween.");
          instance.Dispose();
        }
      }
    }

    public void ScheduleForRecycle(TweenInstance instance) {
      Assert.IsTrue(instance.runnerIndex == TweenInstance.NOT_RUNNING, "Should never schedule a running Tween for recycle.");

      instance.instanceId = TweenInstance.ID_WAITING_FOR_RECYCLE;
      _toRecycle.Enqueue(instance);
    }

    public void AddTween(TweenInstance instance) {
      if (_runningCount >= _runningTweens.Length) {
        Utils.DoubleCapacity(ref _runningTweens);
      }

      instance.runnerIndex = _runningCount;
      _runningTweens[_runningCount++] = instance;

      //Dispatch events here, right when the tween has started and state is valid
      if (instance.curPercent == 0.0f) {
        if (instance.OnLeaveStart != null) {
          instance.OnLeaveStart();
        }
      } else if (instance.curPercent == 1.0f) {
        if (instance.OnLeaveEnd != null) {
          instance.OnLeaveEnd();
        }
      }
    }

    public void RemoveTween(TweenInstance instance) {
      if (instance.runnerIndex == -1) {
        return;
      }

      --_runningCount;
      if (_runningCount < 0) {
        throw new Exception("Removed more tweens than were started!");
      }

      int index = instance.runnerIndex;

      _runningTweens[_runningCount].runnerIndex = index;
      _runningTweens[index] = _runningTweens[_runningCount];

      instance.runnerIndex = -1;

      //Dispatch events here, right when tween is stopped and state is valid
      if (instance.curPercent == 1.0f) {
        if (instance.OnReachEnd != null) {
          instance.OnReachEnd();
        }
      } else if (instance.curPercent == 0.0f) {
        if (instance.OnReachStart != null) {
          instance.OnReachStart();
        }
      }

      //Instance might have been re-added in one of the above callbacks
      if (instance.runnerIndex == -1 && instance.returnToPoolUponStop) {
        ScheduleForRecycle(instance);
      }
    }
  }
}
