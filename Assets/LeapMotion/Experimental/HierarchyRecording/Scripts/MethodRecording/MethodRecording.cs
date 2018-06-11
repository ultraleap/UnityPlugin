/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Recording {

  public abstract class MethodRecording : MonoBehaviour {
    public Mode mode { get; private set; }

    protected virtual void Awake() {
      HierarchyRecorder.OnBeginRecording += () => {
        if (gameObject != null) {
          EnterRecordingMode();
        }
      };
    }

    public abstract float GetDuration();

    public virtual void EnterRecordingMode() {
      mode = Mode.Recording;
    }

    public virtual void ExitRecordingMode(string savePath) {
      mode = Mode.None;
    }

    public virtual void EnterPlaybackMode() {
      mode = Mode.Playback;
    }

    public abstract void SweepTime(float from, float to);

    public enum Mode {
      None = 0,
      Recording = 1,
      Playback = 2
    }
  }

  public abstract class BasicMethodData<T> : ScriptableObject {
    public List<float> times;
    public List<T> args;
  }

  public abstract class BasicMethodRecording<T, K> : MethodRecording where T : BasicMethodData<K> {

    public T data;

    public override void EnterRecordingMode() {
      base.EnterRecordingMode();
      data = ScriptableObject.CreateInstance<T>();
    }

    public override void ExitRecordingMode(string savePath) {
      base.ExitRecordingMode(savePath);
#if UNITY_EDITOR
      AssetDatabase.CreateAsset(data, savePath);
#endif
    }

    public override sealed float GetDuration() {
      if (data.times.Count == 0) {
        return 0;
      } else {
        return data.times[data.times.Count - 1];
      }
    }

    public override sealed void SweepTime(float from, float to) {
      int startIndex = data.times.BinarySearch(from);
      int endIndex = data.times.BinarySearch(to);

      if (startIndex < 0) {
        startIndex = ~startIndex;
      }

      if (endIndex < 0) {
        endIndex = ~endIndex;
      }

      for (int i = startIndex; i < endIndex; i++) {
        InvokeArgs(data.args[i]);
      }
    }

    protected void SaveArgs(K state) {
      if (data.times == null) data.times = new List<float>();
      if (data.args == null) data.args = new List<K>();

      data.times.Add(HierarchyRecorder.instance.recordingTime);
      data.args.Add(state);
    }

    protected abstract void InvokeArgs(K state);
  }
}
